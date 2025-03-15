using System;
using System.Collections.Generic;

namespace SocketNetworking.Tests.LocalTests
{
    /// <summary>
    /// Wrote this in my java class to prove that I could write the same thing we are learning in a diff language to a friend. Have fun I guess?
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LinkedList<T>
    {
        public LinkedList()
        {

        }

        public LinkedList(IEnumerable<T> values)
        {
            foreach (T item in values)
            {
                Add(item);
            }
        }

        public LinkedNode<T> Head;

        public void ResetHead(T value)
        {
            Head = new LinkedNode<T>(value, Head);
        }

        public virtual T Find(T value)
        {
            LinkedNode<T> ptr = Head;
            while (ptr != null)
            {
                if (ptr.Value.Equals(value))
                {
                    return ptr.Value;
                }
            }
            return default;
        }

        public virtual void Add(T value)
        {
            if (Head == null)
            {
                Head = new LinkedNode<T>(value);
                return;
            }
            AddRecursive(Head, ref value);
        }

        protected virtual void AddRecursive(LinkedNode<T> ptr, ref T value)
        {
            if (ptr == null)
            {
                return;
            }
            if (ptr.Next == null)
            {
                ptr.Next = new LinkedNode<T>(value);
                return;
            }
            AddRecursive(ptr.Next, ref value);
        }

        public virtual void Insert(int offset, ref T value)
        {
            if (Head == null)
            {
                if (offset != 0)
                {
                    throw new ArgumentOutOfRangeException();
                }
                Add(value);
                return;
            }
            int current = 0;
            InsertRecursive(ref current, offset, Head, ref value);
        }

        public virtual void Insert(T value, ref T next)
        {
            InsertRecursive(Head, value, next);
        }

        protected virtual void InsertRecursive(LinkedNode<T> ptr, T value, T nextVal)
        {
            if (ptr.Value.Equals(value))
            {
                ptr.Next = new LinkedNode<T>(nextVal, ptr.Next);
                return;
            }
            InsertRecursive(ptr, value, nextVal);
        }

        protected virtual void InsertRecursive(ref int current, int offset, LinkedNode<T> ptr, ref T value)
        {
            if (ptr == null)
            {
                return;
            }
            if (ptr.Next == null && current != offset)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (current == offset)
            {
                LinkedNode<T> reference = ptr.Next;
                ptr.Next = new LinkedNode<T>(value, ptr.Next);
                return;
            }
            current++;
            InsertRecursive(ref current, offset, ptr.Next, ref value);
        }

        public virtual void Remove(T value)
        {
            RemoveRecursive(Head, ref value);
        }

        protected virtual void RemoveRecursive(LinkedNode<T> ptr, ref T value)
        {
            if (ptr == null)
            {
                return;
            }
            if (ptr.Next == null)
            {
                return;
            }
            if (ptr == Head)
            {
                if (ptr.Value.Equals(value))
                {
                    Head = Head.Next;
                    return;
                }
            }
            if (ptr.Next.Value.Equals(value))
            {
                LinkedNode<T> reference = ptr.Next.Next;
                ptr.Next = reference;
                return;
            }
            RemoveRecursive(ptr.Next, ref value);
        }

        public long Size
        {
            get
            {
                int count = 0;
                LinkedNode<T> ptr = Head;
                if (Head == null)
                {
                    return count;
                }
                while (ptr != null)
                {
                    count++;
                    ptr = ptr.Next;
                }
                return count;
            }
        }

        public override string ToString()
        {
            LinkedNode<T> ptr = Head;
            string result = string.Empty;
            while (ptr != null)
            {
                result += ptr.Value.ToString();
                if (ptr.Next != null)
                {
                    result += "->";
                }
                ptr = ptr.Next;
            }
            return result;
        }
    }


    public class LinkedNode<T>
    {
        public LinkedNode(T Value)
        {
            this.Value = Value;
            this.Next = null;
        }

        public LinkedNode(T value, LinkedNode<T> next) : this(value)
        {
            Next = next;
        }

        public T Value;

        public LinkedNode<T> Next;

        public void Append(T value)
        {
            if (Next == null)
            {
                Next = new LinkedNode<T>(value);
                return;
            }
        }
    }
}
