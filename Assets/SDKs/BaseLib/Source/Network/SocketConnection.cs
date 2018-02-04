using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Security.Cryptography;
using System.Text;

using BaseLib;
using JsonFx;

#pragma warning disable 0219

public enum ConnectionState
{
    DISCONNECTED,

    CONNECTING,

    /// Socket connect is established
    CONNECTED,

    /// Login success and received new session key.
    VALIDATED,

    ///... Lost connection from server, but session key still not expired (if expired, will
    /// be DISCONNECTED state). User can send the session key to GameServer 
    /// to restore session.
    OUT_OF_CONNECTION,

    ///... Connection is down, and user is sending session key to GameServer to restore session.
    /// 原则上说，restore session只需要把服务端自动计算的数据发送给客户端即可，例如体力值，
    /// 其它数据可以不发送。
    RECONNECTING, 
}

public enum SocketMsgTypes
{
    SOCKET_CONNECTED = -1,
    SOCKET_DISCONNECTED = -2,
    SOCKET_VALIDATED = -3,
}

public class ConnectionEvent
{
    public int type;
    public MsgBase message;
}

public interface IConnectionHost
{
    string ClientMsgToString(int msgID);
    string ServerMsgToString(int msgID);

    bool ContainsServerMsgID(int msgID);
    bool ContainsClientMsgID(int msgID);

    /// Return whether need further processing.
    bool ReceivingFilter(byte[] messageBytes, int nLength);
    /// Return whether need further processing. 
    bool SendingFilter(MsgBase pMessage);

    void ReceivedListener(MsgBase pMessage);
    void SentListener(MsgBase pMessage, byte[] messageBytes, int nLength);
}

public class _DefaultConnectionHost : IConnectionHost
{
    virtual public string ClientMsgToString(int msgID) { return msgID.ToString(); }
    virtual public string ServerMsgToString(int msgID) { return msgID.ToString(); }

    virtual public bool ContainsServerMsgID(int msgID) { return true; }
    virtual public bool ContainsClientMsgID(int msgID) { return true; }

    virtual public bool ReceivingFilter(byte[] messageBytes, int nLength) { return true; }
    virtual public bool SendingFilter(MsgBase pMessage) { return true; }

    virtual public void ReceivedListener(MsgBase pMessage) { }
    virtual public void SentListener(MsgBase pMessage, byte[] messageBytes, int nLength) { }
}


/*
ToDo: 
    *. Limit message queue size ( if reach limit size, disconnect connection and notify up layer).  
    *. Can message buffer overflow?
    *. Unit test two or more message in one buffer.
    *. Unit test one message be spliced.  

------ 通信安全问题 ------
注意：要保证用户名和密码网络传输安全，要确保登录验证的流程使用RSA加密，例如：
1. 用户通过公钥加密密码，发送给服务器
2. 服务器通过私钥解密，获取密码（然后散列后和数据库里的值比较）
这样，即使网络包被抓去，由于没有私钥，黑客也无法获得用户密码。简单的做法是上述流程直接走https。

登录之后的流程，就不适合用RSA了，因为效率低。
考虑到登录后的安全问题主要出在：报文协议被破解，从而可以模拟报文并发送给服务器，从而更容易作弊。
我们这时可以这样做：
1. 客户端用公钥加密AES的Key，传递给服务器
2. 服务器用私钥解密获得AES的Key
3. 后续报文客户端均用该AES key来加密报文
这样，黑客抓到的都是加密后的客户端报文，但是由于没有AES Key，自然无法破解报文。
但这个方法有个漏洞：如果客户端被反编译了，那么黑客直接就能获取AES Key，那么报文直接就被破解了。
该漏洞无解。不过可以进行优化，例如客户端进行加壳，代码混淆等。


注意：该类已经废弃，仅供参考！！！！！！
*/
public class ConnectionOld : EventDispatcher<int, MsgBase>, _IDisposable
{
    private const int CLIENT_SESSION_SEND_KEY = 0;    
    private const int CLIENT_SESSION_HEARDBEAT = 1;
    private const int SERVER_SESSION_UPDATE_KEY = 1;

    private const float HEART_BEAT_INTERVAL = 30f;
    private const string IV_KEY = "abcdefghijklmnop";
    /// Send buffer size
    public const int SEND_BUFFER_SIZE = 1024 * 64;

