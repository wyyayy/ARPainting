using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Text;

using JsonFx.Json;

using BaseLib;
using Debugger = BaseLib.Debugger;

public enum _InternalMsgType
{
    Connected,
    ConnectFailed,
    Disconnected,
}

public enum TimeoutType
{
    Handshake,
    Heartbeat,
    RPCCall,
}

struct _InternalMsg
{
    public _InternalMsg(_InternalMsgType type, Exception e = null)
    {
        this.type = type;
        this.exception = e;
    }

    public _InternalMsgType type;
    public Exception exception;
}

/*
注意：该类用来和TDServer里的Connection (基于VertX的) 类进行通信，基本已经废弃。
最新的请使用NetWork.ActorNet.Connection(用来和Actor的Connection进行通信)
*/
public class VertXClient : EventDispatcher<ushort, Message>
{
    /// Receive buffer size
    /// 注意：
    /// 1. 对于大报文，需要通过组包来处理
    /// 2. 接收缓冲的大小并不限制报文的最大长度。报文的最大长度由报文的Len字段的位数决定。
    public const int RECEIVE_BUFFER_SIZE = 1024 * 20;

    public const int HEAD_SIZE = 2;

    /// Connection disconnected by server or abnormally.
    public Action<Exception> OnDisconnected;

    /// For RPC call timeout, the second param is msg's ID.
    public Action<TimeoutType, ushort> OnTimeout;

    /// Return true if need further processing.
    public Func<byte[], int, bool> OnReceivingFilter;
    public Action<Message> OnReceived;
    /// Return true if need further processing.
    public Func<Message, bool> OnSendingFilter;

    public ConnectionState state { get { return _connectionState; } }
    private ConnectionState _connectionState;

    private TcpClient _tcpClient;
    private NetworkStream _networkStream;

    private Queue<Message> __sendMsgQueue;
    private QuickList<Byte[]> __rawMsgQueue;
    private QuickList<_InternalMsg> __sysMsgQueue;

    private byte[] __receiveBuffer;

    /// RPC support -----
    protected Dictionary<int, Message> _rpcCallMap;
    protected ushort _rpcIndex;

    /// Flags 
    private bool __bIsReceiveHeader;
    private int __nBytesReceived;
    private int __nMsgLength;

    protected string _hostName;
    protected int _port;

    ///---
    private Action<bool, Exception> __onConnect;

    /*
    Two things need lock
            1. TCPClient is not thread safe
            2. MessageQueue will be access from main thread and low level net thread.
    For simplicity, we lock both these two things with '__lock'.
    */
    private object __lock;

    /// Stores current time i n seconds
    private float __fCurrentSeconds;

    public VertXClient()
    {
        _connectionState = ConnectionState.DISCONNECTED;

        _tcpClient = null;

        __bIsReceiveHeader = true;
        _networkStream = null;

        __receiveBuffer = new byte[RECEIVE_BUFFER_SIZE];
        __nBytesReceived = 0;

        __sendMsgQueue = new Queue<Message>();
        __rawMsgQueue = new QuickList<byte[]>();
        __sysMsgQueue = new QuickList<_InternalMsg>();

        __lock = new object();

        _rpcCallMap = new Dictionary<int, Message>();
        _rpcIndex = 0;

        AddNotifyListener((ushort)ServerSysMsgType.HANDSHAKE_TIMEOUT, msg =>
        {
            if (OnTimeout != null) OnTimeout(TimeoutType.Handshake, 0);
        });
    }

    public void AddNotifyListener(ushort type, Action<Message> pListener)
    {
        this.AddEventListener(type, pListener);
    }

    public void RemoveNotifyListener(ushort type, Action<Message> pListener)
    {
        this.RemoveEventListener(type, pListener);
    }

    public Boolean HasNotifyListener(ushort type, Action<Message> pListener)
    {
        return this.HasEventListener(type, pListener);
    }

    public Boolean HasNotifyListener(ushort type)
    {
        return this.HasEventListener(type);
    }

