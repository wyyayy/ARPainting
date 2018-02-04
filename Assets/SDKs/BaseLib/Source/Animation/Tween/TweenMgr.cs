using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace BaseLib
{
    public class TweenMgr
    {
        static public int __UsedTweenerCount 
        {
            get 
            {
                return _floatTweeners.GetUsedCount()
                        + _vector2Tweeners.GetUsedCount()
                        + _vector3Tweeners.GetUsedCount()
                        + _colorTweeners.GetUsedCount();
            } 
        }

        public static void _FreeFloatTweener(Tweener<float, _FloatHelper> tweener) { _floatTweeners.Free(tweener); }
        static protected ObjectPool<Tweener<float, _FloatHelper>> _floatTweeners;
        public static void _FreeVector2Tweener(Tweener<Vector2, _V2Helper> tweener) { _vector2Tweeners.Free(tweener); }
        static protected ObjectPool<Tweener<Vector2, _V2Helper>> _vector2Tweeners;
        public static void _FreeVector3Tweener(Tweener<Vector3, _V3Helper> tweener) { _vector3Tweeners.Free(tweener); }
        static protected ObjectPool<Tweener<Vector3, _V3Helper>> _vector3Tweeners;
        public static void _FreeColorTweener(Tweener<Color, ColorHelper> tweener) { _colorTweeners.Free(tweener); }
        static protected ObjectPool<Tweener<Color, ColorHelper>> _colorTweeners;

        static public void Init()
        {
            _floatTweeners = new ObjectPool<Tweener<float, _FloatHelper>>(128, pool =>
            {
                return new Tweener<float, _FloatHelper>();
            }
            , 200000);

            _vector2Tweeners = new ObjectPool<Tweener<Vector2, _V2Helper>>(128, pool =>
            {
                return new Tweener<Vector2, _V2Helper>();
            }
            , 200000);

            _vector3Tweeners = new ObjectPool<Tweener<Vector3, _V3Helper>>(128, pool =>
            {
                return new Tweener<Vector3, _V3Helper>();
            }
            , 200000);

            _colorTweeners = new ObjectPool<Tweener<Color, ColorHelper>>(128, pool =>
            {
                return new Tweener<Color, ColorHelper>();
            }
            , 200000);
        }

        static public Tweener<float, _FloatHelper> To(IFloatProperty property, float to, float duration, bool scalable = true)
        {
            Tweener<float, _FloatHelper> tweener = _floatTweeners.Get();
            tweener.IncRef();

            var aniMgr = TimerMgr._Instance;
            tweener.SetParams(property, scalable ? aniMgr._Time : aniMgr._RealTime
                                        , duration, property.GetValue(), to - property.GetValue(), scalable);
            tweener.Start();

            return tweener;
        }

        static public Tweener<float, _FloatHelper> To(Func<float> getter, Action<float> setter, float to, float duration, bool scalable = true)
        {
            return To(new GeneralFloatProp(getter, setter), to, duration, scalable);
        }

        static public Tweener<Vector2, _V2Helper> To(IVector2Property property, Vector2 to, float duration, bool scalable = true)
        {
            Tweener<Vector2, _V2Helper> tweener = _vector2Tweeners.Get();
            tweener.IncRef();

            var aniMgr = TimerMgr._Instance;
            tweener.SetParams(property, scalable ? aniMgr._Time : aniMgr._RealTime
                                        , duration, property.GetValue(), to - property.GetValue(), scalable);
            tweener.Start();

            return tweener;
        }

        static public Tweener<Vector2, _V2Helper> To(Func<Vector2> getter, Action<Vector2> setter, Vector2 to, float duration, bool scalable = true)
        {
            return To(new GeneralVector2Prop(getter, setter), to, duration, scalable);
        }

        static public Tweener<Vector3, _V3Helper> To(IVector3Property property, Vector3 to, float duration, bool scalable = true)
        {
            Tweener<Vector3, _V3Helper> tweener = _vector3Tweeners.Get();
            tweener.IncRef();

            var aniMgr = TimerMgr._Instance;
            tweener.SetParams(property, scalable? aniMgr._Time : aniMgr._RealTime
                                        , duration, property.GetValue(), to - property.GetValue(), scalable);
            tweener.Start();

            return tweener;
        }

        static public Tweener<Vector3, _V3Helper> To(Func<Vector3> getter, Action<Vector3> setter, Vector3 to, float duration, bool scalable = true)
        {
            return To(new GeneralVector3Prop(getter, setter), to, duration, scalable);
        }

        static public Tweener<Color, ColorHelper> To(IColorProperty property, Color to, float duration, bool scalable = true)
        {
            Tweener<Color, ColorHelper> tweener = _colorTweeners.Get();
            tweener.IncRef();

            var aniMgr = TimerMgr._Instance;
            tweener.SetParams(property, scalable ? aniMgr._Time : aniMgr._RealTime
                                        , duration, property.GetValue(), to - property.GetValue(), scalable);
            tweener.Start();

            return tweener;
        }

        static public Tweener<Color, ColorHelper> To(Func<Color> getter, Action<Color> setter, Color to, float duration, bool scalable = true)
        {
            return To(new GeneralColorProp(getter, setter), to, duration, scalable);
        }

        static public EaseType GetInvEaseType(EaseType type)
        {
            if (type == EaseType.Linear) return EaseType.Linear;
            else if (type < EaseType.NormalEaseEnd)
            {
			    int modeRet = ((int)type % 3);
			    switch (modeRet)
			    {
				    case 0:
					    return type;
				    case 1:
					    return (EaseType)(type + 1);	
				    case 2:
                        return (EaseType)(type - 1);	
			    }
            }
            else
            {
                Debugger.Assert(false);
            }

            return EaseType.Invalid;
        }

    }
}
