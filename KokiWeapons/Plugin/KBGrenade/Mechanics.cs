using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using FishNet;
using HarmonyLib;
using UnityEngine;
using static KBGrenade.Init;

public static class KBGrenadeMechanics
{
    [HarmonyPatch(typeof(PhysicsGrenade), "RpcLogic___HandleExplosion_4276783012")]
    public static class InsertKBEffect
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var isOwner = AccessTools.Field(typeof(PhysicsGrenade), nameof(PhysicsGrenade.isOwner));
            var ph2 = AccessTools.Field(typeof(PhysicsGrenade), nameof(PhysicsGrenade.ph2));
            var makeBlood = AccessTools.Field(typeof(PhysicsGrenade), nameof(PhysicsGrenade.makeBlood));

            var kbEffect = AccessTools.Method(typeof(KBGrenadeMechanics), nameof(KBAll));
            var implicitBool = AccessTools.Method(typeof(UnityEngine.Object), "op_Implicit");
            var getKBGrenade = AccessTools.Method(typeof(GameObject), nameof(GameObject.GetComponent), parameters: null, generics: [typeof(RepulsionGrenade)]);
            var getTypeFromHandle = AccessTools.Method(typeof(Type), nameof(Type.GetTypeFromHandle), [typeof(System.RuntimeTypeHandle)]);

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
                new CodeInstruction(OpCodes.Call, getKBGrenade),
                new CodeInstruction(OpCodes.Call, implicitBool),
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

        public static void Postfix(PhysicsGrenade __instance)
        {
            __instance.transform.Find("meshScale").gameObject.SetActive(false);
        }
    }
    public static void KBAll(PhysicsGrenade instance, Collider[] colliders, PlayerHealth[] healths)
    {
        healths.Distinct().DoIf(ph => ph, ph =>
        {
            Vector3 force = ph.controller.transform.position - instance.transform.position;
            force.y = 0;
            force.Normalize();

            if (ph.controller.isGrounded)
                ph.controller.transform.position += new Vector3(0, 2.5f, 0);
            ph.controller.CustomAddForce(force, 150);
        });

        Rigidbody rb = null;
        colliders.DoIf(c => c && c.gameObject.TryGetComponent(out rb),
        item =>
        {
            Vector3 force = item.transform.position - instance.transform.position;
            force.y = 0;
            force.Normalize();
            force *= 10;
            force.y = 2;

            item.transform.position += new Vector3(0, 2.5f, 0);
            rb.AddForce(force, ForceMode.Impulse);
        });
    }
}