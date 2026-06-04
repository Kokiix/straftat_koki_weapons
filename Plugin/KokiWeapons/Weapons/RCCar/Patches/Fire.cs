using System.Collections.Generic;
using System.Reflection.Emit;
using FishNet;
using FishNet.Object;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(WeaponHandSpawner), "Fire")]
public static class FirePatch
{
    public static bool Prefix(WeaponHandSpawner __instance)
    {
        if (!__instance.GetComponent<RCCarItem>()) return true;

        if (!PauseManager.Instance.pause && !(__instance.behaviour.playerPickup.currentEnvironmentInteractable != null) && __instance.playerController.IsOwner && __instance.playerController.sync___get_value_canMove() && !(__instance.fireTimer > 0f))
        {
            __instance.fireTimer = __instance.timeBetweenFire;
            if (__instance.currentAmmo == 1 && __instance.canPlace)
            {
                // Place car
                __instance.SpawnObject(__instance.objToSpawn, __instance.position, __instance.rotation);
                __instance.CameraAnimation();
                __instance.WeaponAnimation();

                __instance.currentAmmo = 2;
                __instance.needsAmmo = false;
                __instance.maxInteractionDistance = 0;
            }
            else if (__instance.playerController.IsOwner)
            {
                var car = __instance.gameObject.GetComponent<RCCarItem>().car;
                if (!car.driving)
                    car.BeginDriving(__instance.playerController);
            }
        }

        return false;
    }
}

public static class RCCarLink
{
    public static void Init()
    {
        CreatePlaceItemEvent.PlaceItemEvent += LinkRCCar;
    }

    public static void LinkRCCar(object sender, CreatePlaceItemEvent.PlaceItemEventArgs eventArgs)
    {
        WeaponHandSpawner __instance = eventArgs.spawner;
        GameObject newCar = eventArgs.spawnedObj;

        if (!__instance.gameObject.TryGetComponent(out RCCarItem carItem))
        {
            eventArgs.runOriginalCode = true;
        }
        else
        {
            eventArgs.runOriginalCode = false;
            RCCarNetworking.RPC("LinkCarToCarItem", [
                __instance.gameObject.GetComponent<NetworkObject>().ObjectId,
                newCar.GetComponent<NetworkObject>().ObjectId]);
        }
    }
}

// [HarmonyPatch(typeof(WeaponHandSpawner), "Update")]
// public static class Test
// {
//     public static void Prefix(WeaponHandSpawner __instance)
//     {
//         Debug.LogError(__instance.inLeftHand);
//         Debug.LogError(__instance.inRightHand);
//     }
// }