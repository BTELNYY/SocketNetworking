using System;

namespace SocketNetworking.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
    public class NetworkSerialized : Attribute
    {

    }
}
