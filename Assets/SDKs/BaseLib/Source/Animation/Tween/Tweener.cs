using System;
using System.Collections;

using UnityEngine;

namespace BaseLib
{
    public enum LoopStyle
    {
        /// Each loop cycle restarts from the beginning
        Restart,
        /// The tween moves forward and backwards at alternate cycles
        Yoyo,
        /// Continuously increments the tween at the end of each loop cycle (A to B, B to B+(A-B), and so on), thus always moving "onward".       
        Incremental,
    }

    /// Used for IHelper to do type conversion.
    public interface ITween { }

    /// T is the property type (float, vector2, etc). 
    /// P is the property helper type.
    public class Tweener<PropType, Helper> : Tickable, IYieldInstruction, ITween where Helper : IHelper, new()
    {
        public Tweener<PropType, Helper> SetLoopStyle(LoopStyle newValue) { _LoopStyle = newValue; return this; }
        public Tweener<PropType, Helper> SetLoops(int nCount) { _LoopCount = nCount; return this; }
        /// Valid for EaseType: Back, Bounce, Elastic
        public Tweener<PropType, Helper> SetAmplitude(float fAmplitude) { _Amplitude = fAmplitude; return this; }
        public Tweener<PropType, Helper> OnComplete(Action<Tweener<PropType, Helper>, object> onComplete) { _onComplete = onComplete; return this; }
        protected Action<Tweener<PropType, Helper>, object> _onComplete;
        public Tweener<PropType, Helper> OnLoop(Action<Tweener<PropType, Helper>, object> onLoop) { _onLoop = onLoop; return this; }
        protected Action<Tweener<PropType, Helper>, object> _onLoop;
        public Tweener<PropType, Helper> SetEaseType(EaseType type) { _EaseType = type; _applyDefaultParams();  return this; }
        public Tweener<PropType, Helper> SetUserData(object data) { _userData = data; return this; }

        protected IProperty _property;
        protected Helper _helper;

        /// Tween data ---
        internal int _LoopCount;
        internal LoopStyle _LoopStyle;

        internal float _StartTime;
        internal float _Duration;
        internal float _Amplitude;
        internal float _PausedTime;
        internal EaseType _EaseType;

        internal PropType _FromValue;
        internal PropType _DeltaValue;

        public object userData { get { return _userData; } }
        internal object _userData;

        public Tweener(bool bIsPooled = true)
        {
            _LoopStyle = LoopStyle.Restart;

            _helper = new Helper();
            _helper.Init(this);

            if (bIsPooled) _MarkType(TickableType.Pooled);
        }

        public void SetParams(IProperty property, float fCurTime, float duration
                                       , PropType fromValue, PropType deltaValue, bool scalable)
        {
            Debugger.Assert(GetRef() != 0);

            _property = property;
            _helper.Attach(property);

            _Scalable = scalable;
            _PausedTime = 0;
            _StartTime = fCurTime;
            _Duration = duration;

            _FromValue = fromValue;
            _DeltaValue = deltaValue;

            _EaseType = EaseType.OutQuad;
        }

        public void Start()
        {
            Debugger.Assert(GetRef() != 0);

            _IsDead = false;
            _status = TickerStatus.TICKING;

            ///... Currently, use TickableMgr to tick tween. Consider using TweenMgr to tick tween later.
            TimerMgr._Instance._PlayTickable(this);
        }

        override public void Pause()
        {
            Debugger.Assert(GetRef() != 0);
            Debugger.ConditionalAssert(_isType(TickableType.Pooled)
                                 , !_AutoKill, "You can call pause/resume/stop with the tweener handle only when it is not AutoKill mode.");

            if (_status != TickerStatus.TICKING) return;
            _status = TickerStatus.PUASED;

            var aniMgr = TimerMgr._Instance;
            _PausedTime = _Scalable ? aniMgr._Time : aniMgr._RealTime;
        }

        override public void Resume()
        {
            Debugger.Assert(GetRef() != 0);
            Debugger.ConditionalAssert(_isType(TickableType.Pooled)
                                 , !_AutoKill, "You can call pause/resume/stop with the tweener handle only when it is not AutoKill mode.");

            if (_status != TickerStatus.PUASED) return;
            _status = TickerStatus.TICKING;

            var aniMgr = TimerMgr._Instance;
            _StartTime += ((_Scalable ? aniMgr._Time : aniMgr._RealTime) - _PausedTime);
            _PausedTime = 0;
        }

        override public void Stop()
        {
            Debugger.Assert(GetRef() != 0);
            Debugger.ConditionalAssert(_isType(TickableType.Pooled)
                                 , !_AutoKill, "You can call pause/resume/stop with the tweener handle only when it is not AutoKill mode.");

            _doStop();
        }

        /// A tween will never timeout
        public bool IsTimeout() { Debugger.Assert(GetRef() != 0); return false; }

        override public void _Tick(float fCurrentTime)
        {
            Debugger.Assert(GetRef() != 0);
            Debugger.Assert(this.status == TickerStatus.TICKING);

            float fElapsedTime = fCurrentTime - _StartTime;

            if(fElapsedTime < _Duration)
            {
                _helper.ApplyEase(fElapsedTime);
            }
            else
            {
                if (_LoopCount > 1 || _LoopCount == -1)
                {
                    if(_LoopCount != -1) _LoopCount--;
                    _StartTime = fCurrentTime;

                    _helper.DoLoop(_LoopStyle);

                    if (_onLoop != null) _onLoop(this, _userData);
                }
                else
                {
                    _helper.ApplyToValue();
                    if (_onComplete != null) _onComplete(this, _userData);
                    _doStop();
                }
            }
        }

        protected void _doStop()
        {
            _status = TickerStatus.STOPPED;
            _IsDead = true;

            if (_isType(TickableType.Pooled))
            {
                _userData = null;
            }
        }

        public void _Reset()
        {
            _name = null;

            _onComplete = null;
            _onLoop = null;

            _LoopCount = 0;
            _LoopStyle = LoopStyle.Restart;

            _StartTime = 0;
            _Duration = 0;
            _Amplitude = 1;
            _PausedTime = 0;
            _EaseType = EaseType.OutQuad;

            _userData = null;
            _property = null;
            _helper.Detach();

            _AutoKill = true;
        }

        override protected void _onRelease()
        {           
            if (_isType(TickableType.Pooled))
            {
                Debugger.Assert(_IsDead == true && status == TickerStatus.STOPPED);
                _helper.DoRelease();
            }
        }

        protected void _applyDefaultParams()
        {
            switch(_EaseType)
            {
                case EaseType.InOutBack:
                case EaseType.InBack:
                case EaseType.OutBack:
                    _Amplitude = 1.7f;
                    break;
            }
        }

        /// Implements IYieldInstruction
        public bool IsDone() { Debugger.Assert(GetRef() != 0); return _status == TickerStatus.STOPPED; }
        public void Update(float fTime) { }
        public void Pause(float fTime) { Debugger.Assert(GetRef() != 0); this.Pause(); }
        public void Resume(float fTime) { Debugger.Assert(GetRef() != 0); this.Resume(); }
        public YieldInstructionType GetInstructionType() { Debugger.Assert(GetRef() != 0); return YieldInstructionType.Custom; }
        public void Start(float fTime) { Debugger.Assert(GetRef() != 0); this.Start(); }
        /// void Stop();
    }

}
