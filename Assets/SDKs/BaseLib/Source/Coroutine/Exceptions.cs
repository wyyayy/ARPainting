using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace BaseLib
{
    public class UnsupportedYieldInstruction : Exception
    {
        public UnsupportedYieldInstruction()
            : base("Cannot 'yield return' null or any other objects that doesn't implements IYieldInstruction!")
        {
        }
    }

    public class CoroutineException : Exception
    {
        public string coroutineStackTrace
        {
            get
            {
                StringBuilder strBuilder = new StringBuilder();
                foreach (var co in _coroutineStack)
                {
                    strBuilder.Append("\t");
                    strBuilder.Append(co.ToString());
                    strBuilder.Append("\r\n");
                }
                return strBuilder.ToString();
            }
        }

        public List<CoroutineInfo> coroutineStack { get { return _coroutineStack; } }
        protected List<CoroutineInfo> _coroutineStack;
        public Exception originalException { get { return _originalException; } internal set { _originalException = value; } }
        protected Exception _originalException;

        public override string StackTrace { get { return _originalException.StackTrace; } }

        public CoroutineException(Exception e, Coroutine co)
        {
            _coroutineStack = co.GetCallingStack();
            _originalException = e;
        }

        public override string Message { get { return ToString(); } }

        public override string ToString()
        {
            return _originalException.ToString();
        }
    } 

}



