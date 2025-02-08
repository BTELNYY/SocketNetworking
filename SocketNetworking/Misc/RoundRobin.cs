using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Misc
{
    public class RoundRobin<T> : IList<T>
    {
        List<T> _list = new List<T>();

        int _nextIndex = 0;

        public bool IsRoundRobinExtension
        {
            get
            {
                return typeof(T).GetInterfaces().Any(x => x.GetType() == typeof(IRoundRobinData<T>));
            }
        }

        public T Next()
        {
            T value = _list[_nextIndex];
            if(value is IRoundRobinData<T> val && val.AllowSorting)
            {
                if (IsRoundRobinExtension)
                {
                    _list.Sort();
                    _nextIndex = 0;
                    return _list[_nextIndex];
                }
                IRoundRobinData<T> robinData;
                while (true)
                {
                    robinData = _list[_nextIndex] as IRoundRobinData<T>;
                    if(robinData.AllowChoosing)
                    {
                        break;
                    }
                    if(_nextIndex >= _list.Count)
                    {
                        break;
                    }
                    _nextIndex++;
                }
                if(robinData == null)
                {
                    throw new InvalidOperationException("No suitable value found.");
                }
                return (T)robinData;
            }
            else
            {
                if (_nextIndex >= _list.Count)
                {
                    _nextIndex = 0;
                }
                return _list[_nextIndex++];
            }
        }

        public int Capacity
        {
            get
            {
                return _list.Capacity;
            }
            set
            {
                _list.Capacity = value;
            }
        }

        public RoundRobin() { }

        public RoundRobin(IEnumerable<T> values)
        {
            _list = values.ToList();
        }


        public T this[int index]
        {
            get
            {
                return _list[index];
            }
            set
            {
                _list[index] = value;
            }
        }

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
        }

        public bool Remove(T item)
        {
            return _list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
