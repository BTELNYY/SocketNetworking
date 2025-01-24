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
using System.Security;
using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.Server;
using System.Runtime.CompilerServices;

namespace SocketNetworking.Shared
{
    public class NetworkManager
    {
        public static readonly Type[] AcceptedMethodArugments = new Type[]
        {
            typeof(CustomPacket),
            typeof(NetworkHandle),
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

        public static NetworkDirection WhereAmIDirection
        {
            get
            {
                return LocationToDirection(WhereAmI);
            }
        }

        public static NetworkDirection LocationToDirection(ClientLocation location)
        {
            return (NetworkDirection)location;
        }

        public static Dictionary<Type, NetworkObjectCache> TypeCache = new Dictionary<Type, NetworkObjectCache>();

        public static Dictionary<Type, Type> TypeToTypeWrapper = new Dictionary<Type, Type>();

        /// <summary>
        /// Imports the target assembly, caching: <see cref="ITypeWrapper{T}"/>s, any methods with <see cref="NetworkInvocable"/> (which are on a class with <see cref="INetworkObject"/> implemented) and any <see cref="CustomPacket"/>s  
        /// </summary>
        /// <param name="target"></param>
        public static void ImportAssmebly(Assembly target)
        {
            ImportCustomPackets(target);
            List<Type> applicableTypes = target.GetTypes().ToList();
            foreach (Type t in applicableTypes)
            {
                if (TypeCache.ContainsKey(t) || TypeToTypeWrapper.ContainsValue(t))
                {
                    continue;
                }
                Type baseType = t.BaseType;
                if (t.IsSubclassDeep(typeof(TypeWrapper<>)))
                {
                    object obj = Activator.CreateInstance(t);
                    MethodInfo method = obj.GetType().GetMethod(nameof(TypeWrapper<object>.GetContainedType));
                    Type targetType = (Type)method.Invoke(obj, null);
                    if (!TypeToTypeWrapper.ContainsKey(targetType))
                    {
                        TypeToTypeWrapper.Add(targetType, t);
                    }
                }
                NetworkObjectCache networkObjectCache = new NetworkObjectCache();
                networkObjectCache.Target = t;
                networkObjectCache.Invokables = new List<(MethodInfo, NetworkInvocable)>();
                networkObjectCache.Listeners = new Dictionary<Type, List<PacketListenerData>>();
                networkObjectCache.SyncVars = new List<INetworkSyncVar>();
                foreach (MethodInfo method in t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (method.GetCustomAttribute<PacketListener>() != null)
                    {
                        if (method.GetParameters().Length < AcceptedMethodArugments.Length)
                        {
                            Log.GlobalWarning("Method " + method.Name + " was ignored becuase it doesn't have the proper amount of arguments");
                        }
                        else
                        {
                            bool methodArgsFailed = false;
                            for (int i = 0; i < AcceptedMethodArugments.Length; i++)
                            {
                                Type methodType = method.GetParameters()[i].ParameterType;
                                Type acceptedType = AcceptedMethodArugments[i];
                                if (!methodType.IsSubclassDeep(acceptedType))
                                {
                                    Log.GlobalWarning($"Method {method.Name} doesn't accept the correct paramters, it has been ignored. Note that the correct paramaters are: {string.Join(",", AcceptedMethodArugments.Select(x => x.Name))}");
                                    methodArgsFailed = true;
                                }
                            }
                            if (!methodArgsFailed)
                            {
                                PacketListener attribute = method.GetCustomAttribute<PacketListener>();
                                PacketListenerData data = new PacketListenerData
                                {
                                    Attribute = attribute,
                                    AttachedMethod = method
                                };
                                if (!networkObjectCache.Listeners.ContainsKey(attribute.DefinedType))
                                {
                                    networkObjectCache.Listeners.Add(attribute.DefinedType, new List<PacketListenerData>() { data });
                                }
                                else
                                {
                                    if (!networkObjectCache.Listeners[attribute.DefinedType].Contains(data))
                                    {
                                        networkObjectCache.Listeners[attribute.DefinedType].Add(data);
                                    }
                                }
                            }
                        }
                    }
                    if (method.GetCustomAttribute<NetworkInvocable>() != null)
                    {
                        ValueTuple<MethodInfo, NetworkInvocable> tuple = (method, method.GetCustomAttribute<NetworkInvocable>());
                        if (networkObjectCache.Invokables.Contains(tuple))
                        {
                            Log.GlobalWarning($"Tried to cache duplicate! Type: {t.FullName}, Method: {method.Name}");
                            continue;
                        }
                        networkObjectCache.Invokables.Add(tuple);
                    }
                }
                List<INetworkSyncVar> syncVars = new List<INetworkSyncVar>();
                FieldInfo[] fields = t.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                foreach (FieldInfo field in fields)
                {
                    object value = field.GetValue(t);
                    if (!(value is INetworkSyncVar syncVar))
                    {
                        continue;
                    }
                    if (string.IsNullOrEmpty(syncVar.Name))
                    {
                        syncVar.Name = field.Name;
                    }
                    syncVars.Add(syncVar);
                }
                //PropertyInfo[] properties = t.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                //foreach (PropertyInfo property in properties)
                //{
                //    object value = property.GetValue(t);
                //    if (!(value is INetworkSyncVar syncVar))
                //    {
                //        continue;
                //    }
                //    if (string.IsNullOrEmpty(syncVar.Name))
                //    {
                //        syncVar.Name = property.Name;
                //    }
                //    syncVars.Add(syncVar);
                //}
                networkObjectCache.SyncVars = syncVars;
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

        #region Network Objects

        private static readonly Dictionary<INetworkObject, NetworkObjectData> NetworkObjects = new Dictionary<INetworkObject, NetworkObjectData>();

        private static readonly List<NetworkObjectSpawner> NetworkObjectSpawners = new List<NetworkObjectSpawner>();

        public delegate INetworkSpawnable NetworkObjectSpawnerDelegate(ObjectManagePacket packet, NetworkHandle handle);

        public static bool RegisterSpawner(Type type, NetworkObjectSpawnerDelegate spawner, bool allowSubclasses)
        {
            if(NetworkObjectSpawners.Any(x => x.TargetType == type))
            {
                Log.GlobalError("Can't register that spawner: The type is already registered.");
                return false;
            }
            NetworkObjectSpawner spawnerStruct = new NetworkObjectSpawner()
            {
                Spawner = spawner,
                AllowSubclasses = allowSubclasses,
                TargetType = type
            };
            NetworkObjectSpawners.Add(spawnerStruct);
            return true;
        }

        public static bool UnregisterSpawner(Type type, NetworkObjectSpawnerDelegate spawnerDelegate)
        {
            NetworkObjectSpawner spawner = NetworkObjectSpawners.FirstOrDefault(x => x.TargetType == type && x.Spawner == spawnerDelegate);
            if (spawner == default(NetworkObjectSpawner))
            {
                Log.GlobalError("Can't unregister that spawner: Not Found");
                return false;
            }
            NetworkObjectSpawners.Remove(spawner);
            return true;
        }

        internal static void ModifyNetworkObjectLocal(ObjectManagePacket packet, NetworkHandle handle)
        {
            INetworkObject @object = NetworkObjects.Keys.FirstOrDefault(x => x.NetworkID == packet.NetowrkIDTarget);
            if (@object == default(INetworkObject) && packet.Action != ObjectManagePacket.ObjectManageAction.Create)
            {
                throw new KeyNotFoundException($"No such Network Object with ID {packet.NetowrkIDTarget}, Did you spawn it yet?");
            }
            if(packet.Action != ObjectManagePacket.ObjectManageAction.Create || packet.Action != ObjectManagePacket.ObjectManageAction.ConfirmCreate || packet.Action != ObjectManagePacket.ObjectManageAction.ConfirmDestroy)
            {
                if (WhereAmI == ClientLocation.Remote)
                {
                    if (@object.OwnershipMode == OwnershipMode.Client && handle.Client.ClientID != @object.OwnerClientID)
                    {
                        throw new SecurityException("Attempted to modify an object you do not have permission over.");
                    }
                    if(@object.OwnershipMode == OwnershipMode.Public && !@object.AllowPublicModification)
                    {
                        throw new SecurityException("Attempted to modify a public object which is not accepting public modification.");
                    }
                    if(@object.OwnershipMode == OwnershipMode.Server)
                    {
                        throw new SecurityException("Attempted to modify a server controlled object.");
                    }
                }
            }

            switch(packet.Action)
            {
                case ObjectManagePacket.ObjectManageAction.Create:
                    Type objType = Assembly.Load(packet.AssmeblyName)?.GetType(packet.ObjectClassName);
                    if(objType == null)
                    {
                        throw new NullReferenceException("Cannot find type by name or assmebly.");
                    }
                    NetworkObjectSpawner objSpawner = null;
                    int bestApproxObj = 0;
                    foreach (NetworkObjectSpawner possibleSpawner in NetworkObjectSpawners)
                    {
                        if(possibleSpawner.AllowSubclasses)
                        {
                            int distance = objType.HowManyClassesUp(possibleSpawner.TargetType);
                            if(distance == -1)
                            {
                                continue;
                            }
                            if (distance < bestApproxObj)
                            {
                                bestApproxObj = distance;
                                objSpawner = possibleSpawner;
                            }
                        }
                        else
                        {
                            if(possibleSpawner.TargetType != objType)
                            {
                                continue;
                            }
                            objSpawner = possibleSpawner;
                            break;
                        }
                    }
                    INetworkObject netObj;
                    if (objSpawner == null)
                    {
                        netObj = (INetworkObject)Activator.CreateInstance(objType);
                    }
                    else
                    {
                        netObj = (INetworkObject)objSpawner.Spawner.Invoke(packet, handle);
                    }
                    if(netObj == null)
                    {
                        throw new NullReferenceException($"Failed to spawn {objType.FullName}");
                    }
                    netObj.NetworkID = packet.NetowrkIDTarget;
                    netObj.OwnerClientID = packet.OwnerID;
                    netObj.OwnershipMode = packet.OwnershipMode;
                    ObjectManagePacket creationConfirmation = new ObjectManagePacket()
                    {
                        NetowrkIDTarget = packet.NetowrkIDTarget,
                        Action = ObjectManagePacket.ObjectManageAction.ConfirmCreate,
                    };
                    AddNetworkObject(netObj);
                    handle.Client.Send(creationConfirmation);
                    netObj.OnLocalSpawned(packet);
                    break;
                case ObjectManagePacket.ObjectManageAction.ConfirmCreate:
                    INetworkObject creationTarget = NetworkObjects.Keys.FirstOrDefault(x => x.NetworkID == packet.NetowrkIDTarget);
                    if(creationTarget == default(INetworkObject))
                    {
                        throw new NullReferenceException($"Can't find the object that was confirmed spawned. ID: {packet.NetowrkIDTarget}");
                    }
                    creationTarget.OnNetworkSpawned(handle.Client);
                    SendCreatedPulse(handle.Client, creationTarget);
                    break;
                case ObjectManagePacket.ObjectManageAction.Destroy:
                    INetworkObject destructionTarget = NetworkObjects.Keys.FirstOrDefault(x => x.NetworkID == packet.NetowrkIDTarget);
                    if (destructionTarget == default(INetworkObject))
                    {
                        throw new NullReferenceException($"Can't find the object that should be destroyed. ID: {packet.NetowrkIDTarget}");
                    }
                    SendDestroyedPulse(handle.Client, destructionTarget);
                    RemoveNetworkObject(destructionTarget);
                    destructionTarget.OnClientDestroy(handle.Client);
                    ObjectManagePacket destroyConfirmPacket = new ObjectManagePacket()
                    {
                        NetowrkIDTarget = destructionTarget.NetworkID,
                        Action = ObjectManagePacket.ObjectManageAction.ConfirmDestroy,
                    };
                    handle.Client.Send(destroyConfirmPacket);
                    destructionTarget.Destroy();
                    break;
                case ObjectManagePacket.ObjectManageAction.ConfirmDestroy:
                    SendDestroyedPulse(handle.Client, null);
                    //Already should have destroyed the object.
                    break;
                case ObjectManagePacket.ObjectManageAction.Modify:
                    INetworkObject modificationTarget = NetworkObjects.Keys.FirstOrDefault(x => x.NetworkID == packet.NetowrkIDTarget);
                    if(modificationTarget == default(INetworkObject))
                    {
                        throw new NullReferenceException($"Can't find the object to modify. ID: {packet.NetowrkIDTarget}");
                    }
                    modificationTarget.OnModify(packet, handle.Client);
                    modificationTarget.NetworkID = packet.NewNetworkID;
                    modificationTarget.OwnerClientID = packet.OwnerID;
                    modificationTarget.ObjectVisibilityMode = packet.ObjectVisibilityMode;
                    modificationTarget.OwnershipMode = packet.OwnershipMode;
                    modificationTarget.OnModified(handle.Client);
                    SendModifiedPulse(handle.Client, modificationTarget);
                    break;
                case ObjectManagePacket.ObjectManageAction.ConfirmModify:
                    INetworkObject modificationConfirmTarget = NetworkObjects.Keys.FirstOrDefault(x => x.NetworkID == packet.NetowrkIDTarget);
                    if (modificationConfirmTarget == default(INetworkObject))
                    {
                        throw new NullReferenceException($"Can't find the object to modify. ID: {packet.NetowrkIDTarget}");
                    }
                    modificationConfirmTarget.OnModified(handle.Client);
                    SendModifiedPulse(handle.Client, modificationConfirmTarget);
                    break;
            }
        }

        public static List<INetworkObject> GetNetworkObjects()
        {
            return NetworkObjects.Keys.ToList();
        }

        public static void SendModifiedPulse(NetworkClient client, INetworkObject modifiedObject)
        {
            foreach (INetworkObject networkObject in NetworkObjects.Keys)
            {
                networkObject.OnModified(modifiedObject, client);
            }
        }

        public static void SendCreatedPulse(NetworkClient client, INetworkObject createdObject)
        {
            foreach (INetworkObject networkObject in NetworkObjects.Keys)
            {
                networkObject.OnCreated(createdObject, client);
            }
        }

        public static void SendDestroyedPulse(NetworkClient client, INetworkObject destroyedObject)
        {
            foreach (INetworkObject networkObject in NetworkObjects.Keys)
            {
                networkObject.OnDestroyed(destroyedObject, client);
            }
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

        public static void SendConnectedPulse(NetworkClient client)
        {
            foreach (INetworkObject @object in NetworkObjects.Keys)
            {
                @object.OnConnected(client);
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
        /// A Network ID which hasn't been used before.
        /// </returns>
        public static int GetNextNetworkObjectID()
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
            MethodInfo[] allPacketListeners = typeOfObject.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).Where(x => x.GetCustomAttribute(typeof(PacketListener)) != null).ToArray();
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
                    if (!methodType.IsSubclassDeep(acceptedType))
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
            List<INetworkSyncVar> syncVars = new List<INetworkSyncVar>();
            FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(target);
                if(!(value is INetworkSyncVar syncVar))
                {
                    continue;
                }
                if(string.IsNullOrEmpty(syncVar.Name))
                {
                    syncVar.Name = field.Name;
                }
                syncVars.Add(syncVar);
            }
            //PropertyInfo[] properties = target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            //foreach (PropertyInfo property in properties)
            //{
            //    object value = property.GetValue(target);
            //    if (!(value is INetworkSyncVar syncVar))
            //    {
            //        continue;
            //    }
            //    if (string.IsNullOrEmpty(syncVar.Name))
            //    {
            //        syncVar.Name = property.Name;
            //    }
            //    syncVars.Add(syncVar);
            //}
            NetworkObjectData networkObjectData = new NetworkObjectData
            {
                Listeners = result,
                TargetObject = target,
                SyncVars = syncVars
            };
            return networkObjectData;
        }

        #endregion

        #region Packet Listeners

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
                Log.GlobalWarning($"Packet with ID {header.CustomPacketID} was not fully consumed, the header specified a length which was greater then what was read. Actual: {reader.ReadBytes}, Header: {header.Size}");
            }
            object changedPacket = Convert.ChangeType(packet, packetType);
            NetworkHandle handle = new NetworkHandle(runningClient, (Packet)changedPacket);
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
                    NetworkDirection packetDirection = listener.DefinedDirection;
                    Type listenerType = listener.DefinedType;
                    if (packetType != listenerType)
                    {
                        //Log.Debug("Type dont match!");
                        continue;
                    }
                    object[] allParams = { changedPacket, runningClient, handle };
                    object[] methodArgs = method.MatchParameters(allParams.ToList()).ToArray();
                    //Log.Debug("Checking packet direction");
                    //Log.Debug("Direction of client: " + clientLocation + " Listener direction: " + packetDirection);
                    if (packetDirection == NetworkDirection.Any)
                    {
                        //Log.Debug("Invoking " + method.Name);
                        method.Invoke(runningClient, methodArgs);
                        continue;
                    }
                    if (packetDirection == NetworkDirection.Client && clientLocation == ClientLocation.Remote)
                    {
                        //Log.Debug("Invoking " + method.Name);
                        method.Invoke(runningClient, methodArgs);
                        continue;
                    }
                    if (packetDirection == NetworkDirection.Server && clientLocation == ClientLocation.Local)
                    {
                        //Log.Debug("Invoking " + method.Name);
                        method.Invoke(runningClient, methodArgs);
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
                    if (packetListener.Attribute.DefinedDirection == NetworkDirection.Any)
                    {
                        packetListener.AttachedMethod.Invoke(netObj, new object[] { changedPacket, runningClient });
                        continue;
                    }
                    if (packetListener.Attribute.DefinedDirection == NetworkDirection.Client && clientLocation == ClientLocation.Remote)
                    {
                        packetListener.AttachedMethod.Invoke(netObj, new object[] { changedPacket, runningClient });
                        continue;
                    }
                    if (packetListener.Attribute.DefinedDirection == NetworkDirection.Server && clientLocation == ClientLocation.Local)
                    {
                        packetListener.AttachedMethod.Invoke(netObj, new object[] { changedPacket, runningClient });
                        continue;
                    }
                }
            }
        }

