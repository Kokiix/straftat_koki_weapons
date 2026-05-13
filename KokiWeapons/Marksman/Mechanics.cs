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
    public static void TriggerCoinThrow(FirstPersonController __instance)
    {
        if (__instance.playerPickupScript.behaviourInHand.name == Marksman.name + "(Clone)")
        {
            if (__instance.isZooming) ThrowCoin(__instance);
            __instance.isZooming = false;
        }
    }

    public static void ThrowCoin(FirstPersonController fpc)
    {
        var coin = Object.Instantiate(Marksman.TemplateCoin);
        coin.name = Marksman.name; // Unnecessary after below tag fix
        coin.SetActive(true);
        coin.layer = 18;

        // TODO: coin mesh

        // TODO: add randomization to angle so it isnt so easy to shoot
        coin.transform.position = fpc.playerCameraHolder.transform.position + fpc.dirForward.normalized;
        coin.transform.position += new Vector3(0, -1, 0);

        var coinRB = coin.GetComponent<Rigidbody>();
        var coinTossForce = fpc.dirForward.normalized * 10f;
        coinTossForce.y = 7.5f;
        coinRB.AddForce(coinTossForce, ForceMode.Impulse);

        // TODO: play coin spin animation
    }

    [HarmonyPatch(typeof(Weapon), "TriggerEnvironment")]
    [HarmonyPostfix]
    public static void ReboundCoin(Weapon __instance, GameObject obj)
    {
        // TODO: replace with custom tag
        if (obj.name == Marksman.name)
        {
            // gun.shootserver

        }
    }
}