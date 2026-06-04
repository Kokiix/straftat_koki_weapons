using System.Collections.Generic;
using System.Reflection.Emit;
using FishNet;
using FishNet.Object;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(FirstPersonController), "HandleCameraController")]
public static class HandleNullCamera
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var playerCamera = AccessTools.Field(typeof(FirstPersonController), "playerCamera");
        var implicitBool = AccessTools.Method(typeof(UnityEngine.Object), "op_Implicit");

        var continueFunc = generator.DefineLabel();

        return new CodeMatcher(instructions, generator).Start()
        .InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, playerCamera),
            new CodeInstruction(OpCodes.Call, implicitBool),
            new CodeInstruction(OpCodes.Brtrue, continueFunc),
            new CodeInstruction(OpCodes.Ret))
        .AddLabels([continueFunc])
        .InstructionEnumeration();
    }
}