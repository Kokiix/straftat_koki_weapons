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
        TrapLink handItemLink = __instance.gameObject.GetComponent<TrapLink>();
        if (!handItemLink) return;

        if (KokiWeaponsPlugin.Debug)
            SpawnWeaponOnTaunt.weapons.Add(newTrap);

        var anim = newTrap.transform.Find("TeleTrapPhysMesh").Find("trap_010").GetComponent<Animation>();
        anim["tpmineSphere"].layer = 0;
        anim.Play("tpmineSphere");
        anim["tpmineTorus"].layer = 1;
        anim.Play("tpmineTorus");

        if (handItemLink.otherTrap)
        {
            GameObject otherTrap = handItemLink.otherTrap.gameObject;

            otherTrap.GetComponent<TrapLink>().otherTrap = newTrap;
            newTrap.GetComponent<TrapLink>().otherTrap = otherTrap;

            otherTrap.transform.Find("radius").gameObject.SetActive(true);
            newTrap.transform.Find("radius").gameObject.SetActive(true);
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
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var compareTag = AccessTools.Method(typeof(Component), nameof(Component.CompareTag));
        var gameObject = AccessTools.PropertyGetter(typeof(MonoBehaviour), nameof(MonoBehaviour.gameObject));
        var implicitBool = AccessTools.Method(typeof(UnityEngine.Object), "op_Implicit");

        var getTraplink = AccessTools.Method(typeof(GameObject), nameof(GameObject.GetComponent), null, [typeof(TrapLink)]);
        var otherTrap = AccessTools.Field(typeof(TrapLink), nameof(TrapLink.otherTrap));

        var changeState = AccessTools.Method(typeof(ProximityMine), nameof(ProximityMine.ChangeState));
        var handleExplo = AccessTools.Method(typeof(ProximityMine), nameof(ProximityMine.HandleExplosion));

        return new CodeMatcher(instructions, generator)

        .End().MatchBack(useEnd: false,
            new CodeMatch(OpCodes.Ret))
        .CreateLabel(out Label endFunc)

        .Start().CreateLabel(out Label resumeFunc)

        .Insert(
            // If no traplink, proceed
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Callvirt, gameObject),
            new CodeInstruction(OpCodes.Call, getTraplink),
            new CodeInstruction(OpCodes.Call, implicitBool),
            new CodeInstruction(OpCodes.Brfalse, resumeFunc),

            // Else, check if otherTrap exists...
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Callvirt, gameObject),
            new CodeInstruction(OpCodes.Call, getTraplink),
            new CodeInstruction(OpCodes.Ldfld, otherTrap),
            new CodeInstruction(OpCodes.Call, implicitBool),
            new CodeInstruction(OpCodes.Brfalse, endFunc),

            // ...AND collider is player
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldstr, "Player"),
            new CodeInstruction(OpCodes.Callvirt, compareTag),
            new CodeInstruction(OpCodes.Brfalse, endFunc),

            // Trigger explosion
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, changeState),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, handleExplo),
            new CodeInstruction(OpCodes.Ret))
        .InstructionEnumeration();
    }

    [HarmonyPatch("HandleExplosion")]
    public static bool Prefix(ProximityMine __instance)
    {
        KDBG.Log("tp");
        var link = __instance.gameObject.GetComponent<TrapLink>();
        if (!link) return true;

        Collider[] colliders = Physics.OverlapSphere(__instance.transform.position, __instance.explosionRadius, __instance.bodyLayer);

        ProximityMine otherMine = link.otherTrap.GetComponent<ProximityMine>();
        __instance.detonated = true;
        if (!otherMine.detonated)
        {
            otherMine.ChangeState();
            otherMine.HandleExplosion();
        }

        if (colliders.Length != 0)
        {
            Vector3 destination = otherMine.transform.position;
            var currPlayer = colliders
            .FirstOrDefault(c => c.TryGetComponent(out PlayerHealth health) && health.IsOwner);
            if (currPlayer && currPlayer.TryGetComponent(out PlayerHealth health))
                health.controller.Teleport(destination, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
        }
        __instance.ExplodeServer();
        return false;
    }
}