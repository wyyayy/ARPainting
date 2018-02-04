using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

using BaseLib;

namespace BaseLib
{
    //public interface object { }

    public static class CoroutineExtension
    {
        static public Coroutine StartCoroutine(this object target, IEnumerator coroutineFunc, string name = null)
        {
            Coroutine coroutine = null;
            if (name != null) coroutine = StartCoroutine(target, coroutineFunc, null, null, name);
            else coroutine = StartCoroutine(target, coroutineFunc, null, null, "Coroutine:" + CoroutineMgr._CoroutineIndex++);

            return coroutine;
        }

        static public Coroutine StartCoroutine(this object target, IEnumerator coroutineFunc, Action<Coroutine> onStart, string name = null)
        {
            Coroutine coroutine = null;
            if (name != null) coroutine = StartCoroutine(target, coroutineFunc, onStart, null, name);
            else coroutine = StartCoroutine(target, coroutineFunc, onStart, null, "Coroutine:" + CoroutineMgr._CoroutineIndex++);

            return coroutine;
        }

        static public Coroutine StartCoroutine(this object target, IEnumerator coroutineFunc, Action<Coroutine, CoroutineException> onException, string name = null)
        {
            Coroutine coroutine = null;
            if (name != null) coroutine = StartCoroutine(target, coroutineFunc, null, onException, name);
            else coroutine = StartCoroutine(target, coroutineFunc, null, onException, "Coroutine:" + CoroutineMgr._CoroutineIndex++);

            return coroutine;
        }

        static public Coroutine StartCoroutine(this object target, IEnumerator coroutineFunc
                                                    , System.Action<Coroutine> onStart
                                                    , Action<Coroutine, CoroutineException> onException
                                                    , string name = null)
        {
            Coroutine pCoroutine = CoroutineMgr._Instance._coroutinePool.Get();
            pCoroutine.IncRef();
            pCoroutine.state = CoroutineState.InUse;
            pCoroutine.name = name == null ? "Coroutine:" + CoroutineMgr._CoroutineIndex++ : name;
            pCoroutine._OnException(onException)._OnStart(onStart);
            pCoroutine._Start(coroutineFunc);

            return pCoroutine;
        }

        static public Coroutine AddCoroutine(this object target, string name = null, Action<Coroutine> onStart = null)
        {
            Coroutine pCoroutine = new Coroutine(CoroutineMgr._Instance, null);
            pCoroutine.name = name == null ? "Coroutine:" + CoroutineMgr._CoroutineIndex++ : name;
            pCoroutine._OnStart(onStart);
            pCoroutine.IncRef();
            return pCoroutine;
        }

        /// Second param mean force dispose the coroutine. If false, the coroutine will be disposed when its reference count
        /// decreased to 0 automatically.
        static public void RemoveCoroutine(this object target, Coroutine pCoroutine, bool bDispose = false)
        {
            Debugger.Assert(!pCoroutine._bPooled);
            pCoroutine.DecRef();

            if (bDispose) pCoroutine.Dispose();
        }

        ///---------------------
        /// Garbage free
        static public IYieldInstruction Return(this object target, object returnValue = null)
        {
            _ReturnValue pInstruction = CoroutineMgr._Instance._returnValuePool.Get();
            pInstruction.returnValue = returnValue;

            return pInstruction;
        }

        /// Garbage free
        static public IYieldInstruction WaitTime(this object target, float fTime)
        {
            _WaitTime pWaiter = CoroutineMgr._Instance._waitTimePool.Get();
            pWaiter.SetParams(fTime);

            return pWaiter;
        }

        /// Garbage free
        static public IYieldInstruction WaitFrameEnd(this object target)
        {
            _WaitFrameEnd pWaiter = CoroutineMgr._Instance._waitFramePool.Get();

            return pWaiter;
        }

        /// Garbage free
        static public IYieldInstruction WaitUntil(this object target, Func<bool> pConditionFunc, float fTimeOut = float.PositiveInfinity)
        {
            _WaitConditionFunc pWaiter = CoroutineMgr._Instance._waitConditionPool.Get();

            pWaiter.SetParams(pConditionFunc, fTimeOut, null);
            return pWaiter;
        }

