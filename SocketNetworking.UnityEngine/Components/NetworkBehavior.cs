using SocketNetworking;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SocketNetworking.Attributes;
using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.Server;

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

        public virtual int NetworkID => _netId;

        private int _netId = -1;

        /// <summary>
        /// Sets the objects <see cref="NetworkID"/> and tells all clients about it. Note that: The client must already know the old Network ID, so it is suggested you broadcast in some other way.
        /// </summary>
        /// <param name="id">
        /// The new ID to set the <see cref="NetworkID"/> to.
        /// </param>
        public void ServerSetNetworkID(int id)
        {
            if(NetworkManager.WhereAmI != ClientLocation.Remote)
            {
                throw new InvalidOperationException("Tried to change the NetworkID on the client!");
            }
            _netId = id;
            if (NetworkManager.IsRegistered(this))
            {
                NetworkManager.ModifyNetworkID(this);
            }
            else
            {
                RegisterObject();
            }
            NetworkInvoke(nameof(ClientSetNetId), new object[] { id }, true, false);
        }


        [NetworkInvocable(PacketDirection.Server)]
        private void ClientSetNetId(int id)
        {
            _netId = id;
            if (NetworkManager.IsRegistered(this))
            {
                NetworkManager.ModifyNetworkID(this);
            }
            else
            {
                RegisterObject();
            }
            OnObjectUpdateNetworkIDLocal(id);
        }

        public void SetNetworkID(int id, bool local = false)
        {
            if (local)
            {
                _netId = id;
                return;
            }
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                ServerSetNetworkID(NetworkID);
            }
            if (NetworkManager.WhereAmI == ClientLocation.Local)
            {
                ClientSetNetworkID(NetworkID);
            }
        }

        /// <summary>
        /// Updates the local network id. Note that no check is done if the new ID is correct, so this method can desync if used incorrectly.
        /// </summary>
        /// <param name="id"></param>
        public void ClientSetNetworkID(int id)
        {
            if(NetworkManager.WhereAmI != ClientLocation.Local)
            {
                throw new InvalidOperationException("Tried to change the local network ID on the server! Use ServerSetNetworkID() instead!");
            }
            _netId = id;
            if (NetworkManager.IsRegistered(this))
            {
                NetworkManager.ModifyNetworkID(this);
            }
            else
            {
                RegisterObject();
            }
        }

        public bool IsEnabled => base.enabled;

        public int OwnerClientID
        {
            get
            {
                return _ownerClientID;
            }
            set
            {
                if(NetworkManager.WhereAmI != ClientLocation.Remote)
                {
                    return;
                }
                if (OwnershipMode == OwnershipMode.Server || OwnershipMode == OwnershipMode.Public)
                {
                    if(value != -1)
                    {
                        OwnershipMode = OwnershipMode.Client;
                    }
                }
                NetworkServer.NetworkInvokeOnAll(this, nameof(UpdateOwnerClientIDRpc), new object[] { value });
            }
        }

        private int _ownerClientID = -1;

        [NetworkInvocable(PacketDirection.Server)]
        private void UpdateOwnerClientIDRpc(int id)
        {
            _ownerClientID = id;
        }

        /// <summary>
        /// Updates the local value for the <see cref="OwnerClientID"/>, Note that this does NOT change the owner of this object on the server, and will cause desync if not used incorrectly.
        /// </summary>
        /// <param name="id"></param>
        public void UpdateOwnerClientId(int id)
        {
            _ownerClientID = id;
        }


        /// <summary>
        /// Changes the <see cref="OwnerClientID"/> of the current object, requires the Sender to be the owner of the current object.
        /// </summary>
        /// <param name="newOwner"></param>
        public void ClientChangeOwner(int newOwner)
        {
            NetworkInvoke(nameof(ServerProccessChangeOwnerCommand), new object[] { newOwner });
        }

        [NetworkInvocable(PacketDirection.Client)]
        private void ServerProccessChangeOwnerCommand(int newOwner)
        {
            OwnerClientID = newOwner;
        }

        public OwnershipMode OwnershipMode
        {
            get
            {
                return _ownershipMode;
            }
            set
            {
                if (NetworkManager.WhereAmI != ClientLocation.Remote)
                {
                    return;
                }
                NetworkServer.NetworkInvokeOnAll(this, nameof(UpdateOwnershipModeRpc), new object[] { value });
            }
        }

        [NetworkInvocable(PacketDirection.Server)]
        private void UpdateOwnershipModeRpc(OwnershipMode mode)
        {
            _ownershipMode = mode;
        }

        private OwnershipMode _ownershipMode = OwnershipMode.Server;

        /// <summary>
        /// Updates the local value for the <see cref="OwnershipMode"/>, Note that this does NOT change the Ownership mode of the server object. This will cause desync if used incorrectly.
        /// </summary>
        /// <param name="id"></param>
        public void UpdateOwnershipMode(OwnershipMode mode)
        {
            _ownershipMode = mode;
        }

        public virtual void OnAdded(INetworkObject addedObject)
        {
            
        }

        public virtual void OnRemoved(INetworkObject removedObject)
        {

        }

        public virtual void OnDisconnected(NetworkClient client)
        {
            if(NetworkManager.WhereAmI != ClientLocation.Remote) { return; }
            if(client.ClientID == OwnerClientID)
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

        /// <summary>
        /// Called when <see cref="SetNetworkID(int)"/> is called.
        /// </summary>
        /// <param name="newNetID"></param>
        public virtual void OnObjectUpdateNetworkIDLocal(int newNetID)
        {

        }

        /// <summary>
        /// Called on the server when a client finishes creating the the prefab which is this object.
        /// </summary>
        /// <param name="client"></param>
        public virtual void OnClientObjectCreated(UnityNetworkClient client)
        {
            
        }

        /// <summary>
        /// Ensures the current script is registered as a network object
        /// </summary>
        public virtual void RegisterObject()
        {
            if(NetworkID == -1)
            {
                return;
            }
            if(NetworkManager.IsRegistered(this))
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


        public virtual void NetworkInvoke(string methodName, object[] args)
        {
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkServer.NetworkInvokeOnAll(this, methodName, args);
            }
            else if(NetworkManager.WhereAmI == ClientLocation.Local)
            {
                if(UnityNetworkManager.GameNetworkClient == null)
                {
                    throw new InvalidOperationException("Attempted to networkinvoke using a client when the game client is not set!");
                }
                else
                {
                    NetworkManager.NetworkInvoke(this, UnityNetworkManager.GameNetworkClient, methodName, args);
                }
            }
        }

        public virtual void NetworkInvoke(string methodName, object[] args, bool globalRpc, bool readyOnly)
        {
            if(NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                if(globalRpc)
                {
                    UnityNetworkServer.NetworkInvokeOnAll(this, methodName, args, readyOnly);
                }
                else
                {
                    if(OwnershipMode != OwnershipMode.Client)
                    {
                        throw new InvalidOperationException("Attempted to NetworkInvoke using non-global rpc but the Ownership mode is set to something that isn't client!");
                    }
                    NetworkClient sender = NetworkServer.GetClient(OwnerClientID);
                    if(sender == null)
                    {
                        throw new InvalidOperationException("Attempted to NetworkInvoke using non-global rpc but the Owner client by ID is not found!");
                    }
                    NetworkManager.NetworkInvoke(this, sender, methodName, args);
                }
            }
            else if(NetworkManager.WhereAmI == ClientLocation.Local)
            {
                Log.GlobalWarning("Trying to call a server-only method on the client. in this case, this is fine, but this may be uintended.");
                if (UnityNetworkManager.GameNetworkClient == null)
                {
                    throw new InvalidOperationException("Attempted to networkinvoke using a client when the game client is not set!");
                }
                else
                {
                    NetworkManager.NetworkInvoke(this, UnityNetworkManager.GameNetworkClient, methodName, args);
                }
            }
        }
    }
}
