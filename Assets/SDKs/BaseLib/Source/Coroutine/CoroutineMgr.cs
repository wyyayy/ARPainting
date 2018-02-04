using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using BaseLib;

#pragma warning disable 0219

namespace BaseLib
{
    /// Manager all coroutines.
    public class CoroutineMgr
    {
        static public event Action<CoroutineException> OnUnhandledException;
        static internal void _ThrowException(CoroutineException exception) 
        {
            if (OnUnhandledException != null) OnUnhandledException(exception);
            else throw exception;
        }

        public int runningCoroutineCount { get { return _arrCoroutines.Count; } }
        public static Coroutine currentCoroutine { get { return _Instance._CoEnterLogStack.Peek(); } } 

        internal static CoroutineMgr _Instance;

        private QuickList<Coroutine> _arrCoroutines;
        private QuickList<Coroutine> __arrAddedCoroutines;

        internal ObjectPool<_WaitTime> _waitTimePool;
        internal ObjectPool<_WaitFrameEnd> _waitFramePool;
        internal ObjectPool<_ReturnValue> _returnValuePool;

        internal ObjectPool<_WaitConditionFunc> _waitConditionPool;
        internal ObjectPool<_WaitSignal> _waitSignalPool;

        internal ObjectPool<Coroutine> _coroutinePool;

        internal float _Time;
        internal float _RealTime;

        internal bool _InUpdating;

        /// With this stack, we can get current coroutine object in the coroutine function with CurrentCoroutine().
        internal Stack<Coroutine> _CoEnterLogStack;

        /// Use to naming coroutine.
        static public int _CoroutineIndex;

        public static string GetCallingStackTrace()
        {
            var stack = GetCallingStack();

            StringBuilder strBuilder = new StringBuilder();
            foreach (var co in stack)
            {
                strBuilder.Append("\t");
                strBuilder.Append(co.ToString());
                strBuilder.Append("\r\n");
            }
            return strBuilder.ToString();
        }

        public static List<CoroutineInfo> GetCallingStack()
        {
            return currentCoroutine.GetCallingStack();
        }

        public Coroutine Current()
        {
            if (_Instance._CoEnterLogStack.Count == 0) return null;
            else return _Instance._CoEnterLogStack.Peek();
        }

        /// For Unit test only
        public static int _GetAllPoolUsedCount()
        {
            int count = _Instance._waitTimePool.GetUsedCount()
                    + _Instance._waitFramePool.GetUsedCount()
                    + _Instance._returnValuePool.GetUsedCount()
                    + _Instance._waitConditionPool.GetUsedCount()
                    + _Instance._waitSignalPool.GetUsedCount()
                    + _Instance._coroutinePool.GetUsedCount();

            var temp = _Instance._waitTimePool.GetUsedCount();
            temp = _Instance._waitFramePool.GetUsedCount();
            temp = _Instance._returnValuePool.GetUsedCount();
            temp = _Instance._waitConditionPool.GetUsedCount();
            temp = _Instance._waitSignalPool.GetUsedCount();
            temp = _Instance._coroutinePool.GetUsedCount();

            return count;
        }

        protected CoroutineMgr()
        {
            Debugger.Assert(_Instance == null, "Only one instance can be created for CoroutineMgr!!!");
            _Instance = this;
            _CoEnterLogStack = new Stack<Coroutine>();
        }

        static public void Init()
        {
            if(_Instance == null)
            {
                _Instance = new CoroutineMgr();
                _Instance._init();
            }
            else Debugger.LogWarning("CoroutineMgr.Init() be called more than once!!!");
        }

        protected void _init()
        {
            _arrCoroutines = new QuickList<Coroutine>();
            __arrAddedCoroutines = new QuickList<Coroutine>();

            _waitTimePool = new ObjectPool<_WaitTime>(8, x => new _WaitTime());
            _waitFramePool = new ObjectPool<_WaitFrameEnd>(8, x => new _WaitFrameEnd());
            _waitConditionPool = new ObjectPool<_WaitConditionFunc>(8, x => new _WaitConditionFunc());
            _returnValuePool = new ObjectPool<_ReturnValue>(8, x => new _ReturnValue());
            _waitSignalPool = new ObjectPool<_WaitSignal>(8, x => new _WaitSignal());
           
            _coroutinePool = new ObjectPool<Coroutine>(128, x =>
            {
                Coroutine pNewCoroutine = new Coroutine(this, null);
                pNewCoroutine._bPooled = true;
                pNewCoroutine.state = CoroutineState.Freed;
                return pNewCoroutine;
            });
        }

        internal void _Update(float fTime, float fRealTime)
        {
            _InUpdating = true;

            _Time = fTime;
            _RealTime = fRealTime;

            if (__arrAddedCoroutines.Count != 0)
            {
                foreach (Coroutine addedCoroutine in __arrAddedCoroutines)
                {
                    _arrCoroutines.Add(addedCoroutine);
                }

                __arrAddedCoroutines.Clear();
            }

            ///-----
            int nAliveCount = _arrCoroutines.Count;
            Coroutine[] arrCoroutines = _arrCoroutines._Buffer;

            for (int i = 0; i < nAliveCount; ++i)
            {
                Coroutine pCoroutine = arrCoroutines[i];

                if (pCoroutine == null)
                {
                    int a = 0; a++;
                }

                Debugger.Assert(pCoroutine._bInQueue);

                bool bIsDead = pCoroutine._Update(pCoroutine.scalable ? fTime : fRealTime);

                if (bIsDead)
                {
                    if (i < nAliveCount - 1)
                    {
                        /// Swap with last element
                        _arrCoroutines[i] = _arrCoroutines[nAliveCount - 1];
                        i--;
                        nAliveCount--;
                    }
                    else if (i == nAliveCount - 1)
                    {
                        nAliveCount--;
                    }
                    else Debugger.Assert(false);

                    _arrCoroutines.RemoveTail();

                    if (pCoroutine._bPooled) pCoroutine.DecRef();
                    pCoroutine._bInQueue = false;
                }
            }

            _InUpdating = false;
        }

