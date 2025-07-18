using System;

namespace SocketNetworking.Shared.Serialization
{
    /// <summary>
    /// The <see cref="TypeWrapper{T}"/> class provides basic structure to allow for extension of existing types which do not implement <see cref="IByteSerializable"/> and are not primitives.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class TypeWrapper<T> : IByteSerializable, ITypeWrapper
    {
        public TypeWrapper(T value)
        {
            Value = value;
        }

        public TypeWrapper() { }

        /// <summary>
        /// Returns the type of <typeparamref name="T"/>.
        /// </summary>
        /// <returns></returns>
        public Type GetContainedType()
        {
            return typeof(T);
        }

        /// <summary>
        /// The <typeparamref name="T"/> value contained in the <see cref="TypeWrapper{T}"/>. Note that if you have not called <see cref="Deserialize(byte[])"/> yet, this value will be <see langword="default"/>.
        /// </summary>
        public T Value { get; set; }
        public object RawValue
        {
            get
            {
                return Value;
            }
            set
            {
                if (value is T)
                {
                    Value = (T)value;
                }
            }
        }

        /// <summary>
        /// Serializes the value.
        /// </summary>
        /// <returns>
        /// The serialized value as a <see cref="byte[]"/>
        /// </returns>
        public abstract byte[] Serialize();

        /// <summary>
        /// Deserializies a value.
        /// </summary>
        /// <param name="data">
        /// The <see cref="byte[]"/> to deserialize.
        /// </param>
        /// <returns>The deserialized <see cref="T"/> and the amount of bytes read as a <see cref="int"/></returns>
        public abstract ValueTuple<T, int> Deserialize(byte[] data);

        public int GetLength()
        {
            return Serialize().Length;
        }

        ByteWriter IByteSerializable.Serialize()
        {
            return new ByteWriter(Serialize());
        }

        ByteReader IByteSerializable.Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            (T, int) result = Deserialize(data);
            //hacky fix to prevent stupid BS from getting in my way.
            reader.Read(data.Length);
            return reader;
        }

        public byte[] SerializeRaw()
        {
            return Serialize();
        }

        public (object, int) DeserializeRaw(byte[] data)
        {
            return ((object, int))Deserialize(data);
        }
    }
}
