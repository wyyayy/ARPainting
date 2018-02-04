
namespace BaseLib
{

    /// 
    internal class _WaitMultiCondition : YieldInstruction
    {
        internal ICondition[] _arrConditions = null;
        internal _WaitType _waitTaskType = _WaitType.All;

        protected _WaitType _type;
        protected int _number;
        protected bool _bIsDone = false;

        public void _WaitAll(params ICondition[] conditions)
        {
            _arrConditions = conditions;
            _type = _WaitType.All;
        }

        public void _WaitOneOf(params ICondition[] conditions)
        {
            _arrConditions = conditions;
            _type = _WaitType.OneOf;
        }

        public void _WaitNumOf(int nNum, params ICondition[] conditions)
        {
            _arrConditions = conditions;
            _type = _WaitType.NumOf;
            _number = nNum;
        }

        override public bool IsDone() 
        {
            bool bIsDone = false;

            if(_type == _WaitType.All)
            {
                bIsDone = true;

                foreach (ICondition condition in _arrConditions)
                {
                    if (!condition.IsTrue())
                    {
                        bIsDone = false;
                        break;
                    }
                }
            }
            else if(_type == _WaitType.OneOf)
            {
                foreach (ICondition condition in _arrConditions)
                {
                    if (condition.IsTrue())
                    {
                        bIsDone = true;
                        break;
                    }
                }
            }
            else if(_type == _WaitType.NumOf)
            {
                int nNum = 0;

                foreach (ICondition condition in _arrConditions)
                {
                    if (condition.IsTrue())
                    {
                        nNum++;

                        if(nNum == _number)
                        {
                            bIsDone = true;
                            break;
                        }
                    }
                }
            }

            return bIsDone;
        }
    }

}



