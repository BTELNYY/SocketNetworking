using System;

namespace SocketNetworking.Tests.LocalTests
{
    public class OrderedLinkedList<T> : LinkedList<T> where T : IComparable<T>
    {
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
            var ptr = Head;
            while(ptr != null)
            {
                if(ptr.Next == null)
                {
                    ptr.Next = new LinkedNode<T>(value, null);
                    return;
                }
                if(ptr.Next.Value.CompareTo(value) >= 0)
                {
                    T oldVal = ptr.Value;
                    var oldNext = ptr.Next;
                    ptr.Value = value;
                    ptr.Next = new LinkedNode<T>(oldVal, oldNext);
                    return;
                }
                ptr = ptr.Next;
            }
        }
    }
}