    /// Receive buffer size
    /// 注意：这个buffer可以很小，例如4k，没必要必须是64k
    static public int RECEIVE_BUFFER_SIZE = 1024 * 64; 
    ///static public int RECEIVE_BUFFER_SIZE = 1024 * 4;
    //static public int MSG_BUFFER_SIZE = RECEIVE_BUFFER_SIZE * 2;

    private ConnectionState _connectionState;

    private TcpClient _tcpClient;
    private NetworkStream _networkStream;

    IConnectionHost _pConnectionHost;

    private byte[] __receiveBuffer;
    private int __nBytesReceived;

    /// Flags 
    private bool __bIsReceivingHeader;
    private int __nMsgLength;

    /// Indicates the last BeginWrite has not return.     
    /// NetworkStream的Write并不要求要等待前一个EndWrite后才能继续BeginWrite。
    /// 但是如果当前发送的报文是CLIENT_SESSION_SEND_KEY，则必须等待此报文返回后才能继续Write. 
    /// 这里为了简化，所有Write都通过__bInWriting串行化了
    protected bool __bInWriting;

    private Queue<MsgBase> __sendMsgQueue;
    private QuickList<MsgBase> __receiveMsgQueue;

    /*
    Two things need lock
            1. TCPClient is not thread safe
            2. MessageQueue will be access from main thread and low level net thread.
    For simplicity, we lock both these two things with '__lock'.
    */
    private object __lock;

    private byte[] _sessionKey;
    private byte[] _encryptedSessionKey;

    /// For encryption support
    private RijndaelManaged _pAESMgr;
    private ICryptoTransform _pEncryptor;
    private ICryptoTransform _pDecryptor;

    private Timer _heartBeatTimer;
    private bool _bInUpdateSessionKey;

    private int _nLastSvrTime;
    private int _nLastLocalTime;

    public ConnectionOld(IConnectionHost pConnectionHost = null)
    {
        _connectionState = ConnectionState.DISCONNECTED;

        _tcpClient = null;

        __bIsReceivingHeader = true;
        __bInWriting = false;

        _networkStream = null;

        _sessionKey = null;
        _encryptedSessionKey = null;

        __receiveBuffer = new byte[RECEIVE_BUFFER_SIZE];
        __nBytesReceived = 0;

        __sendMsgQueue = new Queue<MsgBase>();
        __receiveMsgQueue = new QuickList<MsgBase>();

        __lock = new object();

        _pAESMgr = new RijndaelManaged();

        _heartBeatTimer = TimerMgr.AddTimer();
        _heartBeatTimer.LoopCount(MathEx.INFINITE).Interval(HEART_BEAT_INTERVAL).Handler(_onHeartBeat);

        _bInUpdateSessionKey = false;
        _nLastSvrTime = 0;
        _nLastLocalTime = 0;
        __nMsgLength = 0;

        if (pConnectionHost != null)
        {
            _pConnectionHost = pConnectionHost;
        }
        else _pConnectionHost = new _DefaultConnectionHost();
	}

    public void SetConnectionHost(IConnectionHost host)
    {
        _pConnectionHost = host;
    }

    /// encryptedSessionKey is fetched from CenterServer
    public void Connect(string strHostName, int nPort, byte[] sessionKey, byte[] encryptedSessionKey)
    {
        if(IsConnected())
        {
            Debugger.Log("Connect failed! Can do connecting only when a connection is disconnected!!!");
            return;
        }

        _sessionKey = sessionKey;
        _encryptedSessionKey = encryptedSessionKey;

        _recreateEncryptionObjs(_sessionKey);

        try
        {
            _connectionState = ConnectionState.CONNECTING;

            _tcpClient = new TcpClient();
            _tcpClient.BeginConnect(strHostName, nPort, _onConnectedCallback, _tcpClient);
        }
        catch (Exception e)
        {
            _onConnectError(e);
        }
    }

