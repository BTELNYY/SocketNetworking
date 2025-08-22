using HarmonyLib;
using SocketNetworking.UnityEngine.Components;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityTransform.Property
{
    [HarmonyPatch(typeof(Transform), nameof(Transform.localRotation), MethodType.Setter)]
    public class LocalRotationSetPatch
    {
        public static void Prefix(Transform __instance, Quaternion __0)
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
            transform.NetworkLocalRotation = __0;
        }
    }
}
