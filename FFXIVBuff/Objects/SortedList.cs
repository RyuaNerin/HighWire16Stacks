using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace FFXIVBuff.Objects
{
    [DebuggerDisplay("Count = {Count}")]
    public class SortedList<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable
        where T : IComparable<T>
    {
        private readonly List<T> m_lst = new List<T>();

        public SortedList()
        {
            this.m_lst = new List<T>();
        }
        public SortedList(IEnumerable<T> collection)
        {
            this.m_lst = new List<T>();
            this.AddRange(collection);
        }
        public SortedList(int capacity)
        {
            this.m_lst = new List<T>(capacity);
        }

        public int Capacity
        {
            get { return this.m_lst.Capacity; }
            set { this.m_lst.Capacity = value; }
        }
        public int Count
        {
            get { return this.m_lst.Count; }
        }

        public T this[int index]
        {
            get { return this.m_lst[index]; }
            set { this.m_lst[index] = value; }
        }
        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Add(T item)
        {
            int index = this.BinarySearch(item);
            if (index < 0)
                this.m_lst.Insert(~index, item);
        }
        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
                this.Add(item);
        }
        public ReadOnlyCollection<T> AsReadOnly()
        {
            return this.m_lst.AsReadOnly();
        }
        public int BinarySearch(T item)
        {
            return this.m_lst.BinarySearch(item);
        }
        public int BinarySearch(int index, int count, T item)
        {
            return this.m_lst.BinarySearch(index, count, item, Comparer<T>.Default);
        }
        public void Clear()
        {
            this.m_lst.Clear();
        }
        public bool Contains(T item)
        {
            return this.m_lst.BinarySearch(item) != -1;
        }
        public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            return this.m_lst.ConvertAll<TOutput>(converter);
        }
        public void CopyTo(T[] array)
        {
            this.m_lst.CopyTo(array);
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            this.m_lst.CopyTo(array, arrayIndex);
        }
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            this.m_lst.CopyTo(index, array, arrayIndex, count);
        }
        public bool Exists(Predicate<T> match)
        {
            return this.m_lst.Exists(match);
        }
        public T Find(Predicate<T> match)
        {
            return this.m_lst.Find(match);
        }
        public List<T> FindAll(Predicate<T> match)
        {
            return this.m_lst.FindAll(match);
        }
        public int FindIndex(Predicate<T> match)
        {
            return this.m_lst.FindIndex(match);
        }
        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return this.m_lst.FindIndex(startIndex, match);
        }
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            return this.m_lst.FindIndex(startIndex, count, match);
        }
        public T FindLast(Predicate<T> match)
        {
            return this.m_lst.FindLast(match);
        }
        public int FindLastIndex(Predicate<T> match)
        {
            return this.m_lst.FindLastIndex(match);
        }
        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return this.m_lst.FindLastIndex(startIndex, match);
        }
        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            return this.m_lst.FindLastIndex(startIndex, count, match);
        }
        public void ForEach(Action<T> action)
        {
            this.m_lst.ForEach(action);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.m_lst.GetEnumerator();
        }
        public IEnumerator GetEnumerator()
        {
            return this.m_lst.GetEnumerator();
        }
        public List<T> GetRange(int index, int count)
        {
            return this.m_lst.GetRange(index, count);
        }
        public int IndexOf(T item)
        {
            return this.m_lst.IndexOf(item);
        }
        public int IndexOf(T item, int index)
        {
            return this.m_lst.IndexOf(item, index);
        }
        public int IndexOf(T item, int index, int count)
        {
            return this.m_lst.IndexOf(item, index, count);
        }
        [Obsolete]
        public void Insert(int index, T item)
        {
            this.Add(item);
        }
        [Obsolete]
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            this.AddRange(collection);
        }
        public int LastIndexOf(T item)
        {
            return this.m_lst.LastIndexOf(item);
        }
        public int LastIndexOf(T item, int index)
        {
            return this.m_lst.LastIndexOf(item, index);
        }
        public int LastIndexOf(T item, int index, int count)
        {
            return this.m_lst.LastIndexOf(item, index, count);
        }
        public bool Remove(T item)
        {
            return this.m_lst.Remove(item);
        }
        public int RemoveAll(Predicate<T> match)
        {
            return this.m_lst.RemoveAll(match);
        }
        public void RemoveAt(int index)
        {
            this.m_lst.RemoveAt(index);
        }
        public void RemoveRange(int index, int count)
        {
            this.m_lst.RemoveRange(index, count);
        }
        [Obsolete]
        public void Reverse()
        {
        }
        [Obsolete]
        public void Reverse(int index, int count)
        {
        }
        public void Sort()
        {
            this.m_lst.Sort((a, b) => a.CompareTo(b));
        }
        [Obsolete]
        public void Sort(Comparison<T> comparison)
        {
        }
        [Obsolete]
        public void Sort(IComparer<T> comparer)
        {
        }
        [Obsolete]
        public void Sort(int index, int count, IComparer<T> comparer)
        {
        }
        public T[] ToArray()
        {
            return this.m_lst.ToArray();
        }
        public void TrimExcess()
        {
            this.m_lst.TrimExcess();
        }
        public bool TrueForAll(Predicate<T> match)
        {
            return this.m_lst.TrueForAll(match);
        }
    }
}
