using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class KBGrenadeMechanics
{
    [HarmonyPatch(typeof(PhysicsGrenade), "RpcLogic___HandleExplosion_4276783012")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> InsertKBEffect(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var isOwner = AccessTools.Field(typeof(PhysicsGrenade), nameof(PhysicsGrenade.isOwner));
        var ph2 = AccessTools.Field(typeof(PhysicsGrenade), nameof(PhysicsGrenade.ph2));
        var kbEffect = AccessTools.Method(typeof(KBGrenadeMechanics), nameof(KBEffect));
        var getIsKBGrenade = AccessTools.Method(typeof(KBGrenade), nameof(KBGrenade.GetIsKBGrenade));
        var implicitBool = AccessTools.Method(typeof(Object), "op_Implicit");
        var gameObject = AccessTools.PropertyGetter(typeof(PhysicsGrenade), nameof(PhysicsGrenade.gameObject));

        var matcher = new CodeMatcher(instructions, generator)
        .MatchForward(useEnd: false, new CodeMatch(OpCodes.Ldfld, isOwner));

        var loopContinue = (Label)matcher.Advance(1).Instruction.operand;

        matcher.Advance(1).CreateLabel(out Label resumeLoopLogic);

        return matcher.Insert(
            // If mine is not KB continue
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Callvirt, gameObject),
            new CodeInstruction(OpCodes.Call, getIsKBGrenade),
            new CodeInstruction(OpCodes.Call, implicitBool),
            new CodeInstruction(OpCodes.Brfalse, resumeLoopLogic),

            // Execute KB
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, ph2),
            new CodeInstruction(OpCodes.Ldloc, 16),
            new CodeInstruction(OpCodes.Ldelem_Ref),
            new CodeInstruction(OpCodes.Call, kbEffect),
            new CodeInstruction(OpCodes.Br, loopContinue))
        .InstructionEnumeration();
    }

    public static void KBEffect(PlayerHealth ph)
    {
        ph.RemoveHealth(1);
        KokiDebug.Log(ph.name);
    }
}