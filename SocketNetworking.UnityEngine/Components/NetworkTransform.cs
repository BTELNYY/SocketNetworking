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

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkTransform : NetworkObject
    {
        private string uuid = string.Empty;

        public string UUID
        {
            get
            {
                return uuid;
            }
        }

        public override void SendPacket(Packet packet)
        {
            if(packet is NetworkTransformBasePacket basePacket)
            {
                basePacket.UUID = UUID;
                base.SendPacket(basePacket);
                return;
            }
            base.SendPacket(packet);
        }

        void Awake()
        {
            uuid = Guid.NewGuid().ToString();
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
                if (!IsSyncOwner)
                {
                    //Logger.LogWarning($"Tried to set property of {gameObject.name} from illegal client side.");
                    return;
                }
                transform.position = value;
                NetworkTransformPositionUpdatePacket packet = new NetworkTransformPositionUpdatePacket();
                packet.Position = value;
                packet.Rotation = transform.rotation;
                SendPacket(packet);
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
                if (!IsSyncOwner)
                {
                    //Logger.LogWarning($"Tried to set property of {gameObject.name} from illegal client side.");
                    return;
                }
                transform.rotation = value;
                NetworkTransformPositionUpdatePacket packet = new NetworkTransformPositionUpdatePacket();
                packet.Position = transform.position;
                packet.Rotation = value;
                SendPacket(packet);
            }
        }

        [PacketListener(typeof(NetworkTransformPositionUpdatePacket), PacketDirection.Any)]
        private void OnNetworkUpdatePositionOrRotation(NetworkTransformPositionUpdatePacket packet, NetworkClient client)
        {
            if (!ShouldBeReceivingPackets || packet.UUID != UUID)
            {
                return;
            }
            transform.position = packet.Position;
            transform.rotation = packet.Rotation;
        }

        public void NetworkRotate(Vector3 euler, Space relativeTo = Space.Self)
        {
            if (!IsSyncOwner)
            {
                return;
            }
            NetworkTransformRotatePacket packet = new NetworkTransformRotatePacket(euler, relativeTo);
            SendPacket(packet);
        }

        public void NetworkRotate(Vector3 euler, float angle, Space relativeTo = Space.Self)
        {
            if (!IsSyncOwner)
            {
                return;
            }
            NetworkTransformRotatePacket packet = new NetworkTransformRotatePacket(euler, relativeTo);
            packet.Angle = angle;
            SendPacket(packet);
        }

        [PacketListener(typeof(NetworkTransformRotatePacket), PacketDirection.Any)]
        private void OnTransformRotate(NetworkTransformRotatePacket packet, NetworkClient client)
        {
            if (!ShouldBeReceivingPackets || packet.UUID != UUID)
            {
                return;
            }
            if (packet.Angle == float.NaN)
            {
                transform.Rotate(packet.Rotation, packet.Space);
            }
            else
            {
                transform.Rotate(packet.Rotation, packet.Angle, packet.Space);
            }
        }

        public void NetworkRotateAround(Vector3 point, Vector3 axis, float angle)
        {
            if (!IsSyncOwner)
            {
                return;
            }
            NetworkTransformRotateAroundPacket packet = new NetworkTransformRotateAroundPacket(point, axis, angle);
            SendPacket(packet); 
        }

        [PacketListener(typeof(NetworkTransformRotateAroundPacket), PacketDirection.Any)]
        private void OnNetworkRotateAround(NetworkTransformRotateAroundPacket packet, NetworkClient client)
        {
            if (!ShouldBeReceivingPackets || packet.UUID != UUID)
            {
                return;
            }
            transform.RotateAround(packet.Axis, packet.Point, packet.Angle);
        }
    }
}
