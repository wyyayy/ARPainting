// Author: Spark Xie
// Created: 2016/8/22 11:45
// 
// =============================================================
// Contains Daniele Giardini's C# port of the easing equations created by Robert Penner
// (all easing equations except for Flash, InFlash, OutFlash, InOutFlash,
// which use some parts of Robert Penner's equations but were created by Daniele Giardini)
// http://robertpenner.com/easing, see license below:
// =============================================================
//
// TERMS OF USE - EASING EQUATIONS
//
// Open source under the BSD License.
//
// Copyright © 2001 Robert Penner
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification,
// are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// - Redistributions in binary form must reproduce the above copyright notice,
// this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// - Neither the name of the author nor the names of contributors may be used to endorse
// or promote products derived from this software without specific prior written permission.
// - THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
// IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
// STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
// EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using UnityEngine;

#pragma warning disable 1591
namespace BaseLib
{
    public static class Easer
    {
        const float _PiOver2 = Mathf.PI * 0.5f;
        const float _TwoPi = Mathf.PI * 2;

        /// Returns a value between 0 and 1 (inclusive) based on the elapsed time and EaseType selected
        public static float Evaluate(EaseType easeType, float time, float duration, float overshootOrAmplitude, float period)
        {
            switch (easeType) {
            case EaseType.Linear:
                return time / duration;
            case EaseType.InSine:
                return -(float)Math.Cos(time / duration * _PiOver2) + 1;
            case EaseType.OutSine:
                return (float)Math.Sin(time / duration * _PiOver2);
            case EaseType.InOutSine:
                return -0.5f * ((float)Math.Cos(Mathf.PI * time / duration) - 1);
            case EaseType.InQuad:
                return (time /= duration) * time;
            case EaseType.OutQuad:
                return -(time /= duration) * (time - 2);
            case EaseType.InOutQuad:
                if ((time /= duration * 0.5f) < 1) return 0.5f * time * time;
                return -0.5f * ((--time) * (time - 2) - 1);
            case EaseType.InCubic:
                return (time /= duration) * time * time;
            case EaseType.OutCubic:
                return ((time = time / duration - 1) * time * time + 1);
            case EaseType.InOutCubic:
                if ((time /= duration * 0.5f) < 1) return 0.5f * time * time * time;
                return 0.5f * ((time -= 2) * time * time + 2);
            case EaseType.InQuart:
                return (time /= duration) * time * time * time;
            case EaseType.OutQuart:
                return -((time = time / duration - 1) * time * time * time - 1);
            case EaseType.InOutQuart:
                if ((time /= duration * 0.5f) < 1) return 0.5f * time * time * time * time;
                return -0.5f * ((time -= 2) * time * time * time - 2);
            case EaseType.InQuint:
                return (time /= duration) * time * time * time * time;
            case EaseType.OutQuint:
                return ((time = time / duration - 1) * time * time * time * time + 1);
            case EaseType.InOutQuint:
                if ((time /= duration * 0.5f) < 1) return 0.5f * time * time * time * time * time;
                return 0.5f * ((time -= 2) * time * time * time * time + 2);
            case EaseType.InExpo:
                return (time == 0) ? 0 : (float)Math.Pow(2, 10 * (time / duration - 1));
            case EaseType.OutExpo:
                if (time == duration) return 1;
                return (-(float)Math.Pow(2, -10 * time / duration) + 1);
            case EaseType.InOutExpo:
                if (time == 0) return 0;
                if (time == duration) return 1;
                if ((time /= duration * 0.5f) < 1) return 0.5f * (float)Math.Pow(2, 10 * (time - 1));
                return 0.5f * (-(float)Math.Pow(2, -10 * --time) + 2);
            case EaseType.InCirc:
                return -((float)Math.Sqrt(1 - (time /= duration) * time) - 1);
            case EaseType.OutCirc:
                return (float)Math.Sqrt(1 - (time = time / duration - 1) * time);
            case EaseType.InOutCirc:
                if ((time /= duration * 0.5f) < 1) return -0.5f * ((float)Math.Sqrt(1 - time * time) - 1);
                return 0.5f * ((float)Math.Sqrt(1 - (time -= 2) * time) + 1);
            case EaseType.InElastic:
                float s0;
                if (time == 0) return 0;
                if ((time /= duration) == 1) return 1;
                if (period == 0) period = duration * 0.3f;
                if (overshootOrAmplitude < 1) {
                    overshootOrAmplitude = 1;
                    s0 = period / 4;
                } else s0 = period / _TwoPi * (float)Math.Asin(1 / overshootOrAmplitude);
                return -(overshootOrAmplitude * (float)Math.Pow(2, 10 * (time -= 1)) * (float)Math.Sin((time * duration - s0) * _TwoPi / period));
            case EaseType.OutElastic:
                float s1;
                if (time == 0) return 0;
                if ((time /= duration) == 1) return 1;
                if (period == 0) period = duration * 0.3f;
                if (overshootOrAmplitude < 1) {
                    overshootOrAmplitude = 1;
                    s1 = period / 4;
                } else s1 = period / _TwoPi * (float)Math.Asin(1 / overshootOrAmplitude);
                return (overshootOrAmplitude * (float)Math.Pow(2, -10 * time) * (float)Math.Sin((time * duration - s1) * _TwoPi / period) + 1);
            case EaseType.InOutElastic:
                float s;
                if (time == 0) return 0;
                if ((time /= duration * 0.5f) == 2) return 1;
                if (period == 0) period = duration * (0.3f * 1.5f);
                if (overshootOrAmplitude < 1) {
                    overshootOrAmplitude = 1;
                    s = period / 4;
                } else s = period / _TwoPi * (float)Math.Asin(1 / overshootOrAmplitude);
                if (time < 1) return -0.5f * (overshootOrAmplitude * (float)Math.Pow(2, 10 * (time -= 1)) * (float)Math.Sin((time * duration - s) * _TwoPi / period));
                return overshootOrAmplitude * (float)Math.Pow(2, -10 * (time -= 1)) * (float)Math.Sin((time * duration - s) * _TwoPi / period) * 0.5f + 1;
            case EaseType.InBack:
                return (time /= duration) * time * ((overshootOrAmplitude + 1) * time - overshootOrAmplitude);
            case EaseType.OutBack:
                return ((time = time / duration - 1) * time * ((overshootOrAmplitude + 1) * time + overshootOrAmplitude) + 1);
            case EaseType.InOutBack:
                if ((time /= duration * 0.5f) < 1) return 0.5f * (time * time * (((overshootOrAmplitude *= (1.525f)) + 1) * time - overshootOrAmplitude));
                return 0.5f * ((time -= 2) * time * (((overshootOrAmplitude *= (1.525f)) + 1) * time + overshootOrAmplitude) + 2);
            case EaseType.InBounce:
                return Bounce.EaseIn(time, duration, overshootOrAmplitude, period);
            case EaseType.OutBounce:
                return Bounce.EaseOut(time, duration, overshootOrAmplitude, period);
            case EaseType.InOutBounce:
                return Bounce.EaseInOut(time, duration, overshootOrAmplitude, period);

            default:
                Debugger.Assert(false);
                return -1;
            }
        }
    }
}