        #endregion

        #region Sync Vars

        //being called externally
        internal static void UpdateSyncVarsInternal(SyncVarUpdatePacket packet, NetworkClient runner)
        {
            foreach(SyncVarData data in packet.Data)
            {
                if(!(NetworkObjects.Keys.FirstOrDefault(x => x.NetworkID == data.NetworkIDTarget) is INetworkObject obj))
                {
                    Log.GlobalWarning($"No such Network Object with ID '{data.NetworkIDTarget}'");
                    continue;
                }
                NetworkObjectData networkObjectData = NetworkObjects[obj];
                if(!(networkObjectData.SyncVars.FirstOrDefault(x => x.Name == data.TargetVar) is INetworkSyncVar syncVar))
                {
                    Log.GlobalWarning($"No such Network Sync Var '{data.TargetVar}' on object {obj.GetType().FullName}");
                    continue;
                }
                if(syncVar.SyncOwner != OwnershipMode.Public)
                {
                    if(syncVar.SyncOwner == OwnershipMode.Client && WhereAmI == ClientLocation.Remote)
                    {
                        if(runner.ClientID != syncVar.OwnerObject.OwnerClientID)
                        {
                            return;
                        }
                    }
                }
                object value = NetworkConvert.Deserialize(data.Data, out int read);
                syncVar.RawSet(value, runner);
                Log.GlobalDebug($"Updated {syncVar.Name} on {obj.GetType().FullName}. Read {read} bytes as the value.");
            }
        }