    public void Connect(string strHostName, int nPort, Action<bool, Exception> callback)
    {
        _doConnect(strHostName, nPort, callback);
    }

    public IYieldInstruction Connect(string strHostName, int nPort, Action<bool, Exception> callback, bool yieldable = true)
    {
        if(yieldable)
        {
            return this.WaitUntil<string, int, bool, Exception>(this._doConnect, strHostName, nPort, callback);
        }
        else
        {
            _doConnect(strHostName, nPort, callback);
            return null;
        }
    }

    public void Disconnect()
    {
        if (_connectionState == ConnectionState.DISCONNECTED) return;

        __onConnect = null;

        lock (__lock)
        {
            if (_networkStream != null)
            {
                _networkStream.Close();
                _networkStream = null;
            }

            Debugger.Assert(_tcpClient != null);
            _tcpClient.Close();
            _tcpClient = null;

            __nBytesReceived = 0;
            __bIsReceiveHeader = true;

            __sendMsgQueue.Clear();
            __sysMsgQueue.Clear();
            __rawMsgQueue.Clear();

            _rpcCallMap.Clear();            

            _connectionState = ConnectionState.DISCONNECTED;
            __fCurrentSeconds = 0;
        }

    }

    public bool IsValidated()
    {
        return _connectionState == ConnectionState.VALIDATED;
    }

    public bool IsConnected()
    {
#if UNITY_EDITOR
        if (_connectionState == ConnectionState.CONNECTED || _connectionState == ConnectionState.VALIDATED)
        {
            Debugger.Assert(_tcpClient != null && _tcpClient.Connected);
        }
#endif
        return _connectionState == ConnectionState.CONNECTED || _connectionState == ConnectionState.VALIDATED;
    }

    public bool IsDisconnected() { return _connectionState == ConnectionState.DISCONNECTED; }

    public IYieldInstruction Call(Message pMessage, Action<Message> callback, bool yieldable = true)
    {
        if(yieldable)
        {
            return this.WaitUntil<Message, Message>(this._doCall, pMessage, msg =>
            {
                callback(msg);
            });
        }
        else
        {
            _doCall(pMessage, callback);
            return null;
        }
    }

    public void SendNotify(Message pMessage)
    {
        if (!IsConnected()) { Debugger.Log("Connection already down, send message failed!"); return; }

        lock (__lock)
        {
            __sendMsgQueue.Enqueue(pMessage);
        }
    }

    ///------------------
    private void _onWriteCallback(IAsyncResult pAsyncResult)
    {
        lock (__lock)
        {
            if (this.IsDisconnected()) return;

            try
            {
                _networkStream.EndWrite(pAsyncResult);
            }
            catch (Exception e) { _onConnectError(e); }
        }
    }

    private void _onReadCallback(IAsyncResult pAsyncResult)
    {
        lock (__lock)
        {
            if (this.IsDisconnected()) return;

            try
            {
                int numberOfBytesRead = _networkStream.EndRead(pAsyncResult);
                if (numberOfBytesRead == 0)
                {
                    _onConnectError(new Exception("Disconnected by server!"));
                    return;
                }

                __nBytesReceived += numberOfBytesRead;

                var pendingBytes = __bIsReceiveHeader ? HEAD_SIZE : __nMsgLength - HEAD_SIZE;

                if (__nBytesReceived == pendingBytes)
                {
                    Debugger.Assert(__nBytesReceived <= pendingBytes);

                    if (__bIsReceiveHeader)
                    {
                        __bIsReceiveHeader = false;

                        /// Read body size ( Convert first two bytes to ushort)
                        __nMsgLength = (ushort)((__receiveBuffer[1] & 0xff) | (__receiveBuffer[0] << 8));
                        Debugger.Assert(__nMsgLength <= RECEIVE_BUFFER_SIZE);

                        var offset = __nBytesReceived;
                        __nBytesReceived = 0;
                        /// Ready body data
                        _networkStream.BeginRead(__receiveBuffer, offset, __nMsgLength - HEAD_SIZE, _onReadCallback, 0);
                    }
                    else
                    {
                        byte[] rawMsg = new byte[__nMsgLength];
                        Buffer.BlockCopy(__receiveBuffer, 0, rawMsg, 0, __nMsgLength);

                        __rawMsgQueue.Add(rawMsg);

                        /// Continue reading next message
                        __bIsReceiveHeader = true;
                        __nBytesReceived = 0;
                        __nMsgLength = 0;
                        _networkStream.BeginRead(__receiveBuffer, 0, HEAD_SIZE, _onReadCallback, 0);
                    }
                }
                else if (__nBytesReceived < pendingBytes)
                {
                    /// Continue reading remaining bytes
                    _networkStream.BeginRead(__receiveBuffer, __nBytesReceived, pendingBytes - __nBytesReceived, _onReadCallback, 0);
                }
                else Debugger.Assert(false, "Should never run into here!");

            }
            catch (Exception e) { _onConnectError(e); }
        }

    }

