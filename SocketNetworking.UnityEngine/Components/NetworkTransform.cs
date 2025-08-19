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

        public void SyncIfIsDifferent()
        {
            if (!IsPrivileged)
            {
                return;
            }
            if (position.Value != transform.position)
            {
                position.Value = transform.position;
            }
            if (rotation.Value != transform.rotation)
            {
                rotation.Value = transform.rotation;
            }
            if (localRotation.Value != transform.localRotation)
            {
                localRotation.Value = transform.localRotation;
            }
            if (localPosition.Value != transform.localPosition)
            {
                localPosition.Value = transform.localPosition;
            }
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
            transform.localPosition = Vector3.Lerp(transform.localPosition, _localPosition, LerpTime);
            transform.position = Vector3.Lerp(transform.position, _position, LerpTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _localRotation, LerpTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, _rotation, LerpTime);
            if (SyncMode != ComponentSyncMode.FrameUpdate)
            {
                return;
            }
            SyncIfIsDifferent();
        }

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
            //Scale is set and not lerped because, why the fuck would it be lerped?
            scale = new NetworkSyncVar<Vector3>(this, Identity.OwnershipMode, (scale) =>
            {
                transform.localScale = scale;
            });
            rotation = new NetworkSyncVar<Quaternion>(this, Identity.OwnershipMode, (rotation) =>
            {
                //transform.rotation = rotation;
                _rotation = rotation;
            });
            position = new NetworkSyncVar<Vector3>(this, Identity.OwnershipMode, (pos) =>
            {
                //transform.position = pos;
                _position = pos;
            });
            localRotation = new NetworkSyncVar<Quaternion>(this, Identity.OwnershipMode, (localRotation) =>
            {
                //transform.localRotation = localRotation;
                _localRotation = localRotation;
            });
            localPosition = new NetworkSyncVar<Vector3>(this, Identity.OwnershipMode, (localPosition) =>
            {
                //transform.localPosition = localPosition;
                _localPosition = localPosition;
            });
        }

        void OnDestroy()
        {
            UnityNetworkManager.Unregister(this);
        }

        private NetworkSyncVar<Vector3> scale;

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
                    if(NetworkClient.LocalClient == null)
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
                return Time.deltaTime * Mathf.Clamp((float)(Latency / 0.5), 1f, 1000f);
            }
        }

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

        [NetworkInvokable(callLocal: true, broadcast: true)]
        private void GetNetworkRotation(SerializableVector3 euler, Space relativeTo)
        {
            transform.Rotate(euler.Vector, relativeTo);
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
            transform.Rotate(euler.Vector, angle, relativeTo);
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
            transform.RotateAround(point.Vector, axis.Vector, angle);
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
            transform.Translate(vector3.Vector, space);
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
            transform.LookAt(vector3.Vector);
        }
    }
}
