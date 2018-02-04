using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Text;

using JsonFx.Json;

using Debugger = BaseLib.Debugger;

/*
Message format (with RPC and gate routing support)
    Request: 
        | Len | MsgType ! MsgID ! RPC_ID ! (Dest/Src) Type ! (Dest/Src) ID ! Body | 
    Response: 
        | Len | MsgType ! RPC_ID ! (Dest/Src) Type ! (Dest/Src) ID ! Body | 

    Note: RPC response no need MsgID. The receiver can fetch the MsgID with the RPC_ID from RPC message map if needed. 

    Push/Notify: 
        | Len | MsgType ! MsgID ! (Dest/Src) Type ! (Dest/Src) ID ! Body |

*/
public class Message : EventData<ushort>
{
    public static Func<int, string> MsgIDToName = id => { return id.ToString(); };

    public static Message CONNECTION_DOWN = new Message((int)SocketLogicErrType.CONNECTION_DOWN);
    public static Message REJECTED_BY_SENDING_FILTER = new Message((int)SocketLogicErrType.REJECTED_BY_SENDING_FILTER);

    public const int LEN_SIZE = 2;
    public const int MSG_TYPE_SIZE = 1;
    public const int MSG_ID_SIZE = 2;
    public const int RPC_ID_SIZE = 2;
    public const int DEST_OR_SRC_TYPE_SIZE = 1;
    public const int DEST_OR_SRC_ID_SIZE = 4; 

    ///---
    public ushort len;

    public byte msgType;

    public ushort msgID { get { return this.type; } set { this.type = value; } }

    public ushort RPC_ID;

    public byte destOrSrcType;

    public int destOrSrcID;

    public string body;

    protected Dictionary<string, object> _body;

    public Action<Message> _Callback;

    /// Record the RPC call start time. Used for checking RPC call timeout.
    internal float _RPCCallStartTime;

    public Message()
    {
        _body = new Dictionary<string, object>();
    }

    static public Message NewEmptyMsg()
    {
        return null;
    }

    public Message(ushort type)
    {
        this.type = type;
        _body = new Dictionary<string, object>();
    }

    public bool IsRPCCall()
    {
        var ret = (msgType == MsgType.RPCCall);
        Debugger.ConditionalAssert(ret == true, this.msgID != 0);
        return ret;
    }

    public bool IsRPCReturn()
    {
        var ret = (msgType == MsgType.RPCReturn2Gate) || (msgType == MsgType.RPCReturn2Client);
        Debugger.ConditionalAssert(ret == true, this.RPC_ID != 0);
        return ret;
    }

    internal bool _HasField(byte filed)
    {
        return (msgType & filed) != 0;
    }

    public Dictionary<string, object> GetJson()
    {
        //Debugger.Assert(body != null && body != "");
        //return JsonReader.Deserialize(body) as Dictionary<string, object>;
        return _body;
    }

    public void SetValue(string key, object value)
    {
        Debugger.Assert(_body != null);
        _body[key] = value;
    }

    public bool GetBool(string key)
    {
        Debugger.Assert(_body.ContainsKey(key));
        return (bool)_body[key];
    }

    public int GetInt(string key)
    {
        Debugger.Assert(_body.ContainsKey(key));
        return (int)_body[key];
    }

    public string GetString(string key)
    {
        Debugger.Assert(_body.ContainsKey(key));
        return (string)_body[key];
    }

