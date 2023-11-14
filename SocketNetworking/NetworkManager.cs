using SocketNetworking.Attributes;
using SocketNetworking.Exceptions;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SocketNetworking
{
    public static class NetworkManager
    {
        private static readonly Type[] AcceptedMethodArugments = new Type[]
        {
            typeof(CustomPacket),
            typeof(NetworkClient),
        };

        private static readonly Dictionary<int, Type> AdditionalPacketTypes = new Dictionary<int, Type>();

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
            if (NetworkObjects.ContainsKey(networkObject))
            {
                Log.Warning("Tried to add network object that already exists.");
                return false;
            }
            else
            {
                NetworkObjectData data = GetNetworkObjectData(networkObject);
                NetworkObjects.Add(networkObject, data);
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
            if (!NetworkObjects.ContainsKey(networkObject))
            {
                Log.Warning("Tried to remove NetworObject that doesn't exist.");
                return false;
            }
            else
            {
                NetworkObjects.Remove(networkObject);
                return true;
            }
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
            List<INetworkObject> objects = NetworkObjects.Keys.Where(x => x.NetworkID == header.NetworkIDTarget && x.IsActive).ToList();
            Type packetType = AdditionalPacketTypes[header.CustomPacketID];
            Packet packet = (Packet)Activator.CreateInstance(AdditionalPacketTypes[header.CustomPacketID]);
            packet.Deserialize(data);
            object changedPacket = Convert.ChangeType(packet, packetType);
            //This may look not very effecient, but you arent checking EVERY possible object, only the ones which match the TargetID.
            //The other way I could do this is by making a nested dictionary hell hole, but I dont want to do that.
            foreach (INetworkObject netObj in objects)
            {
                Type typeOfObject = netObj.GetType();
                List<PacketListenerData> packetListeners = NetworkObjects[netObj].Listeners[packetType];
                foreach(PacketListenerData packetListener in packetListeners)
                {
                    if(packetListener.Attribute.DefinedDirection == PacketDirection.Any)
                    {
                        packetListener.AttachedMethod.Invoke(netObj, new object[] { changedPacket, runningClient });
                        continue;
                    }
                    if(packetListener.Attribute.DefinedDirection == PacketDirection.Client && clientLocation == ClientLocation.Local)
                    {
                        packetListener.AttachedMethod.Invoke(netObj, new object[] { changedPacket, runningClient });
                        continue;
                    }
                    if (packetListener.Attribute.DefinedDirection == PacketDirection.Server && clientLocation == ClientLocation.Remote)
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
