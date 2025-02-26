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

namespace SocketNetworking.Example.Basics.SharedData
{
    public class NetworkObjectTest : NetworkAvatarBase
    {
        //Some fun examples
        public NetworkSyncVar<string> Name = new NetworkSyncVar<string>("test");

        public NetworkSyncVar<bool> IsAlive = new NetworkSyncVar<bool>(true);

        public NetworkSyncVar<float> HP = new NetworkSyncVar<float>(100f);

        public NetworkSyncVar<float> Armor = new NetworkSyncVar<float>(100f);

        public override void OnNetworkSpawned(NetworkClient spawner)
        {
            base.OnNetworkSpawned(spawner);
            Log.GlobalInfo("Just got spawned by client " + spawner.ClientID);
            Name.Value = "Bobo";
            IsAlive.Value = false;
            HP.Value = 0f;
            Armor.Value = 0f;
        }

        public override void OnSyncVarChanged(NetworkClient client, INetworkSyncVar what)
        {
            base.OnSyncVarChanged(client, what);
            Log.GlobalInfo($"SyncVar {what.Name} changed to {what.ValueRaw} on object {NetworkID}");
        }
    }
}
