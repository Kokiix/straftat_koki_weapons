using System.Collections;
using FishNet;
using FishNet.Object;
using HarmonyLib;
using MyceliumNetworking;
using UnityEngine;
using Steamworks;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;

namespace TeleportTrap;

[HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
public static class LinkMineOnPlace
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var linkmines = AccessTools.Method(typeof(LinkMineOnPlace), nameof(OnPlace));
        var syncRootobj = AccessTools.Method(typeof(ProximityMine), nameof(ProximityMine.sync___set_value__rootObject));

        return new CodeMatcher(instructions)
            .MatchForward(useEnd: false, new CodeMatch(OpCodes.Call, syncRootobj))
        .Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Call, linkmines))
        .InstructionEnumeration();
    }

    public static void OnPlace(WeaponHandSpawner __instance, GameObject newTrap)
    {
        TPTrap handItemLink = __instance.gameObject.GetComponent<TPTrap>();
        if (!handItemLink) return;

        if (KokiWeaponsPlugin.Debug)
            SpawnWeaponOnTaunt.weapons.Add(newTrap);

        if (handItemLink.otherTrap)
        {
            GameObject otherTrap = handItemLink.otherTrap.gameObject;

            otherTrap.GetComponent<TPTrap>().otherTrap = newTrap;
            newTrap.GetComponent<TPTrap>().otherTrap = otherTrap;

            otherTrap.transform.Find("radius").gameObject.SetActive(true);
            newTrap.transform.Find("radius").gameObject.SetActive(true);
            MyceliumNetwork.RPC(
            KokiWeaponsPlugin.MyceliumID,
            nameof(Networking.ToggleRadius),
            ReliableType.Reliable,
            otherTrap.GetComponent<NetworkObject>().ObjectId);
            MyceliumNetwork.RPC(
            KokiWeaponsPlugin.MyceliumID,
            nameof(Networking.ToggleRadius),
            ReliableType.Reliable,
            newTrap.GetComponent<NetworkObject>().ObjectId);
        }
        else
            handItemLink.otherTrap = newTrap;

        newTrap.GetComponent<ProximityMine>().canActivate = false;
    }
}

// [HarmonyPatch(typeof(ProximityMine), "OnTriggerEnter")]
// public static class Test
// {
//     public static void Prefix()
//     {
//         KDBG.Log("sldkjf");
//     }
// }

[HarmonyPatch(typeof(ProximityMine))]
public static class TPExplosion
{
    [HarmonyPatch("OnTriggerStay")]
    public static void Postfix(ProximityMine __instance)
    {
        var trap = __instance.gameObject.GetComponent<TPTrap>();
        if (!trap) return;

        if (!InstanceFinder.IsServer || trap.clientDetonated || !trap.otherTrap) return;

        __instance.ChangeState();
        __instance.HandleExplosion();
    }

    [HarmonyPatch("HandleExplosion")]
    public static bool Prefix(ProximityMine __instance)
    {
        var link = __instance.gameObject.GetComponent<TPTrap>();
        if (!link) return true;
        if (!InstanceFinder.IsHost || link.clientDetonated) return false;

        Collider[] colliders = Physics.OverlapSphere(__instance.transform.position, __instance.explosionRadius, __instance.bodyLayer);

        link.clientDetonated = true;
        ProximityMine otherMine = link.otherTrap.GetComponent<ProximityMine>();
        if (!link.otherTrap.GetComponent<TPTrap>().clientDetonated)
        {
            otherMine.ChangeState();
            otherMine.HandleExplosion();
        }

        if (colliders.Length != 0)
        {
            Vector3 destination = otherMine.transform.position;
            var fpcs = new HashSet<FirstPersonController>();
            foreach (var c in colliders)
            {
                if (c.TryGetComponent(out PlayerHealth h))
                    fpcs.Add(h.controller);
            }
            foreach (var controller in fpcs)
            {
                if (controller.Owner.IsLocalClient)
                {
                    FirstPersonController.instance.Teleport(destination, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
                    continue;
                }
                ulong.TryParse(controller.Owner.GetAddress(), out ulong steamID);
                MyceliumNetwork.RPCTarget(
                    KokiWeaponsPlugin.MyceliumID,
                    nameof(Networking.TeleportClient),
                    (CSteamID)steamID,
                    ReliableType.Reliable,
                    destination);
            }

        }
        __instance.ExplodeServer();
        return false;
    }
}

[HarmonyPatch(typeof(ProximityMine), "Start")]
public static class StopActivateCoroutine
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var getTraplink = AccessTools.Method(typeof(GameObject), nameof(GameObject.GetComponent), null, [typeof(TPTrap)]);
        var implicitBool = AccessTools.Method(typeof(UnityEngine.Object), "op_Implicit");

        return new CodeMatcher(instructions, generator)
            .End()
            .CreateLabel(out Label ret)

            .Start()
            .Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ProximityMine), nameof(ProximityMine.gameObject))),
            new CodeInstruction(OpCodes.Call, getTraplink),
            new CodeInstruction(OpCodes.Call, implicitBool),
            new CodeInstruction(OpCodes.Brtrue, ret))
            .InstructionEnumeration();
    }
}