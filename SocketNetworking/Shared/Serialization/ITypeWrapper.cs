using System;

namespace SocketNetworking.Shared.Serialization
{
    /// <summary>
    /// The <see cref="ITypeWrapper"/> interface provides base functionality to the <see cref="TypeWrapper{T}"/> class. It is not recommended to use this interface with your own custom typewrappers, use <see cref="TypeWrapper{T}"/> instead.
    /// </summary>
    public interface ITypeWrapper
    {
        /// <summary>
        /// Raw value of the contained object.
        /// </summary>
        object RawValue { get; set; }

        /// <summary>
        /// Returns the <see cref="Type"/> of <see cref="RawValue"/> (if any)
        /// </summary>
        /// <returns></returns>
        Type GetContainedType();

        /// <summary>
        /// Serializes the object.
        /// </summary>
        /// <returns></returns>
        byte[] SerializeRaw();

        /// <summary>
        /// Deserializies a value.
        /// </summary>
        /// <param name="data">
        /// The <see cref="byte[]"/> to deserialize.
        /// </param>
        ValueTuple<object, int> DeserializeRaw(byte[] data);
    }
}
