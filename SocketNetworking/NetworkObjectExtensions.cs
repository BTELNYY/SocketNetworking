using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.Client;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Server;
using SocketNetworking.Shared;

namespace SocketNetworking
{
    public static class NetworkObjectExtensions
    {
        public static void NetworkSetOwner(this INetworkObject obj, int ownerId)
        {
            ObjectManagePacket packet = new ObjectManagePacket()
            {
                Action = ObjectManagePacket.ObjectManageAction.Modify,
                AssmeblyName = obj.GetType().Assembly.FullName,
                OwnerID = ownerId,
                ObjectClassName = obj.GetType().FullName,
                OwnershipMode = obj.OwnershipMode,
                ObjectVisibilityMode = obj.ObjectVisibilityMode,
            };
            if (NetworkManager.WhereAmI == ClientLocation.Local)
            {
                NetworkClient.LocalClient.Send(packet);
            }
            else if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                switch (obj.ObjectVisibilityMode)
                {
                    case ObjectVisibilityMode.ServerOnly:
                        break;
                    case ObjectVisibilityMode.OwnerAndServer:
                        NetworkClient client = NetworkServer.Clients.FirstOrDefault(x => x.ClientID == obj.OwnerClientID);
                        if (client == null)
                        {
                            throw new InvalidOperationException($"Can't find client with ID {obj.OwnerClientID}.");
                        }
                        client.Send(packet);
                        NetworkClient newOwner = NetworkClient.Clients.FirstOrDefault(x => x.ClientID == ownerId);
                        if(newOwner == null)
                        {
                            throw new InvalidOperationException($"Can't find client with ID {newOwner}.");
                        }
                        newOwner.Send(packet);
                        break;
                    case ObjectVisibilityMode.Everyone:
                        NetworkServer.SendToAll(packet, obj);
                        break;
                }
            }
            obj.OwnerClientID = ownerId;
        }

        public static void NetworkSetOwnershipMode(this INetworkObject obj, OwnershipMode mode)
        {
            ObjectManagePacket packet = new ObjectManagePacket()
            {
                Action = ObjectManagePacket.ObjectManageAction.Modify,
                AssmeblyName = obj.GetType().Assembly.FullName,
                OwnerID = obj.OwnerClientID,
                ObjectClassName = obj.GetType().FullName,
                OwnershipMode = mode,
                ObjectVisibilityMode = obj.ObjectVisibilityMode,
            };
            if(NetworkManager.WhereAmI == ClientLocation.Local)
            {
                NetworkClient.LocalClient.Send(packet);
            }
            else if(NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                switch(obj.ObjectVisibilityMode)
                {
                    case ObjectVisibilityMode.ServerOnly:
                        break;
                    case ObjectVisibilityMode.OwnerAndServer:
                        NetworkClient client = NetworkServer.Clients.FirstOrDefault(x => x.ClientID == obj.OwnerClientID);
                        if(client == null)
                        {
                            throw new InvalidOperationException($"Can't find client with ID {obj.OwnerClientID}.");
                        }
                        client.Send(packet);
                        break;
                    case ObjectVisibilityMode.Everyone:
                        NetworkServer.SendToAll(packet, obj);
                        break;
                }
            }
            obj.OwnershipMode = mode;
        }

        public static void NetworkSetVisibility(this INetworkObject obj, ObjectVisibilityMode mode)
        {
            ObjectManagePacket packet = new ObjectManagePacket()
            {
                Action = ObjectManagePacket.ObjectManageAction.Modify,
                AssmeblyName = obj.GetType().Assembly.FullName,
                OwnerID = obj.OwnerClientID,
                ObjectClassName = obj.GetType().FullName,
                OwnershipMode = obj.OwnershipMode,
                ObjectVisibilityMode = mode,
            };
            if(mode == obj.ObjectVisibilityMode)
            {
                return;
            }
            if(mode == ObjectVisibilityMode.ServerOnly)
            {
                throw new InvalidOperationException("You must destroy the object, do not change its visibility to server only!");
            }
            if (NetworkManager.WhereAmI == ClientLocation.Local)
            {
                NetworkClient.LocalClient.Send(packet);
            }
            else if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                switch (obj.ObjectVisibilityMode)
                {
                    case ObjectVisibilityMode.ServerOnly:
                        Log.GlobalWarning("You should not be changing the visibility of a Network object from server. Please spawn the object first.");
                        break;
                    case ObjectVisibilityMode.OwnerAndServer:
                        NetworkClient client = NetworkServer.Clients.FirstOrDefault(x => x.ClientID == obj.OwnerClientID);
                        if (client == null)
                        {
                            throw new InvalidOperationException($"Can't find client with ID {obj.OwnerClientID}.");
                        }
                        client.Send(packet);
                        break;
                    case ObjectVisibilityMode.Everyone:
                        NetworkServer.SendToAll(packet, obj);
                        break;
                }
            }
            obj.ObjectVisibilityMode = mode;
        }
    }
}
