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

        public virtual ByteReader RecieveExtraData(byte[] extraData)
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

        public virtual bool ShouldBeReceivingPacketsFrom(NetworkClient client)
        {
            if (OwnershipMode == OwnershipMode.Server && client.CurrnetClientLocation == ClientLocation.Local)
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

        /// <summary>
        /// Preforms a send operation and syncs data across network, can be called on either client or server, method handles what happens.
        /// </summary>
        /// <param name="packet"></param>
        public virtual void SendPacket(Packet packet)
        {
            SendPacket(packet, this);
        }

        /// <summary>
        /// Preforms a send operation and syncs data across network, can be called on either client or server, method handles what happens.
        /// </summary>
        /// <param name="packet"></param>
        public virtual void SendPacket(Packet packet, INetworkObject target)
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
                    NetworkClient.LocalClient.Send(packet, target);
                }
                else
                {
                    Logger.Warning("Current GameNetworkClient is null!");
                }
                return;
            }
            Logger.Error("Can't find where I am!");
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
