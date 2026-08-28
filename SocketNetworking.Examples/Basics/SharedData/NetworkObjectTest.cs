using SocketNetworking;
using SocketNetworking.Client;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.SyncVars;

namespace Basic.SharedData
{
    public class NetworkObjectTest : NetworkAvatarBase
    {
        //Some fun examples
        public NetworkSyncVar<string> Name;

        public NetworkSyncVar<bool> IsAlive;

        public NetworkSyncVar<float> HP;

        public NetworkSyncVar<float> Armor;

        public override void OnBeforeRegister()
        {
            Name = new NetworkSyncVar<string>(this, "test", nameof(Name));
            IsAlive = new NetworkSyncVar<bool>(this, true, nameof(IsAlive));
            HP = new NetworkSyncVar<float>(this, 100f, nameof(HP));
            Armor = new NetworkSyncVar<float>(this, 100f, nameof(Armor));
        }

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
