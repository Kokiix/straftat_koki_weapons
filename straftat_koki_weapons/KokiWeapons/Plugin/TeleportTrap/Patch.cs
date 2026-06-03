using System.Collections.Generic;
using System.Reflection.Emit;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using HarmonyLib;
using MyceliumNetworking;
using UnityEngine;

public static class UpdateTrapLinkOnPlace
{
    public static void Init()
    {
        CreatePlaceItemEvent.PlaceItemEvent += UpdateTrapLink;
    }

    public static void UpdateTrapLink(object sender, CreatePlaceItemEvent.PlaceItemEventArgs eventArgs)
    {
        WeaponHandSpawner __instance = eventArgs.spawner;
        GameObject newTrap = eventArgs.spawnedObj;

        newTrap.GetComponent<TPTrap>().owner = __instance.transform.root;
        if (!__instance.gameObject.TryGetComponent(out TPLink link))
        {
            Debug.LogError("no trap lnk :(");
            eventArgs.runOriginalCode = true;
        }
        else if (link.otherTrapNob == -1)
        {
            link.otherTrapNob = newTrap.GetComponent<NetworkObject>().ObjectId;
        }
        else
        {
            var nobID1 = newTrap.GetComponent<NetworkObject>().ObjectId;
            var nobID2 = link.otherTrapNob;
            TPTrapNetworking.RPC("LinkMines", [nobID1, nobID2]);
        }
    }
}

[HarmonyPatch(typeof(Weapon), "TriggerEnvironment")]
public static class ExplodeTPTrapOnHit
{
    public static void Prefix(GameObject obj)
    {
        GameObject trap = obj.transform.root.gameObject;
        if (trap.GetComponent<TPTrap>())
        {
            TPTrapNetworking.RPC("DestroyTrapPair", [trap.GetComponent<NetworkObject>().ObjectId, -1]);
        }
    }
}