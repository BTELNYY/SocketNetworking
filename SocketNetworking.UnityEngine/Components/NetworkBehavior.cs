using SocketNetworking;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkBehavior : MonoBehaviour, INetworkObject
    {
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

        public virtual void OnAdded(INetworkObject addedObject)
        {
            
        }

        public virtual void OnRemoved(INetworkObject removedObject)
        {

        }

        public virtual void OnDisconnected(NetworkClient client)
        {
            
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
