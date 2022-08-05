/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

#if UNITY_2021_3_OR_NEWER
using System.Collections.Generic;
using System.Linq;

namespace Atom.Core.Wrappers
{
    public class AtomSafelyQueue<T>
    {
        private readonly object _lock = new();
        private readonly Queue<T> _queue = new();
        private readonly int _size = 0;
        private readonly bool _sort = false;

        public AtomSafelyQueue(bool sort = false) => _sort = sort;
        public AtomSafelyQueue(int size, bool sort = false)
        {
            _size = size;
            _sort = sort;
        }

        public void Push(T item, bool sort = false)
        {
            lock (_lock)
            {
                if (_size > 0)
                {
                    if (_queue.Count < _size)
                        _queue.Enqueue(item);
                }
                else
                    _queue.Enqueue(item);

                if (sort)
                    Sort();
            }
        }

        public T Pull()
        {
            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    T item = _queue.Dequeue();
                    return item;
                }
                else
                    return default;
            }
        }

        public bool TryPull(out T item)
        {
            item = default;
            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    item = _queue.Dequeue();
                    return true;
                }
                else
                    return false;
            }
        }

        public void Sort()
        {
            if (_sort)
            {
                lock (_lock)
                {
                    var values = _queue.ToList();
                    values.Sort();
                    if (values.Count > 0)
                    {
                        _queue.Clear();
                        for (int i = 0; i < values.Count; i++)
                        {
                            T value = values[i];
                            _queue.Enqueue(value);
                        }
                    }
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    int count = _queue.Count;
                    return count;
                }
            }
        }

        public Queue<T> Queue => _queue;
    }
}
#endif