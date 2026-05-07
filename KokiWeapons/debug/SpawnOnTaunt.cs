using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace KokiWeapons.Debug;

// KokiWeaponsPlugin.Logger.LogError("
[HarmonyPatch(typeof(Settings))]
public class Patch
{
    static ItemBehaviour test;
    [HarmonyPatch("IncreaseTauntsAmount")]
    public static void Prefix(Settings __instance)
    {
        // NOT NETWORKED (i think)
        Vector3 playerPos = __instance.localPlayer.playerCameraHolder.transform.position + __instance.localPlayer.dirForward.normalized;
        playerPos.y -= 0.5f;
        GameObject weaponBase = SpawnerManager.NameToWeaponDict["AR15"];
        GameObject weaponInstance = Object.Instantiate(weaponBase, playerPos, Quaternion.identity);
        __instance.localPlayer.ServerManager.Spawn(weaponInstance);
        weaponInstance.GetComponent<ItemBehaviour>().DispenserDrop(Vector3.zero);
        weaponInstance.GetComponent<Rigidbody>().isKinematic = true;
    }
}