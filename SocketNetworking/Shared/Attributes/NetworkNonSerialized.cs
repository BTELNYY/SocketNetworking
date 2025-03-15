using System;

namespace SocketNetworking.Shared.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NetworkNonSerialized : Attribute
    {
    }
}
