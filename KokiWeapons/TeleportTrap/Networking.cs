using FishNet.Broadcast;
using FishNet.Managing.Server;
using HarmonyLib;

public struct APMineToTPTrapBroadcast : IBroadcast
{
    public int NobID;
}

public static class TPTrapNetworking
{
    [HarmonyPatch(typeof(ServerManager), "Spawn")]
    [HarmonyPrefix]
    public static void DetectServerSpawnTPTrap()
    {
        KokiDebug.Log("aaaaaaaaa");
    }
}