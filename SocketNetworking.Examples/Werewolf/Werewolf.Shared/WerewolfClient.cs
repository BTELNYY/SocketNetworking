using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.Shared.Attributes;.

namespace Werewolf.Shared
{
    public class WerewolfClient : TcpNetworkClient
    {
        public PlayerAvatar PlayerAvatar
        {
            get
            {
                return Avatar as PlayerAvatar;
            }
        }

        public void SendMessage(ChatMessage message)
        {
            NetworkInvokeOnClient((Action<NetworkHandle, ChatMessage>)RecieveMessage, message);
        }

        [NetworkInvokable(Direction = NetworkDirection.Any)]
        private void RecieveMessage(NetworkHandle handle, ChatMessage message)
        {

        }
    }

    public struct ChatMessage : IByteSerializable
    {
        public string Message { get; set; }

        public string Name { get; set; }

        public Team TargetChannel { get; set; }

        public int ClientID { get; set; }

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Message = reader.ReadString();
            Name = reader.ReadString();
            TargetChannel = (Team)reader.ReadByte();
            ClientID = reader.ReadInt();
            return reader;
        }

        public int GetLength()
        {
            return (int)Serialize().Length;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteString(Message);
            writer.WriteString(Name);
            writer.WriteByte((byte)TargetChannel);
            writer.WriteInt(ClientID);
            return writer;
        }
    }
}
