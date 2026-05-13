using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using MyceliumNetworking;
using Steamworks;
using UnityEngine;

[HarmonyPatch]
public static class MarksmanMechanics
{
    [HarmonyPatch(typeof(FirstPersonController), "SetZoom")]
    [HarmonyPatch(typeof(FirstPersonController), "SetZoomToggle")]
    [HarmonyPostfix]
    public static void TriggerCoinThrow(FirstPersonController __instance)
    {
        if (__instance.playerPickupScript.behaviourInHand && __instance.playerPickupScript.behaviourInHand.name == Marksman.name + "(Clone)")
        {
            if (__instance.isZooming) ThrowCoin(__instance);
            __instance.isZooming = false;
        }
    }

    public static void ThrowCoin(FirstPersonController fpc)
    {
        var coin = UnityEngine.Object.Instantiate(Marksman.TemplateCoin);
        coin.name = "coin"; // Unnecessary after below tag fix
        coin.SetActive(true);
        coin.layer = 18;

        // TODO: coin mesh

        // TODO: add randomization to angle so it isnt so easy to shoo
        coin.transform.position = fpc.playerCamera.transform.position + fpc.playerCamera.transform.forward;
        coin.transform.position += new Vector3(0, -0.5f, 0);

        var coinRB = coin.GetComponent<Rigidbody>();
        var coinTossForce = fpc.playerCamera.transform.forward.normalized * 15f;
        KDBG.Log(coinTossForce);
        coinTossForce.y += 2.5f;
        coinRB.AddForce(coinTossForce, ForceMode.Impulse);

        // TODO: play coin spin animation
    }

    public static int[] bulletPassThroughLayers = [10, 14, 18, 19, 24];
    [HarmonyPatch(typeof(Weapon), "TriggerEnvironment")]
    [HarmonyPostfix]
    public static void ReboundCoin(Weapon __instance, GameObject obj)
    {
        // TODO: 2 ammo removed :(

        // TODO: replace with custom tag
        if (obj.name == "coin")
        {
            var shot = false;
            foreach (var player in SteamLobby.Instance.players)
            {
                var playerPos = player.gameObject.GetComponent<ClientInstance>().PlayerSpawner.player.playerCamera.transform.position;
                KDBG.Log(playerPos);
                var rayCastHits = Physics.RaycastAll(obj.transform.position,
                    playerPos - obj.transform.position,
                    float.PositiveInfinity);
                Array.Sort(rayCastHits, (hitA, hitB) => hitA.distance.CompareTo(hitB.distance));
                foreach (var hit in rayCastHits)
                {
                    var hitObjLayer = hit.transform.gameObject.layer;
                    if (bulletPassThroughLayers.Contains(hitObjLayer)) continue;
                    if (hitObjLayer == 11)
                    {
                        ((Gun)__instance).ShootServer(
                            __instance.damage * Marksman.CoinDamageBoost,
                            obj.transform.position,
                            playerPos - obj.transform.position);
                        shot = true;
                        break;
                    }
                }
                if (shot) break;
            }
        }
    }
}