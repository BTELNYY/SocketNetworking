using SocketNetworking.PacketSystem;
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
        public NetworkDirection SyncOwner { get; }

        T value = default(T);

        public T Value
        {
            get
            {
                return value;
            }
            set
            {
                if (SyncOwner != NetworkDirection.Any && NetworkManager.WhereAmIDirection != SyncOwner)
                {
                    return;
                }
                this.value = value;
                Sync();
            }
        }

        public object ValueRaw
        {
            get
            {
                return Value;
            }
            set
            {
                if(value is T)
                {
                    Value = (T)value;
                }
            }
        }

        public void Set(T value)
        {
            value = Value;
        }

        void Sync()
        {
            SerializedData data = NetworkConvert.Serialize(value);
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

        public NetworkSyncVar(INetworkObject onwer, T value)
        {
            OwnerObject = onwer;
            Value = value;
            SyncOwner = NetworkDirection.Server;
        }

        public NetworkSyncVar(INetworkObject ownerObject, NetworkDirection syncDirection, T value)
        {
            OwnerObject = ownerObject;
            SyncOwner = syncDirection;
            Value = value;
        }

        public NetworkSyncVar(INetworkObject ownerObject, NetworkDirection syncOwner, T value, string name) : this(ownerObject, syncOwner, value)
        {
            _name = name;
        }

        public NetworkSyncVar(INetworkObject ownerObject, T value, string name) : this(ownerObject, value)
        {
            _name = name;
        }
    }
}
