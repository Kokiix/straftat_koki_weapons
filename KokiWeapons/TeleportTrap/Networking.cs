using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using HarmonyLib;

public struct TPTrapConversion : IBroadcast
{
    public int NobID;
}

[HarmonyPatch]
public static class TPTrapNetworking
{
    public static void Init()
    {
        InstanceFinder.ClientManager.RegisterBroadcast<TPTrapConversion>(APMineToTPTrap);
    }

    [HarmonyPatch(typeof(ServerManager), "Spawn")]
    [HarmonyPrefix]
    public static void DetectServerSpawnTPTrap(NetworkObject nob, NetworkConnection ownerConnection = null)
    {
        if (InstanceFinder.IsClient)
        {
            KokiDebug.Log("--------------------------------------------------NOT HOST------------------------------------------------------");
            return;
        }

        KokiDebug.Log("spawn detected");
        if (TeleportTrap.GetTrapLink(nob.gameObject))
        {
            InstanceFinder.ServerManager.Broadcast(new TPTrapConversion
            {
                NobID = nob.ObjectId
            });
        }
    }

    public static void APMineToTPTrap(TPTrapConversion msg)
    {
        KokiDebug.Log("hi");
    }
}