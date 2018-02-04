using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace BaseLib
{
#if DISABLE_POOLING
    public class FastPool<T> where T : class, IPoolable 
#else
    public class FastPool<T> where T : class
#endif
    {
        protected int MIN_ENLARGE_SIZE = 32;

        public delegate T CreateFunc(FastPool<T> pPool);
        public delegate void DestroyFunc(FastPool<T> pObj, T obj);

        /// User data binded to this pool. Can be used to identify this pool.
        public object userData;

        protected CreateFunc _pCreator;

        protected T[] _arrObjectSlots;

        protected int _iCursor;
        protected int _nMaxSize;
        protected int _nPoolSize;

        public FastPool(int nInitSize, CreateFunc pCreator, int nMaxSize = int.MaxValue)
        {
#if DISABLE_POOLING
            _nMaxSize = int.MaxValue;
            _iCursor = -1;
            _nPoolSize = 0;
#else
            SetMaxSize(nMaxSize);

            _pCreator = pCreator;
            _arrObjectSlots = new T[nInitSize];

            _iCursor = -1;
            _nPoolSize = 0;

            for (int i = 0; i < nInitSize; ++i) Add(_pCreator(this));
#endif
        }

        protected FastPool(int nMaxSize = int.MaxValue)
        {
#if DISABLE_POOLING
            _nMaxSize = int.MaxValue;
            _iCursor = -1;
            _nPoolSize = 0;
#else
            SetMaxSize(nMaxSize);

            _pCreator = null;
            _arrObjectSlots = new T[0];

            _iCursor = -1;
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
            return _iCursor + 1;
        }

        public int GetUsedCount()
        {
            return _nPoolSize - (_iCursor + 1);
        }

        /// The method will get a free object from pool directly. It will not create new object if no free object exist. 
        public T Get()
        {
#if DISABLE_POOLING
            return _pCreator(this);
#else
            Debugger.Assert(GetFreeObjectCount() != 0, "FastGetInstance failed, no free object" + _nMaxSize);

            var pObject = _arrObjectSlots[_iCursor];
#if DEBUG
            _arrObjectSlots[_iCursor] = default(T);
#endif
            _iCursor--;

            Debugger.Assert(pObject != null);
            return pObject;
#endif
        }

        /// Will return null if reach max size.
        public T GetOrCreate()
        {
#if DISABLE_POOLING
            return _pCreator(this);
#else
            Debugger.Assert(!(_nPoolSize == _nMaxSize && GetFreeObjectCount() == 0), "Exceed pool max size!!! Max pool size is " + _nMaxSize);

            T pObject;

            if (_iCursor == -1)
            {
                pObject = _pCreator(this);
                _nPoolSize++;
            }
            else
            {
                pObject = _arrObjectSlots[_iCursor];
#if DEBUG
                _arrObjectSlots[_iCursor] = default(T);
#endif
                _iCursor--;
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
            Debugger.Assert(_iCursor + count < _nMaxSize, "Object pool exceed max size! Do you free an object that is not managed by FastPool?");
            Debugger.Assert(arrInstances != null);

            if (_iCursor + count >= _arrObjectSlots.Length)
            {
                _incressSlots();
            }

            _iCursor++;
            Array.Copy(arrInstances, 0, _arrObjectSlots, _iCursor, count);
            _iCursor += count - 1;
#endif
        }

        public void Free(T pInstance)
        {
#if DISABLE_POOLING
            pInstance.Destroy();
#else
            Debugger.Assert(_iCursor + 1 < _nMaxSize, "Object pool exceed max size! Do you free an object that is not managed by FastPool?");
            Debugger.Assert(pInstance != null);

            Debugger.DebugSection(() => 
            {
                foreach(T element in this)
                {
                    Debugger.Assert(element != pInstance
                        , "Object already in pool, cannot FreeInstance it twice! Object is: " + element);
                }
            });

            if (_iCursor + 1 >= _arrObjectSlots.Length)
            {
                _incressSlots();
            }

            _iCursor++;
            _arrObjectSlots[_iCursor] = pInstance;
#endif
        }

        public void Add(T pObject)
        {
#if DISABLE_POOLING
            pObject.Destroy();
#endif
            Debugger.Assert(_iCursor + 1 < _nMaxSize, "Object pool exceed max size!");

            if (_iCursor + 1 >= _arrObjectSlots.Length)
            {
                _incressSlots();
            }

            _iCursor++;

            _arrObjectSlots[_iCursor] = pObject;
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
            if (_arrObjectSlots != null)
            {
                int nSize = GetFreeObjectCount();
                for (int i = 0; i < nSize; ++i)
                {
                    yield return _arrObjectSlots[i];
                }
            }
        }

        public void Clear(DestroyFunc pDestroyer = null)
        {
#if !DISABLE_POOLING
            Debugger.Assert(GetPoolSize() == GetFreeObjectCount());

            if (pDestroyer != null)
            {
                int nSize = GetFreeObjectCount();

                for (int i = 0; i < nSize; ++i)
                {
                    pDestroyer(this, _arrObjectSlots[i]);
                }
            }

            _arrObjectSlots = new T[0];

            _nPoolSize = 0;
            _iCursor = -1;            
#endif
        }

        ///--------
        protected void _incressSlots()
        {
            int nNewSlotCount = _arrObjectSlots.Length << 1;
            if (nNewSlotCount < MIN_ENLARGE_SIZE) nNewSlotCount = MIN_ENLARGE_SIZE;
            if (nNewSlotCount > _nMaxSize) nNewSlotCount = _nMaxSize;

            T[] arrOldSlots = _arrObjectSlots;

            _arrObjectSlots = new T[nNewSlotCount];

            if(_iCursor != -1)
            {
                Array.Copy(arrOldSlots, _arrObjectSlots, _iCursor + 1);
            }
        }
    }

}