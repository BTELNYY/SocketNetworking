using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared;
using SocketNetworking.Shared.SyncVars;

namespace SocketNetworking.Example.SharedData
{
    public class NetworkObjectTest : NetworkAvatarBase
    {
        public NetworkObjectTest()
        {
        }

        public NetworkSyncVar<string> Name = new NetworkSyncVar<string>("test");

        public override void OnNetworkSpawned(NetworkClient spawner)
        {
            base.OnNetworkSpawned(spawner);
            Log.GlobalInfo("Just got spawned by client " + spawner.ClientID);
            Name.Value = "hey";
        }

        public override void OnSyncVarChanged(NetworkClient client, INetworkSyncVar what)
        {
            base.OnSyncVarChanged(client, what);
            Log.GlobalInfo($"SyncVar {what.Name} changed to {what.ValueRaw}");
        }
    }
}
