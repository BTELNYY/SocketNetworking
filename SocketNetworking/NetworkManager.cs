using SocketNetworking.Attributes;
using SocketNetworking.Exceptions;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using SocketNetworking.Misc;

namespace SocketNetworking
{
    public class NetworkManager
    {
        public static readonly Type[] AcceptedMethodArugments = new Type[]
        {
            typeof(CustomPacket),
            typeof(NetworkClient),
        };

        public static readonly Dictionary<int, Type> AdditionalPacketTypes = new Dictionary<int, Type>();

        private static readonly Dictionary<Type, int> _packetCache = new Dictionary<Type, int>();

        private static List<Type> _dynamicAllocatedPackets = new List<Type>();

        public static bool IsDynamicAllocatedPacket(Type packetType)
        {
            return _packetCache.ContainsKey(packetType);
        }

        public static Dictionary<int, string> PacketPairsSerialized
        {
            get
            {
                Dictionary<int, string> dict = new Dictionary<int, string>();
                foreach (int i in AdditionalPacketTypes.Keys)
                {
                    dict.Add(i, AdditionalPacketTypes[i].FullName);
                }
                return dict;
            }
        }

        public static int GetAutoPacketID(CustomPacket packet)
        {
            if (_packetCache.ContainsKey(packet.GetType()))
            {
                return _packetCache[packet.GetType()];
            }
            else
            {
                int newId = AdditionalPacketTypes.Keys.GetFirstEmptySlot();
                if (AdditionalPacketTypes.ContainsKey(newId) && AdditionalPacketTypes[newId] == packet.GetType())
                {
                    throw new CustomPacketCollisionException(newId, packet.GetType(), AdditionalPacketTypes[newId].GetType());
                }
                _packetCache.Add(packet.GetType(), newId);
                _dynamicAllocatedPackets.Add(packet.GetType());
                //AdditionalPacketTypes.Add(newId, packet.GetType());
                return newId;
            }
        }

        /// <summary>
        /// Determines where the current application is running. 
        /// </summary>
        public static ClientLocation WhereAmI
        {
            get
            {
                if (NetworkServer.Active)
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
                    if (NetworkClient.Clients.Any(x => x.CurrnetClientLocation == ClientLocation.Remote))
                    {
                        Log.GlobalError("There are active remote clients even though the server is closed, these clients will now be terminated.");
                        foreach (var x in NetworkClient.Clients)
                        {
                            if (x.CurrnetClientLocation == ClientLocation.Local)
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
            ImportCustomPackets(types);
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
                    Log.GlobalWarning($"Custom packet {packet.GetType().Name} does not implement attribute {nameof(PacketDefinition)} it will be ignored.");
                    continue;
                }
                if (AdditionalPacketTypes.ContainsKey(packet.CustomPacketID))
                {
                    if (AdditionalPacketTypes[packet.CustomPacketID].GetType() == packet.GetType())
                    {
                        Log.GlobalWarning("Trying to register a duplicate packet. Type: " + packet.GetType().FullName);
                        return;
                    }
                    else
                    {
                        throw new CustomPacketCollisionException(packet.CustomPacketID, AdditionalPacketTypes[packet.CustomPacketID], packet.GetType());
                    }
                }
                Log.GlobalInfo($"Adding custom packet with ID {packet.CustomPacketID} and name {packet.GetType().Name}");
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
                    if (AdditionalPacketTypes[customPacketId] == type)
                    {
                        Log.GlobalWarning("Trying to register a duplicate packet. Type: " + type.FullName);
                        return;
                    }
                    else
                    {
                        throw new CustomPacketCollisionException(customPacketId, AdditionalPacketTypes[customPacketId], type);
                    }
                }
                Log.GlobalInfo($"Adding custom packet with ID {customPacketId} and name {type.Name}");
                AdditionalPacketTypes.Add(customPacketId, type);
            }
        }