    private void _onConnectedCallback(IAsyncResult pAsyncResult)
    {
        lock (__lock)
        {
            try
            {
                TcpClient pTCPClient = pAsyncResult.AsyncState as TcpClient;

                Debugger.Assert(System.Object.ReferenceEquals(pTCPClient, _tcpClient));
                pTCPClient.EndConnect(pAsyncResult);

                _tcpClient.NoDelay = true;

                _networkStream = pTCPClient.GetStream();
                Debugger.Assert(_networkStream.CanRead);

                __bIsReceiveHeader = true;
                _networkStream.BeginRead(__receiveBuffer, 0, HEAD_SIZE, _onReadCallback, 0);

                _connectionState = ConnectionState.CONNECTED;

                _InternalMsg sysMsg = new _InternalMsg(_InternalMsgType.Connected);
                __sysMsgQueue.Add(sysMsg);
            }
            catch (Exception e)
            {
                _InternalMsg errMsg = new _InternalMsg(_InternalMsgType.ConnectFailed, e);
                __sysMsgQueue.Add(errMsg);
            }
        }
    }

    private void _onConnectError(Exception e)
    {
        _InternalMsg errMsg = new _InternalMsg(_InternalMsgType.Disconnected, e);
        __sysMsgQueue.Add(errMsg);
    }

    // Update is called once per frame
    public void Update(float curTimeInSeconds)
    {
        __fCurrentSeconds = curTimeInSeconds;

        lock (__lock)
        {
            /// Processing receiving queue
            foreach (Byte[] rawMsgBytes in __rawMsgQueue)
            {
                bool bNeedDispatch;

                if (OnReceivingFilter != null)
                {
                    bNeedDispatch = OnReceivingFilter(rawMsgBytes, rawMsgBytes.Length);
                }
                else bNeedDispatch = true;

                if (bNeedDispatch)
                {
                    Message message = new Message();
                    message.Deserialize(rawMsgBytes, rawMsgBytes.Length);

                    if(message.IsRPCReturn())
                    {
                        if (_rpcCallMap.ContainsKey(message.RPC_ID))
                        {
                            var rpcCallMsg = _rpcCallMap[message.RPC_ID];
                            _rpcCallMap.Remove(message.RPC_ID);
                            if(rpcCallMsg._Callback != null) rpcCallMsg._Callback(message);
                        }
                        else Debugger.Log("Invalid RPC_ID: " + message.RPC_ID);
                    }
                    else if(message.IsRPCCall())
                    {
                        Debugger.Assert(false, "Not supported now!");
                    }
                    else
                    {
                        DispatchEvent(message);
                    }                    

                    if (OnReceived != null) OnReceived(message);
                }
            }

            __rawMsgQueue.Clear();

            /// Process error msg queue
            ///... _InternalMsgType.Disconnected has bug: Disconnect() method will clear the __sysMsgQueue, which will
            /// break current iteration. 
            foreach (_InternalMsg errMsg in __sysMsgQueue)
            {
                switch (errMsg.type)
                {
                    case _InternalMsgType.Connected:
                        if (__onConnect != null) __onConnect(true, null);
                        __onConnect = null;
                        break;

                    case _InternalMsgType.ConnectFailed:
                        if (__onConnect != null) __onConnect(false, errMsg.exception);
                        __onConnect = null;
                        break;

                    case _InternalMsgType.Disconnected:
                        Disconnect();
                        if (OnDisconnected != null) OnDisconnected(errMsg.exception);
                        break;

                }
            }

            __sysMsgQueue.Clear();

            /// Process sending queue
            if (__sendMsgQueue.Count != 0
                && (_connectionState == ConnectionState.CONNECTED || _connectionState == ConnectionState.VALIDATED))
            {
                Message pMessage = __sendMsgQueue.Dequeue();
                _sendMessage(pMessage);
            }

            /// Checking RPC timeout
            foreach(var msg in _rpcCallMap.Values)
            {
                if(__fCurrentSeconds - msg._RPCCallStartTime > 6.0f)
                {
                    if (OnTimeout != null) OnTimeout(TimeoutType.RPCCall, msg.msgID);
                    msg._RPCCallStartTime = __fCurrentSeconds;
                }
            }
        }
    }

