using SocketNetworking.Shared.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SocketNetworking.Shared.PacketSystem.TypeWrappers
{
    /// <summary>
    /// A Generic dictionary class.
    /// </summary>
    /// <typeparam name="TKey">
    /// Any key. Must be a member of <see cref="Packet.SupportedTypes"/>.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// Any value. Must be a member of <see cref="Packet.SupportedTypes"/>.
    /// </typeparam>
    public class SerializableDictionary<TKey, TValue> : IPacketSerializable, IDictionary<TKey, TValue>
    {
        private SerializableList<TKey> keys;

        private SerializableList<TValue> values;

        public Dictionary<TKey, TValue> ContainedDict
        {
            get
            {
                Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
                for (int i = 0; i < keys.Count; i++)
                {
                    dict.Add(keys[i], values[i]);
                }
                return dict;
            }
        }

        public SerializableDictionary(IDictionary<TKey, TValue> keyValuePairs)
        {
            keys = new SerializableList<TKey>(keyValuePairs.Keys);
            values = new SerializableList<TValue>(keyValuePairs.Values);
        }

        public SerializableDictionary(SerializableList<TKey> keys, SerializableList<TValue> values)
        {
            this.keys = keys;
            this.values = values;
        }

        public SerializableDictionary(IEnumerable<TKey> keys, IEnumerable<TValue> values)
        {
            this.keys = new SerializableList<TKey>(keys);
            this.values = new SerializableList<TValue>(values);
        }

        public SerializableDictionary()
        {
            keys = new SerializableList<TKey>();
            values = new SerializableList<TValue>();
        }

        public TValue this[TKey key]
        {
            get
            {
                return ContainedDict[key];
            }
            set
            {
                int index = keys.IndexOf(key);
                values[index] = value;
            }
        }

        public ICollection<TKey> Keys => keys;

        public ICollection<TValue> Values => values;

        public int Count => Math.Max(keys.Count, values.Count);

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            keys.Add(key);
            values.Add(value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            keys.Clear();
            values.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return keys.Contains(item.Key) && values.Contains(item.Value);
        }

        public bool ContainsKey(TKey key)
        {
            return keys.Contains(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            for (int i = arrayIndex; i < keys.Count; i++)
            {
                array.Append(new KeyValuePair<TKey, TValue>(keys[i], values[i]));
            }
        }

        public ByteReader Deserialize(byte[] data)
        {
            int removeAmount = 0;
            ByteReader reader = new ByteReader(data);
            reader.ReadInt();
            keys = reader.ReadPacketSerialized<SerializableList<TKey>>();
            values = reader.ReadPacketSerialized<SerializableList<TValue>>();
            removeAmount += reader.ReadBytes;
            return reader;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ContainedDict.GetEnumerator();
        }

        public int GetLength()
        {
            int counter = 4;
            counter += keys.GetLength();
            counter += values.GetLength();
            return counter;
        }

        public bool Remove(TKey key)
        {
            if (!ContainsKey(key))
            {
                return false;
            }
            int indexOfkey = keys.IndexOf(key);
            keys.RemoveAt(indexOfkey);
            values.RemoveAt(indexOfkey);
            return true;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteInt(GetLength());
            writer.WritePacketSerialized<SerializableList<TKey>>(keys);
            writer.WritePacketSerialized<SerializableList<TValue>>(values);
            return writer;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (!ContainsKey(key))
            {
                value = default;
                return false;
            }
            int index = keys.IndexOf(key);
            value = values[index];
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ContainedDict.GetEnumerator();
        }
    }
}
