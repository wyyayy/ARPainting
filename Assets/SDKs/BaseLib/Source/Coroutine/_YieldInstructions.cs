using System;


namespace BaseLib
{ 
    internal class _ReturnValue : YieldInstruction
    {
        public object returnValue;

        override protected void _onRelease() { returnValue = null; CoroutineMgr._Instance._FreeReturnValue(this); }

        override public YieldInstructionType GetInstructionType() { return YieldInstructionType.ReturnValue; }
    }

    ///-----
    internal class _WaitConditionObj : YieldInstruction
    {
        internal float _fDestTime;
        internal float _fCurTime;
        internal float _fTimeOut;
        internal float _fPausedTime;

        protected ICondition _condition;
        protected System.Action _updateHandler;

        override public void Start(float fTime)
        {
            _fDestTime = fTime + _fTimeOut;
            _fPausedTime = 0;
        }

        override public void Pause(float fTime) 
        {
            _fPausedTime = fTime;
        }

        override public void Resume(float fTime) 
        {
            _fDestTime = _fDestTime + (fTime - _fPausedTime);
            _fPausedTime = 0;
        }

        public void SetParams(ICondition conditionObj, float fTimeOut, System.Action updateHandler)
        {
            _fTimeOut = fTimeOut;
            _condition = conditionObj;
            _updateHandler = updateHandler;
        }

        override public bool IsDone() { return _condition.IsTrue() || _fCurTime >= _fDestTime; }

        override public void Update(float fTime)
        {
            if (_updateHandler != null) _updateHandler();
            _fCurTime = fTime;
        }

        override public bool IsTimeout() { return _fCurTime >= _fDestTime; }
    }

    ///------
    internal class _WaitTime : YieldInstruction
    {
        internal float _fDestTime;
        internal float _fCurTime;
        internal float _fTimeOut;
        internal float _fPausedTime;

        public _WaitTime() { }

        override public void Start(float fTime)
        {
            _fDestTime = fTime + _fTimeOut;
            _fPausedTime = 0;
        }

        public void SetParams(float fTimeOut = 0.0f)
        {
            _fTimeOut = fTimeOut;
        }

        override public void Update(float fTime)
        {
            _fCurTime = fTime;
        }

        override public void Pause(float fTime)
        {
            _fPausedTime = fTime;
        }

        override public void Resume(float fTime)
        {
            _fDestTime = _fDestTime + (fTime - _fPausedTime);
            _fPausedTime = 0;
        }

        override public bool IsDone()
        {
            return _fCurTime >= _fDestTime;
        }

        protected override void _onRelease()
        {
            CoroutineMgr._Instance._FreeTimeWaiter(this);
        }
    }
    ///------
    internal class _WaitConditionFunc : YieldInstruction
    {
        internal float _fDestTime;
        internal float _fCurTime;
        internal float _fTimeOut;
        internal float _fPausedTime;

        protected Func<bool> _waitConditionFunc;
        protected System.Action _updateHandler;

        override public void Start(float fTime)
        {
            _fDestTime = fTime + _fTimeOut;
            _fPausedTime = 0;
        }

        override public void Pause(float fTime)
        {
            _fPausedTime = fTime;
        }

        override public void Resume(float fTime)
        {
            _fDestTime = _fDestTime + (fTime - _fPausedTime);
            _fPausedTime = 0;
        }

        public void SetParams(Func<bool> waitConditionFunc, float fTimeOut, System.Action updateHandler)
        {
            _fTimeOut = fTimeOut;
            _waitConditionFunc = waitConditionFunc;
            _updateHandler = updateHandler;
        }

        override public bool IsDone() { return _waitConditionFunc() || _fCurTime >= _fDestTime; }

        override public void Update(float fTime)
        {
            if(_updateHandler != null) _updateHandler();
            _fCurTime = fTime;
        }

        override public bool IsTimeout() { return _fCurTime >= _fDestTime; }

