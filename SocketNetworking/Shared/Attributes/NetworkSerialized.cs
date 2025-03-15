using System;

namespace SocketNetworking.Shared.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
    public class NetworkSerialized : Attribute
    {

    }
}
