using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

using BaseLib;

public class Timer : Tickable
{	
	public float interval;
    public int loopCount { get { return _nLoopCount; } set { _nLoopCount = value; } }
    public float startDelayTime { get { return _fStartDelayTime; } set { _fStartDelayTime = value; } }

    public Timer LoopCount(int nLoopCount) { _nLoopCount = nLoopCount; return this; }
    public Timer TriggerOnStart(bool triggerOnStart) { _bTriggerOnStart = triggerOnStart; return this; }
    public Timer StartDelayTime(float fStartDealyTime) { _fStartDelayTime = fStartDealyTime; return this; }
    public Timer Interval(float fInterval) { interval = fInterval; return this; }
    public Timer Handler(Action<object> pHandler) { _pHandlerFunc = pHandler; return this; }
    public Timer UserData(object userData) { _pUserData = userData; return this; }
    public Timer Scaleable(bool bValue) { _Scalable = bValue; return this; }

	public object _pUserData;
	
	private Action<object> _pHandlerFunc;

    /// If scaleable is true _fElapsedOrStartUpTime represent elapsed time, or else start up time.
    private float _fStartTime;
    private float _fStartDelayTime;
    private int _nCurLoopCount;
	private int _nLoopCount;

    private bool _bTriggerOnStart;
    /// When a timer has "start delay", this value will be false until "start delay time" elapsed.
    private bool _bStartDelayEnd; 

	public Timer() 
	{
		_pHandlerFunc = null;		
		_pUserData = null;
		
		_fStartTime = 0;
		loopCount = 1;

        _bStartDelayEnd = false;
	}

	public void SetParams(int nCount, float nInterval
											, Action<object> pHandlerFunc
											, object pUserData = null
                                            , float startDelayTime = 0
                                            , bool triggerOnStart = false)
	{
		loopCount = nCount;
		interval = nInterval;
		_pHandlerFunc = pHandlerFunc;
		_pUserData = pUserData;

        _fStartDelayTime = startDelayTime;
        _bTriggerOnStart = triggerOnStart;
	}

    override public void Pause()
    {
        if (_status != TickerStatus.TICKING) return;
        _status = TickerStatus.PUASED;
    }

    override public void Resume()
    {
        if (_status != TickerStatus.PUASED) return;
        _status = TickerStatus.TICKING;
    }

    override public void Stop()
    {
        _Stop();
    }

    public void Restart(bool bTriggerAtStart = false)
    {
        _status = TickerStatus.STOPPED;
        _bTriggerOnStart = bTriggerAtStart;
        Start();
    }
    
    public void ConfigDelay(float fTime, Action<object> pHandler = null, object userData = null)
    {
        _pHandlerFunc = pHandler;
        _pUserData = userData;

        loopCount = 1;

        _bTriggerOnStart = true;
        _fStartDelayTime = fTime;
    }

    public void Delay(float fTime)
    {
        loopCount = 1;

        _bTriggerOnStart = true;
        _fStartDelayTime = fTime;

        Start();
    }

    public void Delay(float fTime, object userData)
    {
        _pUserData = userData;
        Delay(fTime);
    }

    public void Delay(float fTime, Action<object> pHandler, object userData = null)
    {
        _pHandlerFunc = pHandler;
        _pUserData = userData;
        Delay(fTime);
    }

    public void Repeat(int nLoopCount, float fInterval, Action<object> pHandler)
    {
        _pHandlerFunc = pHandler;
        Start(nLoopCount, fInterval, false);
    }

    public void Start(int nLoopCount, float fInterval, bool bTriggerOnStart = false)
    {
        this._nLoopCount = nLoopCount;
        this.interval = fInterval;

        _bTriggerOnStart = bTriggerOnStart;
        
        Start();
    }

