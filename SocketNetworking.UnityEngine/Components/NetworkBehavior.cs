using System;
using System.Collections.Generic;
using System.Linq;
using SocketNetworking.Client;
using SocketNetworking.Server;
using SocketNetworking.Shared;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.Shared.SyncVars;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkBehavior : MonoBehaviour, INetworkObject
    {
        public virtual OwnershipMode FallBackIfOwnerDisconnects
        {
            get
            {
                return OwnershipMode.Public;
            }
        }

        public virtual bool Active
        {
            get
            {
                return gameObject.activeSelf;
            }
            set
            {
                gameObject.SetActive(value);
            }
        }

        public virtual bool Spawnable => true;

        public virtual ObjectVisibilityMode ObjectVisibilityMode { get; set; }

        public virtual bool AllowPublicModification => false;

        public virtual int OwnerClientID { get; set; }

        public virtual OwnershipMode OwnershipMode { get; set; }

        public virtual int NetworkID { get; set; }

        public virtual void OnSync(NetworkClient client)
        {

        }

        public virtual void OnAdded(INetworkObject addedObject)
        {

        }

        public virtual void OnRemoved(INetworkObject removedObject)
        {

        }

        public virtual void OnDisconnected(NetworkClient client)
        {
            if (NetworkManager.WhereAmI != ClientLocation.Remote) { return; }
            if (client.ClientID == OwnerClientID)
            {
                OwnershipMode = FallBackIfOwnerDisconnects;
            }
        }

        public virtual void OnReady(NetworkClient client, bool isReady)
        {

        }

        public virtual void OnConnected(NetworkClient client)
        {

        }

        public virtual void OnNetworkSpawned(NetworkClient spawner)
        {

        }

        public virtual void OnLocalSpawned(ObjectManagePacket packet)
        {

        }

        public virtual ByteReader ReceiveExtraData(byte[] extraData)
        {
            return new ByteReader(extraData);
        }

        public virtual ByteWriter SendExtraData()
        {
            return new ByteWriter();
        }

        public virtual void OnClientDestroy(NetworkClient client)
        {

        }

        public virtual void OnModified(NetworkClient modifier)
        {

        }

        public virtual void OnModified(INetworkObject modifiedObject, NetworkClient modifier)
        {

        }

        public virtual void OnModify(ObjectManagePacket modifier, NetworkClient client)
        {

        }


        public virtual void OnDestroyed(INetworkObject destroyedObject, NetworkClient client)
        {

        }

        public virtual void OnCreated(INetworkObject createdObject, NetworkClient client)
        {

        }

        public virtual void OnServerDestroy()
        {

        }

        public virtual void Destroy()
        {
            GameObject.Destroy(this);
        }


        /// <summary>
        /// Ensures the current script is registered as a network object
        /// </summary>
        public virtual void RegisterObject()
        {
            if (NetworkID == -1)
            {
                return;
            }
            if (NetworkManager.IsRegistered(this))
            {
                return;
            }
            NetworkManager.AddNetworkObject(this);
        }

        public virtual void OnServerStarted()
        {

        }

        public virtual void OnServerReady()
        {

        }

        public virtual void OnServerStopped()
        {

        }

        void Awake()
        {
            NetworkServer.ServerReady += OnServerReady;
            NetworkServer.ServerStopped += OnServerStopped;
            NetworkServer.ServerStarted += OnServerStarted;
        }

        void Start()
        {
            RegisterObject();
        }

        /// <summary>
        /// Checks if the current point (where the code is executed) is the sync owner. If Sync ownership is ignored, always returns true.
        /// </summary>
        public virtual bool IsOwner
        {
            get
            {
                if (OwnershipMode == OwnershipMode.Public)
                {
                    return true;
                }
                else if (OwnershipMode == OwnershipMode.Server && NetworkManager.WhereAmI == ClientLocation.Remote)
                {
                    return true;
                }
                else if (OwnershipMode == OwnershipMode.Client && OwnerClientID == NetworkClient.LocalClient.ClientID)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Determines if the current code is on the client. This is done by checking if <see cref="IsServer"/> is false.
        /// </summary>
        public virtual bool IsClient
        {
            get
            {
                return !IsServer;
            }
        }

        /// <summary>
        /// Determines if the current code that is running is being executed on the server or client. Non-dedicated hosts will also return true in this case.
        /// </summary>
        public virtual bool IsServer
        {
            get
            {
                return NetworkManager.WhereAmI == ClientLocation.Remote;
            }
        }

        /// <summary>
        /// Checks if the current execution point should be getting packets, basically if we arent the sync owner, we should get packets.
        /// </summary>
        public virtual bool ShouldBeReceivingPackets
        {
            get
            {
                if (NetworkID == -1) return false;
                if (NetworkManager.WhereAmI == ClientLocation.Local)
                {
                    return true;
                }
                if (OwnershipMode == OwnershipMode.Public)
                {
                    return true;
                }
                if (OwnershipMode == OwnershipMode.Server && NetworkManager.WhereAmI == ClientLocation.Local)
                {
                    return true;
                }
                if (OwnershipMode == OwnershipMode.Client && NetworkManager.WhereAmI == ClientLocation.Remote)
                {
                    return true;
                }
                return false;
            }
        }

        public virtual bool ShouldBeReceivingPacketsFrom(NetworkHandle handle)
        {
            return ShouldBeReceivingPacketsFrom(handle.Client);
        }

        public virtual bool ShouldBeReceivingPacketsFrom(NetworkClient client)
        {
            if (OwnershipMode == OwnershipMode.Server && client.CurrentClientLocation == ClientLocation.Local)
            {
                return true;
            }
            if (OwnershipMode == OwnershipMode.Client && OwnerClientID == client.ClientID)
            {
                return true;
            }
            if (OwnershipMode == OwnershipMode.Public)
            {
                return true;
            }
            return false;
        }

        public Log Logger
        {
            get
            {
                return Log.GetInstance();
            }
        }

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

        /// <summary>
        /// Preforms a send operation and syncs data across network, can be called on either client or server, method handles what happens.
        /// </summary>
        /// <param name="packet"></param>
        public virtual void Send(TargetedPacket packet, bool priority = false)
        {
            Send(packet, this, priority);
        }

        /// <summary>
        /// Preforms a send operation and syncs data across network, can be called on either client or server, method handles what happens.
        /// </summary>
        /// <param name="packet"></param>
        public virtual void Send(TargetedPacket packet, INetworkObject target, bool priority)
        {
            if (NetworkID == -1)
            {
                return;
            }
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkServer.SendToAll(packet, target);
                return;
            }
            if (NetworkManager.WhereAmI == ClientLocation.Local)
            {
                if (NetworkClient.LocalClient != null)
                {
                    NetworkClient.LocalClient.Send(packet, target, priority);
                }
                else
                {
                    Logger.Warning("Current GameNetworkClient is null!");
                }
                return;
            }
            Logger.Error("Can't find where I am!");
        }


        public virtual void NetworkInvoke(string methodName, params object[] args)
        {
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                if (ObjectVisibilityMode == ObjectVisibilityMode.Everyone)
                {
                    NetworkServer.NetworkInvokeOnAll(this, methodName, args);
                }
                else if (ObjectVisibilityMode == ObjectVisibilityMode.OwnerAndServer)
                {
                    NetworkClient client = this.GetOwner();
                    client?.NetworkInvoke(this, methodName, args);
                }
                else if (ObjectVisibilityMode == ObjectVisibilityMode.ServerOnly)
                {
                    throw new InvalidOperationException("Can't network invoke on objects that don't have remote counter parts. (Object is hidden)");
                }
            }
            else if (NetworkManager.WhereAmI == ClientLocation.Local)
            {
                if (NetworkClient.LocalClient == null)
                {
                    throw new InvalidOperationException("Attempted to networkinvoke using a client when the game client is not set!");
                }
                else
                {
                    //This is a nice check, buts its useless against patching. This is checked again on the server :3
                    if (!this.HasPermission(NetworkClient.LocalClient))
                    {
                        return;
                    }
                    NetworkManager.NetworkInvoke(this, NetworkClient.LocalClient, methodName, args);
                }
            }
        }

        public virtual void OnSyncVarChanged(NetworkClient client, INetworkSyncVar what)
        {

        }

        public virtual void OnSyncVarsChanged()
        {

        }

        public virtual void OnOwnerNetworkSpawned(NetworkClient spawner)
        {

        }

        public virtual void OnOwnerLocalSpawned(NetworkClient spawner)
        {

        }

        public virtual void OnOwnerDisconnected(NetworkClient client)
        {

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
    }
}
