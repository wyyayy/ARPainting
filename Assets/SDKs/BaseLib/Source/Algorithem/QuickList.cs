using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;

using BaseLib;

using Debugger = BaseLib.Debugger;

/// This improved version of the System.Collections.Generic.List that doesn't release the buffer on Clear(), resulting 
/// in better performance and less garbage collection.
public class QuickList<T> ///: System.Collections.IEnumerable
{
	/// <summary>
	/// Direct access to the buffer. Note that you should not use its 'Length' parameter, but instead use BetterList.size.
	/// </summary>
	public T[] _Buffer;

	/// <summary>
	/// Direct access to the buffer's size. Note that it's only public for speed and efficiency. You shouldn't modify it.
	/// </summary>
	public int Count = 0;

	/// <summary>
	/// For 'foreach' functionality.
	/// </summary>
	[DebuggerHidden]
	[DebuggerStepThrough]
	public IEnumerator<T> GetEnumerator ()
	{
		if (_Buffer != null)
		{
			for (int i = 0; i < Count; ++i)
			{
				yield return _Buffer[i];
			}
		}
	}

	/// <summary>
	/// Convenience function. I recommend using .buffer instead.
	/// </summary>
	[DebuggerHidden]
	public T this[int i]
	{
		get 
        {
            Debugger.Assert(i < Count);
            return _Buffer[i]; 
        }
		set 
        {
            Debugger.Assert(i < Count);
            _Buffer[i] = value; 
        }
	}

	/// <summary>
	/// Helper function that expands the size of the array, maintaining the content.
	/// </summary>
	void AllocateMore ()
	{
		T[] newList = (_Buffer != null) ? new T[Mathf.Max(_Buffer.Length << 1, 32)] : new T[32];
		if (_Buffer != null && Count > 0) _Buffer.CopyTo(newList, 0);
		_Buffer = newList;
	}

    public void Reserve(int nCount)
    {
        Debugger.Assert(nCount > Count, "Reserve failed, reserved count must large than current count");

        T[] newList;
        if(_Buffer == null)
        {
            newList = new T[nCount];
        }
        else
        {
            newList = new T[nCount];
            if (Count > 0) _Buffer.CopyTo(newList, 0);
        }

        _Buffer = newList;
    }

	/// <summary>
	/// Trim the unnecessary memory, resizing the buffer to be of 'Length' size.
	/// Call this function only if you are sure that the buffer won't need to resize anytime soon.
	/// </summary>
	void Trim ()
	{
		if (Count > 0)
		{
			if (Count < _Buffer.Length)
			{
				T[] newList = new T[Count];
				for (int i = 0; i < Count; ++i) newList[i] = _Buffer[i];
				_Buffer = newList;
			}
		}
		else _Buffer = null;
	}

	/// <summary>
	/// Clear the array by resetting its size to zero. Note that the memory is not actually released.
	/// </summary>

	public void Clear () { Count = 0; }

	/// <summary>
	/// Clear the array and release the used memory.
	/// </summary>
	public void Release () { Count = 0; _Buffer = null; }

	/// <summary>
	/// Add the specified item to the end of the list.
	/// </summary>
	public void Add (T item)
	{
		if (_Buffer == null || Count == _Buffer.Length) AllocateMore();
		_Buffer[Count++] = item;
	}

    public void AddRange(IList<T> list)
    {
        if (list.Count == 0) return;

        if (_Buffer == null || (Count + list.Count) > _Buffer.Length) Reserve(Count + list.Count);

        foreach (T item in list)
        {
            Add(item);
        }
    }

    public void AddRange<DestType>(QuickList<DestType> list)
    {
        if (list.Count == 0) return;
        if (_Buffer == null || (Count + list.Count) > _Buffer.Length) Reserve(Count + list.Count);
        System.Array.Copy(list._Buffer, 0, _Buffer, Count, list.Count);
        Count += list.Count;
    }

