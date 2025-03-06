using System;
using SocketNetworking;
using SocketNetworking.Attributes;
using SocketNetworking.Client;
using SocketNetworking.PacketSystem;
using SocketNetworking.Server;
using SocketNetworking.Shared;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.Serialization;

namespace BasicChat.Shared
{
    public class ChatClient : TcpNetworkClient
    {
        public void ClientSendMessage(string message)
        {
            Message msg = new Message();
            msg.Content = message;
            msg.Sender = ClientID;
            msg.Target = 0;
            NetworkInvoke(nameof(ServerGetMessage), new object[] { msg });
        }

        [NetworkInvokable(NetworkDirection.Client)]
        private void ServerGetMessage(NetworkHandle handle, Message message)
        {
            if(message.Sender != Avatar.NetworkID)
            {
                Log.Warning("Tried to send message as not myself!");
                return;
            }
            Log.Info($"Message: \"{message.Content}\", Target: {message.Target}, Source Name: {((ChatAvatar)Avatar).Name}");
            if(message.Target == 0)
            {
                NetworkServer.NetworkInvokeOnAll(this, nameof(ClientGetMessage), new object[] { message });
            }
            else
            {
                INetworkObject targetAvatar = NetworkManager.GetNetworkObjectByID(message.Target).Item1;
                if(targetAvatar == null)
                {
                    return;
                }
                NetworkClient owner = targetAvatar.GetOwner();
                if(owner == null)
                {
                    return;
                }
                owner.NetworkInvoke(nameof(ClientGetMessage), new object[] { message });
            }
        }

        [NetworkInvokable(NetworkDirection.Server)]
        private void ClientGetMessage(NetworkHandle handle, Message message)
        {
            INetworkObject obj = NetworkManager.GetNetworkObjectByID(message.Sender).Item1;
            if(obj == null)
            {
                return;
            }
            if(!(obj is ChatAvatar avatar))
            {
                return;
            }
            Console.WriteLine($"{avatar.Name}: {message.Content}");
        }
    }

    public struct Message : IPacketSerializable
    {
        public string Content;

        public int Target;

        public int Sender;

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Content = reader.ReadString();
            Target = reader.ReadInt();
            Sender = reader.ReadInt();
            return reader;
        }

        public int GetLength()
        {
            return Serialize().DataLength;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteString(Content);
            writer.WriteInt(Target);
            writer.WriteInt(Sender);
            return writer;
        }
    }
}
