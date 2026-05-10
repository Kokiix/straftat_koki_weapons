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
        KokiDebug.Log($"spawn detected {go.name}");
        // if (TeleportTrap.GetTrapLink(go))
        if (go.GetComponent<ItemBehaviour>() && !TeleportTrap.GetTrapLink(go)) // debug
        {
            KokiDebug.Log("trying to send to client");
            KokiDebug.Log($"sending {nob.ObjectId}");
            MyceliumNetwork.RPC(KokiWeaponsPlugin.MyceliumID, nameof(KokiWeaponsPlugin.APMineToTPTrap), ReliableType.Reliable, nob.ObjectId);
        }
    }


}