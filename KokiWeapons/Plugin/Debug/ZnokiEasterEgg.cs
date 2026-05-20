// using HarmonyLib;
// using UnityEngine;
// using UnityEngine.InputSystem;

// [HarmonyPatch(typeof(FirstPersonController), "OnEnable")]
// public static class EasterEgg
// {
//     public static GameObject explosionVfx;
//     public static AudioClip explosionAudio;

//     private static FirstPersonController _fpc;
//     private static PlayerControls.PlayerActions _controls;
//     private static int _boomCounter = 0;
//     private static int _boomBoomCounter = 0;

//     public static void Postfix(FirstPersonController __instance)
//     {
//         var tptrap = SpawnerManager.NameToWeaponDict["Teleport Mine"].GetComponent<WeaponHandSpawner>().objToSpawn.GetComponent<TPTrap>();
//         explosionVfx = tptrap.explosionVfx;
//         explosionAudio = tptrap.explosionAudio;

//         _fpc = __instance;
//         _controls = __instance.playerControls.Player;
//         _controls.LeanLeft.performed += Boom;
//         _controls.LeanRight.performed += Boom;
//         _controls.Crouch.performed += Boom;
//     }

//     public static void Boom(InputAction.CallbackContext ctx)
//     {
//         InputAction expectedAction = null;
//         if (_boomCounter == 0)
//         {
//             expectedAction = _controls.LeanLeft;
//         }
//         else if (_boomCounter == 1)
//         {
//             expectedAction = _controls.LeanRight;
//         }
//         else if (_boomCounter == 2)
//         {
//             expectedAction = _controls.Crouch;
//         }

//         if (expectedAction == ctx.action)
//         {
//             if (++_boomCounter != 3) return;
//             _boomCounter = 0;
//             if (++_boomBoomCounter == 2)
//             {
//                 _boomBoomCounter = 0;
//                 Object.Instantiate(explosionVfx, _fpc.transform.position, Quaternion.identity);
//                 SoundManager.Instance.PlaySound(explosionAudio);
//                 _fpc.transform.root.GetComponent<PlayerHealth>().RemoveHealth(100);
//                 _fpc.transform.root.GetComponent<PlayerHealth>().Explode(
//                                 explode: true,
//                                 dismemberment: true,
//                                 memberName: "",
//                                 ejectForceDir: _fpc.transform.forward,
//                                 force: 30f,
//                                 position: _fpc.transform.position + Vector3.up * 2f + Vector3.right);
//             }
//         }
//         else
//         {
//             _boomCounter = 0;
//             _boomBoomCounter = 0;
//         }
//     }
// }