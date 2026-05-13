using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class MarksmanMechanics
{
    [HarmonyPatch(typeof(FirstPersonController), "SetZoom")]
    [HarmonyPatch(typeof(FirstPersonController), "SetZoomToggle")]
    [HarmonyPostfix]
    public static void TriggerCoinThrowToggle(FirstPersonController __instance)
    {
        if (__instance.playerPickupScript.behaviourInHand.name == Marksman.name + "(Clone)")
        {
            if (__instance.isZooming) ThrowCoin();
            __instance.isZooming = false;
        }
    }

    public static void ThrowCoin()
    {
        KokiDebug.Log("throw");
    }
}