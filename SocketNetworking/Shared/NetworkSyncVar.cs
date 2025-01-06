using SocketNetworking.PacketSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Shared
{
    public class NetworkSyncVar<T>
    {
        public INetworkObject OwnerObject { get; }

        T value = default(T);

        public T Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
                Sync();
            }
        }

        void Sync()
        {

        }

        public NetworkSyncVar(T value, INetworkObject onwer)
        {
            OwnerObject = onwer;
            Value = value;
        }
    }
}