        /// Garbage free
        static public IYieldInstruction DoUpdateUntil(this object target, Action updateHandler, Func<bool> pConditionFunc, float fTimeOut = float.PositiveInfinity)
        {
            _WaitConditionFunc pWaiter = CoroutineMgr._Instance._waitConditionPool.Get();
            pWaiter.SetParams(pConditionFunc, fTimeOut, updateHandler);
            return pWaiter;
        }

        static public IYieldInstruction DoUpdateUntil(this object target, Action updateHandler, ICondition pConditionObject, float fTimeOut = float.PositiveInfinity)
        {
            _WaitConditionObj pWaiter = new _WaitConditionObj();
            pWaiter.SetParams(pConditionObject, fTimeOut, updateHandler);
            return pWaiter;
        }

        static public IYieldInstruction WaitUntil(this object target, ICondition pConditionObject, float fTimeOut = float.PositiveInfinity)
        {
            _WaitConditionObj pWaiter = new _WaitConditionObj();
            pWaiter.SetParams(pConditionObject, fTimeOut, null);
            return pWaiter;
        }

        /// Garbage free
        static public IYieldInstruction WaitUntil(this object target, Signal signal, System.Action updateHandler, float fTimeOut = float.PositiveInfinity)
        {
            _WaitSignal pWaiter = CoroutineMgr._Instance._waitSignalPool.Get();
            pWaiter.SetParams(signal, fTimeOut, updateHandler);
            return pWaiter;
        }

        /// Garbage free
        static public IYieldInstruction WaitUntil(this object target, Signal signal, float fTimeOut = float.PositiveInfinity)
        {
            _WaitSignal pWaiter = CoroutineMgr._Instance._waitSignalPool.Get();
            pWaiter.SetParams(signal, fTimeOut, null);
            return pWaiter;
        }

        static public IYieldInstruction DoUpdateUntil<T>(this object target, Action updateHandler, Signal<T> signal, float fTimeOut = float.PositiveInfinity)
        {
            _WaitSignal<T> pWaiter = new _WaitSignal<T>();
            pWaiter.SetParams(signal, fTimeOut, updateHandler);
            return pWaiter;
        }

        static public IYieldInstruction WaitUntil<T>(this object target, Signal<T> signal, float fTimeOut = float.PositiveInfinity)
        {
            _WaitSignal<T> pWaiter = new _WaitSignal<T>();
            pWaiter.SetParams(signal, fTimeOut, null);
            return pWaiter;
        }

        static public IYieldInstruction WaitUntilAll(this object target, params Func<bool>[] conditionFuncs)
        {
            _WaitMultiCondition pWaiter = new _WaitMultiCondition();
            pWaiter._WaitAll(_FuncsToObjs(conditionFuncs));
            return pWaiter;
        }

        static public IYieldInstruction WaitUntilAll(this object target, params ICondition[] conditions)
        {
            _WaitMultiCondition pWaiter = new _WaitMultiCondition();
            pWaiter._WaitAll(conditions);
            return pWaiter;
        }

        static public IYieldInstruction WaitUntilOneOf(this object target, params Func<bool>[] conditionFuncs)
        {
            _WaitMultiCondition pWaiter = new _WaitMultiCondition();
            pWaiter._WaitOneOf(_FuncsToObjs(conditionFuncs));
            return pWaiter;
        }

        static public IYieldInstruction WaitUntilOneOf(this object target, params ICondition[] conditions)
        {
            _WaitMultiCondition pWaiter = new _WaitMultiCondition();
            pWaiter._WaitOneOf(conditions);
            return pWaiter;
        }

        static public WaitMultiInstruction WaitUntilOneOf(this object target, params IYieldInstruction[] conditions)
        {
            WaitMultiInstruction pWaiter = new WaitMultiInstruction();
            pWaiter._WaitOneOf(conditions);
            return pWaiter;
        }

