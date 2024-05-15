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
        /// Sets the objects <see cref="NetworkID"/>.
        /// </summary>
        /// <param name="id">
        /// The new ID to set the <see cref="NetworkID"/> to.
        /// </param>
        public void SetNetworkID(int id)
        {
            _netId = id;
            OnObjectUpdateNetworkIDLocal(id);
            if (NetworkManager.IsRegistered(this))
            {
                NetworkManager.ModifyNetworkID(this);
            }
            else
            {
                RegisterListener();
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

        internal void UpdateOwnerClientId(int id)
        {
            _ownerClientID = id;
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

        internal void UpdateOwnershipMode(OwnershipMode mode)
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
        /// Ensures the current script is registered as a packet listener.
        /// </summary>
        public virtual void RegisterListener()
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
            RegisterListener();
        }
    }
}
