using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.SyncVars;
using SocketNetworking.UnityEngine.TypeWrappers;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkTransform : NetworkComponent
    {
        public ComponentSyncMode SyncMode { get; set; } = ComponentSyncMode.Automatic;

        public float MinimumDifferenceForSync { get; set; } = 0.001f;

        public void SyncIfIsDifferent()
        {
            if (!IsPrivileged)
            {
                return;
            }
            if (position.Value != Identity.gameObject.transform.position && Vector3.Distance(position.Value, Identity.gameObject.transform.position) > MinimumDifferenceForSync)
            {
                position.Value = Identity.gameObject.transform.position;
            }
            if (rotation.Value != Identity.gameObject.transform.rotation)
            {
                rotation.Value = Identity.gameObject.transform.rotation;
            }
            if (localRotation.Value != Identity.gameObject.transform.localRotation)
            {
                localRotation.Value = Identity.gameObject.transform.localRotation;
            }
            if (localPosition.Value != Identity.gameObject.transform.localPosition && Vector3.Distance(localPosition.Value, Identity.gameObject.transform.localPosition) > MinimumDifferenceForSync)
            {
                localPosition.Value = Identity.gameObject.transform.localPosition;
            }
        }

        /// <summary>
        /// Forces the remote clients to instantly move the object to the current position on the server instead of using <see cref="Vector3.Lerp(Vector3, Vector3, float)"/> or <see cref="Quaternion.Lerp(Quaternion, Quaternion, float)"/>. Note that if the client has outdated values for the <see cref="INetworkSyncVar"/>s, the teleportation may be inaccurate. Use <see cref="ServerSync"/> to avoid this.
        /// </summary>
        public void ServerTeleport()
        {
            NetworkInvoke(nameof(ClientTeleport));
        }

        [NetworkInvokable(Direction = NetworkDirection.Server, Broadcast = true)]
        private void ClientTeleport(NetworkHandle handle)
        {
            //prevent stack overflow.
            if (IsClient && IsOwner)
            {
                return;
            }
            Identity.gameObject.transform.localScale = scale.Value;
            Identity.gameObject.transform.rotation = rotation.Value;
            Identity.gameObject.transform.localRotation = localRotation.Value;
            Identity.gameObject.transform.position = position.Value;
            Identity.gameObject.transform.localPosition = localPosition.Value;
        }

        void FixedUpdate()
        {
            if (SyncMode != ComponentSyncMode.PhysicsUpdate)
            {
                return;
            }
            SyncIfIsDifferent();
        }

        void Update()
        {
            if (!IsOwner)
            {
                Identity.gameObject.transform.localScale = scale.Value;
                Identity.gameObject.transform.localPosition = Vector3.Lerp(Identity.gameObject.transform.localPosition, localPosition.Value, LerpTime);
                Identity.gameObject.transform.position = Vector3.Lerp(Identity.gameObject.transform.position, position.Value, LerpTime);
                Identity.gameObject.transform.localRotation = Quaternion.Slerp(Identity.gameObject.transform.localRotation, localRotation.Value, LerpTime);
                Identity.gameObject.transform.rotation = Quaternion.Slerp(Identity.gameObject.transform.rotation, rotation.Value, LerpTime);
            }
            if (SyncMode != ComponentSyncMode.FrameUpdate)
            {
                return;
            }
            SyncIfIsDifferent();
        }

        public override void OnBeforeRegister()
        {
            base.OnBeforeRegister();
            scale = new NetworkSyncVar<Vector3>(this, Identity.OwnershipMode);
            rotation = new NetworkSyncVar<Quaternion>(this, Identity.OwnershipMode);
            position = new NetworkSyncVar<Vector3>(this, Identity.OwnershipMode);
            localRotation = new NetworkSyncVar<Quaternion>(this, Identity.OwnershipMode);
            localPosition = new NetworkSyncVar<Vector3>(this, Identity.OwnershipMode);
        }


        /// <summary>
        /// Synchronizes all <see cref="INetworkSyncVar"/>s on this <see cref="NetworkTransform"/> then calls <see cref="ServerTeleport"/>.
        /// </summary>
        public void ServerSync()
        {
            NetworkPosition = Identity.gameObject.transform.position;
            NetworkRotation = Identity.gameObject.transform.rotation;
            NetworkLocalPosition = Identity.gameObject.transform.localPosition;
            NetworkLocalRotation = Identity.gameObject.transform.localRotation;
            NetworkScale = Identity.gameObject.transform.localScale;
            NetworkInvoke(nameof(ClientUpdatePositionsSafe), Identity.gameObject.transform.position, Identity.gameObject.transform.localPosition, Identity.gameObject.transform.rotation, Identity.gameObject.transform.localRotation, Identity.gameObject.transform.localScale);
            ServerTeleport();
        }

        [NetworkInvokable(Direction = NetworkDirection.Server, Broadcast = true)]
        private void ClientUpdatePositionsSafe(NetworkHandle handle, Vector3 pos, Vector3 lpos, Quaternion rot, Quaternion lrot, Vector3 scale)
        {
            position.RawSet(pos, handle.Client);
            localPosition.RawSet(pos, handle.Client);
            rotation.RawSet(rot, handle.Client);
            localRotation.RawSet(lrot, handle.Client);
            this.scale.RawSet(scale, handle.Client);
        }

        public override void OnNetworkSpawned(NetworkClient spawner)
        {
            base.OnNetworkSpawned(spawner);
            ServerSync();
        }

        void Awake()
        {
            UnityNetworkManager.Register(this);
            if (IsOwner)
            {
                NetworkPosition = Identity.gameObject.transform.position;
                NetworkRotation = Identity.gameObject.transform.rotation;
                NetworkLocalPosition = Identity.gameObject.transform.localPosition;
                NetworkLocalRotation = Identity.gameObject.transform.localRotation;
                NetworkScale = Identity.gameObject.transform.localScale;
            }
        }

        void OnDestroy()
        {
            UnityNetworkManager.Unregister(this);
        }

        public override void Destroy()
        {
            UnityNetworkManager.Unregister(this);
            base.Destroy();
        }

        private NetworkSyncVar<Vector3> scale;

        //private Vector3 _scale;

        private NetworkSyncVar<Quaternion> rotation;

        //private Quaternion _rotation;

        private NetworkSyncVar<Vector3> position;

        //private Vector3 _position;

        private NetworkSyncVar<Vector3> localPosition;

        //private Vector3 _localPosition;

        private NetworkSyncVar<Quaternion> localRotation;

        //private Quaternion _localRotation;

        /// <summary>
        /// Returns the <see cref="NetworkClient.LocalClient"/>s <see cref="NetworkClient.Latency"/> or the <see cref="INetworkObject.OwnerClientID"/>s <see cref="NetworkClient.Latency"/>.
        /// </summary>
        public long Latency
        {
            get
            {
                if (NetworkManager.WhereAmI == ClientLocation.Remote)
                {
                    if (this.GetOwner() == null)
                    {
                        return 0;
                    }
                    return this.GetOwner().Latency;
                }
                else
                {
                    if (NetworkClient.LocalClient == null)
                    {
                        return 0;
                    }
                    return NetworkClient.LocalClient.Latency;
                }
            }
        }

        public float LerpTime
        {
            get
            {
                return Time.deltaTime * Mathf.Clamp((float)(Latency / 0.5), 0.1f, 1000f);
            }
        }

        public Vector3 NetworkScale
        {
            get
            {
                return Identity.gameObject.transform.localScale;
            }
            set
            {
                //if (!IsOwner)
                //{
                //    return;
                //}
                if (scale == null)
                {
                    return;
                }
                Identity.gameObject.transform.localScale = value;
                scale.Value = value;
            }
        }

        public Vector3 NetworkPosition
        {
            get
            {
                return Identity.gameObject.transform.position;
            }
            set
            {
                //if (!IsOwner)
                //{
                //    return;
                //}
                if (position == null)
                {
                    return;
                }
                Identity.gameObject.transform.position = value;
                position.Value = value;
            }
        }

        public Vector3 NetworkLocalPosition
        {
            get
            {
                return Identity.gameObject.transform.localPosition;
            }
            set
            {
                //if (!IsOwner)
                //{
                //    return;
                //}
                if (localPosition == null)
                {
                    return;
                }
                Identity.gameObject.transform.localPosition = value;
                localPosition.Value = value;
            }
        }


        public Quaternion NetworkRotation
        {
            get
            {
                return Identity.gameObject.transform.rotation;
            }
            set
            {
                //if (!IsOwner)
                //{
                //    return;
                //}
                if (rotation == null)
                {
                    return;
                }
                Identity.gameObject.transform.rotation = value;
                rotation.Value = value;
            }
        }

        public Quaternion NetworkLocalRotation
        {
            get
            {
                return Identity.gameObject.transform.localRotation;
            }
            set
            {
                //if (!IsOwner)
                //{
                //    return;
                //}
                if (localRotation == null)
                {
                    return;
                }
                Identity.gameObject.transform.localRotation = value;
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

        [NetworkInvokable(callLocal: true, broadcast: true)]
        private void GetNetworkRotation(SerializableVector3 euler, Space relativeTo)
        {
            Identity.gameObject.transform.Rotate(euler.Vector, relativeTo);
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

        [NetworkInvokable(callLocal: true, broadcast: true)]
        private void GetNetworkRotation(SerializableVector3 euler, float angle, Space relativeTo)
        {
            Identity.gameObject.transform.Rotate(euler.Vector, angle, relativeTo);
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

        [NetworkInvokable(callLocal: true, broadcast: true)]
        private void GetNetworkRotation(SerializableVector3 point, SerializableVector3 axis, float angle)
        {
            Identity.gameObject.transform.RotateAround(point.Vector, axis.Vector, angle);
        }

        public void NetworkTranslate(Vector3 position, Space relativeTo = Space.Self)
        {
            if (!IsOwner)
            {
                return;
            }
            NetworkInvoke(nameof(GetNetworkTranslate), new object[] { new SerializableVector3(position), relativeTo });
        }

        [NetworkInvokable(callLocal: true, broadcast: true)]
        private void GetNetworkTranslate(SerializableVector3 vector3, Space space)
        {
            Identity.gameObject.transform.Translate(vector3.Vector, space);
        }

        public void NetworkLookAt(Vector3 position)
        {
            if (!IsOwner)
            {
                return;
            }
            NetworkInvoke(nameof(GetNetworkLookAt), new object[] { new SerializableVector3(position) });
        }

        [NetworkInvokable(callLocal: true, broadcast: true)]
        private void GetNetworkLookAt(SerializableVector3 vector3)
        {
            Identity.gameObject.transform.LookAt(vector3.Vector);
        }
    }
}
