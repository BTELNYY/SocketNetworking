using HarmonyLib;
using SocketNetworking.UnityEngine.Components;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityTransform.Property
{
    [HarmonyPatch(typeof(Transform), nameof(Transform.localPosition), MethodType.Setter)]
    public class LocalPositionSetPatch
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
                //Log.GlobalWarning($"NetworkTransform is not enabled! Identity: {transform.Identity}");
                return;
            }
            transform.NetworkLocalPosition = __0;
        }
    }
}
