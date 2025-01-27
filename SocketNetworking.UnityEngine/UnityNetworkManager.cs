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

namespace SocketNetworking.UnityEngine
{
    public class UnityNetworkManager : NetworkManager
    {
        private static Dictionary<GameObject, NetworkTransform> _transforms = new Dictionary<GameObject, NetworkTransform>();

        private static Dictionary<GameObject, NetworkAnimator> _animators = new Dictionary<GameObject, NetworkAnimator>();

        private static Dictionary<int, GameObject> _prefabSpawnIds = new Dictionary<int, GameObject>();

        public static List<NetworkBehavior> GetNetworkBehaviors()
        {
            List<NetworkBehavior> behaviors = new List<NetworkBehavior>();
            foreach(INetworkObject obj in GetNetworkObjects().Where(x => x is NetworkBehavior))
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
        /// Registers a <see cref="GameObject"/> to be used as a prefab.
        /// </summary>
        /// <param name="id">
        /// The ID of the prefab, cannot be a duplicate. Use <see cref="UnityNetworkManager.NextAvailablePrefabID"/> to get the next empty prefab ID
        /// </param>
        /// <param name="prefab">
        /// The prefab game object, the object should NOT be destroyed after being registered
        /// </param>
        /// <returns>
        /// True if the method succeeded, false if it failed.
        /// </returns>
        public static bool RegisterPrefab(int id, GameObject prefab)
        {
            if(_prefabSpawnIds.ContainsKey(id))
            {
                Log.GlobalWarning($"Tried to register prefab (ID: {id}, ObjectName: {prefab.name}) with an ID that is already taken. ");
                return false;
            }
            _prefabSpawnIds.Add(id, prefab);
            NetworkPrefab netPrefab = prefab.GetComponent<NetworkPrefab>();
            if(netPrefab == null)
            {
                netPrefab = prefab.AddComponent<NetworkPrefab>();
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

        public static void PrepareForUnity()
        {
            NetworkManager.ImportCustomPackets(Assembly.GetExecutingAssembly());
            NetworkServer.ClientType = typeof(UnityNetworkClient);
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

        public static void Register(NetworkObject obj)
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

        public static void Unregister(NetworkObject obj)
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
