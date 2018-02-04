using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BaseLib
{
    
    /// A double-ended queue (deque), which provides O(1) indexed access, O(1) removals from the front and back, amortized O(1) insertions to the front and back, and O(N) insertions and removals anywhere else (with the operations getting slower as the index approaches the middle).    
    /// The type of elements contained in the deque.
    [DebuggerDisplay("Count = {Count}, Capacity = {Capacity}")]
    [DebuggerTypeProxy(typeof(Deque<>.DebugView))]
    public sealed class Deque<T> : IList<T>, System.Collections.IList
    {        
        /// The default capacity.       
        private const int DefaultCapacity = 8;
        
        /// The circular buffer that holds the view.        
        private T[] buffer;
        
        /// The offset into "buffer" where the view begins.        
        private int offset;
        
        /// Initializes a new instance of the Deque class with the specified capacity.        
        /// The initial capacity. Must be greater than 0.
        public Deque(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException("capacity", "Capacity must be greater than 0.");
            buffer = new T[capacity];
        }

        /// Initializes a new instance of the Deque class with the elements from the specified collection.        
        /// The collection.
        public Deque(IEnumerable<T> collection)
        {
            int count = collection.Count();
            if (count > 0)
            {
                buffer = new T[count];
                _doInsertRange(0, collection, count);
            }
            else
            {
                buffer = new T[DefaultCapacity];
            }
        }
        
        /// Initializes a new instance of the Deque class.        
        public Deque()
            : this(DefaultCapacity)
        {
        }

        #region GenericListImplementations
        /// Gets a value indicating whether this list is read-only. This implementation always returns false.
        /// true if this list is read-only; otherwise, false.
        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }
        
        /// Gets or sets the item at the specified index.        
        /// index The index of the item to get or set.
        /// T:System.ArgumentOutOfRangeException index is not a valid index in this list.
        /// T:System.NotSupportedException This property is set and the list is read-only.
        public T this[int index]
        {
            get
            {
                CheckExistingIndexArgument(_count, index);
                return DoGetItem(index);
            }

            set
            {
                CheckExistingIndexArgument(_count, index);
                DoSetItem(index, value);
            }
        }
        
        /// Inserts an item to this list at the specified index.        
        /// index The zero-based index at which item should be inserted.
        /// item The object to insert into this list.
        /// T:System.ArgumentOutOfRangeException 
        /// index is not a valid index in this list.
        /// 
        /// T:System.NotSupportedException 
        /// This list is read-only.
        /// 
        public void Insert(int index, T item)
        {
            CheckNewIndexArgument(_count, index);
            DoInsert(index, item);
        }
        
        /// Removes the item at the specified index.        
        /// index The zero-based index of the item to remove.
        /// T:System.ArgumentOutOfRangeException 
        /// index is not a valid index in this list.
        /// 
        /// T:System.NotSupportedException 
        /// This list is read-only.
        /// 
        public void RemoveAt(int index)
        {
            CheckExistingIndexArgument(_count, index);
            DoRemoveAt(index);
        }
        
        /// Determines the index of a specific item in this list.        
        /// item The object to locate in this list.
        /// The index of item if found in this list; otherwise, -1.
        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            int ret = 0;
            foreach (var sourceItem in this)
            {
                if (comparer.Equals(item, sourceItem))
                    return ret;
                ++ret;
            }

            return -1;
        }
        
        /// Adds an item to the end of this list.        
        /// item The object to add to this list.
        /// T:System.NotSupportedException 
        /// This list is read-only.
        void ICollection<T>.Add(T item)
        {
            DoInsert(_count, item);
        }

        /// Determines whether this list contains a specific value.        
        /// item The object to locate in this list.
        /// 
        /// true if item is found in this list; otherwise, false.
        bool ICollection<T>.Contains(T item)
        {
            return this.Contains(item, null);
        }

        /// Copies the elements of this list to an T:System.Array, starting at a particular T:System.Array index.        
        /// array The one-dimensional T:System.Array that is the destination of the elements copied from this slice. The T:System.Array must have zero-based indexing.
        /// arrayIndex The zero-based index in array at which copying begins.
        /// T:System.ArgumentNullException 
        /// array is null.
        /// 
        /// T:System.ArgumentOutOfRangeException 
        /// arrayIndex is less than 0.
        /// 
        /// T:System.ArgumentException 
        /// arrayIndex is equal to or greater than the length of array.
        /// -or-
        /// The number of elements in the source T:System.Collections.Generic.ICollection`1 is greater than the available space from arrayIndex to the end of the destination array.
        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array", "Array is null");

            int count = _count;
            CheckRangeArguments(array.Length, arrayIndex, count);
            for (int i = 0; i != count; ++i)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        /// Removes the first occurrence of a specific object from this list.        
        /// item The object to remove from this list.
        /// 
        /// true if item was successfully removed from this list; otherwise, false. This method also returns false if item is not found in this list.
        /// 
        /// T:System.NotSupportedException 
        /// This list is read-only.
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index == -1)
                return false;

            DoRemoveAt(index);
            return true;
        }

        /// Returns an enumerator that iterates through the collection.
        /// 
        /// A T:System.Collections.Generic.IEnumerator`1 that can be used to iterate through the collection.
        public IEnumerator<T> GetEnumerator()
        {
            int count = _count;
            for (int i = 0; i != count; ++i)
            {
                yield return DoGetItem(i);
            }
        }

        /// Returns an enumerator that iterates through a collection.        
        /// 
        /// An T:System.Collections.IEnumerator object that can be used to iterate through the collection.
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
        #region ObjectListImplementations        
        /// Returns whether or not the type of a given item indicates it is appropriate for storing in this container.        
        /// item The item to test.
        /// true if the item is appropriate to store in this container; otherwise, false.
        private bool ObjectIsT(object item)
        {
            if (item is T)
            {
                return true;
            }

            if (item == null)
            {
                var type = typeof(T);
                if (type.IsClass && !type.IsPointer)
                    return true; // classes, arrays, and delegates
                if (type.IsInterface)
                    return true; // interfaces
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return true; // nullable value types
            }

            return false;
        }

        int System.Collections.IList.Add(object value)
        {
            if (!ObjectIsT(value))
                throw new ArgumentException("Item is not of the correct type.", "value");
            AddToBack((T)value);
            return _count - 1;
        }

        bool System.Collections.IList.Contains(object value)
        {
            if (!ObjectIsT(value))
                throw new ArgumentException("Item is not of the correct type.", "value");
            return this.Contains((T)value);
        }

        int System.Collections.IList.IndexOf(object value)
        {
            if (!ObjectIsT(value))
                throw new ArgumentException("Item is not of the correct type.", "value");
            return IndexOf((T)value);
        }

        void System.Collections.IList.Insert(int index, object value)
        {
            if (!ObjectIsT(value))
                throw new ArgumentException("Item is not of the correct type.", "value");
            Insert(index, (T)value);
        }

        bool System.Collections.IList.IsFixedSize
        {
            get { return false; }
        }

        bool System.Collections.IList.IsReadOnly
        {
            get { return false; }
        }

        void System.Collections.IList.Remove(object value)
        {
            if (!ObjectIsT(value))
                throw new ArgumentException("Item is not of the correct type.", "value");
            Remove((T)value);
        }

        object System.Collections.IList.this[int index]
        {
            get
            {
                return this[index];
            }

            set
            {
                if (!ObjectIsT(value))
                    throw new ArgumentException("Item is not of the correct type.", "value");
                this[index] = (T)value;
            }
        }

        void System.Collections.ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array", "Destination array cannot be null.");
            CheckRangeArguments(array.Length, index, _count);

            for (int i = 0; i != _count; ++i)
            {
                try
                {
                    array.SetValue(this[i], index + i);
                }
                catch (InvalidCastException ex)
                {
                    throw new ArgumentException("Destination array is of incorrect type.", ex);
                }
            }
        }

        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }

        object System.Collections.ICollection.SyncRoot
        {
            get { return this; }
        }

        #endregion
        #region GenericListHelpers
      
        /// Checks the index argument to see if it refers to a valid insertion point in a source of a given length.        
        /// sourceLength The length of the source. This parameter is not checked for validity.
        /// index The index into the source.
        /// ArgumentOutOfRangeException index is not a valid index to an insertion point for the source.
        private static void CheckNewIndexArgument(int sourceLength, int index)
        {
            if (index < 0 || index > sourceLength)
            {
                throw new ArgumentOutOfRangeException("index", "Invalid new index " + index + " for source length " + sourceLength);
            }
        }

        
        /// Checks the index argument to see if it refers to an existing element in a source of a given length.
        
        /// sourceLength The length of the source. This parameter is not checked for validity.
        /// index The index into the source.
        /// ArgumentOutOfRangeException index is not a valid index to an existing element for the source.
        private static void CheckExistingIndexArgument(int sourceLength, int index)
        {
            if (index < 0 || index >= sourceLength)
            {
                throw new ArgumentOutOfRangeException("index", "Invalid existing index " + index + " for source length " + sourceLength);
            }
        }

        
        /// Checks the offset and count arguments for validity when applied to a source of a given length. Allows 0-element ranges, including a 0-element range at the end of the source.
        
        /// sourceLength The length of the source. This parameter is not checked for validity.
        /// offset The index into source at which the range begins.
        /// count The number of elements in the range.
        /// ArgumentOutOfRangeException Either offset or count is less than 0.
        /// ArgumentException The range [offset, offset + count) is not within the range [0, sourceLength).
        private static void CheckRangeArguments(int sourceLength, int offset, int count)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", "Invalid offset " + offset);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", "Invalid count " + count);
            }

            if (sourceLength - offset < count)
            {
                throw new ArgumentException("Invalid offset (" + offset + ") or count + (" + count + ") for source length " + sourceLength);
            }
        }

        #endregion

        /// Gets a value indicating whether this instance is empty.        
        private bool IsEmpty
        {
            get { return _count == 0; }
        }
        
        /// Gets a value indicating whether this instance is at full capacity.
        private bool IsFull
        {
            get { return _count == Capacity; }
        }
        
        /// Gets a value indicating whether the buffer is "split" (meaning the beginning of the view is at a later index in buffer than the end).        
        private bool IsSplit
        {
            get
            {
                // Overflow-safe version of "(offset + Count) > Capacity"
                return offset > (Capacity - _count);
            }
        }

        /// Gets or sets the capacity for this deque. This value must always be greater than zero, and this property cannot be set to a value less than Count.        
        /// InvalidOperationException Capacity cannot be set to a value less than Count.
        public int Capacity
        {
            get
            {
                return buffer.Length;
            }

            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value", "Capacity must be greater than 0.");

                if (value < _count)
                    throw new InvalidOperationException("Capacity cannot be set to a value less than Count");

                if (value == buffer.Length)
                    return;

                // Create the new buffer and copy our existing range.
                T[] newBuffer = new T[value];
                if (IsSplit)
                {
                    // The existing buffer is split, so we have to copy it in parts
                    int length = Capacity - offset;
                    Array.Copy(buffer, offset, newBuffer, 0, length);
                    Array.Copy(buffer, 0, newBuffer, length, _count - length);
                }
                else
                {
                    // The existing buffer is whole
                    Array.Copy(buffer, offset, newBuffer, 0, _count);
                }

                // Set up to use the new buffer.
                buffer = newBuffer;
                offset = 0;
            }
        }
        
        /// Gets the number of elements contained in this deque.
        /// The number of elements contained in this deque.
        public int Count { get { return _count; } private set { _count = value; } }
        private int _count;
                
        /// Applies the offset to index, resulting in a buffer index.        
        /// index The deque index.
        /// The buffer index.
        private int _dequeIndexToBufferIndex(int index)
        {
            return (index + offset) % Capacity;
        }
        
        /// Gets an element at the specified view index.
        /// index The zero-based view index of the element to get. This index is guaranteed to be valid.
        /// The element at the specified index.
        private T DoGetItem(int index)
        {
            return buffer[_dequeIndexToBufferIndex(index)];
        }
        
        /// Sets an element at the specified view index.        
        /// index The zero-based view index of the element to get. This index is guaranteed to be valid.
        /// item The element to store in the list.
        private void DoSetItem(int index, T item)
        {
            buffer[_dequeIndexToBufferIndex(index)] = item;
        }

        /// Inserts an element at the specified view index.        
        /// index The zero-based view index at which the element should be inserted. This index is guaranteed to be valid.
        /// item The element to store in the list.
        private void DoInsert(int index, T item)
        {
            EnsureCapacityForOneElement();

            if (index == 0)
            {
                _doAddToFront(item);
                return;
            }
            else if (index == _count)
            {
                _doAddToBack(item);
                return;
            }

            _doInsertRange(index, new[] { item }, 1);
        }
        
        /// Removes an element at the specified view index.
        /// index The zero-based view index of the element to remove. This index is guaranteed to be valid.
        private void DoRemoveAt(int index)
        {
            if (index == 0)
            {
                this.RemoveFromFront();
                return;
            }
            else if (index == _count - 1)
            {
                _doRemoveFromBack();
                return;
            }

            _doRemoveRange(index, 1);
        }

        /// Increments offset by value using modulo-Capacity arithmetic.        
        /// value The value by which to increase offset. May not be negative.
        /// The value of offset after it was incremented.
        private int _postIncrement(int value)
        {
            int ret = offset;
            offset += value;
            offset %= Capacity;
            return ret;
        }

        /// Decrements offset by value using modulo-Capacity arithmetic.        
        /// value The value by which to reduce offset. May not be negative or greater than Capacity.
        /// The value of offset before it was decremented.
        private int _preDecrement(int value)
        {
            offset -= value;
            if (offset < 0)
                offset += Capacity;
            return offset;
        }
        
        /// Inserts a single element to the back of the view. IsFull must be false when this method is called.        
        /// value The element to insert.
        private void _doAddToBack(T value)
        {
            buffer[_dequeIndexToBufferIndex(Count)] = value;
            ++_count;
        }
        
        /// Inserts a single element to the front of the view. IsFull must be false when this method is called.
        /// value The element to insert.
        private void _doAddToFront(T value)
        {
            buffer[_preDecrement(1)] = value;
            ++_count;
        }
        
        /// Removes and returns the last element in the view. IsEmpty must be false when this method is called.        
        /// The former last element.
        private T _doRemoveFromBack()
        {
            T ret = buffer[_dequeIndexToBufferIndex(Count - 1)];
            --_count;
            return ret;
        }
               
        /// Inserts a range of elements into the view.        
        /// index The index into the view at which the elements are to be inserted.
        /// The elements to insert.
        /// collectionCount The number of elements in collection. Must be greater than zero, and the sum of collectionCount and Count must be less than or equal to Capacity.
        private void _doInsertRange(int index, IEnumerable<T> collection, int collectionCount)
        {
            // Make room in the existing list
            if (index < _count / 2)
            {
                // Inserting into the first half of the list

                // Move lower items down: [0, index) -> [Capacity - collectionCount, Capacity - collectionCount + index)
                // This clears out the low "index" number of items, moving them "collectionCount" places down;
                //   after rotation, there will be a "collectionCount"-sized hole at "index".
                int copyCount = index;
                int writeIndex = Capacity - collectionCount;
                for (int j = 0; j != copyCount; ++j)
                    buffer[_dequeIndexToBufferIndex(writeIndex + j)] = buffer[_dequeIndexToBufferIndex(j)];

                // Rotate to the new view
                this._preDecrement(collectionCount);
            }
            else
            {
                // Inserting into the second half of the list

                // Move higher items up: [index, count) -> [index + collectionCount, collectionCount + count)
                int copyCount = _count - index;
                int writeIndex = index + collectionCount;
                for (int j = copyCount - 1; j != -1; --j)
                    buffer[_dequeIndexToBufferIndex(writeIndex + j)] = buffer[_dequeIndexToBufferIndex(index + j)];
            }

            // Copy new items into place
            int i = index;
            foreach (T item in collection)
            {
                buffer[_dequeIndexToBufferIndex(i)] = item;
                ++i;
            }

            // Adjust valid count
            _count += collectionCount;
        }
        
        /// Removes a range of elements from the view.        
        /// index The index into the view at which the range begins.
        /// collectionCount The number of elements in the range. This must be greater than 0 and less than or equal to Count.
        private void _doRemoveRange(int index, int collectionCount)
        {
            if (index == 0)
            {
                // Removing from the beginning: rotate to the new view
                this._postIncrement(collectionCount);
                _count -= collectionCount;
                return;
            }
            else if (index == _count - collectionCount)
            {
                // Removing from the ending: trim the existing view
                _count -= collectionCount;
                return;
            }

            if ((index + (collectionCount / 2)) < _count / 2)
            {
                // Removing from first half of list

                // Move lower items up: [0, index) -> [collectionCount, collectionCount + index)
                int copyCount = index;
                int writeIndex = collectionCount;
                for (int j = copyCount - 1; j != -1; --j)
                    buffer[_dequeIndexToBufferIndex(writeIndex + j)] = buffer[_dequeIndexToBufferIndex(j)];

                // Rotate to new view
                this._postIncrement(collectionCount);
            }
            else
            {
                // Removing from second half of list

                // Move higher items down: [index + collectionCount, count) -> [index, count - collectionCount)
                int copyCount = _count - collectionCount - index;
                int readIndex = index + collectionCount;
                for (int j = 0; j != copyCount; ++j)
                    buffer[_dequeIndexToBufferIndex(index + j)] = buffer[_dequeIndexToBufferIndex(readIndex + j)];
            }

            // Adjust valid count
            _count -= collectionCount;
        }

        /// Doubles the capacity if necessary to make room for one more element. When this method returns, IsFull is false.        
        private void EnsureCapacityForOneElement()
        {
            if (this.IsFull)
            {
                this.Capacity = this.Capacity * 2;
            }
        }
        
        /// Inserts a single element at the back of this deque.
        /// value The element to insert.
        public void AddToBack(T value)
        {
            if (this.IsFull) this.Capacity = this.Capacity * 2;

            buffer[(_count + offset) % Capacity] = value;
            ++_count;
        }

        /// Inserts a single element at the front of this deque.        
        /// value The element to insert.
        public void AddToFront(T value)
        {
            if (this.IsFull) this.Capacity = this.Capacity * 2;

            offset -= 1;
            if (offset < 0) offset += Capacity;

            buffer[offset] = value;
            ++_count;
        }
        
        /// Inserts a collection of elements into this deque.        
        /// index The index at which the collection is inserted.
        /// The collection of elements to insert.
        /// ArgumentOutOfRangeException index is not a valid index to an insertion point for the source.
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            int collectionCount = collection.Count();
            CheckNewIndexArgument(_count, index);

            // Overflow-safe check for "_count + collectionCount > this.Capacity"
            if (collectionCount > Capacity - _count)
            {
                this.Capacity = checked(_count + collectionCount);
            }

            if (collectionCount == 0)
            {
                return;
            }

            this._doInsertRange(index, collection, collectionCount);
        }

        /// Removes a range of elements from this deque.        
        /// offset The index into the deque at which the range begins.
        /// count The number of elements to remove.
        /// ArgumentOutOfRangeException Either offset or count is less than 0.
        /// ArgumentException The range [offset, offset + count) is not within the range [0, Count).
        public void RemoveRange(int offset, int count)
        {
            CheckRangeArguments(_count, offset, count);

            if (count == 0)
            {
                return;
            }

            this._doRemoveRange(offset, count);
        }
        
        /// Removes and returns the last element of this deque.        
        /// The former last element.
        /// InvalidOperationException The deque is empty.
        public T RemoveFromBack()
        {
            if (this.IsEmpty)
                throw new InvalidOperationException("The deque is empty.");

            T ret = buffer[((_count - 1) + offset) % Capacity];
            --_count;

            return ret;
        }

        /// Removes and returns the first element of this deque.
        /// The former first element.
        /// InvalidOperationException The deque is empty.
        public T RemoveFromFront()
        {
            if (this.IsEmpty) throw new InvalidOperationException("The deque is empty.");

            --_count;

            int ret = offset;
            offset += 1;
            offset %= Capacity;

            return buffer[ret];
        }
        
        /// Removes all items from this deque.        
        public void Clear()
        {
            this.offset = 0;
            _count = 0;
        }

        [DebuggerNonUserCode]
        private sealed class DebugView
        {
            private readonly Deque<T> deque;

            public DebugView(Deque<T> deque)
            {
                this.deque = deque;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] Items
            {
                get
                {
                    var array = new T[deque.Count];
                    ((ICollection<T>)deque).CopyTo(array, 0);
                    return array;
                }
            }
        }
    }
}


