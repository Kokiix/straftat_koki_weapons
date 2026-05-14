// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection.Emit;
// using HarmonyLib;
// using UnityEngine;

// public static class KBGrenadeMechanics
// {
//     [HarmonyPatch(typeof(PhysicsGrenade), "RpcLogic___HandleExplosion_4276783012")]
//     public static class InsertKBEffect
//     {
//         public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
//         {
//             var isOwner = AccessTools.Field(typeof(PhysicsGrenade), nameof(PhysicsGrenade.isOwner));
//             var ph2 = AccessTools.Field(typeof(PhysicsGrenade), nameof(PhysicsGrenade.ph2));
//             var makeBlood = AccessTools.Field(typeof(PhysicsGrenade), nameof(PhysicsGrenade.makeBlood));
//             var kbEffect = AccessTools.Method(typeof(KBGrenadeMechanics), nameof(KBAll));
//             var implicitBool = AccessTools.Method(typeof(Object), "op_Implicit");
//             var gameObject = AccessTools.PropertyGetter(typeof(PhysicsGrenade), nameof(PhysicsGrenade.gameObject));

//             return new CodeMatcher(instructions, generator)
//             .MatchForward(useEnd: false,
//             new CodeMatch(OpCodes.Ldstr, "Player"))
//             .CreateLabel(out Label loopEnd)

//             .MatchBack(useEnd: false,
//             new CodeMatch(OpCodes.Stfld, makeBlood))
//             .MatchForward(useEnd: false,
//             new CodeMatch(OpCodes.Ldc_I4_0))
//             .CreateLabel(out Label loopStart)

//             .Insert(
//                 // If mine is not KB resume
//                 new CodeInstruction(OpCodes.Ldarg_0),
//                 new CodeInstruction(OpCodes.Callvirt, gameObject),
//                 new CodeInstruction(OpCodes.Call, getIsKBGrenade), // TODO: use getcomponent
//                 new CodeInstruction(OpCodes.Call, implicitBool),
//                 new CodeInstruction(OpCodes.Brfalse, loopStart),

//                 // Execute KB effect
//                 new CodeInstruction(OpCodes.Ldarg_0),
//                 new CodeInstruction(OpCodes.Ldloc_2),
//                 new CodeInstruction(OpCodes.Ldarg_0),
//                 new CodeInstruction(OpCodes.Ldfld, ph2),
//                 new CodeInstruction(OpCodes.Call, kbEffect),
//                 new CodeInstruction(OpCodes.Br, loopEnd))
//             .InstructionEnumeration();
//         }
//     }
//     public static void KBAll(PhysicsGrenade instance, Collider[] colliders, PlayerHealth[] healths)
//     {
//         healths.Distinct().DoIf(ph => ph, ph =>
//         {
//             Vector3 force = ph.controller.transform.position - instance.transform.position;
//             force.y = 0;
//             force.Normalize();

//             if (ph.controller.isGrounded)
//                 ph.controller.transform.position += new Vector3(0, 2.5f, 0);
//             ph.controller.CustomAddForce(force, 150);
//         });

//         colliders.DoIf(c => c && c.gameObject.GetComponent<Rigidbody>(),
//         item =>
//         {
//             Vector3 force = item.transform.position - instance.transform.position;
//             force.y = 0;
//             force.Normalize();
//             force *= 10;
//             force.y = 2;

//             item.transform.position += new Vector3(0, 2.5f, 0);
//             item.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);
//         });
//     }
// }