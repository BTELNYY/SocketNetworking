using SocketNetworking.Attributes;
using SocketNetworking.Exceptions;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;

namespace SocketNetworking
{
    public class NetworkManager
    {
        private static readonly Type[] AcceptedMethodArugments = new Type[]
        {
            typeof(CustomPacket),
            typeof(NetworkClient),
        };

        private static readonly Dictionary<int, Type> AdditionalPacketTypes = new Dictionary<int, Type>();

        /// <summary>
        /// Determines where the current application is running. 
        /// </summary>
        public static ClientLocation WhereAmI
        {
            get
            {
                if(NetworkServer.Active)
                {
                    if (NetworkClient.Clients.Where(x => x.CurrnetClientLocation == ClientLocation.Local).Count() == 0)
                    {
                        return ClientLocation.Remote;
                    }
                    if (NetworkClient.Clients.Where(x => x.CurrnetClientLocation == ClientLocation.Local).Count() != 0)
                    {
                        return ClientLocation.Unknown;
                    }
                }
                else
                {
                    if(NetworkClient.Clients.Any(x => x.CurrnetClientLocation == ClientLocation.Remote))
                    {
                        Log.Error("There are active remote clients even though the server is closed, these clients will now be terminated.");
                        foreach(var x in NetworkClient.Clients)
                        {
                            if(x.CurrnetClientLocation == ClientLocation.Local)
                            {
                                continue;
                            }
                            x.StopClient();
                        }
                    }
                    if (NetworkClient.Clients.Any())
                    {
                        return ClientLocation.Local;
                    }
                }
                return ClientLocation.Unknown;
            }
        }


        /// <summary>
        /// Scans the provided assembly for all types with the <see cref="PacketDefinition"/> Attribute, then loads them into a dictionary so that the library can call methods on your netowrk objects.
        /// </summary>
        /// <param name="assmebly">
        /// The <see cref="Assembly"/> which to scan.
        /// </param>
        /// <exception cref="CustomPacketCollisionException">
        /// Thrown when 2 or more packets collide by attempting to register themselves on the same PacketID.
        /// </exception>
        public static void ImportCustomPackets(Assembly assmebly)
        {
            List<Type> types = assmebly.GetTypes().Where(x => x.IsSubclassOf(typeof(CustomPacket))).ToList();
            types = types.Where(x => x.GetCustomAttribute(typeof(PacketDefinition)) != null).ToList();
            foreach (Type type in types)
            {
                CustomPacket packet = (CustomPacket)Activator.CreateInstance(type);
                int customPacketId = packet.CustomPacketID;
                if (AdditionalPacketTypes.ContainsKey(customPacketId))
                {
                    throw new CustomPacketCollisionException(customPacketId, AdditionalPacketTypes[customPacketId], type);
                }
                Log.Info($"Adding custom packet with ID {customPacketId} and name {type.Name}");
                AdditionalPacketTypes.Add(customPacketId, type);
            }
        }

        /// <summary>
        /// Adds a list of custom packets to the additional packets dictionary. Note that all packets provided must be instances and they must all have the <see cref="PacketDefinition"/> attribute.
        /// </summary>
        /// <param name="packets">
        /// List of <see cref="CustomPacket"/>s to add.
        /// </param>
        /// <exception cref="CustomPacketCollisionException"></exception>
        public static void ImportCustomPackets(List<CustomPacket> packets)
        {
            foreach (CustomPacket packet in packets)
            {
                if (packet.GetType().GetCustomAttribute(typeof(PacketDefinition)) == null)
                {
                    Log.Warning($"Custom packet {packet.GetType().Name} does not implement attribute {nameof(PacketDefinition)} it will be ignored.");
                    continue;
                }
                if (AdditionalPacketTypes.ContainsKey(packet.CustomPacketID))
                {
                    throw new CustomPacketCollisionException(packet.CustomPacketID, AdditionalPacketTypes[packet.CustomPacketID], packet.GetType());
                }
                Log.Info($"Adding custom packet with ID {packet.CustomPacketID} and name {packet.GetType().Name}");
                AdditionalPacketTypes.Add(packet.CustomPacketID, packet.GetType());
            }
        }

