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
        public string RequestedName;

        public ChatClient()
        {
            AuthenticationProvider = new ChatAuthProvider(this);
            AuthenticationStateChanged += () => 
            {
                if(NetworkManager.WhereAmI == ClientLocation.Remote)
                {
                    ServerSendMessage(new Message()
                    {
                        Target = 0,
                        Sender = 0,
                        Color = ConsoleColor.Magenta,
                        Content = Config.MOTD,
                    });
                }
            };
        }

        public event Action<NetworkHandle, Message> MessageReceived;

        public void ClientSendMessage(string message)
        {
            Message msg = new Message();
            msg.Content = message;
            msg.Sender = ClientID;
            msg.Target = 0;
            NetworkInvoke(nameof(ServerGetMessage), new object[] { msg });
        }

        public void ServerSendMessage(Message message)
        {
            NetworkInvoke(nameof(ClientGetMessage), new object[] { message });
        }

        [NetworkInvokable(NetworkDirection.Client)]
        private void ServerGetMessage(NetworkHandle handle, Message message)
        {
            if(message.Sender != Avatar.NetworkID)
            {
                Log.Warning("Tried to send message as not myself!");
                return;
            }
            if(string.IsNullOrWhiteSpace(message.Content))
            {
                return;
            }
            Log.Info($"Message: \"{message.Content}\", Target: {message.Target}, Source Name: {((ChatAvatar)Avatar).Name}");
            message.Color = ConsoleColor.White;
            MessageReceived?.Invoke(handle, message);
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
            if(message.Sender == 0)
            {
                MessageReceived?.Invoke(handle, message);
                return;
            }
            INetworkObject obj = NetworkManager.GetNetworkObjectByID(message.Sender).Item1;
            if(obj == null)
            {
                return;
            }
            if(!(obj is ChatAvatar avatar))
            {
                return;
            }
            MessageReceived?.Invoke(handle, message);
        }
    }

    public struct Message : IPacketSerializable
    {
        public string Content;

        public int Target;

        public int Sender;

        public ConsoleColor Color;

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Content = reader.ReadString();
            Target = reader.ReadInt();
            Sender = reader.ReadInt();
            Color = (ConsoleColor)reader.ReadByte();
            return reader;
        }

        public int GetLength()
        {
            return Serialize().Length;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteString(Content);
            writer.WriteInt(Target);
            writer.WriteInt(Sender);
            writer.WriteByte((byte)Color);
            return writer;
        }
    }
}
