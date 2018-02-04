using System;

namespace BaseLib
{
    /// Why wrap Action into Signal? Because an Action (delegate) is immutable, you cannot wait an action directly.
    public class Signal
    {
        public object data { get; protected set; }
        protected event Action _listener;

        public void Connect(Action listener)
        {
            Debugger.Assert(!Contains(listener));
            this._listener += listener;
        }

        public void Disconnect(Action listener)
        {
            Debugger.Assert(Contains(listener));
            this._listener -= listener;
        }

        public bool Contains(Action pListener)
        {
            if (_listener != null)
            {
                foreach (Delegate existingHandler in _listener.GetInvocationList())
                {
                    if (existingHandler == (Delegate)pListener) return true;
                }
            }
            return false;
        }

        public void Emit(int code)
        {
            this.data = code;
            if (this._listener != null) this._listener();
        }

        public void Emit()
        {
            data = 1;
            if (this._listener != null) this._listener();
        }

        public bool IsConnected()
        {
            return this._listener != null;
        }

        public void Reset()
        {
            data = 0;
        }
    }

    public class Signal<T>
    {
        public T data { get; protected set; }
        protected event Action<T> _listener;

        public void Connect(Action<T> listener)
        {
            Debugger.Assert(!Contains(listener));
            this._listener += listener;
        }

        public void Disconnect(Action<T> listener)
        {
            Debugger.Assert(Contains(listener));
            this._listener -= listener;
        }

        public bool Contains(Action<T> pListener)
        {
            if (_listener != null)
            {
                foreach (Delegate existingHandler in _listener.GetInvocationList())
                {
                    if (existingHandler == (Delegate)pListener) return true;
                }
            }
            return false;
        }

        public void Emit(T code)
        {
            this.data = code;
            if (_listener != null) _listener(code);
        }

        public bool IsConnected()
        {
            return _listener != null;
        }

        public void Reset()
        {
            data = default(T);
        }
    }
}

