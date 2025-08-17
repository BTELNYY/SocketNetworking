using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SocketNetworking.Client;
using SocketNetworking.Server;
using SocketNetworking.Shared;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.UnityEngine.Components;
using UnityEngine;

namespace SocketNetworking.UnityEngine
{
    public class UnityNetworkManager : NetworkManager
    {
        private static UnityMainThreadDispatcher _dispatcher;

        public static UnityMainThreadDispatcher Dispatcher
        {
            get
            {
                return _dispatcher;
            }
        }

        static GameObject _dispatcherObject;

        public static GameObject DispatcherObject
        {
            get
            {
                return _dispatcherObject;
            }
        }

        static bool _initted = false;

        public static void Init()
        {
            if (_initted)
            {
                return;
            }
            _initted = true;
            NetworkManager.ImportAssembly(Assembly.GetExecutingAssembly());
            NetworkServer.ClientType = typeof(UnityNetworkClient);
            RegisterSpawner(typeof(NetworkIdentity), Spawn, true);
            _dispatcherObject = new GameObject("Dispatcher");
            _dispatcher = _dispatcherObject.AddComponent<UnityMainThreadDispatcher>();
            GameObject.DontDestroyOnLoad(_dispatcherObject);
        }

        private static INetworkSpawnable Spawn(ObjectManagePacket packet, NetworkHandle handle)
        {
            ByteReader reader = new ByteReader(packet.ExtraData);
            UnityObjectData NetworkBehavior = reader.ReadPacketSerialized<UnityObjectData>();
            GameObject prefab = GetPrefabByID(NetworkBehavior.PrefabID);
            GameObject result = GameObject.Instantiate(prefab);
            List<string> trueTree = NetworkBehavior.Tree;
            trueTree.RemoveAt(0);
            trueTree.Reverse();
            GameObject parent = trueTree.FindParentByTree();
            if (parent == null)
            {
                Log.GlobalError("Can't find parent by tree! Tree: " + string.Join("/", trueTree));
            }
            else
            {
                result.transform.parent = parent.transform;
            }
            return result.GetComponent<NetworkIdentity>();
        }

        private static Dictionary<int, NetworkTransform> _transforms = new Dictionary<int, NetworkTransform>();

        private static Dictionary<int, NetworkAnimator> _animators = new Dictionary<int, NetworkAnimator>();

        private static Dictionary<int, GameObject> _prefabSpawnIds = new Dictionary<int, GameObject>();

        public static List<NetworkBehavior> GetNetworkBehaviors()
        {
            List<NetworkBehavior> behaviors = new List<NetworkBehavior>();
            foreach (INetworkObject obj in GetNetworkBehaviors().Where(x => x is NetworkBehavior))
            {
                behaviors.Add(obj as NetworkBehavior);
            }
            return behaviors;
        }

        public static NetworkAnimator GetNetworkAnimator(GameObject obj)
        {
            if (_animators.ContainsKey(obj.GetInstanceID()))
            {
                return _animators[obj.GetInstanceID()];
            }
            return null;
        }

        public static NetworkTransform GetNetworkTransform(GameObject obj)
        {
            if (_transforms.ContainsKey(obj.GetInstanceID()))
            {
                return _transforms[obj.GetInstanceID()];
            }
            return null;
        }

        public static int NextAvailablePrefabID
        {
            get
            {
                return _prefabSpawnIds.Keys.GetFirstEmptySlot();
            }
        }


        /// <summary>
        /// Registers a <see cref="NetworkIdentity"/> to be used as a prefab. The <see cref="GameObject"/> should Not be destroyed after being registered.
        /// </summary>
        /// <param name="id">
        /// </param>
        /// <param name="prefab">
        /// </param>
        /// <returns>
        /// True if the method succeeded, false if it failed.
        /// </returns>
        /// <exception cref="NullReferenceException"></exception>
        public static bool RegisterPrefab(int id, NetworkIdentity identity)
        {
            if (_prefabSpawnIds.ContainsKey(id))
            {
                Log.GlobalWarning($"Tried to register prefab (ID: {id}, ObjectName: {identity.gameObject.name}) with an ID that is already taken. ");
                return false;
            }
            _prefabSpawnIds.Add(id, identity.gameObject);
            identity.PrefabID = id;
            return true;
        }

        /// <summary>
        /// Registers a <see cref="GameObject"/> to be used as a prefab. The <see cref="GameObject"/> should Not be destroyed after being registered. The object Must have a <see cref="NetworkIdentity"/> attached.
        /// </summary>
        /// <param name="id">
        /// </param>
        /// <param name="prefab">
        /// </param>
        /// <returns>
        /// True if the method succeeded, false if it failed.
        /// </returns>
        /// <exception cref="NullReferenceException"></exception>
        public static bool RegisterPrefab(int id, GameObject prefab)
        {
            if (_prefabSpawnIds.ContainsKey(id))
            {
                Log.GlobalWarning($"Tried to register prefab (ID: {id}, ObjectName: {prefab.name}) with an ID that is already taken. ");
                return false;
            }
            _prefabSpawnIds.Add(id, prefab);
            NetworkIdentity netPrefab = prefab.GetComponent<NetworkIdentity>();
            if (netPrefab == null)
            {
                throw new NullReferenceException("All prefabs must have a valid NetworkIdentity.");
            }
            netPrefab.PrefabID = id;
            return true;
        }

        public static GameObject GetPrefabByID(int id)
        {
            if (_prefabSpawnIds.ContainsKey(id))
            {
                return _prefabSpawnIds[id];
            }
            else
            {
                return null;
            }
        }

        public static bool OverrideMultiplayerFlag = false;

        public static bool PlayingMultiplayer
        {
            get
            {
                if (NetworkServer.Active)
                {
                    return true;
                }
                if (NetworkClient.LocalClient != null && NetworkClient.LocalClient.CurrentConnectionState != ConnectionState.Disconnected)
                {
                    return true;
                }
                if(OverrideMultiplayerFlag)
                {
                    return true;
                }
                return false;
            }
        }

        public static void Register(NetworkBehavior obj)
        {
            if (obj.GetType() == typeof(NetworkTransform))
            {
                _transforms.Add(obj.gameObject.GetInstanceID(), (NetworkTransform)obj);
                return;
            }
            if (obj.GetType() == typeof(NetworkAnimator))
            {
                _animators.Add(obj.gameObject.GetInstanceID(), (NetworkAnimator)obj);
                return;
            }
        }

        public static void Unregister(NetworkBehavior obj)
        {
            if (obj.GetType() == typeof(NetworkTransform))
            {
                _transforms.Remove(obj.gameObject.GetInstanceID());
                return;
            }
            if (obj.GetType() == typeof(NetworkAnimator))
            {
                _animators.Remove(obj.gameObject.GetInstanceID());
                return;
            }
        }
    }
}
