
public class MsgType
{
    public const int _HAS_MSG_ID = 0x1 << 0;
    public const int _HAS_RPC_ID = 0x1 << 1;
    public const int _HAS_DEST_OR_SRC_TYPE = 0x1 << 2;
    public const int _HAS_DEST_OR_SRC_ID = 0x1 << 3;

    public const byte SimpleMessage = _HAS_MSG_ID;

    public const byte RPCCall = _HAS_MSG_ID | _HAS_RPC_ID | _HAS_DEST_OR_SRC_TYPE | _HAS_DEST_OR_SRC_ID;
    public const byte RPCReturn2Gate = _HAS_RPC_ID | _HAS_DEST_OR_SRC_TYPE | _HAS_DEST_OR_SRC_ID;
    public const byte RPCReturn2Client = _HAS_RPC_ID;

    public const byte Notify = _HAS_MSG_ID | _HAS_DEST_OR_SRC_TYPE | _HAS_DEST_OR_SRC_ID;

    public static string GetMsgTypeName(int msgType)
    {
        string strName = null;
        switch (msgType)
        {
            case SimpleMessage:
                strName = "SimpleMessage";
                break;

            case RPCCall:
                strName = "RPCCall";
                break;

            case RPCReturn2Gate:
                strName = "RPCReturn2Gate";
                break;

            case RPCReturn2Client:
                strName = "RPCReturn2Client";
                break;

            case Notify:
                strName = "Notify";
                break;
        }

        return strName;
    }
    
}

public enum ServerSysMsgType
{
    HANDSHAKE_TIMEOUT = 10,
}

public class SocketLogicErrType
{
    public const int CONNECTION_DOWN = 0;
    public const int REJECTED_BY_SENDING_FILTER = 1;
}
