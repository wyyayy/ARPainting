using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace BaseLib
{
#if DISABLE_POOLING
    public class ObjectPool<T> where T : class, IPoolable 
#else
    public class ObjectPool<T> where T : class
#endif
    {
        protected int MIN_ENLARGE_SIZE = 32;

        public delegate T CreateFunc(ObjectPool<T> pPool);
        public delegate void DestroyFunc(ObjectPool<T> pObj, T obj);

        /// User data binded to this pool. Can be used to identify this pool.
        public object userData;

        protected CreateFunc _pCreator;

        protected Deque<T> _objectQueue;

        protected int _nMaxSize;
        protected int _nPoolSize;

        public ObjectPool(int nInitSize, CreateFunc pCreator, int nMaxSize = int.MaxValue)
        {
#if DISABLE_POOLING
            _nMaxSize = int.MaxValue;
            _iCursor = -1;
            _nPoolSize = 0;
#else
            _objectQueue = new Deque<T>(nInitSize == 0? 1 : nInitSize);

            SetMaxSize(nMaxSize);

            _pCreator = pCreator;
            _nPoolSize = 0;

            for (int i = 0; i < nInitSize; ++i) Add(_pCreator(this));
#endif
        }

        protected ObjectPool(int nMaxSize = int.MaxValue)
        {
#if DISABLE_POOLING
            _nMaxSize = int.MaxValue;
            _iCursor = -1;
            _nPoolSize = 0;
#else
            SetMaxSize(nMaxSize);

            _pCreator = null;
            _objectQueue = new Deque<T>(1);

            _nPoolSize = 0;
#endif
        }

        /// Prepare some objects into pool. Return old pool size.
        public int Prepare(int nSize)
        {
#if DISABLE_POOLING
            return _nPoolSize;
#else
            Debugger.Assert((_nPoolSize + nSize) <= _nMaxSize);

            int nOldPoolSize = _nPoolSize;
            for (int i = 0; i < nSize; ++i) Add(_pCreator(this));

            Debugger.Assert(_nPoolSize == nOldPoolSize + nSize);

            return nOldPoolSize;
#endif
        }

        public int GetPoolSize()
        {
            return _nPoolSize;
        }

        public int GetMaxSize()
        {
            return _nMaxSize;
        }

        public void SetMaxSize(int nMaxSize)
        {
            Debugger.Assert(nMaxSize > 0 && nMaxSize <= int.MaxValue);
            _nMaxSize = nMaxSize;
        }

        public int GetFreeObjectCount()
        {
            return _objectQueue.Count;
        }

        public int GetUsedCount()
        {
            return _nPoolSize - _objectQueue.Count;
        }

        /// The method will get a free object from pool directly. It will not create new object if no free object exist. 
        public T FastGet()
        {
#if DISABLE_POOLING
            return _pCreator(this);
#else
            Debugger.Assert(GetFreeObjectCount() != 0, "FastGetInstance failed, no free object" + _nMaxSize);

            var pObject = _objectQueue.RemoveFromFront();
            /*
#if DEBUG
            _arrObjectSlots[_iCursor] = default(T);
#endif
            */
            Debugger.Assert(pObject != null);
            return pObject;
#endif
        }

        /// Will return null if reach max size.
        public T Get()
        {
#if DISABLE_POOLING
            return _pCreator(this);
#else
            Debugger.Assert(!(_nPoolSize == _nMaxSize && GetFreeObjectCount() == 0), "Exceed pool max size!!! Max pool size is " + _nMaxSize);

            T pObject;

            if (_objectQueue.Count == 0)
            {
                pObject = _pCreator(this);
                _nPoolSize++;
            }
            else
            {
                pObject = _objectQueue.RemoveFromFront();
                /*
                #if DEBUG
                                _arrObjectSlots[_iCursor] = default(T);
                #endif
                */
            }

            Debugger.Assert(_nPoolSize <= _nMaxSize);
            Debugger.Assert(pObject != null);
            return pObject;
#endif
        }

        public void Free(T[] arrInstances, int count)
        {
#if DISABLE_POOLING
            foreach(var instance in arrInstances)
            {
                instance.Destroy();
            }
#else
            Debugger.Assert(_nPoolSize + count <= _nMaxSize, "Object pool exceed max size! Do you free an object that is not managed by ObjectPool?");
            Debugger.Assert(arrInstances != null);

            Debugger.Assert(false, "Not implemented yet!");
            /// _objectQueue.AddToBack(arrInstances, count);

#endif
        }

        public void Free(T pInstance)
        {
#if DISABLE_POOLING
            pInstance.Destroy();
#else
            Debugger.Assert(_nPoolSize <= _nMaxSize, "Object pool exceed max size! Do you free an object that is not managed by ObjectPool?");
            Debugger.Assert(pInstance != null);

            Debugger.DebugSection(() =>
            {
                foreach (T element in this)
                {
                    Debugger.Assert(element != pInstance
                        , "Object already in pool, cannot FreeInstance it twice! Object is: " + element);
                }
            });

            _objectQueue.AddToBack(pInstance);
#endif
        }

        public void Add(T pObject)
        {
#if DISABLE_POOLING
            pObject.Destroy();
#endif
            Debugger.Assert(_nPoolSize + 1 <= _nMaxSize, "Object pool exceed max size!");

            _objectQueue.AddToBack(pObject);
            _nPoolSize++;
        }

        /// For 'foreach' functionality.
        /// Traverse all free objects in pool.
        [DebuggerHidden]
        [DebuggerStepThrough]
        public IEnumerator<T> GetEnumerator()
        {
#if DISABLE_POOLING
            Debugger.Assert(false, "GetEnumerator not allowed if DISABLE_POOLING");
#endif
            return _objectQueue.GetEnumerator();
        }

        public void Clear(DestroyFunc pDestroyer = null)
        {
#if !DISABLE_POOLING
            Debugger.Assert(GetPoolSize() == GetFreeObjectCount());

            if (pDestroyer != null)
            {
                foreach (var obj in _objectQueue)
                {
                    pDestroyer(this, obj);
                }
            }

            _objectQueue.Clear();
            _nPoolSize = 0;
#endif
        }

    }

}