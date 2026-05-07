using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

// KokiWeaponsPlugin.Logger.LogError(
[HarmonyPatch(typeof(Settings))]
public class SpawnWeaponOnTaunt
{
    public static List<GameObject> weapons = new();

    [HarmonyPatch("IncreaseTauntsAmount")]
    public static void Prefix(Settings __instance)
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            KokiWeaponsPlugin.Logger.LogError("Triggering OnDestroy");
            // KokiWeaponsPlugin.OnDestroy();
            return;
        }

        // NOT NETWORKED (i think) 
        GameObject weaponBase = TeleportTrap.GameObject();

        Vector3 playerPos = __instance.localPlayer.playerCameraHolder.transform.position + __instance.localPlayer.dirForward.normalized;
        playerPos.y -= 0.5f;
        GameObject weaponInstance = Object.Instantiate(weaponBase, playerPos, Quaternion.identity);
        weaponInstance.GetComponent<ItemBehaviour>().DispenserDrop(Vector3.zero);
        weaponInstance.GetComponent<Rigidbody>().isKinematic = true;

        __instance.localPlayer.ServerManager.Spawn(weaponInstance);

        weapons.Add(weaponInstance);
    }
}