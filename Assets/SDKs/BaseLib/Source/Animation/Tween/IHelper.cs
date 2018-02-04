using System;
using System.Collections;

using UnityEngine;

namespace BaseLib
{
    /// Help Tweener to do ease applying, releasing and so on.
    public interface IHelper
    {
        void Init(ITween tween);

        void Attach(IProperty property);
        void Detach();

        void ApplyEase(float fElapsedTime);

        void ApplyToValue();
        void ApplyFromValue();

        void DoLoop(LoopStyle loopStyle);

        void DoRelease();
    }

}