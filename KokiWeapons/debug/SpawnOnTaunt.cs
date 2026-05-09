using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(Settings))]
public class SpawnWeaponOnTaunt
{
    public static List<GameObject> weapons = [];

    [HarmonyPatch("IncreaseTauntsAmount")]
    public static void Prefix(Settings __instance)
    {
        GameObject weaponBase = null;
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            weaponBase = TeleportTrap.TemplateGameObject;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            weaponBase = SpawnerManager.NameToWeaponDict["APMine"];
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            weaponBase = SpawnerManager.NameToWeaponDict["ProximityMine"];
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            KokiDebug.Log(SpawnerManager.Instance.GetRandomSpawnableWeapon().name);
        }

        if (!weaponBase) return;

        Vector3 playerPos = __instance.localPlayer.playerCameraHolder.transform.position + __instance.localPlayer.dirForward.normalized;
        playerPos.y -= 0.5f;
        GameObject weaponInstance = Object.Instantiate(weaponBase, playerPos, Quaternion.identity);

        weaponInstance.GetComponent<ItemBehaviour>().DispenserDrop(Vector3.zero);
        weaponInstance.GetComponent<Rigidbody>().isKinematic = true;
        __instance.localPlayer.ServerManager.Spawn(weaponInstance);

        weapons.Add(weaponInstance);
    }
}