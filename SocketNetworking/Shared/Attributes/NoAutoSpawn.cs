using SocketNetworking.Shared.NetworkObjects;
using SocketNetworking.Shared.SyncVars;
using System;

namespace SocketNetworking.Shared.Attributes
{
    /// <summary>
    /// When applied to a <see cref="INetworkSyncVar"/> field, prevents it from being spawned as a dependency when the dependant <see cref="INetworkObject"/> is spawned.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class NoAutoSpawnAttribute : Attribute
    {

    }
}