        /// <summary>
        /// Adds packets from type list to the additional packets dictionary. Note that all packets must inherit from <see cref="CustomPacket"/> and have the <see cref="PacketDefinition"/> attribute.
        /// </summary>
        /// <param name="importedTypes">
        /// List of <see cref="Type"/>s which meet criteria (Inherit from <see cref="CustomPacket"/> and have the <see cref="PacketDefinition"/> attribute) to add.
        /// </param>
        /// <exception cref="CustomPacketCollisionException"></exception>
        public static void ImportCustomPackets(List<Type> importedTypes)
        {
            List<Type> types = importedTypes.Where(x => x.IsSubclassOf(typeof(CustomPacket))).ToList();
            types = types.Where(x => x.GetCustomAttribute(typeof(PacketDefinition)) != null).ToList();
            foreach (Type type in types)
            {
                CustomPacket packet = (CustomPacket)Activator.CreateInstance(type);
                int customPacketId = packet.CustomPacketID;
                if (AdditionalPacketTypes.ContainsKey(customPacketId))
                {
                    throw new CustomPacketCollisionException(customPacketId, AdditionalPacketTypes[customPacketId], type);
                }
                Log.Info($"Adding custom packet with ID {customPacketId} and name {type.Name}");
                AdditionalPacketTypes.Add(customPacketId, type);
            }
        }

        private static readonly Dictionary<INetworkObject, NetworkObjectData> NetworkObjects = new Dictionary<INetworkObject, NetworkObjectData>();

        public static void SendReadyPulse(NetworkClient sender, bool isReady)
        {
            foreach(INetworkObject @object in NetworkObjects.Keys)
            {
                @object.OnReady(sender, isReady);
            }
        }

        public static void SendObjectCreationCompletePulse(NetworkClient client, int netID)
        {
            foreach(INetworkObject networkObject in NetworkObjects.Keys.Where(x => x.NetworkID == netID))
            {
                networkObject.OnObjectCreationComplete(client);
            }
        }

        public static void SendObjectDestroyedPulse(NetworkClient client, int netID)
        {
            foreach (INetworkObject networkObject in NetworkObjects.Keys.Where(x => x.NetworkID == netID))
            {
                networkObject.OnObjectDestroyed(client);
            }
        }

        public static void SendAddedPulse(INetworkObject addedObject)
        {
            foreach (INetworkObject networkObject in NetworkObjects.Keys.Where(x => x.NetworkID == addedObject.NetworkID))
            {
                networkObject.OnAdded(addedObject);
            }
        }

        public static void SendRemovedPulse(INetworkObject removedObject)
        {
            foreach (INetworkObject networkObject in NetworkObjects.Keys.Where(x => x.NetworkID == removedObject.NetworkID))
            {
                networkObject.OnRemoved(removedObject);
            }
        }

        public static void SendDisconnectedPulse(NetworkClient networkClient)
        {
            foreach (INetworkObject @object in NetworkObjects.Keys)
            {
                @object.OnDisconnected(networkClient);
            }
        }

        public static void SendUpdateNetIDPulse(NetworkClient client, int oldID, int newID)
        {
            foreach(INetworkObject @object in NetworkObjects.Keys.Where(x => x.NetworkID == oldID))
            {
                @object.OnObjectUpdateNetworkIDSynced(client, newID);
            }
        }


        /// <summary>
        /// Determines if the given <see cref="INetworkObject"/> is registered.
        /// </summary>
        /// <param name="networkObject">
        /// The <see cref="INetworkObject"/> to check for.
        /// </param>
        /// <returns>
        /// <see cref="true"/> if network object is registered, <see cref="false"/> if it isn't.
        /// </returns>
        public static bool IsRegistered(INetworkObject networkObject)
        {
            return NetworkObjects.ContainsKey(networkObject);
        }

        /// <summary>
        /// Returns the next available unused NetworkID
        /// </summary>
        /// <returns>
        /// The Network ID which hasn't been used before.
        /// </returns>
        public static int GetNextNetworkID()
        {
            List<int> ids = NetworkObjects.Keys.Select(x => x.NetworkID).ToList();
            if(ids.Count == 0)
            {
                return 1;
            }
            int id = ids.Where(x => !ids.Contains(x + 1)).First();
            if(id == 0)
            {
                return 1;
            }
            return id;
        }

        /// <summary>
        /// Adds a <see cref="INetworkObject"/> to the list of objects which we check the methods of for the <see cref="PacketListener"/> attribute. This will add it to the list of all objects. 
        /// </summary>
        /// <param name="networkObject">
        /// An instance of the a class which implements the <see cref="INetworkObject"/> interface
        /// </param>
        /// <returns>
        /// A <see cref="bool"/> which shows if the method succeeded or not. 
        /// </returns>
        public static bool AddNetworkObject(INetworkObject networkObject)
        {
            if(networkObject.NetworkID == 0)
            {
                Log.Error($"Network Object {networkObject.GetType().Name} was ignored becuase NetworkID 0 is reserved. Please choose another ID.");
                return false;
            }
            if (NetworkObjects.ContainsKey(networkObject))
            {
                Log.Warning("Tried to add network object that already exists.");
                return false;
            }
            else
            {
                NetworkObjectData data = GetNetworkObjectData(networkObject);
                NetworkObjects.Add(networkObject, data);
                SendAddedPulse(networkObject);
                return true;
            }
        }