    public void Disconnect()
    {
        if (_connectionState == ConnectionState.DISCONNECTED) return;

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

            _heartBeatTimer.Stop();
            _bInUpdateSessionKey = false;

            __nBytesReceived = 0;
            __bIsReceivingHeader = true;
            __bInWriting = false;

            __sendMsgQueue.Clear();

            _connectionState = ConnectionState.DISCONNECTED;
        }

    }

    public bool IsValidated()
    {
        return _connectionState == ConnectionState.VALIDATED;
    }

    public bool IsConnected() 
    {
#if UNITY_EDITOR
        if(_connectionState == ConnectionState.CONNECTED || _connectionState == ConnectionState.VALIDATED)
        {
            Debugger.Assert(_tcpClient != null && _tcpClient.Connected);
        }
#endif
        return _connectionState == ConnectionState.CONNECTED || _connectionState == ConnectionState.VALIDATED; 
    }

    public bool IsDisconnected() { return _connectionState == ConnectionState.DISCONNECTED; }

    public void SendMessage(MsgBase pMessage)
    {
        if (!IsConnected())  { Debugger.Log("Connection already down, send message failed!"); return; }

        bool bNeedFurtherProcessing = _pConnectionHost.SendingFilter(pMessage);
        if (!bNeedFurtherProcessing) return;

        lock (__lock)
        {
            __sendMsgQueue.Enqueue(pMessage);
        }
    }

    /// Get evaluated server time.
    public int EvaluateSvrTime()
    {
        //Debugger.Assert(_connectionState == ConnectionState.VALIDATED);
        return _nLastSvrTime + (NetworkStub.CurrentTimeSeconds() - _nLastLocalTime);
    }

    ///------------------
    private byte[] _decryptSessionKey(byte[] encryptedSessionKey)
    {
        return null;
    }

    private void _onWriteCallback(IAsyncResult pAsyncResult)
    {
        lock (__lock)
        {
            if (this.IsDisconnected()) return;

            try
            {               
                _networkStream.EndWrite(pAsyncResult);
                __bInWriting = false;
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

                if (numberOfBytesRead == 0) _onConnectError(new Exception("Disconnected by server!"));
                else
                {
                    __nBytesReceived += numberOfBytesRead;
                    /// Test nagle....
                    /// if (__nByteReceived <= 50) return;

                    while (__nBytesReceived >= MsgBase.HEAD_LENGTH)
                    {
                        if (__bIsReceivingHeader)
                        {
                            __bIsReceivingHeader = false;
                            __nMsgLength = ByteUtils.Byte2Short(__receiveBuffer, 0);

                            if (__nMsgLength == 0) throw new Exception("Message length is zero!!!");

                            Debugger.Assert(__nMsgLength <= RECEIVE_BUFFER_SIZE);
                        }

                        if (__nBytesReceived >= __nMsgLength)
                        {
                            _processMessageData();

                            __bIsReceivingHeader = true;

                            /// Copy remain bytes to buffer beginning.
                            ///... Reserves a second buffer to optimize performance!!! (Array.Copy will allocate temp buffer each time if buffer overlapped).
                            int nRemainByteCount = __nBytesReceived - __nMsgLength;
                            Array.Copy(__receiveBuffer, __nMsgLength, __receiveBuffer, 0, nRemainByteCount);
                            __nBytesReceived = nRemainByteCount;

                            __nMsgLength = 0;
                        }
                        else if (__nBytesReceived < __nMsgLength) break;
                    }

                    /// Try to read following data
                    _networkStream.BeginRead(__receiveBuffer, __nBytesReceived, RECEIVE_BUFFER_SIZE - __nBytesReceived
                                                            , _onReadCallback, 0);
                }

            }
            catch (Exception e)  { _onConnectError(e); }  
        }

    }

    private void _processMessageData()
    {
        Debugger.Assert(__nMsgLength != 0);

        bool bNeedFurtherProcessing = _pConnectionHost.ReceivingFilter(__receiveBuffer, __nMsgLength);
        if (!bNeedFurtherProcessing) return;

        /// Mono has a bug on AES. Must recreate transform object each time.
        if (_pDecryptor != null) _pDecryptor.Dispose();
        _pDecryptor = _pAESMgr.CreateDecryptor();

        /// Check AES blocks size
        Debugger.Assert((__nMsgLength - MsgBase.HEAD_LENGTH) % 16 == 0);

        byte[] bytesDecoded = _pDecryptor.TransformFinalBlock(__receiveBuffer, MsgBase.HEAD_LENGTH
                                                                                           , __nMsgLength - MsgBase.HEAD_LENGTH);

        ByteBuffer pByteBuffer = new ByteBuffer(bytesDecoded);

        //get message meta
        byte encodeType = pByteBuffer.FReadByte();
        int nMessageID = pByteBuffer.FReadInt();
        ushort sCheckCode = pByteBuffer.FReadUShort();
        int nTimestamp = pByteBuffer.FReadInt();

        //get message body
        byte[] bodyBytes = pByteBuffer.FReadBytes(pByteBuffer.size - MsgBase.META_DATA_LENGTH);

        Debugger.Assert(_pConnectionHost.ContainsServerMsgID(nMessageID));

        ///...Debugger.Assert(_isValidMsgFormat());

        if (nMessageID == SERVER_SESSION_UPDATE_KEY)
        {
            /// Update local time stamp
            _nLastSvrTime = nTimestamp;
            _nLastLocalTime = NetworkStub.CurrentTimeSeconds();

            /// process session key
            _onUpdateSessionKey(bodyBytes);
        }
        else
        {
            /// Process normal message
            MsgBase message = new MsgBase(nMessageID);
            message.timeStamp = nTimestamp;
            Debugger.Assert(message.type != SERVER_SESSION_UPDATE_KEY);

            message.DeserializeFrom(bodyBytes);

            __receiveMsgQueue.Add(message);
        }
    }

    private void _onUpdateSessionKey(byte[] encryptedSessionKey)
    {
        byte[] rawSessionKey = _deCodeSessionKey(encryptedSessionKey);
        byte[] realSessionKey = new byte[MsgBase.SESSION_KEY_SIZE];

        Buffer.BlockCopy(rawSessionKey, MsgBase.SESSION_KEY_HEAD_PADDING, realSessionKey, 0, MsgBase.SESSION_KEY_SIZE);

        _sessionKey = realSessionKey;
        _encryptedSessionKey = null;

        _recreateEncryptionObjs(_sessionKey);

        Debugger.Log("Session key updated.......................");

        if (_connectionState == ConnectionState.CONNECTED)
        {
            _connectionState = ConnectionState.VALIDATED;

            _heartBeatTimer.Start();

            /// Dispatch connected message
            MsgBase pConnectedMsg = new MsgBase((int)SocketMsgTypes.SOCKET_VALIDATED);
            __receiveMsgQueue.Add(pConnectedMsg);
        }

        Debugger.Assert(_bInUpdateSessionKey);
        _bInUpdateSessionKey = false;
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

                _connectionState = ConnectionState.CONNECTED;
                __bIsReceivingHeader = true;
                _networkStream = pTCPClient.GetStream();
                Debugger.Assert(_networkStream.CanRead);

                ///... 先read 2个字节的头，然后再read Data，这样逻辑可以简单很多（不需要考虑一次read了n个报文的情况了）
                _networkStream.BeginRead(__receiveBuffer, 0, RECEIVE_BUFFER_SIZE, _onReadCallback, 0);

                _connectionState = ConnectionState.CONNECTED;

                /// Dispatch connected message
                MsgBase pConnectedMsg = new MsgBase((int)SocketMsgTypes.SOCKET_CONNECTED);
                __receiveMsgQueue.Add(pConnectedMsg);
            }
            catch (Exception e) { _onConnectError(e); }
        }

        if (_connectionState == ConnectionState.CONNECTED)
        {
            MsgBase pMessage = new MsgBase(CLIENT_SESSION_SEND_KEY);
            SendMessage(pMessage);
        }
    }

    private void _onConnectError(Exception e)
    {
        Disconnect();

        /// Dispatch disconnected message            
        MsgBase pConnectedMsg = new MsgBase((int)SocketMsgTypes.SOCKET_DISCONNECTED);
        Dictionary<string, object> body = new Dictionary<string, object>();
        body["ErrorMessage"] = e.Message;
        pConnectedMsg._SetReceivedBody(body);
        __receiveMsgQueue.Add(pConnectedMsg);        
    }

	// Update is called once per frame
	public void Update () 
    {
        lock (__lock)
        {
            /// Processing receiving queue
            foreach (MsgBase message in __receiveMsgQueue)
            {
                /// if < 0, it is type of SocketMsgTypes
                if (message.type >= 0)
                {
                    /*
                    Debugger.Log("-------------------------------------\n Received message, type is: "
                                  + _pConnectionHost.ServerMsgToString(message.type)
                                    + " Body: " + message.messageText + "\n-------------------------------------");
                     */
                }

                DispatchEvent(message);
                _pConnectionHost.ReceivedListener(message);
            }

            __receiveMsgQueue.Clear();

            /// Process sending queue
            if(__sendMsgQueue.Count != 0 
                && (_connectionState == ConnectionState.CONNECTED || _connectionState == ConnectionState.VALIDATED)
                && !__bInWriting
                && !_bInUpdateSessionKey)
            {
                MsgBase pMessage = __sendMsgQueue.Dequeue();
                _sendMessage(pMessage);
            }
        }
	}

    ///------------
    public void Dispose()
    {
        Disconnect();
        _heartBeatTimer.Stop();
        TimerMgr.RemoveTimer(_heartBeatTimer);
        _heartBeatTimer = null;
    }

    public bool IsDisposed() { return _heartBeatTimer == null; }

    private void _onHeartBeat(object data)
    {
        if (_bInUpdateSessionKey)
        {
            _onConnectError(new Exception("Heart Beat time out!!!!!!!!!!!!!"));
            return;
        }

        Debugger.Log("Heart Beat-------------------------------------");

        MsgBase pMessage = new MsgBase(CLIENT_SESSION_HEARDBEAT);
        SendMessage(pMessage);
    }

    private void _recreateEncryptionObjs(byte[] sessionKey)
    {
        _pAESMgr.Key = _sessionKey;
        _pAESMgr.IV = UTF8Encoding.UTF8.GetBytes(IV_KEY);
        _pAESMgr.Mode = CipherMode.CBC;
        _pAESMgr.Padding = PaddingMode.PKCS7;

        if (_pEncryptor != null) _pEncryptor.Dispose();
        if (_pDecryptor != null) _pDecryptor.Dispose();

        _pEncryptor = _pAESMgr.CreateEncryptor();
        _pDecryptor = _pAESMgr.CreateDecryptor();
    }

    private void _sendMessage(MsgBase pMessage)
    {
        Debugger.Assert(IsConnected(), "_sendMessage failed, connection already down!");

        try
        {
            if (pMessage.type == CLIENT_SESSION_SEND_KEY)
            {
                Debugger.Assert(!_bInUpdateSessionKey);
                _bInUpdateSessionKey = true;

                ///... Note: Client should never known how to BuildEncryptedSessionKey, modify this later!!!! 
                ByteBuffer pBodyBuffer = MsgBase.BuildMessageBody(_encryptedSessionKey
                                                                                        , CLIENT_SESSION_SEND_KEY
                                                                                        , EvaluateSvrTime());

                ByteBuffer pBuffer = new ByteBuffer(pBodyBuffer.size + MsgBase.HEAD_LENGTH);
                Debugger.Assert(pBuffer.size <= MsgBase.MAX_MSG_LENGTH);

                pBuffer.WriteUShort((ushort)pBuffer.size);
                pBuffer.WriteBytes(pBodyBuffer.GetInternalBuffer());

                __bInWriting = true;
                _networkStream.BeginWrite(pBuffer.GetInternalBuffer(), 0, pBuffer.size, _onWriteCallback, pMessage);
            }
            else
            {
                if(pMessage.type == CLIENT_SESSION_HEARDBEAT)
                {
                    Debugger.Assert(!_bInUpdateSessionKey);
                    _bInUpdateSessionKey = true;
                }

                ///... Use buffer pool for sending later!!!!
                byte[] buffer = new byte[SEND_BUFFER_SIZE];

                /// Mono has a bug on AES, must recreate transform object each time!!!
                if (_pEncryptor != null) _pEncryptor.Dispose();
                _pEncryptor = _pAESMgr.CreateEncryptor();

                int nSize = pMessage.SerializeTo(buffer, _pEncryptor, EvaluateSvrTime());

                __bInWriting = true;
                _networkStream.BeginWrite(buffer, 0, nSize, _onWriteCallback, pMessage);
                _pConnectionHost.SentListener(pMessage, buffer, nSize);
            }
        }
        catch (Exception e) 
        {
            _onConnectError(e);
        }
    }

    private byte[] _deCodeSessionKey(byte[] bytes)
    {
        byte XOR_CONST = 0X12;

        int nMaxLength = bytes.Length;
        byte[] arrByte = new byte[nMaxLength];
        int lastIndex = nMaxLength - 11;
        for (int i = 0; i < bytes.Length; i++)
        {
            if (i < lastIndex)
            {
                arrByte[11 + i] = (byte)(bytes[i] ^ XOR_CONST);
            }
            else
            {
                arrByte[i - lastIndex] = (byte)(bytes[i] ^ XOR_CONST);
            }
        }
        return arrByte;
    }

}
