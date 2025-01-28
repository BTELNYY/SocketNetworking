using SocketNetworking.PacketSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using SocketNetworking.UnityEngine.Components;
using SocketNetworking.Server;
using SocketNetworking.Client;
using SocketNetworking.Shared;
using JetBrains.Annotations;
using SocketNetworking.PacketSystem.Packets;

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

        internal static void Init()
        {
            if (_initted)
            {
                return;
            }
            _initted = true;
            NetworkManager.ImportCustomPackets(Assembly.GetExecutingAssembly());
            NetworkServer.ClientType = typeof(UnityNetworkClient);
            RegisterSpawner(typeof(NetworkIdentity), Spawn, true);
            _dispatcherObject = new GameObject("Dispatcher");
            _dispatcher = _dispatcherObject.AddComponent<UnityMainThreadDispatcher>();
            GameObject.DontDestroyOnLoad(_dispatcherObject);
        }

        private static INetworkSpawnable Spawn(ObjectManagePacket packet, NetworkHandle handle)
        {
            ByteReader reader = new ByteReader(packet.ExtraData);
            UnityNetworkBehavior NetworkBehavior = reader.ReadPacketSerialized<UnityNetworkBehavior>();
            GameObject prefab = GetPrefabByID(NetworkBehavior.PrefabID);
            GameObject result = GameObject.Instantiate(prefab);
            return (INetworkSpawnable)result.GetComponent<NetworkIdentity>();
        }

        private static Dictionary<GameObject, NetworkTransform> _transforms = new Dictionary<GameObject, NetworkTransform>();

        private static Dictionary<GameObject, NetworkAnimator> _animators = new Dictionary<GameObject, NetworkAnimator>();

        private static Dictionary<int, GameObject> _prefabSpawnIds = new Dictionary<int, GameObject>();

        public static List<NetworkBehavior> GetNetworkBehaviors()
        {
            List<NetworkBehavior> behaviors = new List<NetworkBehavior>();
            foreach(INetworkObject obj in GetNetworkBehaviors().Where(x => x is NetworkBehavior))
            {
                behaviors.Add(obj as NetworkBehavior);
            }
            return behaviors;
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
            if(_prefabSpawnIds.ContainsKey(id))
            {
                Log.GlobalWarning($"Tried to register prefab (ID: {id}, ObjectName: {prefab.name}) with an ID that is already taken. ");
                return false;
            }
            _prefabSpawnIds.Add(id, prefab);
            NetworkIdentity netPrefab = prefab.GetComponent<NetworkIdentity>();
            if(netPrefab == null)
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

        public static bool PlayingMultiplayer
        {
            get
            {
                if (NetworkServer.Active)
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
                _transforms.Add(obj.gameObject, (NetworkTransform)obj);
                return;
            }
            if (obj.GetType() == typeof(NetworkAnimator))
            {
                _animators.Add(obj.gameObject, (NetworkAnimator)obj);
                return;
            }
        }

        public static void Unregister(NetworkBehavior obj)
        {
            if (obj.GetType() == typeof(NetworkTransform))
            {
                _transforms.Remove(obj.gameObject);
                return;
            }
            if (obj.GetType() == typeof(NetworkAnimator))
            {
                _animators.Remove(obj.gameObject);
                return;
            }
        }

        public static NetworkTransform GetNetworkTransform(GameObject gameObject)
        {
            if (_transforms.ContainsKey(gameObject))
            {
                return _transforms[gameObject];
            }
            return null;
        }

        public static NetworkAnimator GetNetworkAnimator(GameObject gameObject)
        {
            if (_animators.ContainsKey(gameObject))
            {
                return _animators[gameObject];
            }
            return null;
        }
    }
}