        public static bool ModifyNetworkID(INetworkObject networkObject)
        {
            if (networkObject.NetworkID == 0)
            {
                Log.Error($"Network Object {networkObject.GetType().Name} was ignored becuase NetworkID 0 is reserved. Please choose another ID.");
                return false;
            }
            if (!NetworkObjects.ContainsKey(networkObject))
            {
                Log.Warning("Tried to modify network object that does not exist.");
                return false;
            }
            else
            {
                NetworkObjectData data = GetNetworkObjectData(networkObject);
                NetworkObjects[networkObject] = data;
                return true;
            }
        }

        /// <summary>
        /// Removes a <see cref="INetworkObject"/> from the list of registered objects.
        /// </summary>
        /// <param name="networkObject">
        /// The <see cref="INetworkObject"/> to remove.
        /// </param>
        /// <returns>
        /// A <see cref="bool"/> which shows if the method succeeded or not. 
        /// </returns>
        public static bool RemoveNetworkObject(INetworkObject networkObject)
        {
            if (networkObject.NetworkID == 0)
            {
                Log.Error($"Network Object {networkObject.GetType().Name} was ignored becuase NetworkID 0 is reserved. Please choose another ID.");
                return false;
            }
            if (!NetworkObjects.ContainsKey(networkObject))
            {
                Log.Warning("Tried to remove NetworObject that doesn't exist.");
                return false;
            }
            else
            {
                NetworkObjects.Remove(networkObject);
                SendRemovedPulse(networkObject);
                return true;
            }
        }

