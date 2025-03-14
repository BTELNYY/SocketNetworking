using System;
using System.Collections.Generic;

namespace SocketNetworking.Tests.LocalTests
{
    public class OrderedLinkedList<T> : LinkedList<T> where T : IComparable<T>
    {
        public OrderedLinkedList(IEnumerable<T> values) : base(values)
        {
        }

        public override T Find(T value)
        {
            var ptr = Head;
            while (ptr != null)
            {
                if(ptr.Value.CompareTo(value) >= 0)
                {
                    return default;
                }
                if (ptr.Value.Equals(value))
                {
                    return ptr.Value;
                }
            }
            return default;
        }

        public override void Add(T value)
        {
            base.Add(value);
            Console.WriteLine(ToString());
        }

        protected override void AddRecursive(LinkedNode<T> ptr, ref T value)
        {
            
        }
    }
}
