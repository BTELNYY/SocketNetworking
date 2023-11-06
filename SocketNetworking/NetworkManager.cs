using SocketNetworking.Attributes;
using SocketNetworking.Exceptions;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking
{
    public static class NetworkManager
    {
        private static Dictionary<int, Type> AdditionalPacketTypes = new Dictionary<int, Type>();

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


        private static List<INetworkObject> NetworkObjects = new List<INetworkObject>();


        /// <summary>
        /// Adds a <see cref="INetworkObject"/> to the list of objects which we check the methods of for the <see cref="PacketListener"/> attribute. This will add it to the list of all objects. 
        /// </summary>
        /// <param name="networkObject">
        /// An instance of the a class which implements the <see cref="INetworkObject"/> interface
        /// </param>
        public static void AddNetworkObject(INetworkObject networkObject)
        {
            if (NetworkObjects.Contains(networkObject))
            {
                Log.Warning("Tried to add network object that already exists.");
                return;
            }
            else
            {
                NetworkObjects.Add(networkObject);
            }
        }

        /// <summary>
        /// Default action to trigger all registered <see cref="INetworkObject"/>'s <see cref="PacketListener"/> methods.
        /// </summary>
        /// <param name="header">
        /// The <see cref="PacketHeader"/> of the recieved packet.
        /// </param>
        /// <param name="data">
        /// The data of the packet
        /// </param>
        /// <param name="clientLocation">
        /// The <see cref="ClientLocation"/> from which this function is being called.
        /// </param>
        public static void TriggerPacketListeners(PacketHeader header, byte[] data, ClientLocation clientLocation)
        {
            TriggerPacketListeners(header, data, NetworkObjects, clientLocation);
        }

        /// <summary>
        /// Exposed method to allow force updating of <see cref="INetworkObject"/>s
        /// </summary>
        /// <param name="header">
        /// The <see cref="PacketHeader"/> of the packet you wish to trigger a send of.
        /// </param>
        /// <param name="data">
        /// A <see cref="byte[]"/> of the data of that packet. Note that it is the full data, do not trim out the header.
        /// </param>
        /// <param name="objects">
        /// List of <see cref="INetworkObject"/>s you wish to update the packets
        /// </param>
        public static void TriggerPacketListeners(PacketHeader header, byte[] data, List<INetworkObject> objects, ClientLocation clientLocation)
        {
            objects = objects.Where(x => x.NetworkID == header.NetworkIDTarget).ToList();
            if (!AdditionalPacketTypes.ContainsKey(header.CustomPacketID))
            {
                Log.Error("Unknown Custom packet. ID: " + header.CustomPacketID);
                return;
            }
            Type packetType = AdditionalPacketTypes[header.CustomPacketID];
            Packet packet = (Packet)Activator.CreateInstance(AdditionalPacketTypes[header.CustomPacketID]);
            packet.Deserialize(data);
            List<MethodInfo> methods = new List<MethodInfo>();
            //This may look not very effecient, but you arent checking EVERY possible object, only the ones which match the TargetID.
            //The other way I could do this is by making a nested dictionary hell hole, but I dont want to do that.
            foreach (INetworkObject netObj in objects)
            {
                Type typeOfObject = netObj.GetType();
                MethodInfo[] allPacketListeners = typeOfObject.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(x => x.GetCustomAttribute(typeof(PacketListener)) != null).ToArray();
                List<MethodInfo> validMethods = new List<MethodInfo>();
                foreach (MethodInfo method in allPacketListeners)
                {
                    PacketListener listener = (PacketListener)method.GetCustomAttribute(typeof(PacketListener));
                    if (listener.DefinedType != packetType)
                    {
                        continue;
                    }
                    if (listener.DefinedDirection == PacketDirection.Any)
                    {
                        validMethods.Add(method);
                        continue;
                    }
                    if (listener.DefinedDirection == PacketDirection.Client && clientLocation != ClientLocation.Remote)
                    {
                        continue;
                    }
                    if (listener.DefinedDirection == PacketDirection.Server && clientLocation != ClientLocation.Local)
                    {
                        continue;
                    }
                    validMethods.Add(method);
                }
                foreach (MethodInfo method in validMethods)
                {
                    method.Invoke(netObj, new object[] { packet });
                }
            }
        }
    }
}
