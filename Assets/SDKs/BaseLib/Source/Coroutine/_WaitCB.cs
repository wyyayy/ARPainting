using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

using BaseLib;

namespace BaseLib
{ 
    internal class _WaitCB_P1_R1<P1, TRet1> : YieldInstruction
    {
        protected Action _updateHandler;
        protected bool _bIsDone;

        protected P1 _param1;
        protected Action<TRet1> _callback;

        protected Action<P1, Action<TRet1>> _asyncOpWithCallback;

        public void Call()
        {
            _asyncOpWithCallback(_param1, ret =>
            {
                _bIsDone = true;
                if (_callback != null) _callback(ret);
            });
        }

        public void SetParams(Action<P1, Action<TRet1>> asyncOpWithCallback, P1 param1
                                           , Action updateHandler
                                         , Action<TRet1> callback)
        {
            _updateHandler = updateHandler;
            _callback = callback;
            _asyncOpWithCallback = asyncOpWithCallback;

            _param1 = param1;
        }

        override public bool IsDone() { return _bIsDone; }

        override public void Update(float fTime)
        {
            if (_updateHandler != null) _updateHandler();
        }

        protected override void _onRelease()
        {
            _asyncOpWithCallback = null;
            _updateHandler = null;
        }
    }

    internal class _WaitCB_P2_R1<P1, P2, TRet> : YieldInstruction
    {
        protected Action _updateHandler;
        protected bool _bIsDone;

        protected P1 _param1;
        protected P2 _param2;
        protected Action<TRet> _callback;

        protected Action<P1, P2, Action<TRet>> _asyncOpWithCallback;

        public void Call()
        {
            _asyncOpWithCallback(_param1, _param2, ret =>
            {
                _bIsDone = true;
                if (_callback != null) _callback(ret);
            });
        }

        public void SetParams(Action<P1, P2, Action<TRet>> asyncOpWithCallback
                                            , P1 param1, P2 param2
                                           , Action updateHandler
                                         , Action<TRet> callback)
        {
            _updateHandler = updateHandler;
            _callback = callback;
            _asyncOpWithCallback = asyncOpWithCallback;

            _param1 = param1;
            _param2 = param2;
        }

        override public bool IsDone() { return _bIsDone; }

        override public void Update(float fTime)
        {
            if (_updateHandler != null) _updateHandler();
        }

        protected override void _onRelease()
        {
            _asyncOpWithCallback = null;
            _updateHandler = null;
        }
    }
    ///---
    internal class _WaitCB_P3_R1<P1, P2, P3, TRet> : YieldInstruction
    {
        protected Action _updateHandler;
        protected bool _bIsDone;

        protected P1 _param1;
        protected P2 _param2;
        protected P3 _param3;
        protected Action<TRet> _callback;

        protected Action<P1, P2, P3, Action<TRet>> _asyncOpWithCallback;

        public void Call()
        {
            _asyncOpWithCallback(_param1, _param2, _param3, ret =>
            {
                _bIsDone = true;
                if (_callback != null) _callback(ret);
            });
        }

        public void SetParams(Action<P1, P2, P3, Action<TRet>> asyncOpWithCallback
                                            , P1 param1, P2 param2, P3 param3
                                           , Action updateHandler
                                         , Action<TRet> callback)
        {
            _updateHandler = updateHandler;
            _callback = callback;
            _asyncOpWithCallback = asyncOpWithCallback;

            _param1 = param1;
            _param2 = param2;
            _param3 = param3;
        }

        override public bool IsDone() { return _bIsDone; }

        override public void Update(float fTime)
        {
            if (_updateHandler != null) _updateHandler();
        }

        protected override void _onRelease()
        {
            _asyncOpWithCallback = null;
            _updateHandler = null;
        }
    }
    ///---
    internal class _WaitCB_P1_R2<P1, TRet1, TRet2> : YieldInstruction
    {
        protected Action _updateHandler;
        protected bool _bIsDone;

        protected P1 _param1;
        protected Action<TRet1, TRet2> _callback;

        protected Action<P1, Action<TRet1, TRet2>> _asyncOpWithCallback;

        public void Call()
        {
            _asyncOpWithCallback(_param1, (ret1, ret2) =>
            {
                _bIsDone = true;
                if (_callback != null) _callback(ret1, ret2);
            });
        }

        public void SetParams(Action<P1, Action<TRet1, TRet2>> asyncOpWithCallback, P1 param1
                                           , Action updateHandler
                                         , Action<TRet1, TRet2> callback)
        {
            _updateHandler = updateHandler;
            _callback = callback;
            _asyncOpWithCallback = asyncOpWithCallback;

            _param1 = param1;
        }

        override public bool IsDone() { return _bIsDone; }

        override public void Update(float fTime)
        {
            if (_updateHandler != null) _updateHandler();
        }

        protected override void _onRelease()
        {
            _asyncOpWithCallback = null;
            _updateHandler = null;
        }
    }

    internal class _WaitCB_P2_R2<P1, P2, TRet1, TRet2> : YieldInstruction
    {
        protected Action _updateHandler;
        protected bool _bIsDone;

        protected P1 _param1;
        protected P2 _param2;
        protected Action<TRet1, TRet2> _callback;

        protected Action<P1, P2, Action<TRet1, TRet2>> _asyncOpWithCallback;

        public void Call()
        {
            _asyncOpWithCallback(_param1, _param2, (ret1, ret2) =>
            {
                _bIsDone = true;
                if (_callback != null) _callback(ret1, ret2);
            });
        }

        public void SetParams(Action<P1, P2, Action<TRet1, TRet2>> asyncOpWithCallback, P1 param1, P2 param2
                                           , Action updateHandler
                                         , Action<TRet1, TRet2> callback)
        {
            _updateHandler = updateHandler;
            _callback = callback;
            _asyncOpWithCallback = asyncOpWithCallback;

            _param1 = param1;
            _param2 = param2;
        }

        override public bool IsDone() { return _bIsDone; }

        override public void Update(float fTime)
        {
            if (_updateHandler != null) _updateHandler();
        }

        protected override void _onRelease()
        {
            _asyncOpWithCallback = null;
            _updateHandler = null;
        }
    }
}


