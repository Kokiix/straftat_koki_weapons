using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(Settings))]
public class SpawnWeaponOnTaunt
{
    public static List<GameObject> weapons = [];

    [HarmonyPatch("IncreaseTauntsAmount")]
    public static void Prefix(Settings __instance)
    {
        string weapon = "";
        if (Input.GetKeyDown(KeyCode.Alpha1))
            weapon = "StunGrenade";
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            weapon = "Repulsion Grenade";
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            weapon = "Repulsar";
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            weapon = "Teleport Mine";
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            weapon = "APMine";
        else if (Input.GetKeyDown(KeyCode.Alpha6))
            weapon = "Gun";

        if (weapon == "" || !InstanceFinder.IsServer) return;

        foreach (var fpc in Object.FindObjectsOfType<FirstPersonController>())
        {
            Vector3 playerPos = fpc.playerCameraHolder.transform.position + fpc.dirForward.normalized;
            playerPos.y -= 0.5f;
            GameObject weaponInstance = Object.Instantiate(SpawnerManager.NameToWeaponDict[weapon], playerPos, Quaternion.identity);

            weaponInstance.GetComponent<ItemBehaviour>().DispenserDrop(Vector3.zero);
            weaponInstance.GetComponent<Rigidbody>().isKinematic = true;
            fpc.ServerManager.Spawn(weaponInstance);

            weapons.Add(weaponInstance);
        }
    }
}