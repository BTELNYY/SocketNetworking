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
        /// <summary>
        /// Invokes a network method on the current <see cref="INetworkObject"></see>, provide a <see cref="NetworkClient"/> which will get the network invoke.
        /// </summary>
        /// <param name="object"></param>
        /// <param name="sender"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        public static void NetworkInvoke(this INetworkObject @object, NetworkClient sender, string methodName, object[] args)
        {
            NetworkManager.NetworkInvoke(@object, sender, methodName, args);
        }

        /// <summary>
        /// Invokes a method by name and awaits for a return. This is a blocking operation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="sender"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        /// <param name="timeOutMs">
        /// How long before the system should time out and fail?
        /// </param>
        /// <returns></returns>
        public static T NetworkInvoke<T>(this INetworkObject obj, NetworkClient sender, string methodName, object[] args, float timeOutMs = 5000f)
        {
            return NetworkManager.NetworkInvoke<T>(obj, sender, methodName, args, timeOutMs);
        }

        /// <summary>
        /// Invokes a method. Note that this can only be run on the server, as not NetworkClient is provided. Also note that this will work as all client RPC, meaning every client will get the invocation.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="methodName"></param>
        /// <param name="args"></param>
        public static void NetworkInvoke(this INetworkObject obj, string methodName, object[] args)
        {
            NetworkServer.NetworkInvokeOnAll(obj, methodName, args);
        }

        /// <summary>
        /// Spawns a <see cref="INetworkSpawnable"/> on all clients.
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void NetworkSpawn(this INetworkSpawnable obj)
        {
            if (NetworkManager.WhereAmI == ClientLocation.Local)
            {
                throw new InvalidOperationException("Only servers can spawn objects this way.");
            }
            ObjectManagePacket packet = new ObjectManagePacket()
            {
                Action = ObjectManagePacket.ObjectManageAction.Create,
                AssmeblyName = obj.GetType().Assembly.FullName,
                ObjectClassName = obj.GetType().FullName,
                ExtraData = obj.SendExtraData(),
            };
            NetworkServer.SendToAll(packet);
            obj.OnLocalSpawned(packet);
        }

        public static void NetworkDestroy(this INetworkObject obj)
        {
            if (NetworkManager.WhereAmI == ClientLocation.Local)
            {
                throw new InvalidOperationException("Only servers can spawn objects this way.");
            }
            ObjectManagePacket packet = new ObjectManagePacket()
            {
                Action = ObjectManagePacket.ObjectManageAction.Destroy,
                AssmeblyName = obj.GetType().Assembly.FullName,
                OwnerID = obj.OwnerClientID,
                ObjectClassName = obj.GetType().FullName,
                NewNetworkID = obj.NetworkID,
                OwnershipMode = obj.OwnershipMode,
                ObjectVisibilityMode = obj.ObjectVisibilityMode,
            };
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
                    break;
                case ObjectVisibilityMode.Everyone:
                    NetworkServer.SendToAll(packet, obj);
                    break;
            }
            obj.OnServerDestroy();
            obj.Destroy();
        }

        /// <summary>
        /// Spawns the <see cref="INetworkObject"/>.
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void NetworkSpawn(this INetworkObject obj)
        {
            if(!obj.Spawnable)
            {
                throw new InvalidOperationException("This object is not spawnable.");
            }
            if(NetworkManager.WhereAmI == ClientLocation.Local)
            {
                throw new InvalidOperationException("Only servers can spawn objects this way.");
            }
            ObjectManagePacket packet = new ObjectManagePacket()
            {
                Action = ObjectManagePacket.ObjectManageAction.Create,
                AssmeblyName = obj.GetType().Assembly.FullName,
                OwnerID = obj.OwnerClientID,
                ObjectClassName = obj.GetType().FullName,
                NewNetworkID = obj.NetworkID,
                OwnershipMode = obj.OwnershipMode,
                ObjectVisibilityMode = obj.ObjectVisibilityMode,
                ExtraData = obj.SendExtraData(),
            };
            if(obj.ObjectVisibilityMode == ObjectVisibilityMode.ServerOnly)
            {
                packet.ObjectVisibilityMode = ObjectVisibilityMode.Everyone;
                obj.ObjectVisibilityMode = ObjectVisibilityMode.Everyone;
            }
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
                    break;
                case ObjectVisibilityMode.Everyone:
                    NetworkServer.SendToAll(packet, obj);
                    break;
            }
            obj.OnLocalSpawned(packet);
        }

        /// <summary>
        /// Sets the <see cref="INetworkObject.OwnerClientID"/> network wide.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="ownerId"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void NetworkSetOwner(this INetworkObject obj, int ownerId)
        {
            ObjectManagePacket packet = new ObjectManagePacket()
            {
                Action = ObjectManagePacket.ObjectManageAction.Modify,
                AssmeblyName = obj.GetType().Assembly.FullName,
                OwnerID = ownerId,
                ObjectClassName = obj.GetType().FullName,
                NewNetworkID = obj.NetworkID,
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
                        break;
                    case ObjectVisibilityMode.Everyone:
                        NetworkServer.SendToAll(packet, obj);
                        break;
                }
            }
            obj.OwnerClientID = ownerId;
        }

        /// <summary>
        /// Sets the <see cref="INetworkObject.OwnershipMode"/> network wide.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="mode"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void NetworkSetOwnershipMode(this INetworkObject obj, OwnershipMode mode)
        {
            ObjectManagePacket packet = new ObjectManagePacket()
            {
                Action = ObjectManagePacket.ObjectManageAction.Modify,
                AssmeblyName = obj.GetType().Assembly.FullName,
                OwnerID = obj.OwnerClientID,
                ObjectClassName = obj.GetType().FullName,
                NewNetworkID = obj.NetworkID,
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


        /// <summary>
        /// Changes the <see cref="INetworkObject.NetworkID"/> network wide. This is a dangerous operation, as if you change this and refer to the old ID, the peer will fail to find the <see cref="INetworkObject"/>.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="newId"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void NetworkSetID(this INetworkObject obj, int newId)
        {
            ObjectManagePacket packet = new ObjectManagePacket()
            {
                Action = ObjectManagePacket.ObjectManageAction.Modify,
                AssmeblyName = obj.GetType().Assembly.FullName,
                OwnerID = obj.OwnerClientID,
                ObjectClassName = obj.GetType().FullName,
                NewNetworkID = newId,
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
                        break;
                    case ObjectVisibilityMode.Everyone:
                        NetworkServer.SendToAll(packet, obj);
                        break;
                }
            }
            obj.NetworkID = newId;
        }

        /// <summary>
        /// Sets the <see cref="INetworkObject.ObjectVisibilityMode"/> network wide. If the <see cref="INetworkObject.ObjectVisibilityMode"/> is <see cref="ObjectVisibilityMode.ServerOnly"/>, this method will fail. Use <see cref="NetworkSpawn(INetworkObject)"/>.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="mode"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void NetworkSetVisibility(this INetworkObject obj, ObjectVisibilityMode mode)
        {
            ObjectManagePacket packet = new ObjectManagePacket()
            {
                Action = ObjectManagePacket.ObjectManageAction.Modify,
                AssmeblyName = obj.GetType().Assembly.FullName,
                OwnerID = obj.OwnerClientID,
                ObjectClassName = obj.GetType().FullName,
                OwnershipMode = obj.OwnershipMode,
                NewNetworkID = obj.NetworkID,
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
