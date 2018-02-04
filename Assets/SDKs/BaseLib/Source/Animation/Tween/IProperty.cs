using System;
using System.Collections;

using UnityEngine;

namespace BaseLib
{
    public interface IProperty { }

    ///---
    public interface IFloatProperty : IProperty
    {
        float GetValue();
        void SetValue(float value);
    }

    public class GeneralFloatProp : IFloatProperty
    {
        protected Func<float> _getter;
        protected Action<float> _setter;

        public GeneralFloatProp(Func<float> getter, Action<float> setter)
        {
            _getter = getter;
            _setter = setter;
        }

        public float GetValue() { return _getter(); }
        public void SetValue(float value) { _setter(value); }
    }

    ///---
    public interface IVector2Property : IProperty
    {
        Vector2 GetValue();
        void SetValue(Vector2 value);
    }

    public class GeneralVector2Prop : IVector2Property
    {
        protected Func<Vector2> _getter;
        protected Action<Vector2> _setter;

        public GeneralVector2Prop(Func<Vector2> getter, Action<Vector2> setter)
        {
            _getter = getter;
            _setter = setter;
        }

        public Vector2 GetValue() { return _getter(); }
        public void SetValue(Vector2 value) { _setter(value); }
    }

    ///---
    public interface IVector3Property : IProperty
    {
        Vector3 GetValue();
        void SetValue(Vector3 value);
    }

    public class GeneralVector3Prop : IVector3Property
    {
        protected Func<Vector3> _getter;
        protected Action<Vector3> _setter;

        public GeneralVector3Prop(Func<Vector3> getter, Action<Vector3> setter)
        {
            _getter = getter;
            _setter = setter;
        }

        public Vector3 GetValue() { return _getter(); }
        public void SetValue(Vector3 value) { _setter(value); }
    }

    ///---
    public interface IColorProperty : IProperty
    {
        Color GetValue();
        void SetValue(Color value);
    }

    public class GeneralColorProp : IColorProperty
    {
        protected Func<Color> _getter;
        protected Action<Color> _setter;

        public GeneralColorProp(Func<Color> getter, Action<Color> setter)
        {
            _getter = getter;
            _setter = setter;
        }

        public Color GetValue() { return _getter(); }
        public void SetValue(Color value) { _setter(value); }
    }
}

