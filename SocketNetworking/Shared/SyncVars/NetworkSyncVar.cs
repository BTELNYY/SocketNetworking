using System;
using System.Collections.Generic;
using SocketNetworking.Client;
using SocketNetworking.Server;
using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Shared.SyncVars
{
    /// <summary>
    /// The concrete implementation of <see cref="INetworkSyncVar"/> with generics and comfort methods.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NetworkSyncVar<T> : IEquatable<T>, IEquatable<NetworkSyncVar<T>>, ICloneable, INetworkSyncVar
    {
        public INetworkObject OwnerObject { get; set; }

        /// <summary>
        /// Sets who is allowed to set the value of this Sync var.
        /// </summary>
        public OwnershipMode OwnershipMode
        {
            get
            {
                return _mode;
            }
            set
            {
                _mode = value;
                Sync();
            }
        }

        OwnershipMode _mode;

        T value = default;

        /// <summary>
        /// Identical to <see cref="ValueRaw"/>, but casted to <typeparamref name="T"/>. Calls <see cref="ValueRaw"/> internally to set the value.
        /// </summary>
        public virtual T Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                ValueRaw = value;
                Changed?.Invoke(value);
                _onUpdated?.Invoke(value);
            }
        }

        public object ValueRaw
        {
            get => Value;
            set
            {
                this.value = (T)value;
                Sync();
            }
        }

        /// <summary>
        /// The <see cref="Changed"/> event is fired when the <see cref="Value"/> has been changed either due to network updates or local updates.
        /// </summary>
        public event Action<T> Changed;

        /// <summary>
        /// Determines if <see cref="PacketFlags.Priority"/> will be set when sending packets.
        /// </summary>
        public bool Priority { get; set; } = false;

        public virtual void Sync()
        {
            if (!OwnerObject.Active)
            {
                return;
            }
            SyncVarUpdatePacket packet = GetPacket();
            if (NetworkManager.WhereAmI == ClientLocation.Local)
            {
                if (NetworkClient.LocalClient == null)
                {
                    throw new InvalidOperationException("Tried to modify a SyncVar while the local client was null!");
                }
                if ((OwnershipMode == OwnershipMode.Client && OwnerObject.OwnerClientID != NetworkClient.LocalClient.ClientID && !OwnerObject.HasPrivilege(NetworkClient.LocalClient.ClientID)) || OwnershipMode == OwnershipMode.Server)
                {
                    throw new InvalidOperationException("Tried to modify a SyncVar without permission.");
                }
                NetworkClient.LocalClient.Send(packet);
            }
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                if (OwnershipMode == OwnershipMode.Client)
                {
                    if (OwnerObject.ObjectVisibilityMode == ObjectVisibilityMode.OwnerAndServer)
                    {
                        NetworkClient owner = NetworkServer.GetClient(OwnerObject.OwnerClientID);
                        if (owner == null)
                        {
                            throw new InvalidOperationException("Can't find the owner of this object!");
                        }
                        owner.Send(packet);
                    }
                    else if (OwnerObject.ObjectVisibilityMode == ObjectVisibilityMode.Everyone)
                    {
                        NetworkServer.SendToAll(packet, x => OwnerObject.CheckVisibility(x));
                    }
                }
                else
                {
                    NetworkServer.SendToAll(packet);
                }
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }



        string _name = string.Empty;

        protected NetworkSyncVar()
        {

        }

        public NetworkSyncVar(T value)
        {
            this.value = value;
        }

        private Action<T> _onUpdated;

        public NetworkSyncVar(INetworkObject ownerObject, T value)
        {
            OwnerObject = ownerObject;
            this.value = value;
        }

        public NetworkSyncVar(INetworkObject ownerObject, OwnershipMode syncOwner)
        {
            OwnerObject = ownerObject;
            _mode = syncOwner;
        }

        public NetworkSyncVar(INetworkObject ownerObject, OwnershipMode syncOwner, T value) : this(ownerObject, syncOwner)
        {
            this.value = value;
        }

        public NetworkSyncVar(INetworkObject ownerObject, T value, Action<T> onUpdated)
        {
            OwnerObject = ownerObject;
            this.value = value;
            this._onUpdated = onUpdated;
        }

        public NetworkSyncVar(INetworkObject ownerObject, OwnershipMode syncOwner, Action<T> onUpdated)
        {
            OwnerObject = ownerObject;
            _mode = syncOwner;
            this._onUpdated = onUpdated;
        }

        public NetworkSyncVar(INetworkObject ownerObject, OwnershipMode syncOwner, T value, Action<T> onUpdated) : this(ownerObject, syncOwner, onUpdated)
        {
            this.value = value;
        }

        public bool Equals(T other)
        {
            return other.Equals(value);
        }

        public object Clone()
        {
            return new NetworkSyncVar<T>(OwnerObject, OwnershipMode, value);
        }

        public virtual void RawSet(object value, NetworkClient who)
        {
            this.value = value is T t ? t : default;
            Changed?.Invoke(this.value);
            _onUpdated?.Invoke(this.value);
        }

        public virtual void RawSet(OwnershipMode mode, NetworkClient who)
        {
            _mode = mode;
        }

        public virtual SyncVarData GetData()
        {
            SerializedData data = ByteConvert.Serialize(value);
            SyncVarData syncVarData = new SyncVarData()
            {
                NetworkIDTarget = OwnerObject.NetworkID,
                Data = data,
                TargetVar = Name,
                Mode = OwnershipMode,
            };
            return syncVarData;
        }

        protected virtual SyncVarUpdatePacket GetPacket()
        {
            SyncVarData syncVarData = GetData();
            SyncVarUpdatePacket packet = new SyncVarUpdatePacket()
            {
                Data = new List<SyncVarData> { syncVarData },
            };
            packet.Flags = packet.Flags.SetFlag(PacketFlags.Priority, Priority);
            return packet;
        }

        public virtual void SyncTo(NetworkClient who)
        {
            if (!OwnerObject.CheckVisibility(who))
            {
                return;
            }
            SyncVarUpdatePacket packet = GetPacket();
            who.Send(packet);
        }

        public virtual void SyncTo(NetworkClient who, SyncVarUpdatePacket packet)
        {
            if (!OwnerObject.CheckVisibility(who))
            {
                return;
            }
            who.Send(packet);
        }

        public bool Equals(NetworkSyncVar<T> other)
        {
            return other.Value.Equals(Value);
        }
    }
}
