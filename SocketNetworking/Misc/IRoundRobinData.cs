using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketNetworking.Misc
{
    public interface IRoundRobinData<T> : IComparable<T>
    {
        bool AllowChoosing { get; }

        bool AllowSorting { get; }
    }
}
