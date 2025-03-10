using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security;
using SocketNetworking.Attributes;
using SocketNetworking.Client;
using SocketNetworking.Exceptions;
using SocketNetworking.Misc;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using SocketNetworking.Server;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.Shared.SyncVars;

namespace SocketNetworking.Shared
{
    public class NetworkManager
    {
        public static Log Log { get; private set; } = new Log("[Network Manager]");

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
                    if (NetworkClient.Clients.Where(x => x.CurrentClientLocation == ClientLocation.Local).Count() == 0)
                    {
                        return ClientLocation.Remote;
                    }
                    if (NetworkClient.Clients.Where(x => x.CurrentClientLocation == ClientLocation.Local).Count() != 0)
                    {
                        return ClientLocation.Unknown;
                    }
                }
                else
                {
                    if (NetworkClient.Clients.Any(x => x.CurrentClientLocation == ClientLocation.Remote))
                    {
                        Log.Error("There are active remote clients even though the server is closed, these clients will now be terminated.");
                        foreach (var x in NetworkClient.Clients)
                        {
                            if (x.CurrentClientLocation == ClientLocation.Local)
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

        public static Dictionary<Type, Type> TypeToTypeWrapper = new Dictionary<Type, Type>();

        private static ConcurrentDictionary<ulong, Assembly> assemblyNames = new ConcurrentDictionary<ulong, Assembly>();

        internal static bool HasAssemblyHash(Assembly assembly)
        {
            ulong hash = assembly.FullName.GetULongStringHash();
            if (!assemblyNames.ContainsKey(hash))
            {
                return false;
            }
            return true;
        }

        internal static Assembly GetAssemblyFromHash(ulong hash)
        {
            if(assemblyNames.ContainsKey(hash))
            {
                return assemblyNames[hash];
            }
            return null;
        }

        internal static ulong GetHashFromAssembly(Assembly assembly)
        {
            ulong hash = assembly.FullName.GetULongStringHash();
            if(!assemblyNames.ContainsKey(hash))
            {
                assemblyNames.TryAdd(hash, assembly);
            }
            return hash;
        }

        /// <summary>
        /// Imports the target assembly, caching: <see cref="ITypeWrapper{T}"/>s, any methods with <see cref="NetworkInvokable"/> (which are on a class with <see cref="INetworkObject"/> implemented) and any <see cref="CustomPacket"/>s  
        /// </summary>
        /// <param name="target"></param>
        public static void ImportAssmebly(Assembly target)
        {
            ImportCustomPackets(target);
            List<Type> applicableTypes = target.GetTypes().ToList();
            foreach (Type t in applicableTypes)
            {
                var data = GetNetworkObjectData(t);
                if(data == null)
                {
                    continue;
                }
                PreCache.Add(data);
            }
            GetHashFromAssembly(target);
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
                    Log.Warning($"Custom packet {packet.GetType().Name} does not implement attribute {nameof(PacketDefinition)} it will be ignored.");
                    continue;
                }
                if (AdditionalPacketTypes.ContainsKey(packet.CustomPacketID))
                {
                    if (AdditionalPacketTypes[packet.CustomPacketID].GetType() == packet.GetType())
                    {
                        Log.Warning("Trying to register a duplicate packet. Type: " + packet.GetType().FullName);
                        return;
                    }
                    else
                    {
                        throw new CustomPacketCollisionException(packet.CustomPacketID, AdditionalPacketTypes[packet.CustomPacketID], packet.GetType());
                    }
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
                    if (AdditionalPacketTypes[customPacketId] == type)
                    {
                        Log.Warning("Trying to register a duplicate packet. Type: " + type.FullName);
                        return;
                    }
                    else
                    {
                        throw new CustomPacketCollisionException(customPacketId, AdditionalPacketTypes[customPacketId], type);
                    }
                }
                Log.Info($"Adding custom packet with ID {customPacketId} and name {type.Name}");
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

        private static readonly ConcurrentDictionary<INetworkObject, NetworkObjectData> NetworkObjects = new ConcurrentDictionary<INetworkObject, NetworkObjectData>();

        private static readonly List<NetworkObjectData> PreCache = new List<NetworkObjectData>();

        public static (INetworkObject, NetworkObjectData) GetNetworkObjectByID(int id)
        {
            INetworkObject obj = NetworkObjects.Keys.FirstOrDefault(x => x.NetworkID == id);
            if (obj != null)
            {
                return (obj, NetworkObjects[obj]);
            }
            return (null, null);
        }

        private static readonly List<NetworkObjectSpawner> NetworkObjectSpawners = new List<NetworkObjectSpawner>();

        /// <summary>
        /// Allows for spawning of an <see cref="INetworkObject"/> controlled via external code. Note that <see cref="ObjectManagePacket"/> will be null if the <see cref="INetworkObject"/> type is the same as <see cref="NetworkServer.ClientAvatar"/>.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="handle"></param>
        /// <returns></returns>
        public delegate INetworkSpawnable NetworkObjectSpawnerDelegate(ObjectManagePacket packet, NetworkHandle handle);

        public static bool RegisterSpawner(Type type, NetworkObjectSpawnerDelegate spawner, bool allowSubclasses)
        {
            if(NetworkObjectSpawners.Any(x => x.TargetType == type))
            {
                Log.Error("Can't register that spawner: The type is already registered.");
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
                Log.Error("Can't unregister that spawner: Not Found");
                return false;
            }
            NetworkObjectSpawners.Remove(spawner);
            return true;
        }

        public static NetworkObjectSpawner GetBestSpawner(Type type)
        {
            NetworkObjectSpawner objSpawner = null;
            int bestApproxObj = 0;
            foreach (NetworkObjectSpawner possibleSpawner in NetworkObjectSpawners)
            {
                if (possibleSpawner.AllowSubclasses)
                {
                    int distance = type.HowManyClassesUp(possibleSpawner.TargetType);
                    if (distance == -1)
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
                    if (possibleSpawner.TargetType != type)
                    {
                        continue;
                    }
                    objSpawner = possibleSpawner;
                    break;
                }
            }
            return objSpawner;
        }

        internal static void ModifyNetworkObjectLocal(ObjectManagePacket packet, NetworkHandle handle)
        {
            //Spawning
            if (packet.Action == ObjectManagePacket.ObjectManageAction.Create)
            {
                Type objType = packet.ObjectType;
                if (objType == null)
                {
                    throw new NullReferenceException("Cannot find type by name or assembly.");
                }
                (INetworkObject, NetworkObjectData) existingObject = GetNetworkObjectByID(packet.NewNetworkID);
                if (existingObject.Item1 != null)
                {
                    if(existingObject.Item1.GetType() != objType)
                    {
                        existingObject.Item1.LocalDestroy();
                    }
                    else
                    {
                        ObjectManagePacket alreadyExistsPacket = new ObjectManagePacket()
                        {
                            Action = ObjectManagePacket.ObjectManageAction.AlreadyExists,
                            ObjectType = objType,
                            NewNetworkID = packet.NewNetworkID,
                        };
                        handle.Client.Send(alreadyExistsPacket);
                        return;
                    }
                }
                NetworkObjectSpawner objSpawner = GetBestSpawner(objType);
                INetworkObject netObj;
                if (objSpawner == null)
                {
                    netObj = (INetworkObject)Activator.CreateInstance(objType);
                }
                else
                {
                    netObj = (INetworkObject)objSpawner.Spawner.Invoke(packet, handle);
                }
                if (netObj == null)
                {
                    throw new NullReferenceException($"Failed to spawn {objType.FullName}");
                }
                netObj.NetworkID = packet.NewNetworkID;
                netObj.OwnerClientID = packet.OwnerID;
                netObj.OwnershipMode = packet.OwnershipMode;
                netObj.Active = packet.Active;
                ObjectManagePacket creationConfirmation = new ObjectManagePacket()
                {
                    NetworkIDTarget = netObj.NetworkID,
                    Action = ObjectManagePacket.ObjectManageAction.ConfirmCreate,
                };
                handle.Client.Send(creationConfirmation);
                AddNetworkObject(netObj);
                netObj.OnLocalSpawned(packet);
                if(netObj.OwnerClientID == handle.Client.ClientID)
                {
                    netObj.OnOwnerLocalSpawned(handle.Client);
                }
                return;
            }

            foreach (INetworkObject @object in NetworkObjects.Keys.Where(x => x.NetworkID == packet.NetworkIDTarget))
            {
                //Security
                if (packet.Action != ObjectManagePacket.ObjectManageAction.Create && packet.Action != ObjectManagePacket.ObjectManageAction.ConfirmCreate && packet.Action != ObjectManagePacket.ObjectManageAction.ConfirmDestroy && packet.Action != ObjectManagePacket.ObjectManageAction.AlreadyExists)
                {
                    if (WhereAmI == ClientLocation.Remote)
                    {
                        if (@object.OwnershipMode == OwnershipMode.Client && handle.Client.ClientID != @object.OwnerClientID)
                        {
                            throw new SecurityException($"Attempted to modify an object you do not have permission over. Action: {packet.Action}");
                        }
                        if (@object.OwnershipMode == OwnershipMode.Public && !@object.AllowPublicModification)
                        {
                            throw new SecurityException($"Attempted to modify a public object which is not accepting public modification. Action: {packet.Action}");
                        }
                        if (@object.OwnershipMode == OwnershipMode.Server)
                        {
                            throw new SecurityException($"Attempted to modify a server controlled object. Action: {packet.Action}");
                        }
                    }
                }
                switch (packet.Action)
                {
                    //Unused
                    case ObjectManagePacket.ObjectManageAction.Create:
                        throw new InvalidOperationException("Creation in modification loop (Internal Error)");
                    case ObjectManagePacket.ObjectManageAction.ConfirmCreate:
                        @object.OnNetworkSpawned(handle.Client);
                        if(@object.OwnerClientID == handle.Client.ClientID)
                        {
                            @object.OnOwnerNetworkSpawned(handle.Client);
                        }
                        SendCreatedPulse(handle.Client, @object);
                        @object.SyncVars(handle.Client);
                        break;
                    case ObjectManagePacket.ObjectManageAction.AlreadyExists:
                        //this is fine, just ignore it!
                        break;
                    case ObjectManagePacket.ObjectManageAction.Destroy:
                        INetworkObject destructionTarget = @object;
                        if (destructionTarget == default(INetworkObject))
                        {
                            throw new NullReferenceException($"Can't find the object that should be destroyed. ID: {packet.NetworkIDTarget}");
                        }
                        SendDestroyedPulse(handle.Client, destructionTarget);
                        RemoveNetworkObject(destructionTarget);
                        destructionTarget.OnClientDestroy(handle.Client);
                        ObjectManagePacket destroyConfirmPacket = new ObjectManagePacket()
                        {
                            NetworkIDTarget = destructionTarget.NetworkID,
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
                        INetworkObject modificationTarget = @object;
                        if (modificationTarget == default(INetworkObject))
                        {
                            throw new NullReferenceException($"Can't find the object to modify. ID: {packet.NetworkIDTarget}");
                        }
                        modificationTarget.OnModify(packet, handle.Client);
                        modificationTarget.NetworkID = packet.NewNetworkID;
                        modificationTarget.OwnerClientID = packet.OwnerID;
                        modificationTarget.ObjectVisibilityMode = packet.ObjectVisibilityMode;
                        modificationTarget.OwnershipMode = packet.OwnershipMode;
                        modificationTarget.Active = packet.Active;
                        modificationTarget.OnModified(handle.Client);
                        SendModifiedPulse(handle.Client, modificationTarget);
                        break;
                    case ObjectManagePacket.ObjectManageAction.ConfirmModify:
                        INetworkObject modificationConfirmTarget = @object;
                        if (modificationConfirmTarget == default(INetworkObject))
                        {
                            throw new NullReferenceException($"Can't find the object to modify. ID: {packet.NetworkIDTarget}");
                        }
                        modificationConfirmTarget.OnModified(handle.Client);
                        SendModifiedPulse(handle.Client, modificationConfirmTarget);
                        break;
                }
            }
        }

        #region Pulse

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
                if(@object.OwnerClientID == networkClient.ClientID)
                {
                    @object.OnOwnerDisconnected(networkClient);
                }
            }
        }

        public static void SendConnectedPulse(NetworkClient client)
        {
            foreach (INetworkObject @object in NetworkObjects.Keys)
            {
                @object.OnConnected(client);
            }
        }

        #endregion

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

        #region Network Object Get/Set/Remove

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

        public static List<INetworkObject> GetNetworkObjects()
        {
            return NetworkObjects.Keys.ToList();
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
                networkObject.EnsureNetworkIDIsGiven();
            }
            if (NetworkObjects.ContainsKey(networkObject))
            {
                Log.Warning("Tried to add network object that already exists.");
                return false;
            }
            else
            {
                NetworkObjectData data = GetNetworkObjectData(networkObject);
                NetworkObjects.TryAdd(networkObject, data);
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
                NetworkObjects.TryRemove(networkObject, out var value);
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

        public static NetworkObjectData GetNetworkObjectData(Type t)
        {
            if (PreCache.Any(x => x.TargetObject == t) || TypeToTypeWrapper.ContainsValue(t))
            {
                return PreCache.FirstOrDefault(x => x.TargetObject == t);
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
            NetworkObjectData networkObjectCache = new NetworkObjectData();
            networkObjectCache.TargetObject = t;
            networkObjectCache.Invokables = new List<(MethodInfo, NetworkInvokable)>();
            networkObjectCache.Listeners = new Dictionary<Type, List<PacketListenerData>>();
            networkObjectCache.SyncVars = new List<FieldInfo>();
            foreach (MethodInfo method in t.GetMethodsDeep(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttribute<PacketListener>() != null)
                {
                    if (method.GetParameters().Length < AcceptedMethodArugments.Length)
                    {
                        Log.Warning("Method " + method.Name + " was ignored becuase it doesn't have the proper amount of arguments");
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
                                Log.Warning($"Method {method.Name} doesn't accept the correct paramters, it has been ignored. Note that the correct paramaters are: {string.Join(",", AcceptedMethodArugments.Select(x => x.Name))}");
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
                if (method.GetCustomAttribute<NetworkInvokable>() != null)
                {
                    ValueTuple<MethodInfo, NetworkInvokable> tuple = (method, method.GetCustomAttribute<NetworkInvokable>());
                    if (networkObjectCache.Invokables.Contains(tuple))
                    {
                        Log.Warning($"Tried to cache duplicate! Type: {t.FullName}, Method: {method.Name}");
                        continue;
                    }
                    networkObjectCache.Invokables.Add(tuple);
                }
            }
            List<FieldInfo> syncVars = new List<FieldInfo>();
            FieldInfo[] fields = t.GetAllFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                if (!field.FieldType.GetInterfaces().Contains(typeof(INetworkSyncVar)))
                {
                    continue;
                }
                syncVars.Add(field);
            }
            networkObjectCache.SyncVars = syncVars;
            return networkObjectCache;
        }

        /// <summary>
        ///  Creates or finds a <see cref="NetworkObjectData"/> instance.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static NetworkObjectData GetNetworkObjectData(INetworkObject target)
        {
            Type typeOfObject = target.GetType();
            NetworkObjectData data = GetNetworkObjectData(typeOfObject);
            foreach(FieldInfo field in data.SyncVars)
            {
                INetworkSyncVar var;
                if(field.GetValue(target) == null)
                {
                    var = (INetworkSyncVar)Activator.CreateInstance(field.FieldType, target, target.OwnershipMode);
                }
                else
                {
                    var = (INetworkSyncVar)field.GetValue(target);
                }
                if(var.OwnerObject == default)
                {
                    var.OwnerObject = target;
                }
                if(var.Name == string.Empty)
                {
                    var.Name = field.Name;
                }
                field.SetValue(target, var);
            }
            return data;
        }
        #endregion

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
            ClientLocation clientLocation = runningClient.CurrentClientLocation;
            if(header.Type != PacketType.Custom)
            {
                Log.Error("Non Custom packets cannot be used in PacketListeners");
                return;
            }
            CustomPacket cPacket = new CustomPacket();
            cPacket.Deserialize(data);
            if (!AdditionalPacketTypes.ContainsKey(cPacket.CustomPacketID))
            {
                Log.Error("Unknown Custom packet. ID: " + cPacket.CustomPacketID);
                return;
            }
            Type packetType = AdditionalPacketTypes[cPacket.CustomPacketID];
            //Log.Debug(packetType.Name);
            Packet packet = (Packet)Activator.CreateInstance(AdditionalPacketTypes[cPacket.CustomPacketID]);
            ByteReader reader = packet.Deserialize(data);
            if (reader.ReadBytes < header.Size)
            {
                //Log.Warning($"Packet with ID {cPacket.CustomPacketID} was not fully consumed, the header specified a length which was greater then what was read. Actual: {reader.ReadBytes}, Header: {header.Size}");
            }
            object changedPacket = Convert.ChangeType(packet, packetType);
            NetworkHandle handle = new NetworkHandle(runningClient, (Packet)changedPacket);
            if (cPacket.NetworkIDTarget == 0)
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
            List<INetworkObject> objects = NetworkObjects.Keys.Where(x => x.NetworkID == cPacket.NetworkIDTarget && x.Active).ToList();
            if (objects.Count == 0)
            {
                Log.Warning($"Target NetworkID revealed no active objects registered! ID: {cPacket.NetworkIDTarget}");
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
            List<SyncVarData> publicReplicated = new List<SyncVarData>();
            foreach(SyncVarData data in packet.Data)
            {
                foreach(INetworkObject obj in NetworkObjects.Keys.Where(x => x.NetworkID == data.NetworkIDTarget))
                {
                    NetworkObjectData networkObjectData = NetworkObjects[obj];
                    FieldInfo field = networkObjectData.SyncVars.FirstOrDefault(x => x.Name == data.TargetVar);
                    if (field == default)
                    {
                        Log.Warning($"No such Network Sync Var '{data.TargetVar}' on object {obj.GetType().FullName}");
                        continue;
                    }
                    INetworkSyncVar syncVar = field.GetValue(obj) as INetworkSyncVar;
                    if(syncVar == default)
                    {
                        Log.Warning($"Network Sync Var '{data.TargetVar}' on object {obj.GetType().FullName} is not an actual sync var, but is listed as one.");
                        continue;
                    }
                    if (syncVar.SyncOwner != OwnershipMode.Public)
                    {
                        if (syncVar.SyncOwner == OwnershipMode.Client && WhereAmI == ClientLocation.Remote)
                        {
                            if (runner.ClientID != syncVar.OwnerObject.OwnerClientID)
                            {
                                return;
                            }
                        }
                    }
                    object value = ByteConvert.Deserialize(data.Data, out int read);
                    syncVar.RawSet(value, runner);
                    syncVar.RawSet(data.Mode, runner);
                    syncVar.OwnerObject.OnSyncVarChanged(runner, syncVar);
                    if (WhereAmI == ClientLocation.Remote && syncVar.OwnerObject.ObjectVisibilityMode != ObjectVisibilityMode.OwnerAndServer)
                    {
                        if(!publicReplicated.Any(x => x.NetworkIDTarget == data.NetworkIDTarget && x.TargetVar == data.TargetVar))
                        {
                            publicReplicated.Add(data);
                        }
                    }
                }
                foreach (INetworkObject obj in NetworkObjects.Keys.Where(x => x.NetworkID == data.NetworkIDTarget))
                {
                    obj.OnSyncVarsChanged();
                }
            }
            if(WhereAmI == ClientLocation.Remote)
            {
                SyncVarUpdatePacket replicatePacket = new SyncVarUpdatePacket()
                {
                    Data = publicReplicated,
                };
                NetworkServer.SendToAll(replicatePacket);
            }
        }

        #endregion

        #region Network Invoke

        private static List<int> NetworkInvokations = new List<int>();

        private static List<NetworkInvokationResultPacket> Results = new List<NetworkInvokationResultPacket>();

        public static event Action<NetworkInvokationResultPacket> OnNetworkInvokationResult;

        public static bool IsReady(int callBack, out NetworkInvokationResultPacket packet)
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

        public static NetworkInvokationResultPacket ConsumeNetworkInvokationResult(int callback)
        {
            if (!IsReady(callback, out NetworkInvokationResultPacket packet))
            {
                Log.Warning("Attempted to get not ready network invokation result with callback ID: " + callback);
                return null;
            }
            Results.Remove(packet);
            return packet;
        }

        private static MethodInfo GetNetworkInvokeMethod(MethodInfo[] methods, Type[] arguments, string name)
        {
            string[] expectedArgs = arguments.Select(x => x.FullName).ToArray();
            foreach (MethodInfo curMethod in methods)
            {
                if(curMethod.Name != name)
                {
                    continue;
                }
                List<string> methodArgs = curMethod.GetParameters().Select(y => y.ParameterType.FullName).ToList();
                if(!methodArgs.Any() && expectedArgs.Any())
                {
                    continue;
                }
                if(expectedArgs.Any())
                {
                    if (curMethod.GetParameters()[0].ParameterType.IsSubclassDeep(typeof(NetworkClient)) && !arguments[0].IsSubclassDeep(typeof(NetworkClient)))
                    {
                        methodArgs.RemoveAt(0);
                    }
                    else if (curMethod.GetParameters()[0].ParameterType.IsSubclassDeep(typeof(NetworkHandle)) && !arguments[0].IsSubclassDeep(typeof(NetworkHandle)))
                    {
                        methodArgs.RemoveAt(0);
                    }
                }
                else
                {
                    return curMethod;
                }
                if (methodArgs.ToArray().ArraysEqual(expectedArgs))
                {
                    return curMethod;
                }
            }
            Log.GlobalError($"Can't find method {name} with args {string.Join(",", arguments.Select(x => x.Name))}");
            return null;
        }

        internal static void NetworkInvoke(NetworkInvokationResultPacket packet, NetworkClient Receiver)
        {
            if (packet.IgnoreResult)
            {
                //return;
            }
            if (!packet.Success)
            {
                Log.Error("Network Invokation failed. Error: " + packet.ErrorMessage);
            }
            Results.Add(packet);
            OnNetworkInvokationResult?.Invoke(packet);
        }

        internal static object NetworkInvoke(NetworkInvokationPacket packet, NetworkClient Receiver)
        {
            Assembly assmebly = Assembly.Load(packet.TargetTypeAssmebly);
            Type targetType = assmebly.GetType(packet.TargetType);
            if (targetType == null)
            {
                throw new NetworkInvocationException($"Cannot find type: '{packet.TargetType}'.", new NullReferenceException());
            }
            object target = Receiver;
            List<object> targets = new List<object>();
            if (packet.NetworkIDTarget != 0)
            {
                List<INetworkObject> netObjs = NetworkObjects.Keys.Where(x => x.NetworkID == packet.NetworkIDTarget && x.GetType() == targetType).ToList();
                if (netObjs.Count != 0)
                {
                    target = netObjs[0];
                    targets.AddRange(netObjs);
                }
            }
            else
            {
                if (targetType.IsSubclassDeep(typeof(NetworkClient)))
                {
                    targets.Add(target);
                }
            }
            if (targets.Count == 0)
            {
                throw new NetworkInvocationException("Unable to find any networkobjects with ID: " + packet.NetworkIDTarget);
            }
            if (target == null)
            {
                throw new NetworkInvocationException($"Unable to find the Object this packet is referencing.");
            }
            Type[] arguments = packet.Arguments.Select(x => x.Type).ToArray();
            MethodInfo[] methods = GetNetworkObjectData(target.GetType()).Invokables.Select(x => x.Item1).ToArray();
            MethodInfo method = GetNetworkInvokeMethod(methods, arguments, packet.MethodName);
            if (method == null)
            {
                throw new NetworkInvocationException($"Cannot find method: '{packet.MethodName}' in type: {targetType.FullName}, Methods: {string.Join("\n", methods.Select(x => x.ToString()))}", new NullReferenceException());
            }
            NetworkInvokable invokable = method.GetCustomAttribute<NetworkInvokable>();
            if (invokable.Direction == NetworkDirection.Server && Receiver.CurrentClientLocation == ClientLocation.Remote)
            {
                throw new SecurityException($"Attempted to invoke network method from incorrect direction. Method: {method.Name}");
            }
            if (invokable.Direction == NetworkDirection.Client && Receiver.CurrentClientLocation == ClientLocation.Local)
            {
                throw new SecurityException($"Attempted to invoke network method from incorrect direction. Method: {method.Name}");
            }
            if (invokable.SecureMode && Receiver.CurrentClientLocation != ClientLocation.Local)
            {
                if (target is NetworkClient client && client.ClientID != Receiver.ClientID)
                {
                    throw new SecurityException($"Attempted to invoke network method which the client does not own. {method.Name}");
                }
                if (target is INetworkObject owned)
                {
                    if (owned.OwnershipMode == OwnershipMode.Client && owned.OwnerClientID != Receiver.ClientID)
                    {
                        throw new SecurityException("Attempted to invoke network method which the client does not own.");
                    }
                    else if (owned.OwnershipMode == OwnershipMode.Server && Receiver.CurrentClientLocation != ClientLocation.Local)
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
                        Log.Warning("Method marked secure on an object takes NetworkClient as its first argument, please replace this with NetworkHandle. Method: " + packet.MethodName);
                    }
                }
            }
            List<object> args = new List<object>();
            if (method.GetParameters()[0].ParameterType.IsSubclassDeep(typeof(NetworkClient)))
            {
                args.Add(Receiver);
            }
            if (method.GetParameters()[0].ParameterType.IsSubclassDeep(typeof(NetworkHandle)))
            {
                NetworkHandle handle = new NetworkHandle(Receiver, packet);
                args.Add(handle);
            }
            foreach (SerializedData data in packet.Arguments)
            {
                object obj = ByteConvert.Deserialize(data, out int read);
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
            NetworkInvokations.Remove(packet.NetworkIDTarget);
            NetworkInvokationResultPacket resultPacket = new NetworkInvokationResultPacket();
            resultPacket.CallbackID = packet.CallbackID;
            resultPacket.Result = ByteConvert.Serialize(result);
            resultPacket.IgnoreResult = packet.IgnoreResult;
            Receiver.Send(resultPacket);
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
        /// The arguments that should be provided to the method. Note that these arguments are serialized by <see cref="ByteConvert"/>. Note: if your method has <see cref="NetworkClient"/> as its first argument, you do not have to inlude it, but you can.
        /// </param>
        /// <exception cref="NetworkInvocationException"></exception>
        /// <exception cref="SecurityException"></exception>
        public static void NetworkInvoke(object target, NetworkClient sender, string methodName, object[] args, bool priority = false)
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
            Type[] arguments = args.Select(x => x.GetType()).ToArray();
            MethodInfo[] methods = GetNetworkObjectData(target.GetType()).Invokables.Select(x => x.Item1).ToArray();
            MethodInfo method = GetNetworkInvokeMethod(methods, arguments, methodName);
            if (method == null) 
            {
                throw new NetworkInvocationException($"Cannot find method: '{methodName}' in type: {target.GetType().FullName}, Methods: {string.Join("\n", methods.Select(x => x.ToString()))}", new NullReferenceException());
            }
            NetworkInvokable invocable = method.GetCustomAttribute<NetworkInvokable>();
            if (invocable.Direction == NetworkDirection.Client && sender.CurrentClientLocation == ClientLocation.Remote)
            {
                throw new SecurityException($"Attempted to invoke network method from incorrect direction. Method: {method.Name}");
            }
            if (invocable.Direction == NetworkDirection.Server && sender.CurrentClientLocation == ClientLocation.Local)
            {
                throw new SecurityException($"Attempted to invoke network method from incorrect direction. Method: {method.Name}");
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
                    else if (owned.OwnershipMode == OwnershipMode.Server && sender.CurrentClientLocation != ClientLocation.Remote)
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
                    if (method.GetParameters()[0].ParameterType.IsSubclassDeep(typeof(NetworkClient)))
                    {
                        Log.Warning("Method marked secure takes a NetworkClient as the first argument, please replace this with NetworkHandle. Method: " + methodName);
                    }
                }
            }
            NetworkInvokationPacket packet = new NetworkInvokationPacket();
            packet.TargetTypeAssmebly = Assembly.GetAssembly(target.GetType()).GetName().FullName;
            packet.NetworkIDTarget = targetID;
            packet.MethodName = methodName;
            foreach (var arg in args)
            {
                SerializedData data = ByteConvert.Serialize(arg);
                packet.Arguments.Add(data);
            }
            packet.TargetType = target.GetType().FullName;
            int callbackID = NetworkInvokations.GetFirstEmptySlot();
            NetworkInvokations.Add(callbackID);
            packet.CallbackID = callbackID;
            packet.IgnoreResult = true;
            sender.Send(packet, priority);
        }

        public static T NetworkInvoke<T>(object target, NetworkClient sender, string methodName, object[] args, float msTimeOut = 5000, bool priority = false)
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
            Type[] arguments = args.Select(x => x.GetType()).ToArray();
            MethodInfo[] methods = GetNetworkObjectData(target.GetType()).Invokables.Select(x => x.Item1).ToArray();
            MethodInfo method = GetNetworkInvokeMethod(methods, arguments, methodName);
            if (method.ReturnType != typeof(T))
            {
                throw new NetworkInvocationException("Cannot invoke method, return type is incorrect.", new InvalidCastException());
            }
            if (method == null)
            {
                throw new NetworkInvocationException($"Cannot find method: '{methodName}' in type: {target.GetType().FullName}, Methods: {string.Join("\n", methods.Select(x => x.ToString()))}", new NullReferenceException());
            }
            NetworkInvokable invocable = method.GetCustomAttribute<NetworkInvokable>();
            if (invocable.Direction == NetworkDirection.Client && sender.CurrentClientLocation == ClientLocation.Remote)
            {
                throw new SecurityException($"Attempted to invoke network method from incorrect direction. Method: {method.Name}");
            }
            if (invocable.Direction == NetworkDirection.Server && sender.CurrentClientLocation == ClientLocation.Local)
            {
                throw new SecurityException($"Attempted to invoke network method from incorrect direction. Method: {method.Name}");
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
                    else if (owned.OwnershipMode == OwnershipMode.Server && sender.CurrentClientLocation != ClientLocation.Remote)
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
                        Log.Warning("Method marked secure on an object takes NetworkClient as the first argument, please replace this with NetworkHandle. : " + methodName);
                    }
                }
            }
            NetworkInvokationPacket packet = new NetworkInvokationPacket();
            packet.TargetTypeAssmebly = Assembly.GetAssembly(target.GetType()).GetName().FullName;
            packet.NetworkIDTarget = targetID;
            packet.MethodName = methodName;
            packet.IgnoreResult = false;
            foreach (var arg in args)
            {
                SerializedData data = ByteConvert.Serialize(arg);
                packet.Arguments.Add(data);
            }
            packet.TargetType = target.GetType().FullName;
            int callbackID = NetworkInvokations.GetFirstEmptySlot();
            NetworkInvokations.Add(callbackID);
            packet.CallbackID = callbackID;
            sender.Send(packet, priority);
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
                    Log.Error($"NetworkInvoke on method {methodName} failed becuase the NetworkClient is not connected.");
                    break;
                }
                if (stopwatch.ElapsedMilliseconds > msTimeOut)
                {
                    Log.Error($"NetworkInvoke on method {methodName} timed out after {msTimeOut}ms of processing.");
                    break;
                }
            }
            //Log.Debug($"NetowkInvoke on {methodName} successfully returned and took {stopwatch.ElapsedMilliseconds}ms");
            NetworkInvokationResultPacket resultPacket = networkResultAwaiter.ResultPacket;
            if (resultPacket == null)
            {
                Log.Error($"NetworkInvoke on method {methodName} failed remotely! Error: null");
                return default;
            }
            if (!resultPacket.Success)
            {
                Log.Error($"NetworkInvoke on method {methodName} failed remotely! Error: " + resultPacket.ErrorMessage);
                return default;
            }
            object result = ByteConvert.Deserialize(resultPacket.Result, out int read);
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

    public class NetworkObjectData
    {
        public Dictionary<Type, List<PacketListenerData>> Listeners;

        public List<FieldInfo> SyncVars;

        public List<ValueTuple<MethodInfo, NetworkInvokable>> Invokables;

        public Type TargetObject;
    }

    public class PacketListenerData
    {
        public PacketListener Attribute;

        public MethodInfo AttachedMethod;
    }
}
