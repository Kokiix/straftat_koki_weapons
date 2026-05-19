using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(WeaponHandSpawner), "Fire")]
public static class FirePatch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var fireTimer = AccessTools.Field(typeof(WeaponHandSpawner), "fireTimer");

        // Does this item have RCCarItem component?
        var gameObject = AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject));
        var getRCCarItem = AccessTools.Method(typeof(GameObject), nameof(GameObject.GetComponent), parameters: null, generics: [typeof(RCCarItem)]);
        var implicitBool = AccessTools.Method(typeof(UnityEngine.Object), "op_Implicit");

        // If so, custom fire
        var handleFire = AccessTools.Method(typeof(FirePatch), "HandleFire");

        var continueFunc = generator.DefineLabel();

        return new CodeMatcher(instructions, generator)
        .MatchForward(useEnd: true,
        new CodeMatch(OpCodes.Stfld, fireTimer),
        new CodeMatch(OpCodes.Ldarg_0))
        .InsertAndAdvance(
            // Does this item have RCCarItem component?
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Callvirt, gameObject),
            new CodeInstruction(OpCodes.Call, getRCCarItem),
            new CodeInstruction(OpCodes.Call, implicitBool),
            new CodeInstruction(OpCodes.Brfalse, continueFunc),

            // If so, custom fire
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, handleFire),
            new CodeInstruction(OpCodes.Ret))
        .AddLabels([continueFunc])
        .InstructionEnumeration();
    }

    public static void HandleFire(WeaponHandSpawner __instance)
    {
        if (__instance.currentAmmo == 1)
        {
            // Place car
            __instance.SpawnObject(__instance.objToSpawn, __instance.position, __instance.rotation);
            __instance.CameraAnimation();
            __instance.WeaponAnimation();

            __instance.currentAmmo = 2;
            __instance.needsAmmo = false;
            __instance.maxInteractionDistance = 0;
        }
        else
        {
            // Enter car
            Debug.LogError("tasering");
            __instance.transform.root.gameObject.GetComponent<FirstPersonController>().sync___set_value_canMove(false, true);
        }
    }
}

public static class RCCarLink
{
    public static void Init()
    {
        InsertPlaceItemEvent.PlaceItemEvent += LinkRCCar;
    }

    public static void LinkRCCar(object sender, InsertPlaceItemEvent.SpawnEventArgs eventArgs)
    {
        WeaponHandSpawner __instance = eventArgs.spawner;
        GameObject newCar = eventArgs.spawnedObj;

        if (!__instance.gameObject.TryGetComponent(out RCCar car))
        {
            eventArgs.runOriginalCode = true;
        }
        else
        {
            __instance.gameObject.GetComponent<RCCarItem>().car = car;
        }
    }
}