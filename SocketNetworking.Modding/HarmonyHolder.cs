using HarmonyLib;

namespace SocketNetworking.Modding
{
    public static class HarmonyHolder
    {
        static HarmonyHolder()
        {
            Harmony = new Harmony("com.btelnyy.socketnetworking.patching");
        }

        public static Harmony Harmony { get; }
    }
}
