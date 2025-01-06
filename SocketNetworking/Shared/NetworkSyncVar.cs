using SocketNetworking.PacketSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Shared
{
    public class NetworkSyncVar<T> : IEquatable<T>, ICloneable
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
                if(SyncOwner != NetworkDirection.Any && NetworkManager.WhereAmIDirection != SyncOwner)
                {
                    return;
                }
                this.value = value;
                Sync();
            }
        }

        internal void Set(T value)
        {
            value = Value;
        }

        void Sync()
        {
            SerializedData data = NetworkConvert.Serialize(value);
        }

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
    }
}
