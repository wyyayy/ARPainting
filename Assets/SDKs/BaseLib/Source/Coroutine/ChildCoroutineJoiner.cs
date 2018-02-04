using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BaseLib
{
    public class ChildCoroutineJoiner : YieldInstruction
    {
        public int childCoroutineCount { get { return _childCoroutines.Count; } }

        //protected float _fTimeout;
        protected List<Coroutine> _childCoroutines;
        protected int _nRunningChildCoCount;
        protected Coroutine _pCoroutine;

        ~ChildCoroutineJoiner()
        {
            Debugger.DebugSection(() => 
            {
                if (_childCoroutines != null)
                {
                    Debugger.Assert(_childCoroutines.Count == 0);
                }

                if(_pCoroutine != null)
                {
                    Debugger.Assert(!_pCoroutine._HasChildCoStartListener(_onChildCoStart));
                    Debugger.Assert(!_pCoroutine._HasChildCoStopListener(_onChildCoStop));
                }
            });
        }

        public ChildCoroutineJoiner(Coroutine pCoroutine)
        {
            _pCoroutine = pCoroutine;
            _pCoroutine.IncRef();

            _childCoroutines = new List<Coroutine>();

            _pCoroutine._OnChildCoStart += _onChildCoStart;
            _pCoroutine._OnChildCoStop += _onChildCoStop;
        }

        protected void _onChildCoStart(Coroutine co)
        {
            _childCoroutines.Add(co);
            co.IncRef();
            _nRunningChildCoCount++;
        }

        protected void _onChildCoStop(Coroutine co)
        {
            _nRunningChildCoCount--;
            Debugger.Assert(_nRunningChildCoCount >= 0);
        }

        override public bool IsDone()
        {
            return _nRunningChildCoCount == 0;
        }

        override public void Stop()
        {
            _nRunningChildCoCount = 0;
        }

        override public void Start(float fTime)
        {
            Debugger.Assert(_childCoroutines.Count != 0, "Child coroutine count is 0, please make sure ChildCoroutineJoiner be instantiated before all child coroutine started!!!");
        }

        override protected void _onRelease()
        {
            foreach (var co in _childCoroutines)
            {
                co.DecRef();
            }
            _childCoroutines.Clear();
            _pCoroutine.DecRef();

            _pCoroutine._OnChildCoStart -= _onChildCoStart;
            _pCoroutine._OnChildCoStop -= _onChildCoStop;
        }

        public IYieldInstruction Join()
        {
            return this;
        }
    }
}