    public void AddRange(QuickList<T> list)
    {
        if (list.Count == 0) return;
        if (_Buffer == null || (Count + list.Count) > _Buffer.Length) Reserve(Count + list.Count);
        System.Array.Copy(list._Buffer, 0, _Buffer, Count, list.Count);
        Count += list.Count;
    }

	/// <summary>
	/// Insert an item at the specified index, pushing the entries back.
	/// </summary>
	public void Insert (int index, T item)
	{
        if (_Buffer == null || Count == _Buffer.Length) AllocateMore();

		if (index > -1 && index < Count)
		{
			for (int i = Count; i > index; --i) _Buffer[i] = _Buffer[i - 1];
			_Buffer[index] = item;
			++Count;
		}
		else Add(item);
	}

	/// <summary>
	/// Returns 'true' if the specified item is within the list.
	/// </summary>
	public bool Contains (T item)
	{
		if (_Buffer == null) return false;
        for (int i = 0; i < Count; ++i)
        {
            if (_Buffer[i].Equals(item)) return true;
        }
		return false;
	}

	/// <summary>
	/// Return the index of the specified item.
	/// </summary>
	public int IndexOf (T item)
	{
		if (_Buffer == null) return -1;
		for (int i = 0; i < Count; ++i) if (_Buffer[i].Equals(item)) return i;
		return -1;
	}

	/// <summary>
	/// Remove the specified item from the list. Note that RemoveAt() is faster and is advisable if you already know the index.
	/// </summary>
	public bool Remove (T item)
	{
		if (_Buffer != null)
		{
			EqualityComparer<T> comp = EqualityComparer<T>.Default;

			for (int i = 0; i < Count; ++i)
			{
				if (comp.Equals(_Buffer[i], item))
				{
					--Count;
					_Buffer[i] = default(T);
					for (int b = i; b < Count; ++b) _Buffer[b] = _Buffer[b + 1];
					_Buffer[Count] = default(T);
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Remove an item at the specified index.
	/// </summary>
	public void RemoveAt (int index)
	{
		if (_Buffer != null && index > -1 && index < Count)
		{
			--Count;
			_Buffer[index] = default(T);
			for (int b = index; b < Count; ++b) _Buffer[b] = _Buffer[b + 1];
			_Buffer[Count] = default(T);
		}
	}

	/// Remove an item from the end.
	public T Pop ()
	{
		if (_Buffer != null && Count != 0)
		{
			T val = _Buffer[--Count];
			_Buffer[Count] = default(T);
			return val;
		}
		return default(T);
	}

    public void RemoveTail()
    {
        _Buffer[Count - 1] = default(T);
        Count--;
    }

    public void RemoveTail(int count)
    {
        Debugger.Assert(count >= 0 && count <= this.Count);

        for (int i = this.Count - count; i < this.Count; ++i )
        {
            _Buffer[i] = default(T);
        }

        this.Count -= count;
    }

	/// <summary>
	/// Mimic List's ToArray() functionality, except that in this case the list is resized to match the current size.
	/// </summary>
	public T[] ToArray () { Trim(); return _Buffer; }

	/// <summary>
	/// List.Sort equivalent. Manual sorting causes no GC allocations.
	/// </summary>
	[DebuggerHidden]
	[DebuggerStepThrough]
	public void Sort (CompareFunc comparer)
	{
		int start = 0;
		int max = Count - 1;
		bool changed = true;

		while (changed)
		{
			changed = false;

			for (int i = start; i < max; ++i)
			{
				// Compare the two values
				if (comparer(_Buffer[i], _Buffer[i + 1]) > 0)
				{
					// Swap the values
					T temp = _Buffer[i];
					_Buffer[i] = _Buffer[i + 1];
					_Buffer[i + 1] = temp;
					changed = true;
				}
				else if (!changed)
				{
					// Nothing has changed -- we can start here next time
					start = (i == 0) ? 0 : i - 1;
				}
			}
		}
	}

	/// <summary>
	/// Comparison function should return -1 if left is less than right, 1 if left is greater than right, and 0 if they match.
	/// </summary>
	public delegate int CompareFunc (T left, T right);
}