        ///----
        /// When use unity, pass Time.time as fTime and Time.realtimeSinceStartup as fRealTime.
        static public void Update(float fTime, float fRealTime)
        {
            _Instance._Update(fTime, fRealTime);
        }

        static public Coroutine StartCoroutine(IEnumerator coroutineFunc, string name = null)
        {
            Coroutine coroutine = null;
            if (name != null) coroutine = StartCoroutine(coroutineFunc, null, null, name);
            else coroutine = StartCoroutine(coroutineFunc, null, null, "Coroutine:" + _CoroutineIndex++);

            return coroutine;
        }

        static public Coroutine StartCoroutine(IEnumerator coroutineFunc, Action<Coroutine> onStart, string name = null)
        {
            Coroutine coroutine = null;
            if (name != null) coroutine = StartCoroutine(coroutineFunc, onStart, null, name);
            else coroutine = StartCoroutine(coroutineFunc, onStart, null, "Coroutine:" + _CoroutineIndex++);

            return coroutine;
        }

        static public Coroutine StartCoroutine(IEnumerator coroutineFunc, Action<Coroutine, CoroutineException> onException, string name = null)
        {
            Coroutine coroutine = null;
            if (name != null) coroutine = StartCoroutine(coroutineFunc, null, onException, name);
            else coroutine = StartCoroutine(coroutineFunc, null, onException, "Coroutine:" + _CoroutineIndex++);

            return coroutine;
        }

        static public Coroutine StartCoroutine(IEnumerator coroutineFunc
                                                    , System.Action<Coroutine> onStart
                                                    , Action<Coroutine, CoroutineException> onException
                                                    , string name = null)
        {
            Coroutine pCoroutine = _Instance._coroutinePool.Get();
            pCoroutine.IncRef();
            pCoroutine.state = CoroutineState.InUse;
            pCoroutine.name = name == null ? "Coroutine:" + _CoroutineIndex++ : name;
            pCoroutine._OnException(onException)._OnStart(onStart);
            pCoroutine._Start(coroutineFunc);

            return pCoroutine;
        }

        static public Coroutine AddCoroutine(string name = null, Action<Coroutine> onStart = null)
        {
            Coroutine pCoroutine = new Coroutine(_Instance, null);
            pCoroutine.name = name == null ? "Coroutine:" + _CoroutineIndex++ : name;
            pCoroutine._OnStart(onStart);
            pCoroutine.IncRef();
            return pCoroutine;
        }

        /// Second param mean force dispose the coroutine. If false, the coroutine will be disposed when its reference count
        /// decreased to 0 automatically.
        static public void RemoveCoroutine(Coroutine pCoroutine, bool bDispose = false)
        {
            Debugger.Assert(!pCoroutine._bPooled);
            pCoroutine.DecRef();

            if (bDispose) pCoroutine.Dispose();
        }
 
        /// ---------------
        internal void _FreeTimeWaiter(_WaitTime pWaiter)
        {
            Debugger.Assert(pWaiter.GetRef() == 0);
            _waitTimePool.Free(pWaiter);
        }

        internal void _FreeFrameWaiter(_WaitFrameEnd pWaiter)
        {
            Debugger.Assert(pWaiter.GetRef() == 0);
            _waitFramePool.Free(pWaiter);
        }

        internal void _FreeCoroutine(Coroutine pWaiter)
        {
            Debugger.Assert(pWaiter.GetRef() == 0);
            _coroutinePool.Free(pWaiter);
        }

        internal void _FreeConditionWaiter(_WaitConditionFunc pWaiter)
        {
            Debugger.Assert(pWaiter.GetRef() == 0);
            _waitConditionPool.Free(pWaiter);
        }

        internal void _FreeSignalWaiter(_WaitSignal pWaiter)
        {
            Debugger.Assert(pWaiter.GetRef() == 0);
            _waitSignalPool.Free(pWaiter);
        }

        internal void _FreeReturnValue(_ReturnValue pReturnValue)
        {
            _returnValuePool.Free(pReturnValue);
        }

        internal void _AddToQueue(Coroutine pCoroutine)
        {
            Debugger.DebugSection(() =>
            {
                if (pCoroutine._bPooled)
                {
                    Debugger.Assert(pCoroutine.state == CoroutineState.InUse);
                }
                else
                {
                    Debugger.Assert(pCoroutine.state == CoroutineState.Stopped);
                }
            });

            Debugger.Assert(!_IsCoroutineInQueue(pCoroutine));

            pCoroutine._bInQueue = true;

            if (_InUpdating) __arrAddedCoroutines.Add(pCoroutine);
            else _arrCoroutines.Add(pCoroutine);
        }

        internal bool _IsCoroutineInQueue(Coroutine pCoroutine)
        {
            var bInQueue = __arrAddedCoroutines.Contains(pCoroutine) || _arrCoroutines.Contains(pCoroutine);
            Debugger.Assert(pCoroutine._bInQueue == bInQueue);
            return bInQueue;
        }

    }

}


