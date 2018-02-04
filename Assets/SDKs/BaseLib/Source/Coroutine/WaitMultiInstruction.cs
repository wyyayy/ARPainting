using System.Collections.Generic;


namespace BaseLib
{

    /// 
    public class WaitMultiInstruction : YieldInstruction
    {
        internal IYieldInstruction[] _arrInstructions = null;
        internal _WaitType _waitTaskType = _WaitType.All;

        protected _WaitType _type;
        protected int _number;
        protected bool _bIsDone = false;

        /// The first complete instruction when use _WaitType.OneOf
        protected IYieldInstruction _firstDoneInstruction;
        /// Store the complete instructions when use _WaitType.NumOf
        protected List<IYieldInstruction> _doneInstructions;

        public IYieldInstruction firstDoneInstruction { get { return _firstDoneInstruction; } }
        public List<IYieldInstruction> doneInstructions { get { return _doneInstructions; } }

        internal void _WaitAll(params IYieldInstruction[] conditions)
        {
            _arrInstructions = conditions;
            _type = _WaitType.All;
        }

        internal void _WaitOneOf(params IYieldInstruction[] conditions)
        {
            _arrInstructions = conditions;
            _type = _WaitType.OneOf;
        }

        internal void _WaitNumOf(int nNum, params IYieldInstruction[] conditions)
        {
            _doneInstructions = new List<IYieldInstruction>();
            _arrInstructions = conditions;
            _type = _WaitType.NumOf;
            _number = nNum;
        }

        override public void Start(float fTime)
        {
            foreach(var instruction in _arrInstructions)
            {
                instruction.Start(fTime);
                instruction.IncRef();
            }
        }

        override public void Update(float fTime) 
        {
            foreach (var instruction in _arrInstructions)
            {
                instruction.Update(fTime);
            }
        }

        override public bool IsDone() 
        {
            bool bIsDone = false;

            if(_type == _WaitType.All)
            {
                bIsDone = true;

                foreach (var instruction in _arrInstructions)
                {
                    if (!instruction.IsDone())
                    {
                        bIsDone = false;
                        break;
                    }
                }
            }
            else if(_type == _WaitType.OneOf)
            {
                foreach (var instruction in _arrInstructions)
                {
                    if (instruction.IsDone())
                    {
                        _firstDoneInstruction = instruction;
                        bIsDone = true;
                        break;
                    }
                }
            }
            else if(_type == _WaitType.NumOf)
            {
                if(_doneInstructions.Count != 0)
                {
                    return true;
                }
                else
                {
                    Debugger.Assert(_doneInstructions.Count <= _arrInstructions.Length);

                    int nNum = 0;
                    foreach (var instruction in _arrInstructions)
                    {
                        if (instruction.IsDone())
                        {
                            nNum++;

                            if (nNum == _number)
                            {
                                bIsDone = true;
                                break;
                            }
                        }
                    }

                    if (bIsDone)
                    {
                        foreach (var instruction in _arrInstructions)
                        {
                            if (instruction.IsDone())
                            {
                                _doneInstructions.Add(instruction);
                            }
                        }
                    }
                }

            }

            return bIsDone;
        }

        protected override void _onRelease()
        {
            foreach(var instruction in _arrInstructions)
            {
                instruction.Stop();
                instruction.DecRef();
            }

            _arrInstructions = null;
            _bIsDone = false;
        }
    }

}