        protected override void _onRelease()
        {
            _waitConditionFunc = null;
            _updateHandler = null;

            CoroutineMgr._Instance._FreeConditionWaiter(this);
        }
    }

    ///------
    internal class _WaitSignal : YieldInstruction
    {
        internal float _fDestTime;
        internal float _fCurTime;
        internal float _fTimeOut;
        internal float _fPausedTime;

        protected Signal _signal;
        protected System.Action _updateHandler;

        protected bool _bIsDone;

        override public void Start(float fTime)
        {
            _fDestTime = fTime + _fTimeOut;
            _fPausedTime = 0;
        }

        public void SetParams(Signal signal, float fTimeOut, System.Action updateHandler)
        {
            _fTimeOut = fTimeOut;
            _signal = signal;
            _updateHandler = updateHandler;

            _signal.Connect(_onSignal);
        }

        override public void Pause(float fTime)
        {
            _fPausedTime = fTime;
        }

        override public void Resume(float fTime)
        {
            _fDestTime = _fDestTime + (fTime - _fPausedTime);
            _fPausedTime = 0;
        }

        override public bool IsDone() 
        {
            return _bIsDone || _fCurTime >= _fDestTime; 
        }

        override public void Update(float fTime)
        {
            if (_updateHandler != null) _updateHandler();
            _fCurTime = fTime;
        }

        override public bool IsTimeout() 
        {
            if (_fCurTime >= _fDestTime)
            {
                if (_signal.IsConnected()) _signal.Disconnect(_onSignal);
                return true;
            }
            else return false;
        }

        public override void Stop()
        {
            if (_signal != null)
            {
                if (_signal.IsConnected()) _signal.Disconnect(_onSignal);
                _signal = null;
            }
            _updateHandler = null;
            _bIsDone = false;
        }

        protected override void _onRelease()
        {
            Debugger.Assert(_signal == null);
            Debugger.Assert(_updateHandler == null);

            CoroutineMgr._Instance._FreeSignalWaiter(this);
        }
    
        protected void _onSignal()
        {
            if (_signal.IsConnected()) _signal.Disconnect(_onSignal);
            _bIsDone = true;
        }
    }

    ///-----
    internal class _WaitSignal<T> : YieldInstruction
    {
        internal float _fDestTime;
        internal float _fCurTime;
        internal float _fTimeOut;
        internal float _fPausedTime;

        protected Signal<T> _signal;
        protected System.Action _updateHandler;

        protected bool _bIsDone;

        override public void Start(float fTime)
        {
            _fDestTime = fTime + _fTimeOut;
            _fPausedTime = 0;
        }

        public void SetParams(Signal<T> signal, float fTimeOut, System.Action updateHandler)
        {
            _fTimeOut = fTimeOut;
            _signal = signal;
            _updateHandler = updateHandler;

            _bIsDone = false;

            _signal.Connect(_onSignal);
        }

        override public void Pause(float fTime)
        {
            _fPausedTime = fTime;
        }

        override public void Resume(float fTime)
        {
            _fDestTime = _fDestTime + (fTime - _fPausedTime);
            _fPausedTime = 0;
        }

        override public bool IsDone() { return _bIsDone || _fCurTime >= _fDestTime; }

        override public void Update(float fTime)
        {
            if (_updateHandler != null) _updateHandler();
            _fCurTime = fTime;
        }

        override public bool IsTimeout() { return _fCurTime >= _fDestTime; }

        public override void Stop()
        {
            if(_signal != null)
            {
                if (_signal.IsConnected()) _signal.Disconnect(_onSignal);
                _signal = null;
            }
            _updateHandler = null;
            _bIsDone = false;            
        }

        protected void _onSignal(T code)
        {
            _bIsDone = true;
        }
    }

    ///------
    internal class _WaitFrameEnd : YieldInstruction
    {
        public _WaitFrameEnd() { }

        override public void Update(float fTime) { }
        override public bool IsDone() { return true; }

        protected override void _onRelease()
        {
            CoroutineMgr._Instance._FreeFrameWaiter(this);
        }
    }
}