        #endregion

        #region Network Invoke

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

        private static MethodInfo GetNetworkInvokeMethod(MethodInfo[] methods, Type[] arguments)
        {
            string[] expectedArgs = arguments.Select(x => x.FullName).ToArray();
            foreach (MethodInfo m in methods)
            {
                List<string> m_args = m.GetParameters().Select(y => y.ParameterType.FullName).ToList();
                if (m.GetParameters()[0].ParameterType.IsSubclassDeep(typeof(NetworkClient)) && !arguments[0].IsSubclassDeep(typeof(NetworkClient)))
                {
                    m_args.RemoveAt(0);
                }
                if (m_args.ToArray().ArraysEqual(expectedArgs))
                {
                    return m;
                }
            }
            return null;
        }

        internal static void NetworkInvoke(NetworkInvocationResultPacket packet, NetworkClient reciever)
        {
            if (packet.IgnoreResult)
            {
                //return;
            }
            if (!packet.Success)
            {
                Log.GlobalError("Network Invocation failed. Error: " + packet.ErrorMessage);
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
            List<object> targets = new List<object>();
            if (packet.NetworkObjectTarget != 0)
            {
                List<INetworkObject> netObjs = NetworkObjects.Keys.Where(x => x.NetworkID == packet.NetworkObjectTarget && x.GetType() == targetType).ToList();
                if (netObjs.Count != 0)
                {
                    targets.AddRange(netObjs);
                }
            }
            else if (targetType.IsSubclassDeep(typeof(NetworkClient)))
            {
                targets.Add(target);
            }
            if (targets.Count == 0)
            {
                throw new NetworkInvocationException("Unable to find any networkobjects with ID: " + packet.NetworkObjectTarget);
            }
            if (target == null)
            {
                throw new NetworkInvocationException($"Unable to find the Object this packet is referencing.");
            }
            Type[] arguments = packet.Arguments.Select(x => x.Type).ToArray();
            MethodInfo[] methods = targetType.GetMethodsDeep(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.GetCustomAttribute<NetworkInvocable>() != null && x.Name == packet.MethodName).ToArray();
            MethodInfo method = GetNetworkInvokeMethod(methods, arguments);
            if (method == null)
            {
                throw new NetworkInvocationException($"Cannot find method: '{packet.MethodName}' in type: {targetType.FullName}, Methods: {string.Join("\n", methods.Select(x => x.ToString()))}", new NullReferenceException());
            }
            NetworkInvocable invocable = method.GetCustomAttribute<NetworkInvocable>();
            if (invocable.Direction == NetworkDirection.Server && reciever.CurrnetClientLocation == ClientLocation.Remote)
            {
                throw new SecurityException("Attempted to invoke network method from incorrect direction.");
            }
            if (invocable.Direction == NetworkDirection.Client && reciever.CurrnetClientLocation == ClientLocation.Local)
            {
                throw new SecurityException("Attempted to invoke network method from incorrect direction.");
            }
            if (invocable.SecureMode && reciever.CurrnetClientLocation != ClientLocation.Local)
            {
                if (target is NetworkClient client && client.ClientID != reciever.ClientID)
                {
                    throw new SecurityException("Attempted to invoke network method which the client does not own.");
                }
                if (target is INetworkObject owned)
                {
                    if (owned.OwnershipMode == OwnershipMode.Client && owned.OwnerClientID != reciever.ClientID)
                    {
                        throw new SecurityException("Attempted to invoke network method which the client does not own.");
                    }
                    else if (owned.OwnershipMode == OwnershipMode.Server && reciever.CurrnetClientLocation != ClientLocation.Local)
                    {
                        throw new SecurityException("Attempted to invoke network method which the client does not own.");
                    }
                    else if (owned.OwnershipMode == OwnershipMode.Public)
                    {
                        //do nothing, everyone owns this object.
                    }
                }
                if (!(target is INetworkObject) && !(target is NetworkClient))
                {
                    if (!method.GetParameters()[0].ParameterType.IsSubclassDeep(typeof(NetworkClient)))
                    {
                        Log.GlobalWarning("Method marked secure on an object which doesn't implement INetworkOwned and isn't a NetworkClient subclass does not take NetworkClient as its first argument. Consider: securing the method by adding the argument or adding INetworkOwned to the class defention, or move the method to a subclass of NetworkClient. Method: " + packet.MethodName);
                    }
                }
            }
            List<object> args = new List<object>();
            if (method.GetParameters()[0].ParameterType.IsSubclassDeep(typeof(NetworkClient)))
            {
                args.Add(reciever);
            }
            foreach (SerializedData data in packet.Arguments)
            {
                object obj = NetworkConvert.Deserialize(data, out int read);
                args.Add(obj);
            }
            object result = null;
            if (!packet.IgnoreResult)
            {
                foreach (object obj in targets)
                {
                    result = method.Invoke(obj, args.ToArray());
                }
            }
            else
            {
                foreach (object obj in targets)
                {
                    method.Invoke(obj, args.ToArray());
                }
            }
            NetworkInvocations.Remove(packet.NetworkObjectTarget);
            NetworkInvocationResultPacket resultPacket = new NetworkInvocationResultPacket();
            resultPacket.CallbackID = packet.CallbackID;
            resultPacket.Result = NetworkConvert.Serialize(result);
            resultPacket.IgnoreResult = packet.IgnoreResult;
            reciever.Send(resultPacket);
            return result;
        }

        /// <summary>
        /// Sends a network invocation to the target <see cref="NetworkClient"/> with the target <see cref="object"/> which has to be a <see cref="INetworkObject"/> or a <see cref="NetworkClient"/>.
        /// </summary>
        /// <param name="target">
        /// The target <see cref="object"/>, must be either <see cref="INetworkObject"/> or <see cref="NetworkClient"/>.
        /// </param>
        /// <param name="sender">
        /// The <see cref="NetworkClient"/> sender of the invocation.
        /// </param>
        /// <param name="methodName">
        /// The method name of the object from target argument. Note that the method msut be a non-static method. The access modifier does not matter.
        /// </param>
        /// <param name="args">
        /// The arguments that should be provided to the method. Note that these arguments are serialized by <see cref="NetworkConvert"/>. Note: if your method has <see cref="NetworkClient"/> as its first argument, you do not have to inlude it, but you can.
        /// </param>
        /// <exception cref="NetworkInvocationException"></exception>
        /// <exception cref="SecurityException"></exception>
        public static void NetworkInvoke(object target, NetworkClient sender, string methodName, object[] args)
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
            else if (!(target is NetworkClient client))
            {
                throw new NetworkInvocationException($"Provided type is not allowed. Type: {target.GetType().FullName}", new ArgumentException("Can't cast to NetworkClient."));
            }
            Type targetType = target.GetType();
            if (targetType == null)
            {
                throw new NetworkInvocationException($"Cannot find type: '{target.GetType()}'.", new NullReferenceException());
            }
            Type[] arguments = args.Select(x => x.GetType()).ToArray();
            MethodInfo[] methods = targetType.GetMethodsDeep(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.GetCustomAttribute<NetworkInvocable>() != null && x.Name == methodName).ToArray();
            MethodInfo method = GetNetworkInvokeMethod(methods, arguments);
            if (method == null)
            {
                throw new NetworkInvocationException($"Cannot find method: '{methodName}' in type: {targetType.FullName}, Methods: {string.Join("\n", methods.Select(x => x.ToString()))}", new NullReferenceException());
            }
            NetworkInvocable invocable = method.GetCustomAttribute<NetworkInvocable>();
            if (invocable.Direction == NetworkDirection.Client && sender.CurrnetClientLocation == ClientLocation.Remote)
            {
                throw new SecurityException("Attempted to invoke network method from incorrect direction.");
            }
            if (invocable.Direction == NetworkDirection.Server && sender.CurrnetClientLocation == ClientLocation.Local)
            {
                throw new SecurityException("Attempted to invoke network method from incorrect direction.");
            }
            if (invocable.SecureMode && WhereAmI != ClientLocation.Remote)
            {
                if (target is NetworkClient client && client.ClientID != sender.ClientID)
                {
                    throw new SecurityException("Attempted to invoke network method which the client does not own.");
                }
                if (target is INetworkObject owned)
                {
                    if (owned.OwnershipMode == OwnershipMode.Client && owned.OwnerClientID != sender.ClientID)
                    {
                        throw new SecurityException("Attempted to invoke network method which the client does not own.");
                    }
                    else if (owned.OwnershipMode == OwnershipMode.Server && sender.CurrnetClientLocation != ClientLocation.Remote)
                    {
                        throw new SecurityException("Attempted to invoke network method which the client does not own.");
                    }
                    else if (owned.OwnershipMode == OwnershipMode.Public)
                    {
                        //do nothing, everyone owns this object.
                    }
                }
                if (!(target is INetworkObject) && !(target is NetworkClient))
                {
                    if (!method.GetParameters()[0].ParameterType.IsSubclassDeep(typeof(NetworkClient)))
                    {
                        Log.GlobalWarning("Method marked secure on an object which doesn't implement INetworkOwned and isn't a NetworkClient subclass does not take NetworkClient as its first argument. Consider: securing the method by adding the argument or adding INetworkOwned to the class defention, or move the method to a subclass of NetworkClient. Method: " + methodName);
                    }
                }
            }
            NetworkInvocationPacket packet = new NetworkInvocationPacket();
            packet.TargetTypeAssmebly = Assembly.GetAssembly(targetType).GetName().FullName;
            packet.NetworkObjectTarget = targetID;
            packet.MethodName = methodName;
            foreach (var arg in args)
            {
                SocketNetworking.Shared.SerializedData data = NetworkConvert.Serialize(arg);
                packet.Arguments.Add(data);
            }
            packet.TargetType = targetType.FullName;
            int callbackID = NetworkInvocations.GetFirstEmptySlot();
            NetworkInvocations.Add(callbackID);
            packet.CallbackID = callbackID;
            packet.IgnoreResult = true;
            sender.Send(packet);
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
            else if (!(target is NetworkClient client))
            {
                throw new NetworkInvocationException($"Provided type is not allowed. Type: {target.GetType().FullName}", new ArgumentException("Can't cast to NetworkClient."));
            }
            Type targetType = target.GetType();
            if (targetType == null)
            {
                throw new NetworkInvocationException($"Cannot find type: '{target.GetType()}'.", new NullReferenceException());
            }
            Type[] arguments = args.Select(x => x.GetType()).ToArray();
            MethodInfo[] methods = targetType.GetMethodsDeep(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.GetCustomAttribute<NetworkInvocable>() != null && x.Name == methodName).ToArray();
            MethodInfo method = GetNetworkInvokeMethod(methods, arguments);
            if (method.ReturnType != typeof(T))
            {
                throw new NetworkInvocationException("Cannot invoke method, return type is incorrect.", new InvalidCastException());
            }
            if (method == null)
            {
                throw new NetworkInvocationException($"Cannot find method: '{methodName}' in type: {targetType.FullName}, Methods: {string.Join("\n", methods.Select(x => x.ToString()))}", new NullReferenceException());
            }
            NetworkInvocable invocable = method.GetCustomAttribute<NetworkInvocable>();
            if (invocable.Direction == NetworkDirection.Client && sender.CurrnetClientLocation == ClientLocation.Remote)
            {
                throw new SecurityException("Attempted to invoke network method from incorrect direction.");
            }
            if (invocable.Direction == NetworkDirection.Server && sender.CurrnetClientLocation == ClientLocation.Local)
            {
                throw new SecurityException("Attempted to invoke network method from incorrect direction.");
            }
            if (invocable.SecureMode && WhereAmI != ClientLocation.Remote)
            {
                if (target is NetworkClient client && client.ClientID != sender.ClientID)
                {
                    throw new SecurityException("Attempted to invoke network method which the client does not own.");
                }
                if (target is INetworkObject owned)
                {
                    if (owned.OwnershipMode == OwnershipMode.Client && owned.OwnerClientID != sender.ClientID)
                    {
                        throw new SecurityException("Attempted to invoke network method which the client does not own.");
                    }
                    else if (owned.OwnershipMode == OwnershipMode.Server && sender.CurrnetClientLocation != ClientLocation.Remote)
                    {
                        throw new SecurityException("Attempted to invoke network method which the client does not own.");
                    }
                    else if (owned.OwnershipMode == OwnershipMode.Public)
                    {
                        //do nothing, everyone owns this object.
                    }
                }
                if (!(target is INetworkObject) && !(target is NetworkClient))
                {
                    if (!method.GetParameters()[0].ParameterType.IsSubclassOf(typeof(NetworkClient)))
                    {
                        Log.GlobalWarning("Method marked secure on an object which doesn't implement INetworkOwned and isn't a NetworkClient subclass does not take NetworkClient as its first argument. Consider: securing the method by adding the argument or adding INetworkOwned to the class defention, or move the method to a subclass of NetworkClient. Method: " + methodName);
                    }
                }
            }
            NetworkInvocationPacket packet = new NetworkInvocationPacket();
            packet.TargetTypeAssmebly = Assembly.GetAssembly(targetType).GetName().FullName;
            packet.NetworkObjectTarget = targetID;
            packet.MethodName = methodName;
            packet.IgnoreResult = false;
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
                if (!sender.IsTransportConnected)
                {
                    Log.GlobalError($"NetworkInvoke on method {methodName} failed becuase the NetworkClient is not connected.");
                    break;
                }
                if (stopwatch.ElapsedMilliseconds > msTimeOut)
                {
                    Log.GlobalError($"NetworkInvoke on method {methodName} timed out after {msTimeOut}ms of proccessing.");
                    break;
                }
            }
            Log.GlobalDebug($"NetowkInvoke on {methodName} successfully returned and took {stopwatch.ElapsedMilliseconds}ms");
            NetworkInvocationResultPacket resultPacket = networkResultAwaiter.ResultPacket;
            if (resultPacket == null)
            {
                Log.GlobalError($"NetworkInvoke on method {methodName} failed remotely! Error: null");
                return default;
            }
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

        public static TResult NetworkInvoke<TResult>(object target, NetworkClient sender, Func<TResult> func)
        {
            return NetworkInvoke<TResult>(target, sender, func.Method.Name, new object[] { });
        }

        #endregion
    }


    public class NetworkObjectSpawner
    {
        public NetworkManager.NetworkObjectSpawnerDelegate Spawner;

        public bool AllowSubclasses;

        public Type TargetType;
    }

    public class NetworkObjectCache
    {
        public Type Target;

        public Dictionary<Type, List<PacketListenerData>> Listeners;

        public List<ValueTuple<MethodInfo, NetworkInvocable>> Invokables;

        public List<INetworkSyncVar> SyncVars;
    }

    public class NetworkObjectData
    {
        public Dictionary<Type, List<PacketListenerData>> Listeners;

        public List<INetworkSyncVar> SyncVars;

        public INetworkObject TargetObject;
    }

    public class PacketListenerData
    {
        public PacketListener Attribute;

        public MethodInfo AttachedMethod;
    }
}
