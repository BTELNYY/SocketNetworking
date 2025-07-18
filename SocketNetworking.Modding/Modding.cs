namespace SocketNetworking.Modding
{
    public class Modding
    {
        public static void Patch()
        {
            HarmonyHolder.Harmony.PatchAll();
        }
    }
}
