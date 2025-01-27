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

        public virtual bool IsEnabled => base.enabled;

        public virtual bool Spawnable => true;

        public virtual ObjectVisibilityMode ObjectVisibilityMode { get; set; }

        public virtual bool AllowPublicModification => false;

        public virtual int OwnerClientID { get; set; }

        public virtual OwnershipMode OwnershipMode { get; set; }

        public virtual int NetworkID { get; set; }

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

        public virtual void OnNetworkSpawned(NetworkClient spawner)
        {

        }

        public virtual void OnLocalSpawned(ObjectManagePacket packet)
        {

        }

        public virtual void RecieveExtraData(byte[] extraData)
        {

        }

        public virtual byte[] SendExtraData()
        {
            return new byte[0];
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
                if(NetworkClient.LocalClient == null)
                {
                    throw new InvalidOperationException("Attempted to networkinvoke using a client when the game client is not set!");
                }
                else
                {
                    NetworkManager.NetworkInvoke(this, NetworkClient.LocalClient, methodName, args);
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
                if (NetworkClient.LocalClient == null)
                {
                    throw new InvalidOperationException("Attempted to networkinvoke using a client when the game client is not set!");
                }
                else
                {
                    NetworkManager.NetworkInvoke(this, NetworkClient.LocalClient, methodName, args);
                }
            }
        }
    }
}
