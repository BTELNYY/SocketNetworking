using Microsoft.Win32.SafeHandles;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared;

namespace SocketNetworking.PacketSystem.TypeWrappers
{
    public class SerializableList<T> : IPacketSerializable, IList<T>
    {
        private List<T> _internalList;

        private Type TType;

        public SerializableList(IEnumerable<T> values)
        {
            if(typeof(T) == typeof(object))
            {
                if(values.Count() != 0)
                {
                    TType = values.ElementAt(0).GetType();
                }
                else
                {
                    TType = typeof(T);
                }
            }
            else
            {
                TType = typeof(T);
            }
            if (!NetworkConvert.SupportedTypes.Contains(TType) && !TType.GetInterfaces().Contains(typeof(IPacketSerializable)))
            {
                throw new ArgumentException($"Array type ({TType.FullName}) is not supported, use one of the supported types instead.", "values");
            }
            _internalList = new List<T>();
        }

        public SerializableList()
        {
            TType = typeof(T);
            if (!NetworkConvert.SupportedTypes.Contains(TType) && !TType.GetInterfaces().Contains(typeof(IPacketSerializable)))
            {
                throw new ArgumentException($"Array type ({TType.FullName}) is not supported, use one of the supported types instead.", "values");
            }
            _internalList = new List<T>();
        }

        public List<T> ContainedList => _internalList;

        public void OverwriteContained(IEnumerable<T> values)
        {
            _internalList = values.ToList();
        }

        public int Count => ContainedList.Count;

        public bool IsReadOnly => false;

        public T this[int index] 
        { 
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        public int Deserialize(byte[] data)
        {
            if(_internalList.Count > 0)
            {
                throw new InvalidOperationException("The array must be empty in order to deserialize.");
            }
            int usedBytes = 0;
            ByteReader reader = new ByteReader(data);
            if (reader.IsEmpty)
            {
                return usedBytes;
            }
            int length = reader.ReadInt();
            usedBytes += 4;
            while (reader.DataLength > 0)
            {
                int currentChunkLength = reader.ReadInt();
                usedBytes += 4;
                if (currentChunkLength == 0)
                {
                    break;
                }
                byte[] readBytes = reader.Read(currentChunkLength);
                DeserializeAndAdd(readBytes);
                //reader.Remove(currentChunkLength);
                usedBytes += currentChunkLength;
            }
            return usedBytes;
        }

        private void DeserializeAndAdd(byte[] data)
        {
            if (_internalList == null)
            {
                _internalList = new List<T>();
            }
            _internalList.Add(NetworkConvert.DeserializeRaw<T>(data));
        }

        public int GetLength()
        {
            //We have this becuase our "counter" and "size" flags must exist. So the structure goes:
            //Length, Elements
            //Each element gets a length value attached to it.
            int size = sizeof(int);
            if (_internalList.Count == 0)
            {
                return 0;
            }
            foreach(T element in _internalList)
            {
                size += sizeof(int);
                if(element.GetType().GetInterfaces().Contains(typeof(IPacketSerializable))) 
                {
                    IPacketSerializable serializable = (IPacketSerializable)element;
                    size += serializable.Serialize().Length;
                }
                else
                {
                    size += typeof(T).SizeOf();
                }
            }
            return size;
        }

        public byte[] Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteInt(GetLength());
            foreach(T element in _internalList)
            {
                //Dont need to worry about type safety as we check in constructor
                byte[] finalBytes = NetworkConvert.Serialize(element).Data;
                int dataLength = finalBytes.Length;
                writer.WriteInt(dataLength);
                writer.Write(finalBytes);
            }
            return writer.Data;
        }

        public int IndexOf(T item)
        {
            return _internalList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _internalList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _internalList.RemoveAt(index);
        }

        public void Add(T item)
        {
            _internalList.Add(item);
        }

        public void Clear()
        {
            _internalList.Clear();
        }

        public bool Contains(T item)
        {
            return _internalList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _internalList.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }
}
