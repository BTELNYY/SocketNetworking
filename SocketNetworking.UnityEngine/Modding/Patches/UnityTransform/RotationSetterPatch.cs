using HarmonyLib;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityTransform
{
    [HarmonyPatch(typeof(Transform), nameof(Transform.rotation), MethodType.Setter)]
    public class RotationSetterPatch
    {
        public static bool Prefix(Transform __instance, Quaternion value)
        {
            Components.NetworkTransform netTransform = UnityNetworkManager.GetNetworkTransform(__instance.gameObject);
            if (netTransform != null)
            {
                netTransform.NetworkRotation = value;
                return false;
            }
            return true;
        }
    }
}
