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

        [NetworkInvokable(Direction = NetworkDirection.Server, Broadcast = true, CallLocal = true)]
        private void ClientTeleport(NetworkHandle handle)
        {
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
            Identity.gameObject.transform.localScale = _scale;
            Identity.gameObject.transform.localPosition = Vector3.Lerp(Identity.gameObject.transform.localPosition, _localPosition, LerpTime);
            Identity.gameObject.transform.position = Vector3.Lerp(Identity.gameObject.transform.position, _position, LerpTime);
            Identity.gameObject.transform.localRotation = Quaternion.Slerp(Identity.gameObject.transform.localRotation, _localRotation, LerpTime);
            Identity.gameObject.transform.rotation = Quaternion.Slerp(Identity.gameObject.transform.rotation, _rotation, LerpTime);
            if (SyncMode != ComponentSyncMode.FrameUpdate)
            {
                return;
            }
            SyncIfIsDifferent();
        }

        public override void OnBeforeRegister()
        {
            base.OnBeforeRegister();
            scale = new NetworkSyncVar<Vector3>(this, Identity.OwnershipMode, (scale) =>
            {
                _scale = scale;
            });
            rotation = new NetworkSyncVar<Quaternion>(this, Identity.OwnershipMode, (rotation) =>
            {
                //Identity.gameObject.transform.rotation = rotation;
                _rotation = rotation;
            });
            position = new NetworkSyncVar<Vector3>(this, Identity.OwnershipMode, (pos) =>
            {
                //Identity.gameObject.transform.position = pos;
                _position = pos;
            });
            localRotation = new NetworkSyncVar<Quaternion>(this, Identity.OwnershipMode, (localRotation) =>
            {
                //Identity.gameObject.transform.localRotation = localRotation;
                _localRotation = localRotation;
            });
            localPosition = new NetworkSyncVar<Vector3>(this, Identity.OwnershipMode, (localPosition) =>
            {
                //Identity.gameObject.transform.localPosition = localPosition;
                _localPosition = localPosition;
            });
        }

        public void ServerSync()
        {
            NetworkPosition = Identity.gameObject.transform.position;
            NetworkRotation = Identity.gameObject.transform.rotation;
            NetworkLocalPosition = Identity.gameObject.transform.localPosition;
            NetworkLocalRotation = Identity.gameObject.transform.localRotation;
            NetworkScale = Identity.gameObject.transform.localScale;
            NetworkInvoke(nameof(ClientTeleport));
        }

        public override void OnNetworkSpawned(NetworkClient spawner)
        {
            base.OnNetworkSpawned(spawner);
            ServerSync();
        }

        void Awake()
        {
            UnityNetworkManager.Register(this);
            _rotation = Identity.gameObject.transform.rotation;
            _localRotation = Identity.gameObject.transform.localRotation;
            _position = Identity.gameObject.transform.position;
            _localPosition = Identity.gameObject.transform.localPosition;
            _scale = Identity.gameObject.transform.localScale;
            NetworkPosition = Identity.gameObject.transform.position;
            NetworkRotation = Identity.gameObject.transform.rotation;
            NetworkLocalPosition = Identity.gameObject.transform.localPosition;
            NetworkLocalRotation = Identity.gameObject.transform.localRotation;
            NetworkScale = Identity.gameObject.transform.localScale;
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

        private Vector3 _scale;

        private NetworkSyncVar<Quaternion> rotation;

        private Quaternion _rotation;

        private NetworkSyncVar<Vector3> position;

        private Vector3 _position;

        private NetworkSyncVar<Vector3> localPosition;

        private Vector3 _localPosition;

        private NetworkSyncVar<Quaternion> localRotation;

        private Quaternion _localRotation;

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
                if (!IsOwner)
                {
                    return;
                }
                if (scale == null)
                {
                    return;
                }
                _scale = value;
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
                if (!IsOwner)
                {
                    return;
                }
                if (position == null)
                {
                    return;
                }
                _position = value;
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
                if (!IsOwner)
                {
                    return;
                }
                if (localPosition == null)
                {
                    return;
                }
                _localPosition = value;
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
                if (!IsOwner)
                {
                    return;
                }
                if (rotation == null)
                {
                    return;
                }
                _rotation = value;
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
                if (!IsOwner)
                {
                    return;
                }
                if (localRotation == null)
                {
                    return;
                }
                _localRotation = value;
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
