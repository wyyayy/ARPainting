
namespace BaseLib
{
    /**
     * Reference counter 
     * @author Spark
     */
    public interface IRefCounter
    {
        void IncRef();
        void DecRef();

        int GetRef();
    }

    public abstract class RefCounter : IRefCounter
    {
        protected int _nRefCount = 0;

        public int GetRef() { return _nRefCount; }
        virtual public void IncRef() { _nRefCount++; }

        public void DecRef()
        {
            Debugger.Assert(_nRefCount > 0);
            _nRefCount--;

            if (_nRefCount == 0) _onRelease();
        }

        abstract protected void _onRelease();
    }
}

