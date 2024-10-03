using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem
{
    public abstract class TypeWrapper<T>
    {
        public TypeWrapper(T value)
        {
            Value = value;
        }

        public TypeWrapper() { }

        public Type GetContainedType()
        {
            return typeof(T);
        }

        public T Value { get; set; }

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
    }
}