        static public WaitMultiInstruction WaitUntilAll(this object target, params IYieldInstruction[] instructions)
        {
            WaitMultiInstruction pWaiter = new WaitMultiInstruction();
            pWaiter._WaitAll(instructions);
            return pWaiter;
        }

        static public WaitMultiInstruction WaitUntilNumOf(this object target, int nNum, params IYieldInstruction[] instructions)
        {
            WaitMultiInstruction pWaiter = new WaitMultiInstruction();
            pWaiter._WaitNumOf(nNum, instructions);
            return pWaiter;
        }

        static public IYieldInstruction WaitUntilNumOf(this object target, int nNum, params Func<bool>[] conditionFuncs)
        {
            _WaitMultiCondition pWaiter = new _WaitMultiCondition();
            pWaiter._WaitNumOf(nNum, _FuncsToObjs(conditionFuncs));
            return pWaiter;
        }

        static public IYieldInstruction WaitUntilNumOf(this object target, int nNum, params ICondition[] conditions)
        {
            _WaitMultiCondition pWaiter = new _WaitMultiCondition();
            pWaiter._WaitNumOf(nNum, conditions);
            return pWaiter;
        }

        static private ICondition[] _FuncsToObjs(Func<bool>[] conditionFuncs)
        {
            ICondition[] conditions = new ICondition[conditionFuncs.Length];

            int i = 0;

            foreach (Func<bool> func in conditionFuncs)
            {
                Condition funcCondition = new Condition(func);
                conditions[i++] = funcCondition;
            }

            return conditions;
        }

        ///--- Wait method that has a callback ----
        static public IYieldInstruction WaitUntil<P1, TRet>(this object target, Action<P1, Action<TRet>> asyncOpByCallback, P1 param1
                                                                                    , Action<TRet> callback = null)
        {
            var waiter = new _WaitCB_P1_R1<P1, TRet>();
            waiter.SetParams(asyncOpByCallback, param1, null, callback);
            waiter.Call();
            return waiter;
        }

        static public IYieldInstruction WaitUntil<P1, P2, TRet>(this object target, Action<P1, P2, Action<TRet>> asyncOpByCallback, P1 param1, P2 param2
                                                                                    , Action<TRet> callback = null)
        {
            var waiter = new _WaitCB_P2_R1<P1, P2, TRet>();
            waiter.SetParams(asyncOpByCallback, param1, param2, null, callback);
            waiter.Call();
            return waiter;
        }

        static public IYieldInstruction WaitUntil<P1, P2, P3, TRet>(this object target, Action<P1, P2, P3, Action<TRet>> asyncOpByCallback
                                                                                    , P1 param1, P2 param2, P3 param3
                                                                                    , Action<TRet> callback = null)
        {
            var waiter = new _WaitCB_P3_R1<P1, P2, P3, TRet>();
            waiter.SetParams(asyncOpByCallback, param1, param2, param3, null, callback);
            waiter.Call();
            return waiter;
        }

        ///---
        static public IYieldInstruction WaitUntil<P1, TRet1, TRet2>(this object target, Action<P1, Action<TRet1, TRet2>> asyncOpByCallback
                                                                                    , P1 param1
                                                                                    , Action<TRet1, TRet2> callback = null)
        {
            var waiter = new _WaitCB_P1_R2<P1, TRet1, TRet2>();
            waiter.SetParams(asyncOpByCallback, param1, null, callback);
            waiter.Call();
            return waiter;
        }

        static public IYieldInstruction WaitUntil<P1, P2, TRet1, TRet2>(this object target, Action<P1, P2, Action<TRet1, TRet2>> asyncOpByCallback
                                                                                    , P1 param1, P2 param2
                                                                                    , Action<TRet1, TRet2> callback = null)
        {
            var waiter = new _WaitCB_P2_R2<P1, P2, TRet1, TRet2>();
            waiter.SetParams(asyncOpByCallback, param1, param2, null, callback);
            waiter.Call();
            return waiter;
        }

    }

}


