using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

/// Runs server side only! This is necessary because it's the only point where the hand item and physics item are present in the same context.
[HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
public static class UpdateTrapLinkOnPlace
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var gameObject = AccessTools.PropertyGetter(typeof(MonoBehaviour), nameof(MonoBehaviour.gameObject));
        var getComponentLink = AccessTools.Method(typeof(GameObject), nameof(GameObject.GetComponent), null, [typeof(TPLink)]);
        var implicitBool = AccessTools.Method(typeof(UnityEngine.Object), "op_Implicit");

        var updateTrapLink = AccessTools.Method(typeof(UpdateTrapLinkOnPlace), nameof(UpdateTrapLinkOnPlace.UpdateTrapLink));

        return new CodeMatcher(instructions)
        .End().CreateLabel(out Label ret)
        .Insert(
            // If WeaponHandSpawner's gameobj has a traplink...
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Callvirt, gameObject),
            new CodeInstruction(OpCodes.Call, getComponentLink),
            new CodeInstruction(OpCodes.Call, implicitBool),
            new CodeInstruction(OpCodes.Brfalse, ret),

            // Update it.
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Call, updateTrapLink))
        .InstructionEnumeration();
    }

    public static void UpdateTrapLink(WeaponHandSpawner __instance, GameObject newTrap)
    {
        if
    }
}