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

        var gameObject = AccessTools.PropertyGetter(typeof(Component), "gameObject");
        var getRCCarItem = AccessTools.Method(typeof(GameObject), "GetComponent", parameters: null, generics: [typeof(RCCarItem)]);
        var implicitBool = AccessTools.Method(typeof(UnityEngine.Object), "op_Implicit");

        var handleFire = AccessTools.Method(typeof(FirePatch), "HandleFire");

        var continueFunc = generator.DefineLabel();

        return new CodeMatcher(instructions, generator)
        .MatchForward(useEnd: true,
        new CodeMatch(OpCodes.Stfld, fireTimer),
        new CodeMatch(OpCodes.Ldarg_0))
        .InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Callvirt, gameObject),
            new CodeInstruction(OpCodes.Call, getRCCarItem),
            new CodeInstruction(OpCodes.Call, implicitBool),
            new CodeInstruction(OpCodes.Brfalse, continueFunc),
            new CodeInstruction(OpCodes.Call, handleFire),
            new CodeInstruction(OpCodes.Ret))
        .AddLabels([continueFunc])
        .InstructionEnumeration();
    }

    public static void HandleFire(WeaponHandSpawner __instance)
    {
        Debug.Log("firing");
    }
}