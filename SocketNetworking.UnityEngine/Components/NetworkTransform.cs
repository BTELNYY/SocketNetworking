using SocketNetworking.Attributes;
using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Serialization;
using SocketNetworking.UnityEngine.TypeWrappers;
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
                SerializableVector3 vec = new SerializableVector3(value);
                NetworkInvoke(nameof(GetNewNetworkPosition), new object[] { vec });
            }
        }

        [NetworkInvokable]
        private void GetNewNetworkPosition(SerializableVector3 position)
        {
            transform.position = position.Vector;
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkPosition = position.Vector;
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
                SerializableQuaternion quat = new SerializableQuaternion(value);
                NetworkInvoke(nameof(GetNewNetworkRotation), new object[] { quat });
            }
        }

        [NetworkInvokable]
        private void GetNewNetworkRotation(SerializableQuaternion rotation)
        {
            transform.rotation = rotation.Quaternion;
            if(NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkRotation = rotation.Quaternion;
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
            if(NetworkManager.WhereAmI == ClientLocation.Remote)
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
            if(NetworkManager.WhereAmI == ClientLocation.Remote)
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
            if(NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkLookAt(vector3.Vector);
            }
        }
    }
}
