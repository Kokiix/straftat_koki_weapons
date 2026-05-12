using System.Collections;
using FishNet;
using FishNet.Object;
using HarmonyLib;
using MyceliumNetworking;
using UnityEngine;
using Steamworks;
using System.Collections.Generic;
using System.Reflection.Emit;

[HarmonyPatch]
public static class TPTrapMechanics
{
    [HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
    [HarmonyPrefix]
    static void SwapTemplateGO(WeaponHandSpawner __instance, ref GameObject obj)
    {
        if (__instance.gameObject.GetComponent<TrapLink>())
            obj = TeleportTrap.TemplatePhysGameObject;
    }

    [HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> LinkMineOnPlace(IEnumerable<CodeInstruction> instructions)
    {
        var linkmines = AccessTools.Method(typeof(TPTrapMechanics), nameof(OnPlace));

        return new CodeMatcher(instructions)
        .MatchForward(useEnd: false, new CodeMatch(OpCodes.Call,
            AccessTools.Method(typeof(ProximityMine), nameof(ProximityMine.sync___set_value__rootObject))))
        .Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Call, linkmines))
        .InstructionEnumeration();
    }

    public static void OnPlace(WeaponHandSpawner __instance, GameObject newTrap)
    {
        TrapLink connector = __instance.gameObject.GetComponent<TrapLink>();
        if (!connector) return;

        ProximityMine mine = newTrap.GetComponent<ProximityMine>();
        mine.activated = false;
        mine.canActivate = false;
        mine.stunMine = false; // Used as detonated flag

        var anim = newTrap.transform.Find("TeleTrapPhysMesh(Clone)").Find("trap_010").GetComponent<Animation>();
        anim["sphere"].layer = 0;
        anim.Play("sphere");
        anim["torus"].layer = 1;
        anim["torus"].weight = 1;
        anim["torus"].enabled = true;
        anim.Play("torus");

        MyceliumNetwork.RPC(CustomWeaponNetworkManager.MyceliumID,
        nameof(CustomWeaponNetworkManager.DisplayClientVisual), ReliableType.Reliable,
        newTrap.GetComponent<NetworkObject>().ObjectId, nameof(TeleportTrap.ConvertToPhysTPTrap), false);

        if (connector.otherTrap)
        {
            GameObject otherTrap = connector.otherTrap.gameObject;

            TeleportTrap.GetTrapLink(otherTrap).otherTrap = newTrap;
            TeleportTrap.GetTrapLink(newTrap).otherTrap = otherTrap;

            ProximityMine otherMine = otherTrap.GetComponent<ProximityMine>();
            otherTrap.transform.Find("radius(Clone)").gameObject.SetActive(true);
            newTrap.transform.Find("radius(Clone)").gameObject.SetActive(true);

            MyceliumNetwork.RPC(CustomWeaponNetworkManager.MyceliumID,
            nameof(CustomWeaponNetworkManager.DisplayClientVisual), ReliableType.Reliable,
            otherTrap.GetComponent<NetworkObject>().ObjectId, nameof(CustomWeaponNetworkManager.ToggleRadius), false);

            MyceliumNetwork.RPC(CustomWeaponNetworkManager.MyceliumID,
            nameof(CustomWeaponNetworkManager.DisplayClientVisual), ReliableType.Reliable,
            newTrap.GetComponent<NetworkObject>().ObjectId, nameof(CustomWeaponNetworkManager.ToggleRadius), false);
        }
        else
            connector.otherTrap = newTrap;
    }

    [HarmonyPatch(typeof(ProximityMine), "OnTriggerStay")]
    [HarmonyPostfix]
    public static void DetectExplosion(ProximityMine __instance)
    {
        if (!TeleportTrap.GetTrapLink(__instance.gameObject)) return;

        GameObject otherTrap = TeleportTrap.GetTrapLink(__instance.gameObject).otherTrap;

        if (!InstanceFinder.IsServer || __instance.stunMine || !otherTrap) return;

        __instance.ChangeState();
        __instance.HandleExplosion();
    }

