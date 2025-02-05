using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Shared;

namespace SocketNetworking.ExampleSharedData
{
    public class NetworkObjectTest : NetworkAvatarBase
    {
        public override void OnNetworkSpawned(NetworkClient spawner)
        {
            base.OnNetworkSpawned(spawner);
            Log.GlobalInfo("Just got spawned by client " + spawner.ClientID);
        }
    }
}
