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
    [HarmonyPostfix]
    public static void DetectServerSpawnTPTrap(NetworkObject nob)
    {
        GameObject go = nob.gameObject;
        if (TeleportTrap.GetTrapLink(go))
        {
            MyceliumNetwork.RPC(KokiWeaponsPlugin.MyceliumID, nameof(KokiWeaponsPlugin.APMineToTPTrap), ReliableType.Reliable, nob.ObjectId, false);
        }
    }


}