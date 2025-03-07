using System;

namespace SocketNetworking.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NetworkNonSerialized : Attribute
    {
    }
}