	public void Start()
	{
        Debugger.Assert(_nLoopCount >= 1 || _nLoopCount == -1, "Loop Count must >= 1 or  equal -1");

        if (_bTriggerOnStart)
        {
            if(_fStartDelayTime == 0)
            {
                if (_nLoopCount > 1 || _nLoopCount == -1)
                {
                    _Start();
                    
                    /// Since already trigger at start, so reduce _nCurLoopCount by one.
                    _nCurLoopCount--;

                    TimerMgr._Instance._PlayTickable(this);

                    _bStartDelayEnd = true;
                    _pHandlerFunc(_pUserData);
                }
                else // _nLoopCount == 1
                {
                    Debugger.Assert(_nLoopCount == 1);
                    _nCurLoopCount--;

                    /// Since _Stop() will clean user data, we store it into a temp var first.
                    object tempUserData = _pUserData;
					var tempHandler = _pHandlerFunc;
                    _Stop();

                    _bStartDelayEnd = true;
                    tempHandler(tempUserData);
                }

            }
            else
            {
                _bStartDelayEnd = false;
                _Start();
                TimerMgr._Instance._PlayTickable(this);
            }
        }
        else
        {
            if (_fStartDelayTime == 0) _bStartDelayEnd = true;
            else _bStartDelayEnd = false;

            _Start();
            TimerMgr._Instance._PlayTickable(this);
        }
		
	}

    /// Get elapsed time since startup or last timer event.
    public float GetElapsedTime()
    {
        return TimerMgr._Instance._RealTime - _fStartTime;
    }

	//----------------------
    override public void _Tick(float fTime)
	{
        Debugger.Assert(_status == TickerStatus.TICKING);

        float fElapsedTime = fTime - _fStartTime;
        Debugger.Assert(fElapsedTime >= 0);

        if(_bStartDelayEnd)
        {
            if (fElapsedTime >= interval)
            {
                if (_nCurLoopCount > 0 || _nCurLoopCount < 0)
                {
                    if(_nCurLoopCount > 0) _nCurLoopCount--;

                    _fStartTime = fTime - (fElapsedTime - interval);

                    _pHandlerFunc(_pUserData);
                }
                else if (_nCurLoopCount == 0)
                {
                    /// Since _Stop() will clean user data, we store it into a temp var first.
                    var tempUserData = _pUserData;
                    var tempHandler = _pHandlerFunc;
                    _Stop();
                    _fStartTime = 0;
                    tempHandler(tempUserData);
                }
                else Debugger.Assert(false);
            }
        }
        else /// _bStartDelayEnd == false
        {
            if (fElapsedTime >= _fStartDelayTime)
            {
                _bStartDelayEnd = true;
                _fStartTime = TimerMgr._Instance._RealTime - (fElapsedTime - _fStartDelayTime);

                if(_bTriggerOnStart)
                {
                    Debugger.Assert(_nCurLoopCount >= 0);

                    if (_nCurLoopCount == 0)
                    {
                        /// Since _Stop() will clean user data, we store it into a temp var first.
                        var tempUserData = _pUserData;
                        var tempHandler = _pHandlerFunc;
                        _Stop();
                        tempHandler(tempUserData);
                    }
                    else
                    {
                        _nCurLoopCount--;
                        _pHandlerFunc(_pUserData);
                    }
                }
            }
        }
	}		

	public void _Start()
	{
		_status = TickerStatus.TICKING;	
	
		_nCurLoopCount = _nLoopCount;
        _nCurLoopCount--;

		_IsDead = false;

        _fStartTime = _Scalable?  TimerMgr._Instance._Time : TimerMgr._Instance._RealTime;

        this.IncRef();
	}	

	public void _Stop()
	{
        if (_status == TickerStatus.STOPPED) return;

		_status = TickerStatus.STOPPED;
		_IsDead = true;

        if (_isType(TickableType.Pooled))
        {
            _pUserData = null;
        }

        this.DecRef();
	}

    override protected void _onRelease()
    {
        // Release referenced resources.
        _pHandlerFunc = null;

        if(_isType(TickableType.Pooled))
        {
            Debugger.Assert(_IsDead == true && status == TickerStatus.STOPPED);
            TimerMgr._Instance._FreeTimer(this);
        }
    }
}





