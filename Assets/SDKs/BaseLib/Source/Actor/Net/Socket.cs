using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

using BaseLib;

namespace Actor.Net
{
    public enum _SysMsgType
    {
        Connected,
        ConnectFailed,
        Disconnected,
    }

    struct _SysMsg
    {
        public _SysMsg(_SysMsgType type, Exception e = null)
        {
            this.type = type;
            this.exception = e;
        }

        public _SysMsgType type;
        public Exception exception;
    }

    /// Create send/receive threads to make sure socket operations not block main thread.
    public class Socket
    {
        public bool connected { get { return _tcpClient == null? false : _tcpClient.Connected; } }

        /// Connection disconnected by server or abnormally.
        public Action<Exception> OnDisconnected;

        public event Action<byte[]> OnData;

        /// Return true if need further processing.
        public Func<byte[], int, bool> OnReceivingFilter;
        public Action<byte[], int> OnReceivingFiltered;

        protected const int LEN_SIZE = 2;
        protected const int MAX_BUF_SIZE = 65536;
        protected const int MAX_DATA_SIZE = MAX_BUF_SIZE - 2;

        protected byte[] _arrRecvBuffer = new byte[MAX_BUF_SIZE];

        protected volatile bool _bIsNetDown;

        protected TcpClient _tcpClient;

        protected Thread _receiveThread;
        protected Thread _sendThread;

        protected Queue<byte[]> _receiveQueue = new Queue<byte[]>();
        protected Queue<byte[]> _sendQueue = new Queue<byte[]>();

        protected object _receiveLock = new object();
        protected object _sendLock = new object();

        private Queue<_SysMsg> __sysMsgQueue = new Queue<_SysMsg>();
        private object __sysQueueLock = new object();

        protected volatile Exception __error;

        private Action<bool, Exception> __onConnect;

        public Socket()
        {
        }

        ///... 这里需要创建线程调用Connect，或者采用异步的方法ConnectAsync
        public void Connect(string host, int port, Action<bool, Exception> callback)
        {
            Debugger.Assert(!connected);

            __onConnect = callback;

            try
            {
                _tcpClient  = new TcpClient();

                _tcpClient.BeginConnect(host, port, _onConnectCallback, _tcpClient);
                ///_tcpClient.Connect(host, port);
            }
            catch (Exception exception)
            {
                if (callback != null) callback(false, exception);
                return;
            }

        }

        private void _onConnectCallback(IAsyncResult pAsyncResult)
        {
            lock (__sysQueueLock)
            {
                try
                {
                    TcpClient pTCPClient = pAsyncResult.AsyncState as TcpClient;

                    Debugger.Assert(System.Object.ReferenceEquals(pTCPClient, _tcpClient));
                    pTCPClient.EndConnect(pAsyncResult);

                    _tcpClient.NoDelay = true;

                    _bIsNetDown = false;
                    __error = null;

                    _receiveThread = new Thread(new ParameterizedThreadStart(_receiveThreadFunc));
                    _receiveThread.IsBackground = true;
                    _receiveThread.Start(_tcpClient.GetStream());

                    _sendThread = new Thread(new ParameterizedThreadStart(_sendThreadFunc));
                    _sendThread.IsBackground = true;
                    _sendThread.Start(_tcpClient.GetStream());

                    _SysMsg sysMsg = new _SysMsg(_SysMsgType.Connected);
                    __sysMsgQueue.Enqueue(sysMsg);
                }
                catch (Exception e)
                {
                    _SysMsg errMsg = new _SysMsg(_SysMsgType.ConnectFailed, e);
                    __sysMsgQueue.Enqueue(errMsg);
                }
            }
        }

        public void Disconnect()
        {
            if (_bIsNetDown) return;

            /// Note: this piece of code must before thread.Join()!
            _bIsNetDown = true;

            if (_tcpClient != null)
            {
                _tcpClient.Close();
                _tcpClient = null;
            }

            if (_sendThread != null)
            {
                if (_sendThread.IsAlive) _sendThread.Join();
                else _sendThread.Abort();
                _sendThread = null;
            }
            if (_receiveThread != null)
            {
                if (_receiveThread.IsAlive) _receiveThread.Join();
                else _receiveThread.Abort();
                _receiveThread = null;
            }

            lock (_sendLock) { _sendQueue.Clear(); }
            lock (_receiveLock) { _receiveQueue.Clear(); }
            lock (__sysQueueLock) { __sysMsgQueue.Clear(); }
        }

