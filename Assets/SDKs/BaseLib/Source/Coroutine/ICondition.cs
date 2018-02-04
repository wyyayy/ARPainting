using System;
using System.Collections.Generic;

namespace BaseLib
{
    /// Implements this interface to indicate a condition.
    public interface ICondition
    {
        bool IsTrue();
    }

    public class AlwaysTrue : ICondition
    {
        public bool IsTrue() { return true; }
    }

    public class AlwaysFalse : ICondition
    {
        public bool IsTrue() { return false; }
    }

    public class Condition : ICondition
    {
        protected Func<bool> _pConditionFunc;

        public Condition(Func<bool> pConditionFunc)
        {
            _pConditionFunc = pConditionFunc;
        }

        public bool IsTrue()
        {
            return _pConditionFunc();
        } 
    }

    ///--------
    public class NotOperator : ICondition
    {
        protected ICondition _srcCondition;

        public NotOperator(ICondition src)
        {
            _srcCondition = src;
        }

        public bool IsTrue()
        {
            return !_srcCondition.IsTrue();
        }
    }

    public class OrOperator : ICondition
    {
        protected List<ICondition> _conditions;

        public OrOperator(ICondition a, ICondition b)
        {
            _conditions = new List<ICondition>();
            _conditions.Add(a);
            _conditions.Add(b);
        }

        public OrOperator(params ICondition[] conditions)
        {
            _conditions = new List<ICondition>();
            _conditions.AddRange(conditions);
        }

        public bool IsTrue()
        {
            foreach (var condition in _conditions)
            {
                if (condition.IsTrue()) return true;
            }

            return false;
        }
    }

    public class AndOperator : ICondition
    {
        protected List<ICondition> _conditions;

        public AndOperator(ICondition a, ICondition b)
        {
            _conditions = new List<ICondition>();
            _conditions.Add(a);
            _conditions.Add(b);
        }

        public AndOperator(params ICondition[] conditions)
        {
            _conditions = new List<ICondition>();
            _conditions.AddRange(conditions);
        }

        public bool IsTrue()
        {
            foreach(var condition in _conditions)
            {
                if (!condition.IsTrue()) return false;
            }

            return true;
        }
    }
}

