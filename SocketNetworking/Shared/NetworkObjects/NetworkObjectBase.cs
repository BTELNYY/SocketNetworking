using System;
using SocketNetworking.Client;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.Shared.SyncVars;

namespace SocketNetworking.Shared.NetworkObjects
{
    public class NetworkObjectBase : INetworkObject
    {
        public virtual int OwnerClientID { get; set; }

        public NetworkClient OwnerClient => this.GetOwner();

        public virtual OwnershipMode OwnershipMode { get; set; }

        public virtual bool AllowPublicModification => false;

        public virtual OwnershipMode FallBackIfOwnerDisconnects => OwnershipMode.Server;

        public virtual ObjectVisibilityMode ObjectVisibilityMode { get; set; }

        public virtual int NetworkID { get; set; }

        public virtual bool Active { get; set; }

        public virtual bool Spawnable => true;

        public event Action Destroyed;

        public virtual void Destroy()
        {
            Destroyed?.Invoke();
        }

        public virtual void OnAdded(INetworkObject addedObject)
        {

        }

        public virtual void OnClientDestroy(NetworkClient client)
        {

        }

        public virtual void OnConnected(NetworkClient client)
        {

        }

        public virtual void OnCreated(INetworkObject createdObject, NetworkClient client)
        {

        }

        public virtual void OnDestroyed(INetworkObject destroyedObject, NetworkClient client)
        {

        }

        public virtual void OnDisconnected(NetworkClient client)
        {

        }

        public virtual void OnLocalSpawned(ObjectManagePacket packet)
        {

        }

        public event Action Modified;

        public virtual void OnModified(NetworkClient modifier)
        {
            Modified?.Invoke();
        }

        public virtual void OnModified(INetworkObject modifiedObject, NetworkClient modifier)
        {

        }

        public virtual void OnModify(ObjectManagePacket modification, NetworkClient modifier)
        {

        }

        public event Action Spawned;

        public virtual void OnNetworkSpawned(NetworkClient spawner)
        {
            Spawned?.Invoke();
        }

        public virtual void OnOwnerDisconnected(NetworkClient client)
        {

        }

        public virtual void OnOwnerLocalSpawned(NetworkClient spawner)
        {

        }

        public virtual void OnOwnerNetworkSpawned(NetworkClient spawner)
        {

        }

        public virtual void OnReady(NetworkClient client, bool isReady)
        {

        }

        public virtual void OnRemoved(INetworkObject removedObject)
        {

        }

        public virtual void OnServerDestroy()
        {

        }

        public virtual void OnSync(NetworkClient client)
        {

        }

        public event Action<NetworkClient, INetworkSyncVar> SyncVarChanged;

        public virtual void OnSyncVarChanged(NetworkClient client, INetworkSyncVar what)
        {
            SyncVarChanged?.Invoke(client, what);
        }

        public event Action SyncVarsChanged;

        public virtual void OnSyncVarsChanged()
        {
            SyncVarsChanged?.Invoke();
        }

        public virtual ByteReader ReceiveExtraData(byte[] extraData)
        {
            return new ByteReader(extraData);
        }

        public virtual ByteWriter SendExtraData()
        {
            return new ByteWriter();
        }
    }
}
