using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BaseLib
{
    public enum PropertyType
    {
        Color,
        Position,
        Scale,

    }

    public static class TweenExtensions
    {
        // static public Dictionary<object, >

        public static Tweener<Color, ColorHelper> DoAlpha(this Material target, float endValue, float duration, bool scalable = true)
        {
            Color destColor = target.color;
            destColor.a = endValue;

            return TweenMgr.To(() => target.color
                                         , x => { target.color = x; }
                                         , destColor, duration, scalable);
        }

        ///---
        public static Tweener<Color, ColorHelper> DoColor(this SpriteRenderer target, Color endValue, float duration, bool scalable = true)
        {
            return TweenMgr.To(() => target.color
                                         , x => { target.color = x; }
                                         , endValue, duration, scalable);
        }

        public static Tweener<Color, ColorHelper> DoAlpha(this SpriteRenderer target, float endValue, float duration, bool scalable = true)
        {
            Color destColor = target.color;
            destColor.a = endValue;

            return TweenMgr.To(() => target.color
                                         , x => { target.color = x; }
                                         , destColor, duration, scalable);
        }

        public static Tweener<Vector3, _V3Helper> DoScale(this SpriteRenderer target, Vector3 endValue, float duration, bool scalable = true)
        {
            return TweenMgr.To(() => target.transform.localScale, x => target.transform.localScale = x, endValue, duration, scalable);
        }

        public static Tweener<Vector3, _V3Helper> DoMove(this Transform target, Vector3 endValue, float duration, bool scalable = true)
        {
            return TweenMgr.To(() => target.transform.position, x => target.transform.position = x, endValue, duration, scalable);
        }

        ///-------------
        public static Tweener<Color, ColorHelper> DoAlpha(this Image target, float endValue, float duration, bool scalable = true)
        {
            Color destColor = target.color;
            destColor.a = endValue;

            return TweenMgr.To(() => target.color
                                         , x => { target.color = x; }
                                         , destColor, duration, scalable);
        }

        public static Tweener<Color, ColorHelper> DoAlpha(this Text target, float endValue, float duration, bool scalable = true)
        {
            Color destColor = target.color;
            destColor.a = endValue;

            return TweenMgr.To(() => target.color
                                         , x => { target.color = x; }
                                         , destColor, duration, scalable);
        }

        public static Tweener<Vector2, _V2Helper> DoSize(this Image target, Vector2 endValue, float duration, bool scalable = true)
        {
            return TweenMgr.To(() => target.rectTransform.sizeDelta, x => target.rectTransform.sizeDelta = x, endValue, duration, scalable);
        }

        public static Tweener<Vector3, _V3Helper> DoScale(this Image target, Vector3 endValue, float duration, bool scalable = true)
        {
            return TweenMgr.To(() => target.transform.localScale, x => target.transform.localScale = x, endValue, duration, scalable);
        }

        public static Tweener<Vector2, _V2Helper> DoAnchorPos(this Image target, Vector2 endValue, float duration, bool scalable = true)
        {
            return TweenMgr.To(() => target.rectTransform.anchoredPosition, x => target.rectTransform.anchoredPosition = x, endValue, duration, scalable);
        }

        public static Tweener<Vector2, _V2Helper> DoAnchorPos(this RectTransform target, Vector2 endValue, float duration, bool scalable = true)
        {
            return TweenMgr.To(() => target.anchoredPosition, x => target.anchoredPosition = x, endValue, duration, scalable);
        }

        public static Tweener<Color, ColorHelper> DoColor(this Image target, Color endValue, float duration, bool scalable = true)
        {
            return TweenMgr.To(() => target.color, x => target.color = x, endValue, duration, scalable); 
        }

        public static Tweener<Vector3, _V3Helper> DoScale(this Transform target, Vector3 endValue, float duration, bool scalable = true)
        {
            return TweenMgr.To(() => target.localScale, x => target.localScale = x, endValue, duration, scalable);
        }

    }
}