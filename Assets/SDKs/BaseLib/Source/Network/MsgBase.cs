using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

using JsonFx.Json;
using System.Text;

using BaseLib;

/// Old message format. Obsolete
sealed public class MsgBase : EventData<int>
{
    public const int SESSION_KEY_HEAD_PADDING = 2;
    public const int SESSION_KEY_TAIL_PADDING = 3;

    public const int SESSION_KEY_SIZE = 16;
    public const int RAW_SESSION_KEY_SIZE = SESSION_KEY_SIZE + SESSION_KEY_HEAD_PADDING + SESSION_KEY_TAIL_PADDING; 

    public const byte MESSAGE_ENCODE_TYPE_JSON = 0;
    public const byte MESSAGE_ENCODE_TYPE_PROTOBUF = 1;

    /// Use ushort to count message length. So the max message length is 64k
    public const int HEAD_LENGTH = 2; 
    public const int META_DATA_LENGTH = 11;

    public const int MAX_MSG_LENGTH = 65536;
    public const int MAX_CONTENT_LENGTH = MAX_MSG_LENGTH - META_DATA_LENGTH - HEAD_LENGTH;

    /// For unit test only
    public object userData;

    ///--- Length 
    public ushort length;

    public int timeStamp;

    public string messageText;

    ///--- Body
    private object _sendBody;
    private Dictionary<string, object> _receivedBody;

    public MsgBase() 
    {
        type = -1;
        length = 0;
        timeStamp = 0;
        messageText = null;
        _sendBody = null;
        _receivedBody = null;
    }

    public MsgBase(int nMsgID)
        : base(nMsgID)
    {
        length = 0;
        timeStamp = 0;
        messageText = null;
        _sendBody = null;
        _receivedBody = null;
    }

    public void SetRootValue(string name, object value)
    {
        if(_sendBody == null)
        {
            _sendBody = new Dictionary<string, object>();
        }

        Debugger.Assert(_sendBody is Dictionary<string, object>);
        var body = _sendBody as Dictionary<string, object>;
        body[name] = value;
    }

    public void SetBody(object body)
    {
        _sendBody = body;
    }

    public void _SetReceivedBody(Dictionary<string, object> body)
    {
        _receivedBody = body;
    }

    public T GetBody<T>()
    {
        T body = JsonReader.Deserialize<T>(messageText);
        return body;
    }

    public object GetBody(Type type)
    {
        object body = JsonReader.Deserialize(messageText, type);
        return body;
    }

    public Dictionary<string, object> GetBody()
    {
        if(_receivedBody == null)
        {
            _receivedBody = JsonReader.Deserialize(messageText) as Dictionary<string, object>;
        }
        return _receivedBody;
    }

    override public string ToString()
    {
        if(_sendBody != null)
        {
            /// This is a 'send' message
            return "Message, type = " + this.type + ", TimeStamp = " + timeStamp + ", body = " + JsonWriter.Serialize(_sendBody);
        }
        else
        {
            if (messageText != null)
            {
                /// This is a received message
                return "Message, type = " + this.type + ", TimeStamp = " + timeStamp + ", body = " + messageText;
            }
            else
            {
                /// This is a "send" message that has no body.
                return "Message, type = " + this.type + ", TimeStamp = " + timeStamp + ", body = {}";
            }
        }
    }

    /// Get sent body string
    public string GetBodyString()
    {
        return JsonWriter.Serialize(_sendBody);
    }

    public int SerializeTo(byte[] buffer, ICryptoTransform pEncryptor, int nTimeStamp) 
    {
        string strBodyString = _sendBody != null? JsonWriter.Serialize(_sendBody) : "{}";

        byte[] bodyBytes = Encoding.UTF8.GetBytes(strBodyString);

        int nContentLength = (ushort)bodyBytes.Length;
        Debugger.Assert(nContentLength <= MsgBase.MAX_CONTENT_LENGTH);

        /// Create body (Meta + Content)
        this.timeStamp = nTimeStamp;
        ByteBuffer pBodyBuffer = BuildMessageBody(bodyBytes, this.type, nTimeStamp);

        Debugger.Assert(pBodyBuffer.position == pBodyBuffer.size);

        /// Encrypt body
        byte[] encryptedBytes = pEncryptor.TransformFinalBlock(pBodyBuffer.GetInternalBuffer(), 0, pBodyBuffer.size);

        ByteBuffer pFinalBuffer = new ByteBuffer(buffer);

        length = (ushort)(HEAD_LENGTH + encryptedBytes.Length);
        Debugger.Assert(HEAD_LENGTH + encryptedBytes.Length <= ConnectionOld.SEND_BUFFER_SIZE);

        pFinalBuffer.WriteUShort(length);
        pFinalBuffer.WriteBytes(encryptedBytes);

        return length;
    }

    public void DeserializeFrom(byte[] byteData)
    {
        this.messageText = Encoding.UTF8.GetString(byteData, 0, byteData.Length);
    }

    public static ByteBuffer BuildMessageBody(byte[] arrBody, int nMessageID, int nTimeStamp)
    {
        ByteBuffer pBuffer = new ByteBuffer(arrBody.Length + MsgBase.META_DATA_LENGTH);

        ushort sCheckCode = NetworkStub.GetCheckCode(arrBody);
        byte byteEncodeType = MsgBase.MESSAGE_ENCODE_TYPE_JSON;

        pBuffer.WriteByte(byteEncodeType);
        pBuffer.WriteInt(nMessageID);
        pBuffer.WriteUShort((ushort)sCheckCode);
        pBuffer.WriteInt(nTimeStamp);
        pBuffer.WriteBytes(arrBody);

        return pBuffer;
    }
}


