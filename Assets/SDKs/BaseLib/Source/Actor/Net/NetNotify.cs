using System;

using Actor.Serializable;

namespace Actor.Net
{
    /* 
        Binary format: same as NetMessage
    */
    public class NetNotify : NetMessage
    {
	    public NetNotify() : base()
	    {		
		    _headerType = HeaderType.Notify;
	    }
		
	    public NetNotify(short notifyMsgType, ISerializableData data) : base(notifyMsgType, data)
	    {		
		    _headerType = HeaderType.Notify;
	    }
    }
}