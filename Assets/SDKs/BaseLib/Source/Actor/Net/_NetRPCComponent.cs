using System;
using System.Collections;
using System.Collections.Generic;

using BaseLib;

namespace Actor.Net
{  
    /// Note: no RPC call handling logic.
    public class _NetRPCComponent
    {	
	    protected Dictionary<ushort, _RPCRetHandler> _rpcRetHandlers;
	    protected int __rpcIndex;

        protected Connection _connection;
	
	    public _NetRPCComponent(Connection connection)
	    {		
            _connection = connection;
		    _rpcRetHandlers = new Dictionary<ushort, _RPCRetHandler>();
	    }

	    public void Clear()
	    {
            foreach(var retHandler in _rpcRetHandlers)
            {
                retHandler.Value._Clear();
            }
		    _rpcRetHandlers.Clear();

		    __rpcIndex = 1;
	    }
	
	    public _RPCRetHandler Call(NetRPCCall callMessage
							, Action<_NetRPCReturn, Exception> callback
							, long timeout)
        {
		    __rpcIndex = 1 + ((__rpcIndex + 1) % 65534);
		    callMessage._SetRPCID(__rpcIndex);		
		
		    _RPCRetHandler rpcRetHandler = new _RPCRetHandler(this, callMessage
													    , timeout, callback);
		
		    _rpcRetHandlers.Add((ushort)__rpcIndex, rpcRetHandler);

		    _connection.SendNetMessage(callMessage);
		
		    return rpcRetHandler;
	    }
			
	    public void _OnRPCReturn(_NetRPCReturn rpcReturn)
	    {
		    Debugger.Assert(rpcReturn._GetRPCID() != 0);

		    /// Call the callback. 
		    _RPCRetHandler rpcRetHandler = _rpcRetHandlers[rpcReturn._GetRPCID()];
            _rpcRetHandlers.Remove(rpcReturn._GetRPCID());
		
		    if(rpcRetHandler != null)
		    {
			    rpcReturn.type = rpcRetHandler._CallMessage.type;			
			    rpcRetHandler._HandleRPCReturn(rpcReturn, null);
		    }
		    else
		    {
				string msg = string.Format("Received a RPC response, but the RPC_ID {0} is invalid (not in _rpcCallMap). " 
                                                             + "The RPC call may be timeout or RPC response corrupted!"
                                                                , rpcReturn._GetRPCID());
                Debugger.LogError(msg);
            }
	    }
		
	
	    /// Internal used by _RPCRetHandler.
	    public void _OnRPCTimeout(_RPCRetHandler rpcRetHandler)
	    {		
		    bool ret = _rpcRetHandlers.Remove(rpcRetHandler._CallMessage._GetRPCID());
		
		    if(!ret)
		    {
			    string msg = string.Format("RPC call {0} timeout, but it is not in the _rpcRetHandlerMap!", rpcRetHandler._CallMessage);
                Debugger.LogError(msg);
		    }
	    }	
	
	    /// Internal used by _RPCRetHandler.
	    public void _CancelRPCCall(_RPCRetHandler rpcRetHandler)
	    {		
		    if(_rpcRetHandlers.Remove(rpcRetHandler._CallMessage._GetRPCID()))
		    {
			    rpcRetHandler._HandleRPCReturn(null, NetMessage.CANCELED);			
			    Debugger.LogWarning("RPC call " +  rpcRetHandler._CallMessage + " was canceled!");
		    }
		    else
		    {
                Debugger.LogWarning("RPC call " + rpcRetHandler._CallMessage + " canceling failed, the call already done!");
		    }
	    }		  	
    }

}