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
using SocketNetworking.Server;
using SocketNetworking.Client;
using SocketNetworking.Shared;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkObject : NetworkBehavior
    {
        /// <summary>
        /// Checks if the current point (where the code is executed) is the sync owner. If Sync ownership is ignored, always returns true.
        /// </summary>
        public virtual bool IsOwner
        {
            get
            {
                if(OwnershipMode == OwnershipMode.Public)
                {
                    return true;
                }
                else if(OwnershipMode == OwnershipMode.Server && NetworkManager.WhereAmI == ClientLocation.Remote)
                {
                    return true;
                }
                else if(OwnershipMode == OwnershipMode.Client && OwnerClientID == UnityNetworkManager.GameNetworkClient.ClientID)
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
            if(OwnershipMode == OwnershipMode.Server && client.CurrnetClientLocation == ClientLocation.Local)
            {
                return true;
            }
            if(OwnershipMode == OwnershipMode.Client && OwnerClientID == client.ClientID)
            {
                return true;
            }
            if(OwnershipMode == OwnershipMode.Public)
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