        public void Send(byte[] arrSend)
        {
            if (((arrSend == null) || (arrSend.Length <= 0)) || (arrSend.Length > MAX_DATA_SIZE))
            {
                throw new ArgumentException("no data to send | size exceeded");
            }

            lock (_sendLock) _sendQueue.Enqueue(arrSend);
        }

        public void Update()
        {
            lock (_receiveLock)
            {
                while (_receiveQueue.Count > 0)
                {
                    byte[] item = _receiveQueue.Dequeue();
                    OnData(item);
                }
            }

            lock(__sysQueueLock)
            {
                while(__sysMsgQueue.Count != 0)
                {
                    _SysMsg sysMsg = __sysMsgQueue.Dequeue();

                    switch (sysMsg.type)
                    {
                        case _SysMsgType.Connected:
                            if (__onConnect != null) __onConnect(true, null);
                            __onConnect = null;
                            break;

                        case _SysMsgType.ConnectFailed:
                            if (__onConnect != null) __onConnect(false, sysMsg.exception);
                            __onConnect = null;
                            break;

                        case _SysMsgType.Disconnected:
                            Disconnect();
                            if (OnDisconnected != null) OnDisconnected(sysMsg.exception);
                            break;
                    }

                }
            }
        }

        ///------------------------------------
        protected void _receiveThreadFunc(object pThreadArgument)
        {
            NetworkStream pNetStream = pThreadArgument as NetworkStream;

            try
            {
                while (!_bIsNetDown)
                {
                    /// Receive head
                    _doReceive(pNetStream, _arrRecvBuffer, 0, 2);

                    int len = (_arrRecvBuffer[0] << 8) | _arrRecvBuffer[1];
                    if ((len <= 0) || (len > MAX_DATA_SIZE))
                    {
                        __error = new Exception(string.Format("Incorrect message length ({0}), length should not exceed 64K", len));
                        return;
                    }

                    /// Receive body - 2
                    _doReceive(pNetStream, _arrRecvBuffer, LEN_SIZE, len - LEN_SIZE);

                    byte[] destBuffer = new byte[len];
                    Array.Copy(_arrRecvBuffer, 0, destBuffer, 0, len);

                    if (OnReceivingFilter != null)
                    {
                        if (OnReceivingFilter(destBuffer, len))
                        {
                            lock (_receiveLock) _receiveQueue.Enqueue(destBuffer);
                        }
                        else
                        {
                            if (OnReceivingFiltered != null) OnReceivingFiltered(destBuffer, len);
                        }
                    }
                    else lock (_receiveLock) _receiveQueue.Enqueue(destBuffer);
                }
            }
            catch(Exception e)
            {
                _SysMsg sysMsg = new _SysMsg(_SysMsgType.Disconnected, e);
                lock (__sysQueueLock) { __sysMsgQueue.Enqueue(sysMsg); }
            }
        }

        protected void _doReceive(NetworkStream pNetStream, byte[] arrRecv, int offset, int nSize)
        {
            int num2;
            for (int i = 0; i < nSize; i += num2)
            {
                num2 = pNetStream.Read(arrRecv, i + offset, nSize - i);

                if (num2 <= 0)
                {
                    throw new Exception("remote disconnected");
                }
            }
        }

        protected void _sendThreadFunc(object pThreadArgument)
        {
            NetworkStream pNetStream = pThreadArgument as NetworkStream;
            while (!_bIsNetDown)
            {
                byte[] dataBytes = null;

                lock (_sendLock)
                {
                    if (_sendQueue.Count > 0) dataBytes = _sendQueue.Dequeue();
                }

                if (dataBytes == null) Thread.Sleep(8);
                else
                {
                    try
                    {
                        pNetStream.Write(dataBytes, 0, dataBytes.Length);
                    }
                    catch(Exception e)
                    {
                        _SysMsg sysMsg = new _SysMsg(_SysMsgType.Disconnected, e);
                        lock (__sysQueueLock) { __sysMsgQueue.Enqueue(sysMsg); }
                        break;
                    }
                }
            }
        }
    }

}

