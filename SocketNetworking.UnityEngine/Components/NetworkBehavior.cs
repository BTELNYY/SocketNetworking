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

        public virtual void OnObjectDestroyed(NetworkClient client)
        {
            
        }

        /// <summary>
        /// Called when the network changes a NetworkID.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="newNetID"></param>
        public virtual void OnObjectUpdateNetworkIDSynced(NetworkClient client, int newNetID)
        {
            
        }
        
        /// <summary>
        /// Called when <see cref="SetNetworkID(int)"/> is called.
        /// </summary>
        /// <param name="newNetID"></param>
        public virtual void OnObjectUpdateNetworkIDLocal(int newNetID)
        {

        }

        public virtual void OnObjectCreationComplete(NetworkClient client)
        {
            
        }

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
