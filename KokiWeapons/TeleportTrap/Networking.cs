using System;
using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Serializing;
using HarmonyLib;
using UnityEngine;

public struct TPTrapConversion : IBroadcast
{
    public int NobID;
}

public static class TPTrapSerializers
{
    public static void WriteTPTrap(this Writer writer, TPTrapConversion value)
    {
        writer.WriteInt32(value.NobID);
    }
    public static TPTrapConversion ReadTPTrap(this Reader reader)
    {
        return new TPTrapConversion
        {
            NobID = reader.ReadInt32()
        };
    }
}

[HarmonyPatch]
public static class TPTrapNetworking
{
    public static void Init()
    {
        GenericWriter<TPTrapConversion>.Write = TPTrapSerializers.WriteTPTrap;
        GenericReader<TPTrapConversion>.Read = TPTrapSerializers.ReadTPTrap;
        InstanceFinder.ClientManager.RegisterBroadcast<TPTrapConversion>(APMineToTPTrap);

    }

    [HarmonyPatch(typeof(ServerManager), "Spawn", new Type[] { typeof(NetworkObject), typeof(NetworkConnection) })]
    [HarmonyPrefix]
    public static void DetectServerSpawnTPTrap(NetworkObject nob)
    {
        KokiDebug.Log($"spawn detected {nob.gameObject.name}");
        // if (TeleportTrap.GetTrapLink(nob.gameObject))
        // {
        InstanceFinder.ServerManager.Broadcast(new TPTrapConversion
        {
            NobID = nob.ObjectId
        });
        // }
    }

    public static void APMineToTPTrap(TPTrapConversion msg)
    {
        if (InstanceFinder.IsServer) return;

        KokiDebug.Log("trying conversion");
        InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(msg.NobID, out NetworkObject nob);
        GameObject apmine = nob.gameObject;
        TeleportTrap.ConvertAPToTPTrap(apmine);
    }
}