using HarmonyLib;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityTransform
{
    [HarmonyPatch(typeof(Transform), nameof(Transform.localRotation), MethodType.Setter)]
    public class LocalRotationSetterPatch
    {
        public static bool Prefix(Transform __instance, Quaternion value)
        {
            Components.NetworkTransform netTransform = UnityNetworkManager.GetNetworkTransform(__instance.gameObject);
            if (netTransform != null)
            {
                netTransform.NetworkLocalRotation = value;
                return false;
            }
            return true;
        }
    }
}
