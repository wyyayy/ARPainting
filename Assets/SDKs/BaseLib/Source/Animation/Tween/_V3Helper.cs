using System;
using System.Collections;

using UnityEngine;

namespace BaseLib
{
 
    public class _V3Helper : IHelper
    {
        protected IVector3Property _property;
        protected Tweener<Vector3, _V3Helper> _tweener;

        public void Init(ITween tween)
        {
            _tweener = tween as Tweener<Vector3, _V3Helper>;
        }

        public void Attach(IProperty property)
        {
            _property = property as IVector3Property;
        }

        public void Detach()
        {
            _property = null;
        }

        public void ApplyEase(float fElapsedTime)
        {
            Vector3 destValue = _tweener._FromValue
                                    + _tweener._DeltaValue * Easer.Evaluate(_tweener._EaseType, fElapsedTime
                                                                                                , _tweener._Duration, _tweener._Amplitude, 0);
            _property.SetValue(destValue);
        }

        public void ApplyToValue()
        {
            _property.SetValue(_tweener._FromValue + _tweener._DeltaValue);
        }

        public void ApplyFromValue()
        {
            _property.SetValue(_tweener._FromValue);
        }

        public void DoLoop(LoopStyle loopStyle)
        {
            if (loopStyle == LoopStyle.Yoyo)
            {
                _tweener._EaseType = TweenMgr.GetInvEaseType(_tweener._EaseType);
                _tweener._FromValue = _tweener._FromValue + _tweener._DeltaValue;
                _tweener._DeltaValue = -_tweener._DeltaValue;
            }
            else if (loopStyle == LoopStyle.Restart)
            {
                _property.SetValue(_tweener._FromValue);
            }
            else if (loopStyle == LoopStyle.Incremental)
            {
                _tweener._FromValue = _tweener._FromValue + _tweener._DeltaValue;
            }
        }

        public void DoRelease()
        {
            _tweener._Reset();
            TweenMgr._FreeVector3Tweener(_tweener);
        }
    }

}