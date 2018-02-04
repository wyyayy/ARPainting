using UnityEngine;
using System.Collections;

public class StateEvent
{
    public object data;
}

public class FSMState 
{
    /// get\none
    public int stateType;

    public FSMState(int stateType)
    {
        this.stateType = stateType;
    }

    virtual public void Entry(object beginParam) { }
	virtual public void Exit(){}

    virtual public void OnEvent(StateEvent stateEvent) {}
}