        /// <summary>
        /// Removes any <see cref="INetworkObject"/> which match the ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>
        /// How many objects are removed.
        /// </returns>
        public static int RemoveAllNetworkObjectsByID(int id)
        {
            List<INetworkObject> networkObjects = NetworkObjects.Keys.Where(x => x.NetworkID == id).ToList();
            int removed = 0;
            foreach(INetworkObject netObj in networkObjects)
            {
                if (RemoveNetworkObject(netObj))
                {
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>
        /// Finds all <see cref="PacketListener"/>s in a <see cref="INetworkObject"/> and creates a <see cref="NetworkObjectData"/> instance.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static NetworkObjectData GetNetworkObjectData(INetworkObject target)
        {
            Type typeOfObject = target.GetType();
            MethodInfo[] allPacketListeners = typeOfObject.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x => x.GetCustomAttribute(typeof(PacketListener)) != null).ToArray();
            Dictionary<Type, List<PacketListenerData>> result = new Dictionary<Type, List<PacketListenerData>>();
            if(target.NetworkID == 0)
            {
                Log.Error($"Network Object {target.GetType().Name} was ignored becuase NetworkTargetID 0 is reserved.");
                return new NetworkObjectData()
                {
                    Listeners = new Dictionary<Type, List<PacketListenerData>>(),
                    TargetObject = target
                };
            }
            foreach(MethodInfo method in allPacketListeners)
            {
                if(method.GetParameters().Length < AcceptedMethodArugments.Length)
                {
                    Log.Warning("Method " + method.Name + " was ignored becuase it doesn't have the proper amount of arguments.");
                    continue;
                }
                bool methodArgsFailed = false;
                for (int i = 0; i < AcceptedMethodArugments.Length; i++)
                {
                    Type methodType = method.GetParameters()[i].ParameterType;
                    Type acceptedType = AcceptedMethodArugments[i];
                    if(!methodType.IsSubclassOf(acceptedType))
                    {
                        Log.Warning($"Method {method.Name} doesn't accept the correct paramters, it has been ignored. Note that the correct paramaters are: {string.Join(",", AcceptedMethodArugments.Select(x => x.Name))}");
                        methodArgsFailed = true;
                    }
                }
                if(methodArgsFailed == true)
                {
                    continue;
                }
                PacketListener attribute = method.GetCustomAttribute<PacketListener>();
                PacketListenerData data = new PacketListenerData
                {
                    Attribute = attribute,
                    AttachedMethod = method
                };
                Log.Debug($"Add method: {method.Name}, Listens for: {attribute.DefinedType.Name}, From Direction: {attribute.DefinedDirection}");
                if (result.ContainsKey(attribute.DefinedType))
                {
                    result[attribute.DefinedType].Add(data);
                }
                else
                {
                    result.Add(attribute.DefinedType, new List<PacketListenerData> { data });
                }
            }
            NetworkObjectData networkObjectData = new NetworkObjectData
            {
                Listeners = result,
                TargetObject = target
            };
            return networkObjectData;
        }


        /// <summary>
        /// Updates all packet listeners.
        /// </summary>
        /// <param name="header">
        /// The <see cref="PacketHeader"/> of the packet you wish to trigger a send of.
        /// </param>
        /// <param name="data">
        /// A <see cref="byte[]"/> of the data of that packet. Note that it is the full data, do not trim out the header.
        /// </param>
        /// <param name="runningClient">
        /// A reference to a <see cref="NetworkClient"/> which ran this method.
        /// </param>
        public static void TriggerPacketListeners(PacketHeader header, byte[] data, NetworkClient runningClient)
        {
            ClientLocation clientLocation = runningClient.CurrnetClientLocation;
            if (!AdditionalPacketTypes.ContainsKey(header.CustomPacketID))
            {
                Log.Error("Unknown Custom packet. ID: " + header.CustomPacketID);
                return;
            }
            Type packetType = AdditionalPacketTypes[header.CustomPacketID];
            Packet packet = (Packet)Activator.CreateInstance(AdditionalPacketTypes[header.CustomPacketID]);
            ByteReader reader = packet.Deserialize(data);
            if(reader.ReadBytes < header.Size)
            {
                Log.Warning($"Packet with ID {header.CustomPacketID} was not fully consumed, the header specified a length which was greater then what was read.");
            }
            object changedPacket = Convert.ChangeType(packet, packetType);
            if (header.NetworkIDTarget == 0)
            {
                Log.Debug("Handle Client-Client communication!");
                MethodInfo[] clientMethods = runningClient.GetType().GetMethods().ToArray();
                foreach(MethodInfo method in clientMethods)
                { 
                    PacketListener listener = method.GetCustomAttribute<PacketListener>();
                    if(listener == null)
                    {
                        continue;
                    }
                    PacketDirection packetDirection = listener.DefinedDirection;
                    Type listenerType = listener.DefinedType;
                    if(packetType != listenerType)
                    {
                        //Log.Debug("Type dont match!");
                        continue;
                    }
                    //Log.Debug("Checking packet direction");
                    //Log.Debug("Direction of client: " + clientLocation + " Listener direction: " + packetDirection);
                    if (packetDirection == PacketDirection.Any)
                    {
                        //Log.Debug("Invoking " + method.Name);
                        method.Invoke(runningClient, new object[] { changedPacket, runningClient });
                        continue;
                    }
                    if (packetDirection == PacketDirection.Client && clientLocation == ClientLocation.Remote)
                    {
                        //Log.Debug("Invoking " + method.Name);
                        method.Invoke(runningClient, new object[] { changedPacket, runningClient });
                        continue;
                    }
                    if (packetDirection == PacketDirection.Server && clientLocation == ClientLocation.Local)
                    {
                        //Log.Debug("Invoking " + method.Name);
                        method.Invoke(runningClient, new object[] { changedPacket, runningClient });
                        continue;
                    }
                }
                return;
            }
            List<INetworkObject> objects = NetworkObjects.Keys.Where(x => x.NetworkID == header.NetworkIDTarget && x.IsEnabled).ToList();
            if(objects.Count == 0)
            {
                Log.Warning("Target NetworkID revealed no active objects registered!");
                return;
            }
            //This may look not very effecient, but you arent checking EVERY possible object, only the ones which match the TargetID.
            //The other way I could do this is by making a nested dictionary hell hole, but I dont want to do that.
            foreach (INetworkObject netObj in objects)
            {
                if (!NetworkObjects[netObj].Listeners.ContainsKey(packetType) && objects.Count == 1)
                {
                    Log.Warning($"Can't find any listeners for packet type: {packetType.Name} in object type: {netObj.GetType().Name}, it is also the only object for this NetworkID that is enabled.");
                    return;
                }
                else if(!NetworkObjects[netObj].Listeners.ContainsKey(packetType) && objects.Count > 1)
                {
                    continue;
                }
                List<PacketListenerData> packetListeners = NetworkObjects[netObj].Listeners[packetType];
                Log.Debug($"Packet listeners for type {netObj.GetType().Name}: {packetListeners.Count}");
                foreach(PacketListenerData packetListener in packetListeners)
                {
                    if(packetListener.Attribute.DefinedDirection == PacketDirection.Any)
                    {
                        packetListener.AttachedMethod.Invoke(netObj, new object[] { changedPacket, runningClient });
                        continue;
                    }
                    if(packetListener.Attribute.DefinedDirection == PacketDirection.Client && clientLocation == ClientLocation.Remote)
                    {
                        packetListener.AttachedMethod.Invoke(netObj, new object[] { changedPacket, runningClient });
                        continue;
                    }
                    if (packetListener.Attribute.DefinedDirection == PacketDirection.Server && clientLocation == ClientLocation.Local)
                    {
                        packetListener.AttachedMethod.Invoke(netObj, new object[] { changedPacket, runningClient });
                        continue;
                    }
                }
            }
        }


    }

    public struct NetworkObjectData
    {
        public Dictionary<Type, List<PacketListenerData>> Listeners;

        public INetworkObject TargetObject;
    }

    public struct PacketListenerData
    {
        public PacketListener Attribute;

        public MethodInfo AttachedMethod;
    }
}
