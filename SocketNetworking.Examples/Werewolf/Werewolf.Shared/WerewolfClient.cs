using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.PacketSystem.Packets;

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

        public override void InitLocalClient()
        {
            base.InitLocalClient();
            AvatarChanged += WerewolfClient_AvatarChanged;
        }

        private void WerewolfClient_AvatarChanged(SocketNetworking.Shared.NetworkObjects.INetworkAvatar obj)
        {
            if (CurrentClientLocation != ClientLocation.Local)
            {
                return;
            }
            NetworkInvokeOnClient((Action<NetworkHandle, string>)SetName, ClientName);
        }

        [NetworkInvokable(Direction = NetworkDirection.Client)]
        private void SetName(NetworkHandle handle, string name)
        {
            PlayerAvatar.Name = name;
        }

        public string ClientName = string.Empty;

        public void SendMessage(ChatMessage message)
        {
            NetworkInvokeOnClient((Action<NetworkHandle, ChatMessage>)ReceiveMessage, message);
        }

        [NetworkInvokable(Direction = NetworkDirection.Any)]
        private void ReceiveMessage(NetworkHandle handle, ChatMessage message)
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