    [HarmonyPatch(typeof(ProximityMine), "HandleExplosion")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> InsertHandleExplosion(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var resumeAfterInject = generator.DefineLabel();
        LocalBuilder isTPTrap = generator.DeclareLocal(typeof(bool));

        var handleExplosion = AccessTools.Method(typeof(TPTrapMechanics), nameof(HandleExplosion));
        var getTrapLink = AccessTools.Method(typeof(TeleportTrap), nameof(TeleportTrap.GetTrapLink));
        var implicitBool = AccessTools.Method(typeof(Object), "op_Implicit");
        var matcher = new CodeMatcher(instructions, generator);

        // set isTPTrap variable
        matcher.Start()
        .Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ProximityMine), nameof(ProximityMine.gameObject))),
            new CodeInstruction(OpCodes.Call, getTrapLink),
            new CodeInstruction(OpCodes.Call, implicitBool),
            new CodeInstruction(OpCodes.Stloc, isTPTrap));

        // blow up the mine even if not owner
        var postOwnerCheckLabel = (Label)matcher.MatchForward(useEnd: false,
        new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(NetworkBehaviour), nameof(NetworkBehaviour.IsOwner))))
        .Advance(1).Instruction.operand;
        matcher.Advance(1).Insert(
            new CodeInstruction(OpCodes.Ldloc, isTPTrap),
            new CodeInstruction(OpCodes.Brtrue, postOwnerCheckLabel));

        // logic inside loop over each collider
        var loopContinueLabel = (Label)matcher.MatchForward(useEnd: false,
        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(PlayerHealth), nameof(PlayerHealth.sync___get_value_isKilled))))
        .Advance(1).Instruction.operand;
        matcher.Advance(1).Insert(
            // if instance has no trap link, continue
            new CodeInstruction(OpCodes.Ldloc, isTPTrap),
            new CodeInstruction(OpCodes.Brfalse, resumeAfterInject),

            // else, use TP mine HandleExplosion and skip to next loop cycle
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ProximityMine), nameof(ProximityMine.ph2))),
            new CodeInstruction(OpCodes.Ldloc_3),
            new CodeInstruction(OpCodes.Ldelem_Ref),
            new CodeInstruction(OpCodes.Call, handleExplosion),
            new CodeInstruction(OpCodes.Br, loopContinueLabel)
        ).AddLabels([resumeAfterInject]);
        return matcher.InstructionEnumeration();
    }

    public static void HandleExplosion(ProximityMine __instance, PlayerHealth health)
    {
        if (!InstanceFinder.IsServer) return;

        ProximityMine otherMine = TeleportTrap.GetTrapLink(__instance.gameObject).otherTrap.GetComponent<ProximityMine>();
        Vector3 destination = otherMine.transform.position;

        __instance.stunMine = true;
        if (!otherMine.stunMine)
        {
            otherMine.ChangeState();
            otherMine.HandleExplosion();
        }

        FirstPersonController fpc = health.controller;
        if (fpc)
        {
            if (fpc.Owner.IsLocalClient)
            {
                fpc.Teleport(destination, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
            }
            else
            {
                ulong.TryParse(fpc.Owner.GetAddress(), out ulong steamID);
                MyceliumNetwork.RPCTarget(CustomWeaponNetworkManager.MyceliumID, nameof(CustomWeaponNetworkManager.TeleportClient), (CSteamID)steamID, ReliableType.Reliable, destination);
            }
        }
    }

    [HarmonyPatch(typeof(ProximityMine), "Start")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> StopActivateCoroutine(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var getTrapLink = AccessTools.Method(typeof(TeleportTrap), nameof(TeleportTrap.GetTrapLink));

        return new CodeMatcher(instructions, generator)
            .End()
            .CreateLabel(out Label ret)

            .Start()
            .Insert(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(ProximityMine), nameof(ProximityMine.gameObject))),
            new CodeInstruction(OpCodes.Call, getTrapLink),
            new CodeInstruction(OpCodes.Brtrue, ret))
            .InstructionEnumeration();
    }

    [HarmonyPatch(typeof(Weapon), "TriggerEnvironment")]
    [HarmonyPrefix]
    public static bool ExplodeTPTrapOnHit(Weapon __instance, GameObject obj)
    {
        GameObject trap = obj.transform.root.gameObject;
        if (obj.CompareTag("Mine") && TeleportTrap.GetTrapLink(trap))
        {
            if (!InstanceFinder.IsHost)
            {
                MyceliumNetwork.RPC(CustomWeaponNetworkManager.MyceliumID,
                nameof(CustomWeaponNetworkManager.ExplodeMineFromClient), ReliableType.Reliable,
                obj.transform.root.gameObject.GetComponent<NetworkObject>().ObjectId);
                return false;
            }
            GameObject otherTrap = TeleportTrap.GetTrapLink(trap).otherTrap;
            if (otherTrap)
            {
                otherTrap.transform.Find("radius(Clone)").gameObject.SetActive(false);
                MyceliumNetwork.RPC(CustomWeaponNetworkManager.MyceliumID,
                nameof(CustomWeaponNetworkManager.DisplayClientVisual), ReliableType.Reliable,
                otherTrap.GetComponent<NetworkObject>().ObjectId, nameof(CustomWeaponNetworkManager.ToggleRadius), false);
            }
            obj.transform.root.GetComponent<ProximityMine>().ChangeState();
            obj.transform.root.GetComponent<ProximityMine>().ExplodeServer();
            return false;
        }

        return true;
    }
}