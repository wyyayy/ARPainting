
namespace Actor.Net
{

    /// HeaderType determines which NetMessage's subclass used for a message.
    public class HeaderType
    {
        public const short _HAS_TYPE = 0x1 << 0;
        public const short _HAS_RPC_ID = 0x1 << 1;
        public const short _HAS_DATA_TYPE = 0x1 << 2;


        public const short Invalid = -1;

        public const short Notify = _HAS_TYPE | _HAS_DATA_TYPE;

        public const short RPCCall = _HAS_TYPE | _HAS_RPC_ID | _HAS_DATA_TYPE;
        public const short RPCReturn = _HAS_RPC_ID | _HAS_DATA_TYPE;

        public static int GetHeaderSize(short headerType)
        {
            int nSize = 1; /// HeaderType filed is 1 byte

            if ((headerType & _HAS_TYPE) != 0) nSize += 2;
            if ((headerType & _HAS_RPC_ID) != 0) nSize += 2;
            if ((headerType & _HAS_DATA_TYPE) != 0) nSize += 1;

            return nSize;
        }

        public static string GetHeaderName(int msgType)
        {
            string strName = null;
            switch (msgType)
            {
                case Notify:
                    strName = "Notify";
                    break;

                case RPCCall:
                    strName = "RPCCall";
                    break;

                case RPCReturn:
                    strName = "RPCReturn2Gate";
                    break;
            }

            return strName;
        }

    }

}