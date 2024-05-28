using SocketNetworking.Attributes;
using SocketNetworking;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketNetworking.PacketSystem;
using SocketNetworking.UnityEngine.Packets.NetworkTransform;
using SocketNetworking.UnityEngine.Packets;
using SocketNetworking.UnityEngine.TypeWrappers;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkTransform : NetworkComponent
    {
        public void ServerSyncRotationAndPosition()
        {
            NetworkPosition = NetworkPosition;
            NetworkRotation = NetworkRotation;
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

        [NetworkInvocable]
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

        [NetworkInvocable]
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

        [NetworkInvocable]
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

        [NetworkInvocable]
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

        [NetworkInvocable]
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

        [NetworkInvocable]
        private void GetNetworkTranslate(SerializableVector3 vector3, Space space)
        {
            transform.Translate(vector3.Vector, space);
            if(NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkTranslate(vector3.Vector, space);
            }
        }

        public override void OnClientObjectCreated(UnityNetworkClient client)
        {
            base.OnClientObjectCreated(client);
            NetworkPosition = transform.position;
            NetworkRotation = transform.rotation;
        }
    }
}
