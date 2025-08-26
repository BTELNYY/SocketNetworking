using System;
using System.Collections.Generic;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Serialization;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Components
{
    /// <summary>
    /// Essentially an additional script you can place on an object. Note: You must <b>Always</b> create and register these on init or you will have, interesting issues. See <see cref="NetworkIdentity.ReceiveExtraData(byte[])"/> for more on why you should adhere to my warnings.
    /// </summary>
    public class NetworkComponent : NetworkBehavior
    {

        public override bool IsRegistered
        {
            get
            {
                if (_identity == null)
                {
                    return false;
                }
                return base.IsRegistered;
            }
        }

        /// <summary>
        /// Use <see cref="NetworkIdentity"/> to spawn objects from prefabs.
        /// </summary>
        public override bool Spawnable => false;

        /// <summary>
        /// The <see cref="NetworkIdentity"/> of the current <see cref="GameObject"/>.
        /// </summary>
        public NetworkIdentity Identity
        {
            get
            {
                if (_identity == null)
                {
                    EnsureIdentityExists();
                }
                return _identity;
            }
        }

        private NetworkIdentity _identity = null;

        public void OverrideIdentity(NetworkIdentity identity)
        {
            _identity?.UnregisterComponent(this);
            _identity = identity;
            identity.RegisterComponent(this);
        }

        public override int NetworkID { get => Identity != null ? Identity.NetworkID : 0; set => Identity.NetworkID = value; }

        public override int OwnerClientID { get => Identity.OwnerClientID; set => Identity.OwnerClientID = value; }

        public override bool Active { get => Identity.Active; set => Identity.Active = value; }

        public override ObjectVisibilityMode ObjectVisibilityMode { get => Identity.ObjectVisibilityMode; set => Identity.ObjectVisibilityMode = value; }

        public override OwnershipMode OwnershipMode { get => Identity.OwnershipMode; set => Identity.OwnershipMode = value; }

        public byte[] ComponentData = new byte[0];

        public override bool Enabled
        {
            get
            {
                //we aren't registered?
                if (NetworkID == 0)
                {
                    Enabled = false;
                    return false;
                }
                return base.Enabled;
            }
            set
            {
                base.Enabled = value;
                if (value)
                {
                    OnEnableComponent();
                }
                else
                {
                    OnDisableComponent();
                }
            }
        }

        private void Start()
        {
            if (NetworkManager.WhereAmI == ClientLocation.Unknown || NetworkID == 0)
            {
                Enabled = false;
            }
        }

        public virtual void OnSetupComponent()
        {

        }

        public virtual void OnEnableComponent()
        {

        }

        public virtual void OnDisableComponent()
        {

        }

        public virtual ByteWriter SendComponentData()
        {
            return new ByteWriter(ComponentData);
        }

        public virtual ByteReader ReceiveComponentData(byte[] data)
        {
            ComponentData = data;
            return new ByteReader(data);
        }

        public void EnsureIdentityExists()
        {
            try
            {
                _identity = gameObject.GetComponent<NetworkIdentity>();
                if (_identity == null)
                {
                    throw new InvalidOperationException("All Network Objects must have a NetworkIdentity.");
                }
                OverrideIdentity(_identity);
            }
            catch (Exception e)
            {
                Log.GlobalError($"Identity cannot be created!\n{e}");
            }
        }

        public override void RegisterObject()
        {
            if (gameObject == null)
            {
                return;
            }
            base.RegisterObject();
        }

        public override void Destroy()
        {
            DestroyImmediate(this);
        }

        void Awake()
        {
            EnsureIdentityExists();
            if (NetworkManager.WhereAmI == ClientLocation.Unknown || NetworkID == 0)
            {
                Enabled = false;
            }
        }

        public override IEnumerable<int> PrivilegedIDs
        {
            get
            {
                return Identity.PrivilegedIDs;
            }
            set
            {
                Identity.PrivilegedIDs = value;
            }
        }

        public override bool HasPrivilege(int clientId)
        {
            return Identity.HasPrivilege(clientId);
        }

        public override void GrantPrivilege(int clientId)
        {
            Identity.GrantPrivilege(clientId);
        }

        public override void RemovePrivilege(int clientId)
        {
            Identity.RemovePrivilege(clientId);
        }
    }
}
