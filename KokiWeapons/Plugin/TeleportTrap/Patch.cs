using System.Collections.Generic;
using System.Reflection.Emit;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using HarmonyLib;
using MyceliumNetworking;
using UnityEngine;

namespace TeleportTrap;

/// Runs server side only! This is necessary because it's the only point where the hand item and physics item are present in the same context.
[HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
public static class UpdateTrapLinkOnPlace
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var serverManagerSpawn = AccessTools.Method(typeof(ServerManager), nameof(ServerManager.Spawn), [typeof(GameObject), typeof(NetworkConnection)]);
        var updateTrapLink = AccessTools.Method(typeof(UpdateTrapLinkOnPlace), nameof(UpdateTrapLink));

        return new CodeMatcher(instructions)
        .MatchForward(useEnd: false, new CodeMatch(OpCodes.Callvirt, serverManagerSpawn))
        .Advance(1).Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Call, updateTrapLink))
        .InstructionEnumeration();
    }

    public static void UpdateTrapLink(WeaponHandSpawner __instance, GameObject newTrap)
    {
        if (!__instance.gameObject.TryGetComponent(out TPLink link)) return;
        if (link.otherTrapNob == -1)
        {
            link.otherTrapNob = newTrap.GetComponent<NetworkObject>().ObjectId;
        }
        else
        {
            var nobID1 = newTrap.GetComponent<NetworkObject>().ObjectId;
            var nobID2 = link.otherTrapNob;
            TPTrapNetworking.RPC("LinkMines", [nobID1, nobID2]);
        }
    }
}

[HarmonyPatch(typeof(Weapon), "TriggerEnvironment")]
public static class ExplodeTPTrapOnHit
{
    public static void Prefix(Weapon __instance, GameObject obj)
    {
        GameObject trap = obj.transform.root.gameObject;
        if (obj.CompareTag("Mine") && obj.GetComponent<TPTrap>())
        {
            TPTrapNetworking.RPC("DestroyTrapPair", [trap.GetComponent<NetworkObject>().ObjectId, -1]);
        }
    }
}