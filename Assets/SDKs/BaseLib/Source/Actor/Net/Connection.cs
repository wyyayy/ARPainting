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
using Actor.Serializable;
using Debugger = BaseLib.Debugger;

namespace Actor.Net
{
    public enum TimeoutType
    {
        Handshake,
        Heartbeat,
        RPCCall,
    }

    /*
        Used to do connection with Server's Actor.Connection
    */
    public class Connection
    {
        public const int DEFAULT_RPC_TIMEOUT = 60;

        public ConnectionState state { get { return _connectionState; } }
        private ConnectionState _connectionState;

        /// Connection disconnected by server or abnormally.
        public event Action<Exception> OnDisconnected;
        /// For RPC call timeout, the second param is msg's ID.
        ///...public event Action<TimeoutType, ushort> OnTimeout;

        public Func<NetMessage, bool> NetMessageFilter;

        /// Raw message data filter. Return true if need further processing.
        public Func<byte[], int, bool> ReceivingFilter { get { return _socket.OnReceivingFilter; } set { _socket.OnReceivingFilter = value; } }
        protected Action<byte[]> ReceivingFiltered;
        /// Return true if need further processing.
        public Func<NetMessage, bool> SendingFilter;
        protected Action<NetMessage> OnSendingFiltered;

        ///------------
        protected Socket _socket;

        protected EventDispatcher<short, NetMessage> _messageDispatcher;

        private QuickList<_InternalMsg> __sysMsgQueue;

        /// RPC support -----
        _NetRPCComponent _netRPCComponent;

        protected string _hostName;
        protected int _port;

        ///---
        private Action<bool, Exception> __onConnect;  

        /// Stores current time i n seconds
        private float __fCurrentSeconds;

        public Connection()
        {
            _netRPCComponent = new _NetRPCComponent(this);

            _socket = new Socket();
            _socket.OnData += _onData;

            _messageDispatcher = new EventDispatcher<short, NetMessage>();

            _socket.OnDisconnected += err => 
            {
                _onFatalError(err);
            };

            __sysMsgQueue = new QuickList<_InternalMsg>();

/*
            AddNotifyListener((ushort)ServerSysMsgType.HANDSHAKE_TIMEOUT, msg =>
            {
                if (OnTimeout != null) OnTimeout(TimeoutType.Handshake, 0);
            });
*/
        }

        public float _GetCurTime() { return __fCurrentSeconds; }

        public void AddNotifyListener(short type, Action<NetMessage> pListener)
        {
            _messageDispatcher.AddEventListener(type, pListener);
        }

        public void RemoveNotifyListener(short type, Action<NetMessage> pListener)
        {
            _messageDispatcher.RemoveEventListener(type, pListener);
        }

        public Boolean HasNotifyListener(short type, Action<NetMessage> pListener)
        {
            return _messageDispatcher.HasEventListener(type, pListener);
        }

        public Boolean HasNotifyListener(short type)
        {
            return _messageDispatcher.HasEventListener(type);
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
                Debugger.Assert(_socket != null && _socket.connected);
            }
#endif
            return _connectionState == ConnectionState.CONNECTED || _connectionState == ConnectionState.VALIDATED;
        }

        public bool IsDisconnected() { return _connectionState == ConnectionState.DISCONNECTED; }

        public IYieldInstruction Connect(string strHostName, int nPort, Action<bool, Exception> callback = null, bool yieldable = true)
        {
            _connectionState = ConnectionState.CONNECTING;

            if (yieldable)
            {
                return this.WaitUntil<string, int, bool, Exception>(_doConnect, strHostName, nPort, callback);
            }
            else
            {
                _doConnect(strHostName, nPort, callback);
                return null;
            }
        }

        public void Disconnect()
        {
            __onConnect = null;

            _socket.Disconnect();

            _connectionState = ConnectionState.DISCONNECTED;

            _netRPCComponent.Clear();
            __fCurrentSeconds = 0;
        }

        public IYieldInstruction NetCall(short type, object param, Action<_NetRPCReturn, Exception> callback = null, bool yieldable = true)
        {
            if (yieldable)
            {
                return this.WaitUntil<short, object, _NetRPCReturn, Exception>(_doNetCall, type, param, callback);
            }
            else
            {
                _doNetCall(type, param, callback);
                return null;
            }
        }

