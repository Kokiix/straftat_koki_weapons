using System.Collections.Generic;
using System.Linq;
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
        var makeBlood = AccessTools.Field(typeof(PhysicsGrenade), nameof(PhysicsGrenade.makeBlood));
        var kbEffect = AccessTools.Method(typeof(KBGrenadeMechanics), nameof(KBAll));
        var getIsKBGrenade = AccessTools.Method(typeof(KBGrenade), nameof(KBGrenade.GetIsKBGrenade));
        var implicitBool = AccessTools.Method(typeof(Object), "op_Implicit");
        var gameObject = AccessTools.PropertyGetter(typeof(PhysicsGrenade), nameof(PhysicsGrenade.gameObject));

        return new CodeMatcher(instructions, generator)
        .MatchForward(useEnd: false,
        new CodeMatch(OpCodes.Ldstr, "Player"))
        .CreateLabel(out Label loopEnd)

        .MatchBack(useEnd: false,
        new CodeMatch(OpCodes.Stfld, makeBlood))
        .MatchForward(useEnd: false,
        new CodeMatch(OpCodes.Ldc_I4_0))
        .CreateLabel(out Label loopStart)

        .Insert(
            // If mine is not KB resume
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Callvirt, gameObject),
            new CodeInstruction(OpCodes.Call, getIsKBGrenade),
            // new CodeInstruction(OpCodes.Call, implicitBool),
            new CodeInstruction(OpCodes.Brfalse, loopStart),

            // Execute KB effect
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldloc_2),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, ph2),
            new CodeInstruction(OpCodes.Call, kbEffect),
            new CodeInstruction(OpCodes.Br, loopEnd))
        .InstructionEnumeration();
    }

    public static void KBAll(PhysicsGrenade instance, Collider[] colliders, PlayerHealth[] healths)
    {
        healths.Distinct().Do(ph =>
        {
            if (!ph) return;

            Vector3 force = ph.controller.transform.position - instance.transform.position;
            if (force.y < 0)
                force.y = 0;
            force.Normalize();
            force *= 2.5f;
            if (force.y == 0)
                force.y = 1f;

            // KokiDebug.Log(force);
            // KokiDebug.Log(ph.controller.moveDirection);
            // ph.controller.AddHorizontalForce(force, 10);
            // KokiDebug.Log(ph.controller.moveDirection);

            ph.controller.AddForce(new Vector3(10, 10, 10), 1);
        });
    }

    // [HarmonyPatch(typeof(PhysicsGrenade), "Update")]
    // [HarmonyPrefix]
    // public static bool test()
    // {
    //     return false;
    // }
}