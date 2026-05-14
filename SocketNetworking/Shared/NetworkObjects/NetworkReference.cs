using System;
using SocketNetworking.Shared.PacketSystem.TypeWrappers;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.NetworkObjects
{
    /// <summary>
    /// Represents a safe way to transmit a reference to a <see cref="INetworkObject"/> between servers and clients.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NetworkReference<T> : IByteSerializable where T : INetworkObject
    {
        private T _value = default;

        public NetworkReference(T value)
        {
            _value = value;
        }

        /// <summary>
        /// The stored reference (or <see langword="default"/> if not set.)
        /// </summary>
        public T Value => _value;

        /// <summary>
        /// <see langword="true"/> if the <see cref="Value"/> is not <see langword="null"/> and has a <see cref="INetworkObject.NetworkID"/> that is not 0. Otherwise <see langword="false"/>. 
        /// </summary>
        public bool IsValid => _value != null && _value.NetworkID != 0;

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            int objId = reader.ReadInt();
            Type type = reader.ReadWrapper<SerializableType, Type>();
            (INetworkObject, NetworkObjectData) obj = NetworkManager.FindNetworkObject(objId, type);
            if (obj != default)
            {
                _value = (T)obj.Item1;
            }
            return reader;
        }

        public int GetLength()
        {
            return (int)Serialize().Length;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            Type type = typeof(void);
            int id = -1;
            if (Value != null)
            {
                type = Value.GetType();
                id = Value.NetworkID;
            }
            writer.WriteInt(id);
            SerializableType wType = new SerializableType(type);
            writer.WritePacketSerialized<SerializableType>(wType);
            return writer;
        }
    }
}
