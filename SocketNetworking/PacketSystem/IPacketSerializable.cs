using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.PacketSystem
{
    public interface IPacketSerializable
    {
        /// <summary>
        /// Return the length of the current instance of this object in bytes.
        /// </summary>
        /// <returns>
        /// Length of data in bytes.
        /// </returns>
        int GetLength();

        /// <summary>
        /// Convert object to byte array, called when creating packets.
        /// </summary>
        /// <returns>
        /// byte array which will be written to packet.
        /// </returns>
        ByteWriter Serialize();

        /// <summary>
        /// Method is called on Read(), place your instance logic here. (e.g. reading the data manually from the byte stream)
        /// </summary>
        /// <param name="data">
        /// The full current data stream. Note that the whole stream is given here, as we don't know how much of it is used by your type.
        /// </param>
        /// <returns>
        /// Number of bytes that have been read.
        /// </returns>
        ByteReader Deserialize(byte[] data);
    }
}
