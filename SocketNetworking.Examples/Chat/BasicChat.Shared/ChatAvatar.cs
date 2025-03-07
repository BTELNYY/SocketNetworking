using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.SyncVars;
using SocketNetworking.Shared;
using SocketNetworking.Client;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Attributes;
using SocketNetworking;

namespace BasicChat.Shared
{
    public class ChatAvatar : NetworkAvatarBase
    {
        public ChatAvatar()
        {
            _name = new NetworkSyncVar<string>(this, OwnershipMode.Server);
        }

        public string Name
        {
            get
            {
                return _name.Value;
            }
        }

        private NetworkSyncVar<string> _name;

        public override void OnLocalSpawned(ObjectManagePacket packet)
        {
            base.OnLocalSpawned(packet);
            if (NetworkManager.WhereAmI == ClientLocation.Remote && _name.Value == default)
            {
                _name.RawSet($"Client {OwnerClientID}", null);
            }
        }

        public override void OnDisconnected(NetworkClient client)
        {
            if (client.ClientID == OwnerClientID)
            {
                ChatClient cClient = client as ChatClient;
                ChatServer.SendMessage(new Message()
                {
                    Content = $"{cClient.RequestedName} disconnected.",
                    Color = ConsoleColor.Yellow,
                    Sender = 0,
                    Target = 0,
                });
            }
            base.OnDisconnected(client);
        }

        bool _broadcasted = false;

        public override void OnNetworkSpawned(NetworkClient spawner)
        {
            base.OnNetworkSpawned(spawner);
            ChatClient client = spawner as ChatClient;
            _name.Value = client.RequestedName;
            if(spawner.ClientID == OwnerClientID)
            {
                ChatServer.SendMessage(new Message()
                {
                    Content = $"{client.RequestedName} connected.",
                    Color = ConsoleColor.Yellow,
                    Sender = 0,
                    Target = 0,
                });
            }
        }

        public void ClientSetName(string name)
        {
            NetworkClient.LocalClient.NetworkInvoke(this, nameof(ServerGetNameChangeRequest), new object[] { name });
        }

        [NetworkInvokable(NetworkDirection.Client)]
        private void ServerGetNameChangeRequest(NetworkHandle handle, string name)
        {
            if(name == "")
            {
                _name.Value = handle.Client.ConnectedHostname;
                return;
            }
            _name.Value = name;
        }
    }
}
