using System;
using System.Collections.Concurrent;

namespace SocketNetworking.Misc
{
    /// <summary>
    /// The <see cref="ObjectPool{T}"/> class is intended to limit the amount of borrowed objects of type <typeparamref name="T"/> to prevent memory leaks.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectPool<T>
    {
        private readonly ConcurrentBag<T> _objects;

        private readonly Func<T> _objectGenerator;

        int _capacity;

        /// <summary>
        /// Capacity of the <see cref="ObjectPool{T}"/>. 0 if this pool can grow dynamically.
        /// </summary>
        public int Capacity
        {
            get
            {
                return _capacity;
            }
            set
            {
                _capacity = value;
            }
        }

        public ObjectPool(Func<T> objectGenerator)
        {
            _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
            _objects = new ConcurrentBag<T>();
        }

        public ObjectPool(Func<T> objectGenerator, int capacity)
        {
            _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
            _objects = new ConcurrentBag<T>();
            _capacity = capacity;
            for (int i = 0; i < capacity; i++)
            {
                _objects.Add(_objectGenerator());
            }
        }

        /// <summary>
        /// Tries to borrow an object <typeparamref name="T"/>. If it can't, it will wait until it can.
        /// </summary>
        /// <returns></returns>
        public T Get()
        {
            if (_objects.TryTake(out T item))
            {
                return item;
            }
            else
            {
                if (_capacity != 0)
                {
                    while (true)
                    {
                        if (_objects.TryTake(out T result))
                        {
                            return result;
                        }
                    }
                }
                else
                {
                    return _objectGenerator();
                }
            }
        }

        /// <summary>
        /// Returns an object to the <see cref="ObjectPool{T}"/>.
        /// </summary>
        /// <param name="item"></param>
        public void Return(T item)
        {
            _objects.Add(item);
        }
    }
}
