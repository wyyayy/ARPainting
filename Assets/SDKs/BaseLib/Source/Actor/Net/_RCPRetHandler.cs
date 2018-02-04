using System;
using System.Collections;
using System.Collections.Generic;

using BaseLib;

namespace Actor.Net
{
    public class _RPCRetHandler : RefCounter, IAsyncCall<_NetRPCReturn>
    { 			
	    public NetRPCCall _CallMessage;	
	    public _NetRPCReturn _RetrunMessage;
	 
	    protected Action<_NetRPCReturn, Exception> _OnCallback;
	
	    protected bool _bIsDone;
		
	    private Timer _timer;

	    private _NetRPCComponent _netRPCComponent;	
	    private Exception _error;
		
	    public _RPCRetHandler(_NetRPCComponent rpcComponent
                            , NetRPCCall message
						    , long timeoutTime
						    , Action<_NetRPCReturn, Exception> callback)
	    {
            _netRPCComponent = rpcComponent;
		    _CallMessage = message;		
		    _bIsDone = false;
		    
            _timer = TimerMgr.AddTimer();
            _timer.Handler(_onTimeout);
            _timer.Interval(timeoutTime).Start();
		
		    _OnCallback = callback;			
		    _error = null;			
	    }	
				
	    private void _onTimeout(object timer)
	    {
		    _timer = null;
		
		    String strMsg = String.Format("NetRPC call timeout, message: {0}", _CallMessage.ToString());
		    _HandleRPCReturn(null, new Exception(strMsg));
		    Debugger.Assert(_CallMessage._GetRPCID() != 0);
		
		    _netRPCComponent._OnRPCTimeout(this);
		
		    _bIsDone = true;		
	    }
	
	    public void _HandleRPCReturn(_NetRPCReturn retMsg, Exception exception)
	    {
		    if(_timer != null)
		    {
			    _timer.Stop();
                TimerMgr.RemoveTimer(_timer);
			    _timer = null;
		    }
		
		    _bIsDone = true;
		    _RetrunMessage = retMsg;
		
		    _error = exception;
		
		    if(_OnCallback != null)
		    {
                _OnCallback(retMsg, exception);
		    }
		    else 
		    {
			    /// Should never go here  
			    Debugger.Assert(false);
		    }
	    }
	
	    /// --- Implements IAsyncCall
	    public _NetRPCReturn GetReturn()
	    {		
		    if(_error != null) throw _error;
		    return _RetrunMessage; 
	    }
	
	    public bool IsDone() 
	    {
		    return _bIsDone;
	    }
	
	    /// Note: This doesn't ensure the remote server don't receives RPC call, but only ensure the caller don't 
	    /// receives RPC callback.
	    public void Cancel()
	    {
            if(_timer != null)
            {
                TimerMgr.RemoveTimer(_timer);
                _timer = null;
            }
		    _netRPCComponent._CancelRPCCall(this);
	    }

        public void _Clear()
        {
            if (_timer != null)
            {
                TimerMgr.RemoveTimer(_timer);
                _timer = null;
            }
        }

        public void Update(float fTime) { }

        public void Pause(float fTime) { }
        public void Resume(float fTime) { }

        public void Stop() { }

        public YieldInstructionType GetInstructionType() { return YieldInstructionType.Custom;  }

        public void Start(float fTime) { }

        /// Some instruction can timeout
        public bool IsTimeout() { return false;  }

        override protected void _onRelease()
        {
            if(_timer != null)
            {
                TimerMgr.RemoveTimer(_timer);
                _timer = null;
            }
        }
    }

}