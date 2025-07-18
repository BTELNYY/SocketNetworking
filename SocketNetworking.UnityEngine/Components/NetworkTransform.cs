using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.SyncVars;
using SocketNetworking.UnityEngine.TypeWrappers;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkTransform : NetworkComponent
    {
        public void ServerSync()
        {
            NetworkPosition = NetworkPosition;
            NetworkRotation = NetworkRotation;
            NetworkLocalPosition = NetworkLocalPosition;
            NetworkLocalRotation = NetworkLocalRotation;
            NetworkScale = NetworkScale;
        }

        public override void OnNetworkSpawned(NetworkClient spawner)
        {
            base.OnNetworkSpawned(spawner);
            ServerSync();
        }

        void Awake()
        {
            UnityNetworkManager.Register(this);
        }

        void OnDestroy()
        {
            UnityNetworkManager.Unregister(this);
        }

        public NetworkTransform()
        {
            scale = new NetworkSyncVar<Vector3>(this, Identity.OwnershipMode, (scale) =>
            {
                transform.localScale = scale;
            });
            rotation = new NetworkSyncVar<Quaternion>(this, Identity.OwnershipMode, (rotation) =>
            {
                transform.rotation = rotation;
            });
            position = new NetworkSyncVar<Vector3>(this, Identity.OwnershipMode, (pos) =>
            {
                transform.position = pos;
            });
            localRotation = new NetworkSyncVar<Quaternion>(this, Identity.OwnershipMode, (localRotation) =>
            {
                transform.localRotation = localRotation;
            });
            localPosition = new NetworkSyncVar<Vector3>(this, Identity.OwnershipMode, (localPosition) =>
            {
                transform.localPosition = localPosition;
            });
        }

        private NetworkSyncVar<Vector3> scale;

        private NetworkSyncVar<Quaternion> rotation;

        private NetworkSyncVar<Vector3> position;

        private NetworkSyncVar<Vector3> localPosition;

        private NetworkSyncVar<Quaternion> localRotation;

        public Vector3 NetworkScale
        {
            get
            {
                return transform.localScale;
            }
            set
            {
                if (!IsOwner)
                {
                    return;
                }
                scale.Value = value;
            }
        }

        public Vector3 NetworkPosition
        {
            get
            {
                return transform.position;
            }
            set
            {
                if (!IsOwner)
                {
                    return;
                }
                position.Value = value;
            }
        }

        public Vector3 NetworkLocalPosition
        {
            get
            {
                return transform.localPosition;
            }
            set
            {
                if (!IsOwner)
                {
                    return;
                }
                localPosition.Value = value;
            }
        }


        public Quaternion NetworkRotation
        {
            get
            {
                return transform.rotation;
            }
            set
            {
                if (!IsOwner)
                {
                    return;
                }
                rotation.Value = value;
            }
        }

        public Quaternion NetworkLocalRotation
        {
            get
            {
                return transform.localRotation;
            }
            set
            {
                if (!IsOwner)
                {
                    return;
                }
                localRotation.Value = value;
            }
        }

        public void NetworkRotate(Vector3 euler, Space relativeTo = Space.Self)
        {
            if (!IsOwner)
            {
                return;
            }
            SerializableVector3 vector3 = new SerializableVector3(euler);
            NetworkInvoke(nameof(GetNetworkRotation), new object[] { vector3, relativeTo });
        }

        [NetworkInvokable]
        private void GetNetworkRotation(SerializableVector3 euler, Space relativeTo)
        {
            transform.Rotate(euler.Vector, relativeTo);
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkRotate(euler.Vector, relativeTo);
            }
        }

        public void NetworkRotate(Vector3 euler, float angle, Space relativeTo = Space.Self)
        {
            if (!IsOwner)
            {
                return;
            }
            SerializableVector3 vector3 = new SerializableVector3(euler);
            NetworkInvoke(nameof(GetNetworkRotation), new object[] { vector3, angle, relativeTo });
        }

        [NetworkInvokable]
        private void GetNetworkRotation(SerializableVector3 euler, float angle, Space relativeTo)
        {
            transform.Rotate(euler.Vector, angle, relativeTo);
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkRotate(euler.Vector, angle, relativeTo);
            }
        }

        public void NetworkRotateAround(Vector3 point, Vector3 axis, float angle)
        {
            if (!IsOwner)
            {
                return;
            }
            SerializableVector3 vecPoint = new SerializableVector3(point);
            SerializableVector3 vecAxis = new SerializableVector3(axis);
            NetworkInvoke(nameof(GetNetworkRotation), new object[] { vecPoint, vecAxis, angle });
        }

        [NetworkInvokable]
        private void GetNetworkRotation(SerializableVector3 point, SerializableVector3 axis, float angle)
        {
            transform.RotateAround(point.Vector, axis.Vector, angle);
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkRotateAround(point.Vector, point.Vector, angle);
            }
        }

        public void NetworkTranslate(Vector3 position, Space relativeTo = Space.Self)
        {
            if (!IsOwner)
            {
                return;
            }
            NetworkInvoke(nameof(GetNetworkTranslate), new object[] { new SerializableVector3(position), relativeTo });
        }

        [NetworkInvokable]
        private void GetNetworkTranslate(SerializableVector3 vector3, Space space)
        {
            transform.Translate(vector3.Vector, space);
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkTranslate(vector3.Vector, space);
            }
        }

        public void NetworkLookAt(Vector3 position)
        {
            if (!IsOwner)
            {
                return;
            }
            NetworkInvoke(nameof(GetNetworkLookAt), new object[] { new SerializableVector3(position) });
        }

        [NetworkInvokable]
        private void GetNetworkLookAt(SerializableVector3 vector3)
        {
            transform.LookAt(vector3.Vector);
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkLookAt(vector3.Vector);
            }
        }
    }
}
