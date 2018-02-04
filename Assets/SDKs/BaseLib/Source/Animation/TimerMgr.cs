using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

using BaseLib;

public class TimerMgr : Singleton<TimerMgr>
{
    private const int DEFAULT_TIMER_POOL_SIZE = 10;

    public static Timer REPEAT(int nCount, float fInterval
                                                    , Action<object> pHandler, object pUserData = null
                                                    , float startDelayTime = 0, bool triggerOnStart = false) 
        { return _Instance.Repeat(nCount, fInterval, pHandler, pUserData, startDelayTime, triggerOnStart); }

    public static Timer DELAY(float fTime, Action<object> pHandler, object pUserData = null)
        { return _Instance.Delay(fTime, pHandler, pUserData); }

    public static Timer AddTimer() { return _Instance.AddTickable<Timer>(); }
    public static void RemoveTimer(Timer pTimer) { _Instance.RemoveTickable(pTimer); }

	/// In queue animators
	private QuickList<Tickable> _arrInQueueTickers;

    private int _nManualTickersCount;
	
	/// Timer is a pooled tickers (managed tickers).
	private ObjectPool<Timer> _timerPool;

    internal float _Time;
    internal float _RealTime;

    /// Note: include paused tickers
    public int __AliveTickableCount;
    public int __UsedTimerCount { get { return _timerPool.GetUsedCount(); } }

    /// Init TweenMgr
    public void Init(int nTimerPoolSize = DEFAULT_TIMER_POOL_SIZE)
    {
        Debugger.Assert(_timerPool == null, "Should never call AnimationMgr.Init more than once!");

        _Time = 0;
        _RealTime = 0;
        __AliveTickableCount = 0;

        // Init timer pool
        _timerPool = new ObjectPool<Timer>(nTimerPoolSize, pool => 
        {
            Timer pTimer = new Timer();
            pTimer._Type |= TickableType.Pooled;
            return pTimer;
        });

        _arrInQueueTickers = new QuickList<Tickable>();
    }

    public void Uninit()
    {
        _timerPool.Clear();
        _arrInQueueTickers.Clear();
    }

    /// Add a manual tickable object.
    /// A manual tickable object must be add/remove by AddTickable/RemoveTickable.
    public T AddTickable<T>() where T : Tickable, new()
    {
        T pTickable = new T();
        pTickable.IncRef();

        _nManualTickersCount++;

        return pTickable;
    }

    /// Remove a manual tickable object.
    /// A manual tickable object must be add/remove by AddTickable/RemoveTickable.
    public void RemoveTickable(Tickable pTickable) 
    {
        pTickable.Stop();
        pTickable.DecRef();
        _nManualTickersCount--;
        Debugger.Assert(_nManualTickersCount >= 0);
    }

	/**
	 * Repeat calling specified function with a constant interval.
	 * 
	 * @param	nCount Repeat count.
	 * @param	nInterval Interval between each call.
	 * @param	pHander The function to be called.
	 * @param	userData Use this param to pass some data to pHander.
	 * 
	 * @return Return the internal timer object if is infinite repeating, or else return null.
	 */
    public Timer Repeat(int nCount, float fInterval
													, Action<object> pHandler
													, object pUserData = null
                                                    , float startDelayTime = 0
                                                    , bool triggerOnStart = false)
	{
        if (nCount == 0) return null;

        Timer timer = _timerPool.Get();		
		timer.SetParams(nCount, fInterval, pHandler, pUserData, startDelayTime, triggerOnStart);
        timer.Start();
		
		return nCount == -1? timer : null;
	}

	/**
	 * Delay some time then doing something
	 * @param	nTime  	time to be delayed.
	 * @param	pHandler		The function to be called when time is up.
	 * @param	pUserData 	Use this param to pass some data to pHander.
	 */
	public Timer Delay(float fTime, Action<object> pHandler, object pUserData = null)
	{
        Timer timer = _timerPool.Get();
        timer.Delay(fTime, pHandler, pUserData);
        return timer;
	}

	public void _Update(float fTime, float fRealTime)
	{
        _Time = fTime;
        _RealTime = fRealTime;

        _updateTickables(fTime, fRealTime);
	}

    /// Internal publics
	public void _PlayTickable(Tickable tickable)
	{
		// push tween into list. 
		if (!tickable._InQueue)
		{
            Debugger.Assert(!_arrInQueueTickers.Contains(tickable));

            /// if (__bInUpdateing) __bNewAddedWhenUpdate = true;

            tickable._InQueue = true;
			_arrInQueueTickers.Add(tickable);
		}

        Debugger.ConditionalAssert(tickable._InQueue, _arrInQueueTickers.Contains(tickable));
	}	

    ///------------ Implementation ------------------
    private void _updateTickables(float fTime, float fRealTime)
	{
		int iLastDeadIndex = -1;
        int nCount = _arrInQueueTickers.Count;

        Tickable[] arrTickers = _arrInQueueTickers._Buffer;
        __AliveTickableCount = 0;

		for (int i=0; i<nCount; ++i)
		{
            Tickable pTicker = arrTickers[i];

			if (pTicker._IsDead) 
			{
				iLastDeadIndex = i;
				continue;
			}

            __AliveTickableCount++;

			if (pTicker.status == TickerStatus.PUASED)  continue;

            pTicker._Tick( pTicker._Scalable ? fTime : fRealTime);
		}
		
		if (iLastDeadIndex != -1)
		{
			Tickable lastDeadTicker = _arrInQueueTickers[iLastDeadIndex];	

			lastDeadTicker._InQueue = false;

            if (iLastDeadIndex != _arrInQueueTickers.Count - 1)
            {
                _arrInQueueTickers[iLastDeadIndex] = _arrInQueueTickers.Pop();
            }
            else _arrInQueueTickers.Pop();
		}
	}	

    internal void _FreeTimer(Timer timer)
    {
        _timerPool.Free(timer);
    }

}



