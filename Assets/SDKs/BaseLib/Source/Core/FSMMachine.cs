using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using BaseLib;

/// A finite stack based state machine
public class FSMMachine
{
    public delegate void FSMEventHandler(int destState);

    public FSMState GetCurrentState() { return _stateStack.Peek(); }

    /// Before new state be pushed(before entry be called), will trigger these events.
    private FSMEventHandler OnPushState;
    private FSMEventHandler OnPopState;
    private FSMEventHandler OnBackState;

    private FSMState[] _arrStates;
    private Stack<FSMState> _stateStack;
    private int _defaultState;

    public FSMMachine(string[] strNames
                                , int defaultState = 0
                                , FSMEventHandler pushStateHandler = null
                                , FSMEventHandler popStateHandler = null
                                , FSMEventHandler backStateHandler = null)
    {
        _defaultState = defaultState;

        OnPushState = pushStateHandler;
        OnPopState = popStateHandler;
        OnBackState = backStateHandler;

        _stateStack = new Stack<FSMState>();
        _arrStates = new FSMState[strNames.Length];

        int i = 0;
        foreach (string name in strNames)
        {
            System.Type type  = Assembly.GetCallingAssembly().GetType(name);
            ConstructorInfo constructorInfo = type.GetConstructor(new System.Type[] {});

            _arrStates[i] = (FSMState)constructorInfo.Invoke(new object[] { });
            i++;
        }

        /// Init state machine (state stack).
        _stateStack = new Stack<FSMState>();
        /// Treat 0 as default state
        FSMState state = _arrStates[_defaultState];
        _stateStack.Push(state);
        state.Entry(null);
    }

    public void DispatchEvent(StateEvent stateEvent)
    {
        _stateStack.Peek().OnEvent(stateEvent);
    }

    public void Reset()
    {
        _stateStack.Clear();

        FSMState pDefaultState = _arrStates[_defaultState];
        _stateStack.Push(pDefaultState);
        pDefaultState.Entry(null);
    }

    /// Clear all states except the top one.
    public void ClearStackedStates()
    {
        Debugger.Assert(_stateStack.Count >= 1);
        if (_stateStack.Count == 1) return;

        /// Keep a reference to the top state.
        FSMState pTopState = _stateStack.Pop();

        /// Clear stack, then push the top state back.
        _stateStack.Clear();
        _stateStack.Push(pTopState);
    }

    /// Make destState current state
    public void PushState(int destState, object beginParams)
    {
        FSMState lastState = _stateStack.Peek();
        ///Debugger.Log("Push state from " + lastState + " to " + _arrStates[(int)destState]);

        lastState.Exit();
        FSMState curState = _arrStates[(int)destState];
        _stateStack.Push(curState);

        if (OnPushState != null) OnPushState(destState);

        curState.Entry(beginParams);
    }

    /// Pop(end) current state, then begin the last state. 
    /// Return value: the last state.
    public FSMState PopState(object beginParams)
    {
        FSMState popped = _stateStack.Pop();
        popped.Exit();

        FSMState curState = _stateStack.Peek();

        if (OnPopState != null) OnPopState(curState.stateType);

        curState.Entry(beginParams);

        ///Debugger.Log("Pop state from " + popped + " to " + curState);
        return curState;
    }

    /// <summary>
    public void BackToState(int destState, object beginParams)
    {
        /// End current state
        FSMState popped = _stateStack.Pop();
        popped.Exit();

        bool bFind = false;
        /// Seek the suitable state
        while (_stateStack.Count != 0)
        {
            FSMState curState = _stateStack.Peek();

            if (curState.stateType == (int)destState)
            {
                if (OnBackState != null) OnBackState(destState);

                curState.Entry(beginParams);
                bFind = true;
                ///Debugger.Log("Pop state from " + popped + " to " + curState);
                break;
            }
            else
            {
                _stateStack.Pop();
            }
        }

        Debugger.Assert(bFind, "DoBackToState failed, no state of type " + destState + "in the stack!");
    }

    public int GetCurrentStateType() { return _stateStack.Peek().stateType; }
}
