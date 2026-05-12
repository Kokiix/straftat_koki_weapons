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
    // Now that I think about it isn't this what objToSpawn in WeaponHandSpawner is for? Why doesn't that work?
    [HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
    [HarmonyPrefix]
    static void SwapTemplateGO(WeaponHandSpawner __instance, ref GameObject obj)
    {
        if (__instance.gameObject.GetComponent<TrapLink>())
            obj = TPTrap.TemplatePhysGameObject;
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

        MyceliumNetwork.RPC(TPTrapNetworking.MyceliumID,
        nameof(TPTrapNetworking.DisplayClientVisual), ReliableType.Reliable,
        newTrap.GetComponent<NetworkObject>().ObjectId, nameof(TPTrap.ConvertToPhysTPTrap), false);

        if (connector.otherTrap)
        {
            GameObject otherTrap = connector.otherTrap.gameObject;

            TPTrap.GetTrapLink(otherTrap).otherTrap = newTrap;
            TPTrap.GetTrapLink(newTrap).otherTrap = otherTrap;

            ProximityMine otherMine = otherTrap.GetComponent<ProximityMine>();
            otherTrap.transform.Find("radius(Clone)").gameObject.SetActive(true);
            newTrap.transform.Find("radius(Clone)").gameObject.SetActive(true);

            MyceliumNetwork.RPC(TPTrapNetworking.MyceliumID,
            nameof(TPTrapNetworking.DisplayClientVisual), ReliableType.Reliable,
            otherTrap.GetComponent<NetworkObject>().ObjectId, nameof(TPTrapNetworking.ToggleRadius), false);

            MyceliumNetwork.RPC(TPTrapNetworking.MyceliumID,
            nameof(TPTrapNetworking.DisplayClientVisual), ReliableType.Reliable,
            newTrap.GetComponent<NetworkObject>().ObjectId, nameof(TPTrapNetworking.ToggleRadius), false);
        }
        else
            connector.otherTrap = newTrap;
    }

    [HarmonyPatch(typeof(ProximityMine), "OnTriggerStay")]
    [HarmonyPostfix]
    public static void DetectExplosion(ProximityMine __instance)
    {
        if (!TPTrap.GetTrapLink(__instance.gameObject)) return;

        GameObject otherTrap = TPTrap.GetTrapLink(__instance.gameObject).otherTrap;

        if (!InstanceFinder.IsServer || __instance.stunMine || !otherTrap) return;

        __instance.ChangeState();
        __instance.HandleExplosion();
    }

    [HarmonyPatch(typeof(ProximityMine), "HandleExplosion")]
    [HarmonyPrefix]
    public static bool HandleExplosion(ProximityMine __instance)
    {
        if (!TPTrap.GetTrapLink(__instance.gameObject)) return true;

        if (!InstanceFinder.IsServer) return false;

        ProximityMine otherMine = TPTrap.GetTrapLink(__instance.gameObject).otherTrap.GetComponent<ProximityMine>();
        Collider[] colliders = Physics.OverlapSphere(__instance.transform.position, __instance.explosionRadius, __instance.bodyLayer);

        Vector3 destination = otherMine.transform.position;

        __instance.stunMine = true;
        if (!otherMine.stunMine)
        {
            otherMine.ChangeState();
            otherMine.HandleExplosion();
        }

        if (colliders.Length != 0)
        {
            foreach (Collider c in colliders)
            {
                FirstPersonController fpc = c.GetComponent<FirstPersonController>();
                if (fpc)
                {
                    if (fpc.Owner.IsLocalClient)
                    {
                        fpc.Teleport(destination, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
                    }
                    else
                    {
                        ulong.TryParse(fpc.Owner.GetAddress(), out ulong steamID);
                        MyceliumNetwork.RPCTarget(TPTrapNetworking.MyceliumID, nameof(TPTrapNetworking.TeleportClient), (CSteamID)steamID, ReliableType.Reliable, destination);
                    }
                }
            }
        }
        __instance.ExplodeServer();
        return false;
    }

    [HarmonyPatch(typeof(ProximityMine), "Start")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> StopActivateCoroutine(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var getTrapLink = AccessTools.Method(typeof(TPTrap), nameof(TPTrap.GetTrapLink));

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
    public static void ExplodeTPTrapOnHit(Weapon __instance, GameObject obj)
    {
        GameObject trap = obj.transform.root.gameObject;
        if (obj.CompareTag("Mine") && TPTrap.GetTrapLink(trap))
        {
            if (!InstanceFinder.IsHost)
            {
                MyceliumNetwork.RPC(TPTrapNetworking.MyceliumID,
                nameof(TPTrapNetworking.ExplodeMineFromClient), ReliableType.Reliable,
                obj.transform.root.gameObject.GetComponent<NetworkObject>().ObjectId);
                return;
            }
            GameObject otherTrap = TPTrap.GetTrapLink(trap).otherTrap;
            if (otherTrap)
            {
                otherTrap.transform.Find("radius(Clone)").gameObject.SetActive(false);
                MyceliumNetwork.RPC(TPTrapNetworking.MyceliumID,
                nameof(TPTrapNetworking.DisplayClientVisual), ReliableType.Reliable,
                otherTrap.GetComponent<NetworkObject>().ObjectId, nameof(TPTrapNetworking.ToggleRadius), false);
            }

            obj.transform.root.GetComponent<ProximityMine>().ChangeState();
            obj.transform.root.GetComponent<ProximityMine>().ExplodeServer();
        }
    }
}