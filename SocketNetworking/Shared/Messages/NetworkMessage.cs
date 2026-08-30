using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.Messages
{
    public class NetworkMessage<T> : INetworkMessage<T>
    {
        public NetworkMessage() { }

        public NetworkMessage(INetworkObject source, INetworkObject destination, bool senderIsAuthority, T data)
        {
            Source = source;
            Destination = destination;
            SenderIsAuthority = senderIsAuthority;
            Data = data;
        }

        public INetworkObject Source { get; private set; }

        public INetworkObject Destination { get; private set; }

        public T Data { get; private set; }

        public object DataObject { get; private set; }

        public bool SenderIsAuthority { get; set; }

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Source = reader.ReadObject<INetworkObject>();
            Destination = reader.ReadObject<INetworkObject>();
            //SenderIsAuthority = reader.ReadBool();
            Data = reader.ReadObject<T>();
            DataObject = Data;
            return reader;
        }

        public int GetLength()
        {
            return (int)Serialize().Length;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteObject(Source);
            writer.WriteObject(Destination);
            //writer.WriteBool(SenderIsAuthority);
            writer.WriteObject(Data);
            return writer;
        }
    }
}
