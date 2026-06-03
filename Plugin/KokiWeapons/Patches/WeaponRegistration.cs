// Runs once at game start
using System.Collections.Generic;
using FishNet;
using FishNet.Managing;
using FishNet.Object;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(Resources), "LoadAll", [typeof(string), typeof(System.Type)])]
public static class InsertWeaponsIntoResources
{
    public static void Postfix(string path, ref Object[] __result)
    {
        if (path != "RandomWeapons") return;

        var allWeapons = new List<Object>(__result);
        allWeapons.AddRange(KokiWeaponsPlugin.CustomWeapons);
        __result = allWeapons.ToArray();
    }
}

[HarmonyPatch(typeof(SpawnerManager), "PopulateAllWeapons")]
public static class PostWeaponRegistration
{
    internal static AssetBundle SharedAssets;

    public static void Postfix()
    {
        if (KokiWeaponsPlugin.RegisteredWeapons) return;
        KokiWeaponsPlugin.RegisteredWeapons = true;

        LoadDecals();
        RegisterFishnet.Postfix();
    }

    public static void LoadDecals()
    {
        var vanillaPhysGrenade = SpawnerManager.NameToWeaponDict["HandGrenade"].GetComponent<PhysicsGrenade>();
        foreach (var go in SharedAssets.LoadAllAssets<GameObject>())
        {
            if (go.name.EndsWith("Decal"))
            {
                if (go.name == "explosionDecal")
                {
                    Object.Instantiate(vanillaPhysGrenade.explosionDecal).transform.SetParent(go.transform);
                }
                else if (go.name == "bloodDecal")
                {
                    Object.Instantiate(vanillaPhysGrenade.bloodSplatter).transform.SetParent(go.transform);
                }
            }
        }
    }
}

// Runs each time the player quits to title screen
[HarmonyPatch(typeof(NetworkManager), "Start")]
public static class RegisterFishnet
{
    public static void Postfix()
    {
        if (!KokiWeaponsPlugin.RegisteredWeapons) return;
        var collection = InstanceFinder.NetworkManager.SpawnablePrefabs;

        foreach (var obj in KokiWeaponsPlugin.NetworkObjects)
        {
            if (!obj) continue;
            obj.TryGetComponent(out NetworkObject nob);
            collection.AddObject(nob);
        }
        collection.InitializePrefabRange(0);
    }

    public static void DeregisterFishnet()
    {
        var toRemove = new HashSet<NetworkObject>();

        foreach (var weapon in KokiWeaponsPlugin.NetworkObjects)
        {
            toRemove.Add(weapon.GetComponent<NetworkObject>());
        }
        // foreach (var nob in toRemove)
        // {
        //     nob.PrefabId = 0;
        //     nob.SpawnableCollectionId = 0;
        // }

        var nm = InstanceFinder.NetworkManager;
        var collection = nm.SpawnablePrefabs;
        var netobjTotal = collection.GetObjectCount();
        var baseGameObjs = new List<NetworkObject>();
        for (int i = 0; i < netobjTotal; i++)
        {
            NetworkObject current = collection.GetObject(true, i);

            if (current != null && !toRemove.Contains(current))
            {
                baseGameObjs.Add(current);
            }
        }
        collection.Clear();
        collection.AddObjects(baseGameObjs);
        collection.InitializePrefabRange(0);
    }
}