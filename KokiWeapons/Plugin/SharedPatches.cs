using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using FishNet.Connection;
using FishNet.Managing.Server;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
public static class InsertPlaceItemEvent
{
    public class SpawnEventArgs : EventArgs
    {
        public WeaponHandSpawner spawner;
        public GameObject spawnedObj;
        public bool runOriginalCode = false;
    }
    public static event EventHandler<SpawnEventArgs> PlaceItemEvent;
    public static bool TriggerPlaceItemEvent(WeaponHandSpawner __instance, GameObject obj)
    {
        var args = new SpawnEventArgs { spawner = __instance, spawnedObj = obj };
        PlaceItemEvent.Invoke(null, args);
        return args.runOriginalCode;
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var serverManagerSpawn = AccessTools.Method(typeof(ServerManager), nameof(ServerManager.Spawn), [typeof(GameObject), typeof(NetworkConnection)]);
        var triggerEvent = AccessTools.Method(typeof(InsertPlaceItemEvent), nameof(TriggerPlaceItemEvent));

        return new CodeMatcher(instructions, generator).End()
        .MatchBack(useEnd: false, new CodeMatch(OpCodes.Ret)).CreateLabel(out Label ret)
        .MatchBack(useEnd: false, new CodeMatch(OpCodes.Callvirt, serverManagerSpawn))
        .Advance(1).Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Call, triggerEvent),
            new CodeInstruction(OpCodes.Brfalse, ret))
        .InstructionEnumeration();
    }
}