        /// <summary>
        /// Gets the type of the custom packet based on the ID
        /// </summary>
        /// <param name="id">
        /// The <see cref="int"/> ID of the custom Packet
        /// </param>
        /// <returns>
        /// The Custom Packets <see cref="Type"/> or <see cref="null"/> if the ID is unused.
        /// </returns>
        public static Type GetCustomPacketByID(int id)
        {
            if (AdditionalPacketTypes.ContainsKey(id))
            {
                return AdditionalPacketTypes[id];
            }
            else
            {
                return null;
            }
        }


        private static readonly Dictionary<INetworkObject, NetworkObjectData> NetworkObjects = new Dictionary<INetworkObject, NetworkObjectData>();

        public static List<INetworkObject> GetNetworkObjects()
        {
            return NetworkObjects.Keys.ToList();
        }

        public static void SendReadyPulse(NetworkClient sender, bool isReady)
        {
            foreach (INetworkObject @object in NetworkObjects.Keys)
            {
                @object.OnReady(sender, isReady);
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
            if (ids.Count == 0)
            {
                return 1;
            }
            int id = ids.GetFirstEmptySlot();
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
            if (networkObject.NetworkID == 0)
            {
                Log.GlobalError($"Network Object {networkObject.GetType().Name} was ignored becuase NetworkID 0 is reserved. Please choose another ID.");
                return false;
            }
            if (NetworkObjects.ContainsKey(networkObject))
            {
                Log.GlobalWarning("Tried to add network object that already exists.");
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
                Log.GlobalError($"Network Object {networkObject.GetType().Name} was ignored becuase NetworkID 0 is reserved. Please choose another ID.");
                return false;
            }
            if (!NetworkObjects.ContainsKey(networkObject))
            {
                Log.GlobalWarning("Tried to modify network object that does not exist.");
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
                Log.GlobalError($"Network Object {networkObject.GetType().Name} was ignored becuase NetworkID 0 is reserved. Please choose another ID.");
                return false;
            }
            if (!NetworkObjects.ContainsKey(networkObject))
            {
                Log.GlobalWarning("Tried to remove NetworObject that doesn't exist.");
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
            foreach (INetworkObject netObj in networkObjects)
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
            if (target.NetworkID == 0)
            {
                Log.GlobalError($"Network Object {target.GetType().Name} was ignored becuase NetworkTargetID 0 is reserved.");
                return new NetworkObjectData()
                {
                    Listeners = new Dictionary<Type, List<PacketListenerData>>(),
                    TargetObject = target
                };
            }
            foreach (MethodInfo method in allPacketListeners)
            {
                if (method.GetParameters().Length < AcceptedMethodArugments.Length)
                {
                    Log.GlobalWarning("Method " + method.Name + " was ignored becuase it doesn't have the proper amount of arguments.");
                    continue;
                }
                bool methodArgsFailed = false;
                for (int i = 0; i < AcceptedMethodArugments.Length; i++)
                {
                    Type methodType = method.GetParameters()[i].ParameterType;
                    Type acceptedType = AcceptedMethodArugments[i];
                    if (!methodType.IsSubclassOf(acceptedType))
                    {
                        Log.GlobalWarning($"Method {method.Name} doesn't accept the correct paramters, it has been ignored. Note that the correct paramaters are: {string.Join(",", AcceptedMethodArugments.Select(x => x.Name))}");
                        methodArgsFailed = true;
                    }
                }
                if (methodArgsFailed == true)
                {
                    continue;
                }
                PacketListener attribute = method.GetCustomAttribute<PacketListener>();
                PacketListenerData data = new PacketListenerData
                {
                    Attribute = attribute,
                    AttachedMethod = method
                };
                //Log.Debug($"Add method: {method.Name}, Listens for: {attribute.DefinedType.Name}, From Direction: {attribute.DefinedDirection}");
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
                Log.GlobalError("Unknown Custom packet. ID: " + header.CustomPacketID);
                return;
            }
            Type packetType = AdditionalPacketTypes[header.CustomPacketID];
            Packet packet = (Packet)Activator.CreateInstance(AdditionalPacketTypes[header.CustomPacketID]);
            ByteReader reader = packet.Deserialize(data);
            if (reader.ReadBytes < header.Size)
            {
                Log.GlobalWarning($"Packet with ID {header.CustomPacketID} was not fully consumed, the header specified a length which was greater then what was read.");
            }
            object changedPacket = Convert.ChangeType(packet, packetType);
            if (header.NetworkIDTarget == 0)
            {
                //Log.Debug("Handle Client-Client communication!");
                MethodInfo[] clientMethods = runningClient.GetType().GetMethods().ToArray();
                foreach (MethodInfo method in clientMethods)
                {
                    PacketListener listener = method.GetCustomAttribute<PacketListener>();
                    if (listener == null)
                    {
                        continue;
                    }
                    PacketDirection packetDirection = listener.DefinedDirection;
                    Type listenerType = listener.DefinedType;
                    if (packetType != listenerType)
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
            if (objects.Count == 0)
            {
                Log.GlobalWarning("Target NetworkID revealed no active objects registered!");
                return;
            }
            //This may look not very effecient, but you arent checking EVERY possible object, only the ones which match the TargetID.
            //The other way I could do this is by making a nested dictionary hell hole, but I dont want to do that.
            foreach (INetworkObject netObj in objects)
            {
                if (!NetworkObjects[netObj].Listeners.ContainsKey(packetType) && objects.Count == 1)
                {
                    Log.GlobalWarning($"Can't find any listeners for packet type: {packetType.Name} in object type: {netObj.GetType().Name}, it is also the only object for this NetworkID that is enabled.");
                    return;
                }
                else if (!NetworkObjects[netObj].Listeners.ContainsKey(packetType) && objects.Count > 1)
                {
                    continue;
                }
                List<PacketListenerData> packetListeners = NetworkObjects[netObj].Listeners[packetType];
                //Log.Debug($"Packet listeners for type {netObj.GetType().Name}: {packetListeners.Count}");
                foreach (PacketListenerData packetListener in packetListeners)
                {
                    if (packetListener.Attribute.DefinedDirection == PacketDirection.Any)
                    {
                        packetListener.AttachedMethod.Invoke(netObj, new object[] { changedPacket, runningClient });
                        continue;
                    }
                    if (packetListener.Attribute.DefinedDirection == PacketDirection.Client && clientLocation == ClientLocation.Remote)
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

        private static List<int> NetworkInvocations = new List<int>();

        private static List<NetworkInvocationResultPacket> Results = new List<NetworkInvocationResultPacket>();

        public static event Action<NetworkInvocationResultPacket> OnNetworkInvocationResult;

        public static bool IsReady(int callBack, out NetworkInvocationResultPacket packet)
        {
            if (Results.Where(x => x.CallbackID == callBack).Count() == 0)
            {
                packet = null;
                return false;
            }
            else
            {
                packet = Results.Where(x => x.CallbackID == callBack).FirstOrDefault();
                return true;
            }
        }

        public static NetworkInvocationResultPacket ConsumeNetworkInvocationResult(int callback)
        {
            if (!IsReady(callback, out NetworkInvocationResultPacket packet))
            {
                Log.GlobalWarning("Attempted to get not ready network invocation result with callback ID: " + callback);
                return null;
            }
            Results.Remove(packet);
            return packet;
        }

        internal static void NetworkInvoke(NetworkInvocationResultPacket packet, NetworkClient reciever)
        {
            if (packet.IgnoreResult)
            {
                return;
            }
            Results.Add(packet);
            OnNetworkInvocationResult?.Invoke(packet);
        }

        internal static object NetworkInvoke(NetworkInvocationPacket packet, NetworkClient reciever)
        {
            Assembly assmebly = Assembly.Load(packet.TargetTypeAssmebly);
            Type targetType = assmebly.GetType(packet.TargetType);
            if (targetType == null)
            {
                throw new NetworkInvocationException($"Cannot find type: '{packet.TargetType}'.", new NullReferenceException());
            }
            object target = reciever;
            if (packet.NetworkObjectTarget != 0)
            {
                target = NetworkObjects.Keys.Where(x => x.NetworkID == packet.NetworkObjectTarget && x.GetType() == targetType).First();
            }
            if (target == null)
            {
                throw new NetworkInvocationException($"Unable to find the NetworkObject this packet is referencing.");
            }
            Type[] arguments = packet.Arguments.Select(x => Type.GetType(x.TypeFullName)).ToArray();
            MethodInfo[] methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.GetCustomAttribute<NetworkInvocable>() != null && x.Name == packet.MethodName).ToArray();
            MethodInfo method = null;
            string[] expectedArgs = arguments.Select(x => x.FullName).ToArray();
            foreach (MethodInfo m in methods)
            {
                string[] m_args = m.GetParameters().Select(y => y.ParameterType.FullName).ToArray();
                if (m_args.ArraysEqual(expectedArgs))
                {
                    method = m;
                    break;
                }
            }
            if (method == null)
            {
                throw new NetworkInvocationException($"Cannot find method: '{packet.MethodName}'.", new NullReferenceException());
            }
            List<object> args = new List<object>();
            foreach (SerializedData data in packet.Arguments)
            {
                object obj = NetworkConvert.Deserialize(data, out int read);
                args.Add(obj);
            }
            object result = method.Invoke(target, args.ToArray());
            NetworkInvocations.Remove(packet.NetworkObjectTarget);
            NetworkInvocationResultPacket resultPacket = new NetworkInvocationResultPacket();
            resultPacket.CallbackID = packet.CallbackID;
            resultPacket.Result = NetworkConvert.Serialize(result);
            resultPacket.IgnoreResult = packet.IgnoreResult;
            reciever.Send(resultPacket);
            return result;
        }

        public static T NetworkInvoke<T>(object target, NetworkClient sender, string methodName, object[] args, float msTimeOut = 5000)
        {
            if (target == null)
            {
                throw new NetworkInvocationException($"Unable to find the NetworkObject this packet is referencing.", new ArgumentNullException("target"));
            }
            int targetID = 0;
            if (target is INetworkObject networkObject)
            {
                targetID = networkObject.NetworkID;
            }
            if (!(target is NetworkClient client))
            {
                throw new NetworkInvocationException($"Provided type is not allowed. Type: {target.GetType().FullName}", new ArgumentException("Can't cast to NetworkClient."));
            }
            Type targetType = target.GetType();
            if (targetType == null)
            {
                throw new NetworkInvocationException($"Cannot find type: '{target.GetType()}'.", new NullReferenceException());
            }
            Type[] arguments = args.Select(x => x.GetType()).ToArray();
            MethodInfo[] methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.GetCustomAttribute<NetworkInvocable>() != null && x.Name == methodName).ToArray();
            MethodInfo method = null;
            string[] expectedArgs = arguments.Select(x => x.FullName).ToArray();
            foreach (MethodInfo m in methods)
            {
                string[] m_args = m.GetParameters().Select(y => y.ParameterType.FullName).ToArray();
                if (m_args.ArraysEqual(expectedArgs))
                {
                    method = m;
                    break;
                }
            }
            if (method.ReturnType != typeof(T))
            {
                throw new NetworkInvocationException("Cannot invoke method, return type is incorrect.", new InvalidCastException());
            }
            //x.GetParameters().Select(y => y.ParameterType).ToHashSet() == arguments.ToHashSet()
            if (method == null)
            {
                throw new NetworkInvocationException($"Cannot find method: '{methodName}'.", new NullReferenceException());
            }
            NetworkInvocationPacket packet = new NetworkInvocationPacket();
            packet.TargetTypeAssmebly = Assembly.GetAssembly(targetType).GetName().FullName;
            packet.NetworkObjectTarget = targetID;
            packet.MethodName = methodName;
            foreach (var arg in args)
            {
                SerializedData data = NetworkConvert.Serialize(arg);
                packet.Arguments.Add(data);
            }
            packet.TargetType = targetType.FullName;
            int callbackID = NetworkInvocations.GetFirstEmptySlot();
            NetworkInvocations.Add(callbackID);
            packet.CallbackID = callbackID;
            sender.Send(packet);
            if (method.ReturnType == typeof(void))
            {
                return default;
            }
            Stopwatch stopwatch = Stopwatch.StartNew();
            NetworkResultAwaiter networkResultAwaiter = new NetworkResultAwaiter(callbackID);
            while (!networkResultAwaiter.HasResult)
            {
                if (!client.IsConnected)
                {
                    Log.GlobalError($"NetworkInvoke on method {methodName} failed becuase the NetworkClient is not connected.");
                    break;
                }
                if(stopwatch.ElapsedMilliseconds > msTimeOut)
                {
                    Log.GlobalError($"NetworkInvoke on method {methodName} timed out after {msTimeOut}ms of proccessing.");
                    break;
                }
            }
            NetworkInvocationResultPacket resultPacket = networkResultAwaiter.ResultPacket;
            if (!resultPacket.Success)
            {
                Log.GlobalError($"NetworkInvoke on method {methodName} failed remotely! Error: " + resultPacket.ErrorMessage);
                return default;
            }
            object result = NetworkConvert.Deserialize(resultPacket.Result, out int read);
            if (result == null)
            {
                return default;
            }
            else
            {
                return (T)result;
            }
        }

        public static int NetworkInvoke(object target, NetworkClient sender, string methodName, object[] args, bool ignoreResult = false)
        {
            if (target == null)
            {
                throw new NetworkInvocationException($"Unable to find the NetworkObject this packet is referencing.", new ArgumentNullException("target"));
            }
            int targetID = 0;
            if (target is INetworkObject networkObject)
            {
                targetID = networkObject.NetworkID;
            }
            if (!(target is NetworkClient client))
            {
                throw new NetworkInvocationException($"Provided type is not allowed. Type: {target.GetType().FullName}", new ArgumentException("Can't cast to NetworkClient."));
            }
            Type targetType = target.GetType();
            if (targetType == null)
            {
                throw new NetworkInvocationException($"Cannot find type: '{target.GetType()}'.", new NullReferenceException());
            }
            Type[] arguments = args.Select(x => x.GetType()).ToArray();
            MethodInfo[] methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.GetCustomAttribute<NetworkInvocable>() != null && x.Name == methodName).ToArray();
            MethodInfo method = null;
            string[] expectedArgs = arguments.Select(x => x.FullName).ToArray();
            foreach (MethodInfo m in methods)
            {
                string[] m_args = m.GetParameters().Select(y => y.ParameterType.FullName).ToArray();
                if (m_args.ArraysEqual(expectedArgs))
                {
                    method = m;
                    break;
                }
            }
            //x.GetParameters().Select(y => y.ParameterType).ToHashSet() == arguments.ToHashSet()
            if (method == null)
            {
                throw new NetworkInvocationException($"Cannot find method: '{methodName}'.", new NullReferenceException());
            }
            NetworkInvocationPacket packet = new NetworkInvocationPacket();
            packet.TargetTypeAssmebly = Assembly.GetAssembly(targetType).GetName().FullName;
            packet.NetworkObjectTarget = targetID;
            packet.MethodName = methodName;
            foreach (var arg in args)
            {
                SerializedData data = NetworkConvert.Serialize(arg);
                packet.Arguments.Add(data);
            }
            packet.TargetType = targetType.FullName;
            int callbackID = NetworkInvocations.GetFirstEmptySlot();
            NetworkInvocations.Add(callbackID);
            packet.CallbackID = callbackID;
            sender.Send(packet);
            return callbackID;
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
