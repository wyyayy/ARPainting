using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BaseLib
{
    /// Create send/receive threads to make sure socket operations not block main thread.
    public class SyncSocketWrapper
    {
        public event Action<byte[]> DataHandler;

        protected const int MAX_BUF_SIZE = 65536;
        protected const int MAX_DATA_SIZE = MAX_BUF_SIZE - 2;

        protected byte[] _arrRecvBuffer = new byte[MAX_BUF_SIZE];
        protected byte[] _arrSendBuffer = new byte[MAX_BUF_SIZE];

        protected volatile bool _bIsNetDown;

        protected TcpClient _tcpClient = new TcpClient();

        protected object _receiveLock = new object();
        protected object _sendLock = new object();

        protected Thread _receiveThread;
        protected Thread _sendThread;

        protected Queue<byte[]> _receiveQueue = new Queue<byte[]>();
        protected Queue<byte[]> _sendQueue = new Queue<byte[]>();

        protected volatile string __errorMsg;

        ///... 这里需要创建线程调用Connect，或者采用异步的方法ConnectAsync
        public void Connect(string strServerIp, int nServerPort, Action<bool, Exception> retHandler)
        {
            try
            {
                _tcpClient.Connect(strServerIp, nServerPort);
            }
            catch (Exception exception)
            {
                if (retHandler != null) retHandler(false, exception);
                return;
            }

            if (retHandler != null) retHandler(true, null);

            _bIsNetDown = false;
            __errorMsg = null;

            _receiveThread = new Thread(new ParameterizedThreadStart(_receiveThreadFunc));
            _receiveThread.IsBackground = true;
            _receiveThread.Start(_tcpClient.GetStream());

            _sendThread = new Thread(new ParameterizedThreadStart(_sendThreadFunc));
            _sendThread.IsBackground = true;
            _sendThread.Start(_tcpClient.GetStream());

            DataHandler = data => { Debugger.Log("<<< data received, data length: " + data.Length); };
        }

        public void Disconnect()
        {
            if (_bIsNetDown) return;

            _tcpClient.Close();
            _bIsNetDown = true;
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
            if (__errorMsg != null)
            {
                throw new Exception(__errorMsg);
            }

            lock (_receiveLock)
            {
                while (_receiveQueue.Count > 0)
                {
                    byte[] item = _receiveQueue.Dequeue();
                    DataHandler(item);
                }
            }
        }

        ///------------------------------------
        protected void _receiveThreadFunc(object pThreadArgument)
        {
            NetworkStream pNetStream = pThreadArgument as NetworkStream;
            while (!_bIsNetDown)
            {
                if (!_doReceive(pNetStream, _arrRecvBuffer, 2)) return;

                int num = (_arrRecvBuffer[0] << 8) | _arrRecvBuffer[1];
                if ((num <= 0) || (num > MAX_DATA_SIZE))
                {
                    __errorMsg = string.Format("Incorrect dataSize ({0}), dataSize should not exceed 64KB", num);
                    return;
                }

                if (!_doReceive(pNetStream, _arrRecvBuffer, num)) return;

                byte[] destinationArray = new byte[num];
                Array.Copy(_arrRecvBuffer, 0, destinationArray, 0, num);

                lock (_receiveLock) _receiveQueue.Enqueue(destinationArray);
            }
        }

        protected bool _doReceive(NetworkStream pNetStream, byte[] arrRecv, int nSize)
        {
            try
            {
                int num2;
                for (int i = 0; i < nSize; i += num2)
                {
                    num2 = pNetStream.Read(arrRecv, i, nSize - i);
                    if (num2 <= 0)
                    {
                        __errorMsg = "remote disconnected";
                        return false;
                    }
                }
            }
            catch (Exception exception)
            {
                __errorMsg = __getExceptionMessage(exception);
                return false;
            }
            return true;
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

                if (dataBytes == null) Thread.Sleep(1);
                else
                {
                    int length = dataBytes.Length;
                    _arrSendBuffer[0] = (byte)((length >> 8) & 0xff);
                    _arrSendBuffer[1] = (byte)(length & 0xff);
                    Array.Copy(dataBytes, 0, _arrSendBuffer, 2, dataBytes.Length);

                    if (!_doSend(pNetStream, _arrSendBuffer, dataBytes.Length + 2))
                    {
                        return;
                    }
                }
            }
        }

        protected bool _doSend(NetworkStream pNetStream, byte[] arrSend, int nSendSize)
        {
            try
            {
                pNetStream.Write(_arrSendBuffer, 0, nSendSize);
            }
            catch (Exception exception)
            {
                __errorMsg = __getExceptionMessage(exception);
                return false;
            }
            return true;
        }

        protected string __getExceptionMessage(Exception pException)
        {
            string stackTrace = pException.StackTrace;
            while (pException != null)
            {
                if (pException.Message != null)
                {
                    string str2 = stackTrace;
                    object[] objArray1 = new object[] { str2, "$", pException.GetType(), ":", pException.Message };
                    stackTrace = string.Concat(objArray1);
                }
                pException = pException.InnerException;
            }
            return stackTrace;
        }
    }

}

