using System;
using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Serializing;
using HarmonyLib;
using MyceliumNetworking;
using UnityEngine;

[HarmonyPatch]
public static class TPTrapNetworking
{
    [HarmonyPatch(typeof(ServerManager), "Spawn", new Type[] { typeof(NetworkObject), typeof(NetworkConnection) })]
    [HarmonyPrefix]
    public static void DetectServerSpawnTPTrap(NetworkObject nob)
    {
        KokiDebug.Log($"spawn detected {nob.gameObject.name}");
        if (TeleportTrap.GetTrapLink(nob.gameObject))
        {
            KokiDebug.Log("trying to send to client");
            MyceliumNetwork.RPC(KokiWeaponsPlugin.MyceliumID, nameof(APMineToTPTrap), ReliableType.Reliable, nob.ObjectId);
        }
    }

    [CustomRPC]
    public static void APMineToTPTrap(int nobID)
    {
        if (InstanceFinder.IsServer) return;

        KokiDebug.Log("received nob ID!!");
        bool test = InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(nobID, out NetworkObject nob);
        KokiDebug.Log($"id: {nobID}, found: {test}");
        // TeleportTrap.ConvertAPToTPTrap(nob.gameObject);
    }
}