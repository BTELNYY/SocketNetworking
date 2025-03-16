using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkTransform : NetworkComponent
    {
        public void ServerSyncPositionAndRotation()
        {
            NetworkPosition = NetworkPosition;
            NetworkRotation = NetworkRotation;
        }

        public override void OnNetworkSpawned(NetworkClient spawner)
        {
            base.OnNetworkSpawned(spawner);
            ServerSyncPositionAndRotation();
        }

        public override ByteWriter SendExtraData()
        {
            ByteWriter writer = base.SendExtraData();
            writer.WriteVector3(NetworkPosition);
            writer.WriteQuaternion(NetworkRotation);
            return writer;
        }

        public override ByteReader ReceiveExtraData(byte[] extraData)
        {
            ByteReader reader = base.ReceiveExtraData(extraData);
            transform.position = reader.ReadVector3();
            transform.rotation = reader.ReadQuaternion();
            return reader;
        }

        void Awake()
        {
            UnityNetworkManager.Register(this);
        }

        void OnDestroy()
        {
            UnityNetworkManager.Unregister(this);
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
                transform.position = value;
                NetworkInvoke(nameof(GetNewNetworkPosition), new object[] { value });
            }
        }

        [NetworkInvokable]
        private void GetNewNetworkPosition(Vector3 position)
        {
            transform.position = position;
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkPosition = position;
            }
        }

        public Vector3 NetworkLocalPosition
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
                transform.position = value;
                NetworkInvoke(nameof(GetNewNetworkLocalPosition), new object[] { value });
            }
        }

        [NetworkInvokable]
        private void GetNewNetworkLocalPosition(Vector3 position)
        {
            transform.localPosition = position;
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkLocalPosition = position;
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
                transform.rotation = value;
                NetworkInvoke(nameof(GetNewNetworkRotation), new object[] { value });
            }
        }

        [NetworkInvokable]
        private void GetNewNetworkRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkRotation = rotation;
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
                transform.localRotation = value;
                NetworkInvoke(nameof(GetNewNetworkLocalRotation), new object[] { value });
            }
        }

        [NetworkInvokable]
        private void GetNewNetworkLocalRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkRotation = rotation;
            }
        }

        public void NetworkRotate(Vector3 euler, Space relativeTo = Space.Self)
        {
            if (!IsOwner)
            {
                return;
            }
            NetworkInvoke(nameof(GetNetworkRotation), new object[] { euler, relativeTo });
        }

        [NetworkInvokable]
        private void GetNetworkRotation(Vector3 euler, Space relativeTo)
        {
            transform.Rotate(euler, relativeTo);
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkRotate(euler, relativeTo);
            }
        }

        public void NetworkRotate(Vector3 euler, float angle, Space relativeTo = Space.Self)
        {
            if (!IsOwner)
            {
                return;
            }
            NetworkInvoke(nameof(GetNetworkRotation), new object[] { euler, angle, relativeTo });
        }

        [NetworkInvokable]
        private void GetNetworkRotation(Vector3 euler, float angle, Space relativeTo)
        {
            transform.Rotate(euler, angle, relativeTo);
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkRotate(euler, angle, relativeTo);
            }
        }

        public void NetworkRotateAround(Vector3 point, Vector3 axis, float angle)
        {
            if (!IsOwner)
            {
                return;
            }
            NetworkInvoke(nameof(GetNetworkRotation), new object[] { point, axis, angle });
        }

        [NetworkInvokable]
        private void GetNetworkRotation(Vector3 point, Vector3 axis, float angle)
        {
            transform.RotateAround(point, axis, angle);
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkRotateAround(point, point, angle);
            }
        }

        public void NetworkTranslate(Vector3 position, Space relativeTo = Space.Self)
        {
            if (!IsOwner)
            {
                return;
            }
            NetworkInvoke(nameof(GetNetworkTranslate), new object[] { position, relativeTo });
        }

        [NetworkInvokable]
        private void GetNetworkTranslate(Vector3 vector3, Space space)
        {
            transform.Translate(vector3, space);
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkTranslate(vector3, space);
            }
        }

        public void NetworkLookAt(Vector3 position)
        {
            if (!IsOwner)
            {
                return;
            }
            NetworkInvoke(nameof(GetNetworkLookAt), new object[] { position });
        }

        [NetworkInvokable]
        private void GetNetworkLookAt(Vector3 vector3)
        {
            transform.LookAt(vector3);
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkLookAt(vector3);
            }
        }
    }
}
