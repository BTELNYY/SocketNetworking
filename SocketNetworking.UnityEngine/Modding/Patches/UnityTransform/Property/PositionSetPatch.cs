using HarmonyLib;
using SocketNetworking.UnityEngine.Components;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityTransform.Property
{
    [HarmonyPatch(typeof(Transform), nameof(Transform.position), MethodType.Setter)]
    public class PositionSetPatch
    {
        public static void Prefix(Transform __instance, Vector3 __0)
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
            if (transform.Identity.gameObject.GetInstanceID() != __instance.gameObject.GetInstanceID())
            {
                Log.GlobalWarning($"Mismatch! {transform.Identity}, INSTANCE: {__instance.gameObject.name}");
                return;
            }
            if (transform.SyncMode != ComponentSyncMode.Automatic)
            {
                return;
            }
            if (!transform.Enabled)
            {
                return;
            }
            transform.NetworkPosition = __0;
        }
    }
}
