using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace BaseLib
{
    public enum CoroutineState
    {
        /// Coroutine object already be marked as disposed
        Disposed,
        /// A pooled coroutine is freed to pool.
        Freed,
        /// A pooled coroutine is in use.
        InUse, 

        Stopped,
        Paused,
        /// Note: if state < Running, 'OnUpdate' event will not be trigger!
        Running,
    }

    public struct CoroutineInfo
    {
        public string name;
        public CoroutineState state;
        public bool scalable;

        public CoroutineInfo(string name, CoroutineState state, bool scalable)
        {
            this.name = name;
            this.state = state;
            this.scalable = scalable;
        }

        public override string ToString()
        {
            return this.name;
        }
    }

    public class Coroutine : YieldInstruction, _IDisposable
    {
        /// Valid only when coroutine is Pooled. 
        /// If _AutoKill is true, the coroutine object will be freed to pool automatically when Stopped.
        /// Internal implementation is based on RefCounter. When _AutoKill is false, ref-counter will increased by call IncRef().
        /// When user manually call StopAndKill(), the DecRef() will be called to free the object.
        public bool _AutoKill;

        internal bool _bPooled = false;
        internal bool _bInQueue = false;

        internal CoroutineMgr _pCoroutineMgr;

        public bool scalable = true;

        public string name;

        public CoroutineState state { get { return _state; } internal set { _state = value; } }
        protected CoroutineState _state;

        /// OnUpdate will be called at each frame when coroutine is running. 
        /// You can change coroutine or current yield instruction's state during OnUpdate, and
        /// the state changes will task effect immediately. 
        /// If coroutine is paused, OnUpdate will not be called.
        public Coroutine OnUpdate(Action<IYieldInstruction> onUpdate) { _onUpdate = onUpdate; return this;}
        /// Will be called when a yield instruction done. 
        public Coroutine OnYieldDone(Action<IYieldInstruction> onYieldDone) { _onYieldDone = onYieldDone; return this; }
        /// When coroutine is complete or stopped manually ( call Stop() ), this event will be triggered
        public Coroutine OnStop(Action<Coroutine> onStop) { _onStopped = onStop; return this; }
        /// When coroutine is complete, this event will be triggered.
        public Coroutine OnComplete(Action<Coroutine, object> onComplete) { _onComplete = onComplete; return this; }

        /// When any exception occurred, this event will be triggered.
        internal Coroutine _OnException(Action<Coroutine, CoroutineException> onException) { _onException = onException; return this; }
        /// When coroutine start, this event will be triggered.
        internal Coroutine _OnStart(Action<Coroutine> onStart) { _onStart = onStart; return this; }

        protected Action<IYieldInstruction> _onUpdate;
        protected Action<IYieldInstruction> _onYieldDone;
        protected Action<Coroutine> _onStart;
        protected Action<Coroutine> _onStopped;
        protected Action<Coroutine, object> _onComplete;
        protected Action<Coroutine, CoroutineException> _onException;

        internal event Action<Coroutine> _OnChildCoStart;
        internal bool _HasChildCoStartListener(Action<Coroutine> listener)
        {
            foreach (Action<Coroutine> handler in _OnChildCoStart.GetInvocationList())
            { 
                if (handler == listener) return true; 
            }
            return false;
        }
        internal event Action<Coroutine> _OnChildCoStop;
        internal bool _HasChildCoStopListener(Action<Coroutine> listener) 
        {
            foreach (Delegate handler in _OnChildCoStop.GetInvocationList()) { if (handler == (listener as System.Delegate)) return true; }
            return false;
        }
        
        protected IEnumerator _pCoroutineFunc;
        public IYieldInstruction currentInstruction { get {return _pCurrentInstruction; } }
        protected IYieldInstruction _pCurrentInstruction;

        private bool __bIsManualStopped;
        internal Coroutine _Parent;

        public Coroutine(CoroutineMgr pCoroutineMgr, IEnumerator pCoroutineFunc = null)
        {
            _pCoroutineMgr = pCoroutineMgr;
            _state = CoroutineState.Stopped;
            _AutoKill = true;
        }

        public void Start(IEnumerator pCoroutineFunc)
        {
            Debugger.Assert(!this._bPooled, "Pooled coroutine cannot start manually with Start()");
            _Start(pCoroutineFunc);
        }

        public override YieldInstructionType GetInstructionType()
        {
            return YieldInstructionType.Coroutine;
        }

        public void Pause()
        {
            Pause(scalable ? _pCoroutineMgr._Time : _pCoroutineMgr._RealTime);
        }

        public void Resume()
        {
            Resume(scalable ? _pCoroutineMgr._Time : _pCoroutineMgr._RealTime);
        }

        override public void Pause(float fTime)
        {
            Debugger.Assert(GetRef() != 0);
            Debugger.ConditionalAssert(_bPooled
                                 , !_AutoKill, "You can call pause/resume/stop a pooled coroutine only when AutoKill is false.");
            Debugger.Assert(_state == CoroutineState.Running);

            _state = CoroutineState.Paused;
            _pCurrentInstruction.Pause(fTime);
        }

        override public void Resume(float fTime)
        {
            Debugger.Assert(GetRef() != 0);
            Debugger.ConditionalAssert(_bPooled
                                  , !_AutoKill, "You can call pause/resume/stop a pooled coroutine only when AutoKill is false.");
            Debugger.Assert(_state == CoroutineState.Paused);

            _state = CoroutineState.Running;
            _pCurrentInstruction.Resume(fTime);
        }

        override public void Stop()
        {
            Debugger.Assert(GetRef() != 0);
            Debugger.ConditionalAssert(_bPooled
                                  , !_AutoKill, "You can call pause/resume/stop a pooled coroutine only when AutoKill is false.");

            if (_state == CoroutineState.Stopped) return;

            __bIsManualStopped = true;
            _pCoroutineFunc = null;

            if(_pCurrentInstruction != null)
            {
                _pCurrentInstruction.Stop();
                _pCurrentInstruction.DecRef();
                _pCurrentInstruction = null;
            }

            _doStop();
        }

        override public void IncRef()
        {
            base.IncRef();

            if (_bPooled)
            {
                if (GetRef() > 1)
                {
                    _AutoKill = false;
                }
            }
        }

        virtual public void SetAutoKill(bool bAutoKill)
        {
            if(!IsAlive())
            {
                Debugger.LogWarning("SetAutoKill failed, coroutine is not alive! Current state: " + _state);
                return;
            }

            Debugger.Assert(GetRef() != 0);
            Debugger.Assert(_bInQueue);

            if (_AutoKill == bAutoKill) return;

            _AutoKill = bAutoKill;
            if (!_AutoKill) IncRef();
            else DecRef();
        }

        /// Will Stop it first and then free it back to the pool.
        virtual public void StopAndKill()
        {
            Debugger.Assert(_bPooled);
            Debugger.Assert(!_AutoKill);

            Stop();

            _AutoKill = true;
            DecRef();
        }

        ///----
        protected override void _onRelease()
        {
            if (_bPooled)
            {
                Reset();
                _state = CoroutineState.Freed;
                CoroutineMgr._Instance._FreeCoroutine(this);
            }
            else
            {
                Dispose();
            }
        }

        internal void _Start(IEnumerator pCoroutineFunc)
        {
            Debugger.Assert(GetRef() != 0);

            _Parent = _pCoroutineMgr.Current();
            if (_Parent != null) _Parent._DoChildCoStart(this);

            Debugger.Assert(_state != CoroutineState.Disposed);
            if (_state == CoroutineState.Running || _state == CoroutineState.Paused) Stop();

            try
            {
                _pCoroutineMgr._CoEnterLogStack.Push(this);

                if (!_bInQueue) CoroutineMgr._Instance._AddToQueue(this);

                _pCoroutineFunc = pCoroutineFunc;

                if (_onStart != null) _onStart(this);

                _state = CoroutineState.Running;

                if (_pCoroutineFunc.MoveNext())
                {
                    /// 如果子Coroutine的第一行代码抛出异常，_state会被设置为Stopped
                    if(_state != CoroutineState.Stopped)
                    {
                        Debugger.Assert(_state != CoroutineState.Disposed && _state != CoroutineState.Freed);

                        _pCurrentInstruction = _pCoroutineFunc.Current as IYieldInstruction;

                        if (_pCurrentInstruction == null) throw new UnsupportedYieldInstruction();

                        _pCurrentInstruction.Start(scalable ? _pCoroutineMgr._Time : _pCoroutineMgr._RealTime);
                        _pCurrentInstruction.IncRef();
                    }
                }
                else
                {
                    /// When coroutine has the 'yield break' at first line, will reach here.
                    /// The onStop will be called at next _Update.
                    _state = CoroutineState.Stopped;
                }
            }
            catch(Exception e)
            {
                if (e is CoroutineException) _HandleException(e as CoroutineException);
                else _HandleException(new CoroutineException(e, this));
            }
            finally
            {
                Debugger.Assert(_pCoroutineMgr._CoEnterLogStack.Peek() == this);
                _pCoroutineMgr._CoEnterLogStack.Pop();
            }
        }

        internal void _DoChildCoStart(Coroutine pChild)
        {
            IncRef();
            if (_OnChildCoStart != null) _OnChildCoStart(pChild);
        }

        internal void _DoChildCoStop(Coroutine pChild)
        {
            if (_OnChildCoStop != null) _OnChildCoStop(pChild);
            DecRef();
        }

        /// Return value indicate whether this coroutine is dead.
        internal bool _Update(float fTime)
        {
            bool bIsDead = false;

            try
            {
                if (_onUpdate != null && state == CoroutineState.Running) _onUpdate(_pCurrentInstruction);
            }
            catch (Exception e)
            {
                _HandleException(new CoroutineException(e, this));
                bIsDead = true;
            }

            bool bIsComplete = false;

            if (_state < CoroutineState.Running)
            {
                if (_state == CoroutineState.Paused) return false;
                else if (_state == CoroutineState.Stopped)
                {
                    bIsComplete = true;
                }
                else if (_state == CoroutineState.Disposed)
                {
                    /// Occurred when a manual coroutine is Removed.
                    return true;
                }
                else Debugger.Assert(false);
            }

            try
            {
                _pCoroutineMgr._CoEnterLogStack.Push(this);

                if(bIsComplete)
                {
                    if (!__bIsManualStopped) __doComplete();
                    return true;
                }

                ///-------------------
                bIsDead = false;

                Debugger.Assert(_pCurrentInstruction != null);

                Debugger.DebugSection(() =>
                {
                    if(_pCurrentInstruction.GetInstructionType() == YieldInstructionType.Coroutine)
                    {
                        var coroutine = _pCurrentInstruction as Coroutine;
                        Debugger.ConditionalAssert(coroutine._bPooled, coroutine.state != CoroutineState.Freed);
                    }
                });

                if (_pCurrentInstruction.GetInstructionType() == YieldInstructionType.ReturnValue)
                {                    
                    bIsDead = true;

                    _ReturnValue pReturnValue = _pCurrentInstruction as _ReturnValue;

                    __doComplete(pReturnValue.returnValue);
                }
                else
                {
                    _pCurrentInstruction.Update(fTime);

                    if (_pCurrentInstruction.IsDone())
                    {
                        if (_onYieldDone != null) _onYieldDone(_pCurrentInstruction);

                        if (state != CoroutineState.Stopped)
                        {
                            Debugger.Assert(_pCurrentInstruction != null);
                            _pCurrentInstruction.Stop();

                            if (!_pCoroutineFunc.MoveNext())
                            {
                                _pCurrentInstruction.DecRef();
                                _pCurrentInstruction = null;

                                bIsDead = true;
                                __doComplete();
                            }
                            else
                            {
                                if(_state != CoroutineState.Stopped)
                                {
                                    Debugger.Assert(_state != CoroutineState.Freed && _state != CoroutineState.Disposed);
                                    Debugger.Assert(_pCoroutineFunc != null, "Coroutine function is null but still in update, you may be operated on a dead coroutine!!!");

                                    /// Why defRef here? Because MoveNext() may cause current instruction
                                    _pCurrentInstruction.DecRef();

                                    _pCurrentInstruction = _pCoroutineFunc.Current as IYieldInstruction;
                                    if (_pCurrentInstruction == null) throw new UnsupportedYieldInstruction();

                                    _pCurrentInstruction.Start(fTime);
                                    _pCurrentInstruction.IncRef();
                                }
                            }
                        }
                    }
                             
                }
            }
            catch (Exception e)
            {
                Debugger.Assert(!(e is CoroutineException));
                _HandleException(new CoroutineException(e, this));
                bIsDead = true;
            }
            finally
            {
                Debugger.Assert(_pCoroutineMgr._CoEnterLogStack.Peek() == this);
                _pCoroutineMgr._CoEnterLogStack.Pop();
            }

            return bIsDead;
        }

        public void Reset()
        {
            Debugger.Assert(state == CoroutineState.Stopped);
            Debugger.Assert(_pCurrentInstruction == null);

            _AutoKill = true;

            _onUpdate = null;
            _onComplete = null;
            _onStopped = null;
            _onStart = null;
            _onException = null;
            _onYieldDone = null;

            if(_Parent != null)
            {
                _Parent.DecRef();
                _Parent = null;
            }            

            __bIsManualStopped = false;
        }

        public List<CoroutineInfo> GetCallingStack()
        {
            List<CoroutineInfo> stack = new List<CoroutineInfo>();

            stack.Add(new CoroutineInfo(name, state, scalable));

            Coroutine parent = _Parent;

            while(parent != null)
            {
                stack.Add(new CoroutineInfo(parent.name, parent.state, parent.scalable));
                parent = parent._Parent;
            }

            return stack;
        }

        public void Dispose()
        {
            Debugger.Assert(_state == CoroutineState.Stopped, "Must stop a coroutine before dispose it!!!");
            Debugger.Assert(!_bPooled, "Pooled coroutine cannot be manual disposed!!!");
            Debugger.Assert(GetRef() == 0, "Cannot dispose a coroutine if it still referenced by other Objects (such as another Coroutine) !");

            Reset();
            _pCoroutineFunc = null;
            _state = CoroutineState.Disposed;
        }

        /// Check if an object is disposed.
        public bool IsDisposed()
        {
            return _state == CoroutineState.Disposed;
        }

        override public bool IsDone()
        {
            return _state == CoroutineState.Stopped;
        }

        public bool IsRunning()
        {
            return _state == CoroutineState.Running;
        }

        public bool IsAlive()
        {
            return _state == CoroutineState.Running || _state == CoroutineState.Paused || _state == CoroutineState.InUse;
        }

        override public void Update(float fDeltaTime) { }

        public override string ToString() { return this.name; }

        ///--------
        private void __doComplete(object returnValue = null)
        {
            _pCoroutineFunc = null;

            if (_pCurrentInstruction != null)
            {
                _pCurrentInstruction.DecRef();
                _pCurrentInstruction = null;
            }

            if (_onComplete != null) _onComplete(this, returnValue);

            _doStop();
        }
        
        protected void _doStop()
        {
            if (_onStopped != null) _onStopped(this);

            _state = CoroutineState.Stopped;
            if (_Parent != null)
            {
                _Parent._DoChildCoStop(this);
                _Parent = null;
            } 
        }

        internal void _HandleException(CoroutineException e)
        {
            if(_pCurrentInstruction != null)
            {
                _pCurrentInstruction.Stop();
                _pCurrentInstruction.DecRef();
                _pCurrentInstruction = null;
            }

            _pCoroutineFunc = null;
            _state = CoroutineState.Stopped;

            if (_onException != null)
            {
                _onException(this, e);
            }
            else
            {
                if(_Parent != null)
                {
                    /// 'Throw up' to parent
                    _Parent._HandleException(e);
                }
                else
                {
                    CoroutineMgr._ThrowException(e);
                }
            }

            if (_onStopped != null)
            {
                _onStopped(this);
                _Parent = null;
            }
        }
    }

}


