using HarmonyLib;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Modding.Patches.UnityTransform
{
    [HarmonyPatch(typeof(Transform), nameof(Transform.localPosition), MethodType.Setter)]
    public class LocalPositionSetterPatch
    {
        public static bool Prefix(Transform __instance, Vector3 value)
        {
            Components.NetworkTransform netTransform = UnityNetworkManager.GetNetworkTransform(__instance.gameObject);
            if (netTransform != null)
            {
                netTransform.NetworkLocalPosition = value;
                return false;
            }
            return true;
        }
    }
}
