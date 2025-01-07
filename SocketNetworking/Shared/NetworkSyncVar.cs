using SocketNetworking.Client;
using SocketNetworking.PacketSystem;
using SocketNetworking.PacketSystem.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Shared
{
    public class NetworkSyncVar<T> : IEquatable<T>, ICloneable, INetworkSyncVar
    {
        public INetworkObject OwnerObject { get; }

        /// <summary>
        /// Sets who is allowed to set the value of this Sync var.
        /// </summary>
        public OwnershipMode SyncOwner { get; }

        T value = default(T);

        public virtual T Value
        {
            get
            {
                return value;
            }
            set
            {
 
                this.value = value;
            }
        }

        public virtual void NetworkSet(object value, NetworkClient who)
        {
            if(SyncOwner != OwnershipMode.Public)
            {
                if(NetworkManager.WhereAmI == ClientLocation.Local && SyncOwner == OwnershipMode.Client && who.ClientID != OwnerObject.OwnerClientID)
                {
                    return;
                }
            }
            Value = (T)value;
            Sync();
        }

        public object ValueRaw
        {
            get
            {
                return Value;
            }
        }

        public virtual void Set(T value, NetworkClient who)
        {
            value = Value;
        }

        void Sync()
        {
            SerializedData data = NetworkConvert.Serialize(value);
            SyncVarData syncVarData = new SyncVarData()
            {
                NetworkIDTarget = OwnerObject.NetworkID,
                Data = data,
                TargetVar = Name,
            };
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

        public bool Equals(T other)
        {
            return other.Equals(value);
        }

        public object Clone()
        {
            return new NetworkSyncVar<T>(OwnerObject, SyncOwner, value);
        }

        public void RawSet(object value, NetworkClient who)
        {
            if(value is T t)
            {
                Set(t, who);
            }
        }

        public NetworkSyncVar(INetworkObject owner, T value)
        {
            OwnerObject = owner;
            Value = value;
            SyncOwner = OwnershipMode.Server;
        }

        public NetworkSyncVar(INetworkObject ownerObject, OwnershipMode syncDirection, T value)
        {
            OwnerObject = ownerObject;
            SyncOwner = syncDirection;
            Value = value;
        }

        public NetworkSyncVar(INetworkObject ownerObject, OwnershipMode syncOwner, T value, string name) : this(ownerObject, syncOwner, value)
        {
            _name = name;
        }

        public NetworkSyncVar(INetworkObject ownerObject, T value, string name) : this(ownerObject, value)
        {
            _name = name;
        }
    }
}
