using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

using BaseLib;

public enum TickerStatus
{
	 /// Animator is in playing.
	 TICKING = 0,	 
	/// Animator is paused.
	 PUASED,	 
	 /// Animator is stopped.
	 STOPPED,
}

public struct TickableType
{
    public static int Default = 0;

    public static int Pooled = 0x1 << 0;
}

abstract public class Tickable : RefCounter
{
    public string name { get { return _name; } }
    protected string _name;

    /// Current animator's status, see AnimatorStatus for details.
    public TickerStatus status { get {return _status; } }

    public void _MarkType(int type) { _Type |= type; }
	public int _Type;
	protected TickerStatus _status;

    //-------- Internal property --------------
    public bool _InQueue;

    /// Valid only when tickable is Pooled. 
    /// If _AutoKill is true, the tickable object will be freed to pool automatically when Stopped.
    /// Internal implementation is based on RefCounter. When _AutoKill is false, will call IncRef, and when user manually
    /// call Kill(), the DecRef will be called to free the object.
    public bool _AutoKill;
    public bool _IsDead;
    public bool _Scalable;

	public Tickable()
	{
		_IsDead = false;
		_InQueue = false;
		
		_status = TickerStatus.STOPPED;
		
		_Type = TickableType.Default;

        _Scalable = true;
        _AutoKill = true;
	}

    override public void IncRef() 
    {
        base.IncRef();

        if(_isType(TickableType.Pooled))
        {
            if(GetRef() > 1)
            {
                _AutoKill = false;
            }
        }
    }

    virtual public void SetAutoKill(bool bAutoKill)
    {
        Debugger.Assert(_status != TickerStatus.STOPPED);
        Debugger.Assert(_isType(TickableType.Pooled));

        if (_AutoKill == bAutoKill) return;

        _AutoKill = bAutoKill;
        if (!_AutoKill) IncRef();
        else DecRef();
    }

    /// Will Stop the tickable first and then free back it to pool.
    virtual public void StopAndKill()
    {
        Debugger.Assert(_isType(TickableType.Pooled));
        Debugger.Assert(!_AutoKill);

        Stop();

        _AutoKill = true;
        DecRef();
    }

	/**
	 * @param	nDeltaTime 
	 * @return	return true if this tickable object is dead, or else false.
	 */
    abstract public void _Tick(float fTime);

    abstract public void Pause();
    abstract public void Resume();
    abstract public void Stop();

	//----------------- Internal methods -----------------------------------		
	public void _SetStatus(TickerStatus newStatus)
	{
		_status = newStatus;
	}

    public bool _isType(int type)
    {
        return (_Type & type) != 0;
    }

}
