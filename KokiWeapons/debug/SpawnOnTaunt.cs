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
        GameObject weaponBase = null;
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (!TPTrap.TemplateGameObject)
                TPTrap.InitTemplate();
            weaponBase = TPTrap.TemplateGameObject;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (!KBGrenade.TemplateGameObject)
                KBGrenade.InitTemplate();
            weaponBase = KBGrenade.TemplateGameObject;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (!Marksman.TemplateGameObject)
                Marksman.InitTemplate();
            weaponBase = Marksman.TemplateGameObject;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            weaponBase = SpawnerManager.NameToWeaponDict["Gun"];
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
        }

        if (!weaponBase || !InstanceFinder.IsServer) return;

        foreach (var fpc in Object.FindObjectsOfType<FirstPersonController>())
        {
            Vector3 playerPos = fpc.playerCameraHolder.transform.position + fpc.dirForward.normalized;
            playerPos.y -= 0.5f;
            GameObject weaponInstance = Object.Instantiate(weaponBase, playerPos, Quaternion.identity);

            weaponInstance.GetComponent<ItemBehaviour>().DispenserDrop(Vector3.zero);
            weaponInstance.GetComponent<Rigidbody>().isKinematic = true;
            fpc.ServerManager.Spawn(weaponInstance);

            weapons.Add(weaponInstance);
        }
    }
}