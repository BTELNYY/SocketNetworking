using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.PacketSystem
{
    public abstract class TypeWrapper<T>
    {
        public Type GetContainedType()
        {
            return typeof(T);
        }

        public T Value { get; set; }

        public abstract byte[] Serialize();

        public abstract ValueTuple<T, int> Deserialize(byte[] data);
    }
}
