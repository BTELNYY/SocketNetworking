using System;
using System.Collections.Generic;
using System.Linq;
using SocketNetworking.Client;
using SocketNetworking.Server;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.Shared.SyncVars;

namespace SocketNetworking.Shared.NetworkObjects
{
    /// <summary>
    /// The <see cref="NetworkObjectBase"/> class is the recommended class for <see cref="INetworkObject"/>s. it has additional methods which may be useful. 
    /// </summary>
    public class NetworkObjectBase : INetworkObject
    {
        public virtual int OwnerClientID { get; set; }

        /// <summary>
        /// The <see cref="NetworkClient"/> who owns this object. This value is always <see langword="null"/> on clients.
        /// </summary>
        public NetworkClient OwnerClient => this.GetOwner();

        /// <summary>
        /// The <see cref="INetworkAvatar"/> of the <see cref="NetworkClient"/> who owns this object. Available on the server and client.
        /// </summary>
        public INetworkAvatar OwnerAvatar => this.GetOwnerAvatar();

        public virtual OwnershipMode OwnershipMode { get; set; }

        public virtual bool AllowPublicModification => false;

        public virtual OwnershipMode FallBackIfOwnerDisconnects => OwnershipMode.Server;

        public virtual ObjectVisibilityMode ObjectVisibilityMode { get; set; }

        public virtual int NetworkID { get; set; }

        public virtual bool Active { get; set; } = true;

        public virtual bool Spawnable => true;

        public virtual IEnumerable<int> PrivilegedIDs
        {
            get
            {
                return _privs;
            }
            set
            {
                _privs = value.ToList();
            }
        }

        public virtual List<INetworkObject> RequiredObjects => new List<INetworkObject>();

        public int SpawnPriority => 0;

        /// <summary>
        /// This event is called when <see cref="Destroy"/> is called.
        /// </summary>
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

        /// <summary>
        /// This event is called when <see cref="OnModified(NetworkClient)"/> is called.
        /// </summary>
        public event Action<NetworkClient> Modified;

        public virtual void OnModified(NetworkClient modifier)
        {
            Modified?.Invoke(modifier);
        }

        public virtual void OnModified(INetworkObject modifiedObject, NetworkClient modifier)
        {

        }

        public virtual void OnModify(ObjectManagePacket modification, NetworkClient modifier)
        {

        }

        /// <summary>
        /// Called when the <see cref="NetworkObjectBase"/> is spawned by a client. 
        /// </summary>
        public event Action<NetworkClient> Spawned;

        public virtual void OnNetworkSpawned(NetworkClient spawner)
        {
            Spawned?.Invoke(spawner);
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

        /// <summary>
        /// Called when a <see cref="INetworkSyncVar"/> has changed on the <see cref="NetworkObjectBase"/>.
        /// </summary>
        public event Action<NetworkClient, INetworkSyncVar> SyncVarChanged;

        public virtual void OnSyncVarChanged(NetworkClient client, INetworkSyncVar what)
        {
            SyncVarChanged?.Invoke(client, what);
        }

        /// <summary>
        /// Called when <see cref="INetworkSyncVar"/>s are finished being modified.
        /// </summary>
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

        protected List<int> _privs = new List<int>();

        public virtual bool HasPrivilege(int clientId)
        {
            return _privs.Contains(clientId);
        }

        public virtual void GrantPrivilege(int clientId)
        {
            if (!HasPrivilege(clientId))
            {
                _privs.Add(clientId);
            }
        }

        public virtual void RemovePrivilege(int clientId)
        {
            if (HasPrivilege(clientId))
            {
                _privs.Remove(clientId);
            }
        }

        public virtual void OnBeforeRegister()
        {

        }

        public virtual void OnAfterRegister()
        {

        }

        /// <summary>
        /// Uses <see cref="NetworkManager.NetworkInvoke(object, NetworkClient, string, bool, object[])"/> to call a method. If called on the <see cref="ClientLocation.Local"/>, will attempt to call the server. if called on the <see cref="ClientLocation.Remote"/>, will call the function on all clients connected.
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <exception cref="NullReferenceException"></exception>
        public void NetworkInvoke(string methodName, params object[] args)
        {
            if (NetworkManager.WhereAmI == ClientLocation.Local)
            {
                if (NetworkClient.LocalClient == null)
                {
                    throw new NullReferenceException("LocalClient is null and we are on the local peer.");
                }
                NetworkClient.LocalClient.NetworkInvoke(this, methodName, args);
            }
            else if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkServer.NetworkInvokeOnAll(this, methodName, args);
            }
        }

        /// <summary>
        /// Calls <see cref="NetworkManager.NetworkInvoke{T}(object, NetworkClient, string, object[])"/> on this object. If called on the <see cref="ClientLocation.Local"/>, will call the method on the server. If called on the <see cref="ClientLocation.Remote"/>, the target client will be the owner. (if any, found by <see cref="OwnerClientID"/>.)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public T NetworkInvokeBlocking<T>(string methodName, params object[] args)
        {
            if (NetworkManager.WhereAmI == ClientLocation.Local)
            {
                if (NetworkClient.LocalClient == null)
                {
                    throw new NullReferenceException("LocalClient is null and we are on the local peer.");
                }
                return NetworkClient.LocalClient.NetworkInvokeBlocking<T>(this, methodName, 5000, args);
            }
            else if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkClient owner = this.GetOwner();
                if (owner != null)
                {
                    return owner.NetworkInvokeBlocking<T>(this, methodName, 5000, args);
                }
            }
            throw new InvalidOperationException();
        }
    }
}