    public void Deserialize(byte[] buffer, int nNumOfTypes)
    {
        ByteBuffer byteBuffer = new ByteBuffer(buffer, nNumOfTypes, 0);
        this.len = byteBuffer.FReadUShort();
        this.msgType = byteBuffer.FReadByte();

        int nMetaSize = LEN_SIZE + MSG_TYPE_SIZE;

        if ((msgType & MsgType._HAS_MSG_ID) != 0)
        {
            nMetaSize += MSG_ID_SIZE;
            this.msgID = byteBuffer.FReadUShort();
        }

        if ((msgType & MsgType._HAS_RPC_ID) != 0)
        {
            nMetaSize += RPC_ID_SIZE;
            this.RPC_ID = byteBuffer.FReadUShort();
        }

        if ((msgType & MsgType._HAS_DEST_OR_SRC_TYPE) != 0)
        {
            nMetaSize += DEST_OR_SRC_TYPE_SIZE;
            this.destOrSrcType = byteBuffer.FReadByte();
        }

        if ((msgType & MsgType._HAS_DEST_OR_SRC_ID) != 0)
        {
            nMetaSize += DEST_OR_SRC_ID_SIZE;
            this.destOrSrcID = byteBuffer.FReadInt();
        }

        this.body = byteBuffer.FReadString(nNumOfTypes - nMetaSize);
        _body = JsonReader.Deserialize(body) as Dictionary<string, object>;
    }

    public void Serialize(out byte[] rawBytes, out int length)
    {
        Debugger.Assert(type != 0);

        if (body == null)
        {
            if (_body == null) body = "{}";
            else body = JsonWriter.Serialize(_body);
        }

        bool bHasRPC_ID = false;
        bool bHasMsgID = false;
        bool bHasDestOrSrcType = false;
        bool bHasDestOrSrcID = false;

        length = LEN_SIZE + MSG_TYPE_SIZE;

        if ((msgType & MsgType._HAS_MSG_ID) != 0)
        {
            bHasMsgID = true;
            length += MSG_ID_SIZE;
        }

        if( (msgType & MsgType._HAS_RPC_ID) != 0)
        {
            bHasRPC_ID = true;
            length += RPC_ID_SIZE;
        }

        if ((msgType & MsgType._HAS_DEST_OR_SRC_TYPE) != 0)
        {
            bHasDestOrSrcType = true;
            length += DEST_OR_SRC_TYPE_SIZE;
        }

        if ((msgType & MsgType._HAS_DEST_OR_SRC_ID) != 0)
        {
            bHasDestOrSrcID = true;
            length += DEST_OR_SRC_ID_SIZE;
        }

        Debugger.Assert(body.Length <= ushort.MaxValue - length);
        length += body.Length;

        ///... Use buffer pool later!!!!
        this.len = (ushort)length;
        ByteBuffer byteBuffer = new ByteBuffer(length);

        byteBuffer.WriteUShort((ushort)length);
        byteBuffer.WriteByte(msgType);

        if (bHasMsgID) byteBuffer.WriteUShort(msgID);
        if (bHasRPC_ID) byteBuffer.WriteUShort(RPC_ID);
        if (bHasDestOrSrcType) byteBuffer.WriteByte(destOrSrcType);
        if (bHasDestOrSrcID) byteBuffer.WriteInt(destOrSrcID);        

        byteBuffer.WriteString(body);

        rawBytes = byteBuffer.GetInternalBuffer();
    }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder(100);
        builder.Append("{ Len: ").Append(len)
                    .Append(", MsgType: ").Append(MsgType.GetMsgTypeName(msgType));

        if (this._HasField(MsgType._HAS_MSG_ID)) builder.Append(", MsgID: ").Append(MsgIDToName(msgID));
        if(this._HasField(MsgType._HAS_RPC_ID)) builder.Append(", RPC_ID: ").Append(RPC_ID);
        if(this._HasField(MsgType._HAS_DEST_OR_SRC_TYPE)) builder.Append(", Dest/Src_Type: ").Append(destOrSrcType);
        if(this._HasField(MsgType._HAS_DEST_OR_SRC_ID)) builder.Append(", Dest/Src_ID: ").Append(destOrSrcID);
                    
        builder.Append(", [Body]: ").Append(body == null? JsonWriter.Serialize(_body) : body).Append(" }");

        return builder.ToString();
    }
}
