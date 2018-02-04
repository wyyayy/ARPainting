using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

using BaseLib;

public class DEventDispatcher<TypeClass> : EventDispatcher<TypeClass, EventData<TypeClass>> where TypeClass : struct { }
public class SEventDispatcher : EventDispatcher<int, EventData<int>> { }

public class EventData<TypeClass> where TypeClass : struct
{
	public TypeClass type;
	
    public EventData() { }

	public EventData(TypeClass type)
	{
		this.type =type;
	}
}

public class EventDispatcher<TypeClass, EventClass> where EventClass : EventData<TypeClass>, new()  
                                                                            where TypeClass : struct
{
    private class _Handlers
    {
        protected Action<EventClass> handlers;

        private int _count;

        public void Dispatch(EventClass evt)
        {
            if (handlers != null) handlers(evt);
        }

        public void Add(Action<EventClass> handler)
        {
            handlers += handler;
            _count++;
        }

        public void Remove(Action<EventClass> handler)
        {
            handlers -= handler;
            _count--;
        }

        public int GetListenerCount()
        {
            return _count;
        }

        public bool Contains(Action<EventClass> pListener)
        {
            if (handlers != null)
            {
                foreach (Delegate existingHandler in handlers.GetInvocationList())
                {
                    if (existingHandler == (Delegate)pListener) return true;
                }
            }
            return false;
        }
    }

    private Dictionary<TypeClass, _Handlers> _eventMap;

	public EventDispatcher()
	{
        _eventMap = new Dictionary<TypeClass, _Handlers>();
	}
	
	public void AddEventListener(TypeClass type,  Action<EventClass> pListener)
	{
        _Handlers pListenerList = _getListenerList(type);		
		Debugger.Assert(!pListenerList.Contains(pListener), "Event " + type.ToString() + " be listened by same function more than once!");

        pListenerList.Add(pListener);
	}

    public bool HasEventListener(TypeClass type, Action<EventClass> pListener)
    {
        var list = _getListenerList(type, false);
        if(list == null) return false;
        else return list.Contains(pListener);
    }

    public bool HasEventListener(TypeClass type)
    {
        var list = _getListenerList(type, false);
        if (list == null) return false;
        else return list.GetListenerCount() != 0;
    }	

	public bool RemoveEventListener(TypeClass type, Action<EventClass> pListener)
	{
        _Handlers pListenerList = null;

        try
        {
            pListenerList = _eventMap[type];
        }
        catch (Exception) { }

        if (pListenerList != null) 
        {
            if(pListenerList.Contains(pListener))
            {
                pListenerList.Remove(pListener);
                return true;
            }
            else return false;
        }
        else return false;
	}
	
    /// Return the handler's count
	public int DispatchEvent(EventClass evt)
	{     
		_Handlers handlers = _getListenerList(evt.type);
        handlers.Dispatch(evt);
        return handlers.GetListenerCount();
	}

    public void DispatchEvent(TypeClass type)
    {
        EventClass evt = new EventClass();
        evt.type = type;
        DispatchEvent(evt);
    }

    private _Handlers _getListenerList(TypeClass type, bool createIfNotFound = true)
	{
        _Handlers pListeners = null;

        if(_eventMap.ContainsKey(type))
        {
            pListeners = _eventMap[type];
        }
        else if (createIfNotFound)
        {
            pListeners = new _Handlers();
            _eventMap[type] = pListeners;
        }

		return pListeners;
	}	

    public void RemoveAll()
    {
        _eventMap.Clear();
    }

}