    ///------------
    private void _sendMessage(Message pMessage)
    {
        Debugger.Assert(IsConnected(), "_sendMessage failed, connection already down!");

        try
        {
            byte[] rawBuffer;
            int nLength;

            if (pMessage.IsRPCCall())
            {
                _rpcIndex = (ushort)(1 + ((_rpcIndex + 1) % (ushort.MaxValue - 1)));
                pMessage.RPC_ID = _rpcIndex;
                pMessage._RPCCallStartTime = __fCurrentSeconds;
            }

            pMessage.Serialize(out rawBuffer, out nLength);

            if (OnSendingFilter != null)
            {
                if (!OnSendingFilter(pMessage))
                {
                    if (pMessage._Callback != null) pMessage._Callback(Message.REJECTED_BY_SENDING_FILTER);
                }
                else
                {
                    if (pMessage.IsRPCCall())
                    {
                        Debugger.Assert(!_rpcCallMap.ContainsKey(pMessage.RPC_ID));
                        _rpcCallMap.Add(_rpcIndex, pMessage);
                        pMessage._RPCCallStartTime = __fCurrentSeconds;
                    }

                    _networkStream.BeginWrite(rawBuffer, 0, nLength, _onWriteCallback, pMessage);
                }
            }
            else
            {
                if (pMessage.IsRPCCall())
                {
                    Debugger.Assert(!_rpcCallMap.ContainsKey(pMessage.RPC_ID));
                    _rpcCallMap.Add(_rpcIndex, pMessage);
                    pMessage._RPCCallStartTime = __fCurrentSeconds;
                }

                _networkStream.BeginWrite(rawBuffer, 0, nLength, _onWriteCallback, pMessage);
            }
        }
        catch (Exception e)
        {
            _onConnectError(e);
        }
    }

    protected void _doConnect(string strHostName, int nPort, Action<bool, Exception> callback)
    {
        if (IsConnected())
        {
            Debugger.Log("Connect failed! Can do connecting only when a connection is disconnected!!!");
            return;
        }

        __onConnect = callback;

        try
        {
            _connectionState = ConnectionState.CONNECTING;

            _hostName = strHostName;
            _port = nPort;

            _tcpClient = new TcpClient();
            _tcpClient.BeginConnect(strHostName, nPort, _onConnectedCallback, _tcpClient);
        }
        catch (Exception e)
        {
            if (__onConnect != null) __onConnect(false, e);
        }
    }

    protected void _doCall(Message pMessage, Action<Message> callback)
    {
        if (!IsConnected())
        {
            if (callback != null) callback(Message.CONNECTION_DOWN);
            return;
        }

        pMessage.msgType = MsgType.RPCCall;
        pMessage._Callback = callback;

        lock (__lock)
        {
            __sendMsgQueue.Enqueue(pMessage);
        }
    }
}



