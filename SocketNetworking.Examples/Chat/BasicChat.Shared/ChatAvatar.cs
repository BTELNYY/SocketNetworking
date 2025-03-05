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
            if(NetworkManager.WhereAmI == ClientLocation.Remote && packet.NewNetworkID == NetworkID)
            {
                _name.Value = $"Client {OwnerClientID}";
            }
        }

        public void ClientSetName(string name)
        {
            NetworkClient.LocalClient.NetworkInvoke(this, nameof(ServerGetNameChangeRequest), new object[] { name });
        }

        [NetworkInvokable(NetworkDirection.Client)]
        private void ServerGetNameChangeRequest(NetworkHandle handle, string name)
        {
            _name.Value = name;
        }

        public override void OnNetworkSpawned(NetworkClient spawner)
        {
            base.OnNetworkSpawned(spawner);
        }
    }
}