        protected void _doNetCall(short type, object param, Action<_NetRPCReturn, Exception> callback) 
        {		
		    ISerializableData data = null;
            if (param is ISerializableData) data = (ISerializableData)param;		
		    else data = new PojoObject(param);
		
		    NetRPCCall netRPCCall = new NetRPCCall(type, data);

            if (_netRPCComponent == null) _netRPCComponent = new _NetRPCComponent(this);
            _netRPCComponent.Call(netRPCCall, callback, DEFAULT_RPC_TIMEOUT);		
        }

        public void SendNetNotify(NetNotify netNotify)
        {
            this.SendNetMessage(netNotify);
        }

        public void SendNetMessage(NetMessage message)
        {
            if (!IsConnected()) { Debugger.Log("Connection already down, send message failed!"); return; }

            try
            {
                if(SendingFilter != null)
                {
                    if(SendingFilter(message))
                    {
                        ByteBuffer buffer = message.Serialize();
                        _socket.Send(buffer.GetInternalBuffer());
                    }
                    else
                    {
                        if(OnSendingFiltered != null) OnSendingFiltered(message);
                    }
                }
                else
                {
                    ByteBuffer buffer = message.Serialize();
                    _socket.Send(buffer.GetInternalBuffer());
                }
            }
            catch (Exception e) { Debugger.LogError(e); }
        }

        ///------------------
        protected void _doConnect(string strHostName, int nPort, Action<bool, Exception> callback)
        {
            if (IsConnected())
            {
                Debugger.Log("Connect failed! Can do connecting only when a connection is disconnected!!!");
                return;
            }

            __onConnect = callback;

            _socket.Connect(strHostName, nPort, (ret, err) => 
            {
                _connectionState = ret ? ConnectionState.CONNECTED : ConnectionState.DISCONNECTED;
                callback(ret, err);
            });
        }

        private void _onFatalError(Exception e)
        {
            _InternalMsg errMsg = new _InternalMsg(_InternalMsgType.Disconnected, e);
            __sysMsgQueue.Add(errMsg);

            this.Disconnect();
        }

        // Update is called once per frame
        public void Update(float curTimeInSeconds)
        {
            __fCurrentSeconds = curTimeInSeconds;

            _socket.Update();
            __dispatchSysMsgs();
        }

        private void __dispatchSysMsgs()
        {
            /// Process error msg queue
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
        }

        ///------------      
        protected void _onData(byte[] data)
        {
            try
            {
                ByteBuffer buffer = new ByteBuffer(data);

                NetMessage netMessage = null;
                /// Read the headerType first to determine which message class to use
                buffer.position = NetMessage.LEN_SIZE;
                short headerType = buffer.FReadByte();
                buffer.position = 0;

                if (HeaderType.RPCCall == headerType)
                {
                    netMessage = new NetRPCCall();
                }
                else if (HeaderType.RPCReturn == headerType)
                {
                    netMessage = new _NetRPCReturn();
                }
                else if (HeaderType.Notify == headerType)
                {
                    netMessage = new NetNotify();
                }
                else
                {
                    throw NetMessage.INVALID_HEADER_TYPE;
                }

                netMessage.Deserialize(buffer); 

                bool needFurtherProcess = true;
                if (NetMessageFilter != null) needFurtherProcess = NetMessageFilter(netMessage);

                if (needFurtherProcess)
                {
                    if (HeaderType.RPCCall == headerType)
                    {
                        Debugger.Assert(false, "Has not implement!");
                    }
                    else if (HeaderType.RPCReturn == headerType)
                    {
                        _netRPCComponent._OnRPCReturn(netMessage as _NetRPCReturn);
                    }
                    else if (HeaderType.Notify == headerType)
                    {
                        int handlerCount = _messageDispatcher.DispatchEvent(netMessage);

                        if (0 == handlerCount)
                        {
                            Debugger.LogWarning("No message handler for message type: " + netMessage.type);
                        }
                    }
                    else Debugger.Assert(false);
                }
            }
            catch (Exception e)
            {
                _onFatalError(e);
            }
        }

    }

}