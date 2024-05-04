using SocketNetworking;
using SocketNetworking.UnityEngine.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Attributes;
using SocketNetworking.PacketSystem;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkObject : NetworkBehavior
    {
        /// <summary>
        /// Checks if the current point (where the code is executed) is the sync owner. If Sync ownership is ignored, always returns true.
        /// </summary>
        public virtual bool IsSyncOwner
        {
            get
            {
                if(NetworkID == -1) return false;
                if(IgnoreSyncOwner) return true;
                return SyncOwner == NetworkManager.WhereAmI;
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
                if(IgnoreSyncOwner)
                {
                    return true;
                }
                return SyncOwner != NetworkManager.WhereAmI;
            }
        }

        /// <summary>
        /// If set to true, the client/server will completely ignore the SyncOwner
        /// </summary>
        public bool IgnoreSyncOwner = false;

        /// <summary>
        /// Gets the network ID of the object. if a <see cref="NetworkIdentity"/> (or any subclass of it) is present, returns its NetworkID.
        /// </summary>
        public sealed override int NetworkID
        {
            get
            {
                if(Identity == null)
                {
                    return base.NetworkID;
                }
                else
                {
                    return Identity.NetworkID;
                }
            }
        }

        private NetworkIdentity _identity;

        /// <summary>
        /// All objects should have a <see cref="NetworkIdentity"/> attached to them or referenced somehow. This is not a hard coded requirement, but is suggested for larger systems (e.g. the player uses this to prevent having to make 80 NetIDs for one thing)
        /// </summary>
        public NetworkIdentity Identity
        {
            get
            {
                return _identity;
            }
            set
            {
                if(value == null)
                {
                    Logger.Error("Can't set null NetworkIdentity!");
                    return;
                }
                if(_identity != value)
                {
                    if(_identity != null)
                    {
                        _identity.UnregisterObject(this);
                    }
                    _identity = value;
                    _identity.RegisterObject(this);
                }
                else
                {
                    _identity = value;
                }
                SetNetworkID(value.NetworkID);
            }
        }

        void Awake()
        {
            if(this is NetworkIdentity selfId)
            {
                Identity = selfId;
            }
            NetworkIdentity identity = GetComponent<NetworkIdentity>();
            if(Identity == null)
            {
                if(identity != null)
                {
                    Identity = identity;
                }
                else
                {
                    Logger.Warning("Can't find Identity attached to this object!");
                    SetNetworkID(-1);
                    return;
                }
            }
        }

        /// <summary>
        /// Who should be allowed to send sync data?
        /// </summary>
        public ClientLocation SyncOwner = ClientLocation.Remote;

        /// <summary>
        /// If set to false, the client/server will be lenient when checking who should be sending packets
        /// </summary>
        public bool DoStrictMode = true;

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
            if(NetworkID == -1)
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
                if(UnityNetworkManager.GameNetworkClient != null)
                {
                    UnityNetworkManager.GameNetworkClient.Send(packet, target);
                }
                else
                {
                    Logger.Warning("Current GameNetworkClient is null!");
                }
                return;
            }
            Logger.Error("Can't find where I am!");
        }
    }
}
