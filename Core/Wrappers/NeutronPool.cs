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
using System;

namespace Atom.Core.Wrappers
{
    public class AtomPooling<T>
    {
        private readonly AtomSafelyQueue<T> _queue;
        private readonly Func<T> _generator;
        private readonly bool _createNewObjectIfNotAvailable;
        private readonly string _name;
        public int Count => _queue.Count;
        public AtomSafelyQueue<T> Queue => _queue;

        public AtomPooling(Func<T> generator, int capacity, bool createNewObjectIfNotAvailable, bool fillPool, string name)
        {
            _createNewObjectIfNotAvailable = createNewObjectIfNotAvailable;
            _generator = generator;
            _queue = new(capacity);
            _name = name;

            if (fillPool)
            {
                for (var i = 0; i < capacity; i++)
                    Push(_generator());
            }
        }

        public T Pull()
        {
            if (_queue.TryPull(out T item))
                return item;
            else
            {
                if (_createNewObjectIfNotAvailable)
                    return _generator();
                else
                    AtomLogger.PrintError($"Memory leak detected in '{_name}' pool. Please, increase the pool capacity or call the 'Dispose' method!");
            }

            return _generator();
        }

        public void Push(T obj) => _queue.Push(obj);
    }
}
#endif