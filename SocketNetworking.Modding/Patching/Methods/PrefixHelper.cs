using System.Reflection;

namespace SocketNetworking.Modding.Patching.Methods
{
    public class PrefixHelper
    {
        public delegate bool Prefix(object __instance, object[] __args, MethodBase __originalMethod);

        public static void Patch(MethodInfo target, Prefix prefix)
        {
            HarmonyHolder.Harmony.Patch(target, new HarmonyLib.HarmonyMethod(prefix));
        }

        public static void Unpatch(MethodInfo target, Prefix prefix)
        {
            HarmonyHolder.Harmony.Unpatch(target, prefix.GetMethodInfo());
        }
    }
}
