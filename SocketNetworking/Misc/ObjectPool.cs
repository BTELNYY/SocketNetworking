using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Misc
{
    public class ObjectPool<T>
    {
        private readonly ConcurrentBag<T> _objects;

        private readonly Func<T> _objectGenerator;

        int _capacity;

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

        public T Get()
        {
            if(_objects.TryTake(out T item))
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

        public void Return(T item)
        {
            _objects.Add(item);
        }
    }
}
