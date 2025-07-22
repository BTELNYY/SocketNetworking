using HarmonyLib;
using SocketNetworking.UnityEngine.Components;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityTransform.Methods
{
    [HarmonyPatch(typeof(Transform), nameof(Transform.SetPositionAndRotation), MethodType.Normal)]
    public class SetPositionAndRotationPatch
    {
        public static void Prefix(Transform __instance, Vector3 position, Quaternion rotation)
        {
            NetworkTransform transform = UnityNetworkManager.GetNetworkTransform(__instance.gameObject);
            if (transform == null)
            {
                return;
            }
            if (!transform.CheckOwnershipAndPrivilege())
            {
                return;
            }
            if (transform.SyncMode != ComponentSyncMode.Automatic)
            {
                return;
            }
            transform.NetworkPosition = position;
            transform.NetworkRotation = rotation;
        }
    }
}
