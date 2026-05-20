// Runs once at game start
using System.Collections.Generic;
using FishNet;
using FishNet.Managing;
using FishNet.Object;
using HarmonyLib;
using HeathenEngineering.PhysKit;
using UnityEngine;

[HarmonyPatch(typeof(SpawnerManager), "PopulateAllWeapons")]
public static class RegisterWeapons
{
    public static void Postfix()
    {
        if (SpawnerManager.AllWeapons == null) return;

        var CustomWeapons = KokiWeaponsPlugin.CustomWeapons;
        var weaponIdx = SpawnerManager.AllWeapons.Length;

        // Clear existing entries if hot reloaded
        foreach (var weapon in CustomWeapons)
        {
            SpawnerManager.NameToWeaponDict.Remove(weapon.name);
            SpawnerManager.NameToIndexDict.Remove(weapon.name);
        }

        System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length + CustomWeapons.Count);

        foreach (var weapon in CustomWeapons)
        {
            SpawnerManager.AllWeapons[weaponIdx++] = weapon;
            SpawnerManager.NameToWeaponDict.Add(weapon.name, weapon);
            SpawnerManager.NameToIndexDict.Add(weapon.name, SpawnerManager.AllWeapons.Length - 1);
        }

        // NetworkObjects.AddRange([
        //     SpawnerManager.NameToWeaponDict["Repulsion Grenade"]
        //         .GetComponent<TrickShot>().template.gameObject,
        //     SpawnerManager.NameToWeaponDict["Teleport Mine"]
        //     .GetComponent<WeaponHandSpawner>().objToSpawn
        // ]);

        KBGrenade.Init.Run();
        DecalSwap.SwapDecals();
        KokiWeaponsPlugin.RegisteredWeapons = true;
        RegisterFishnet.Postfix(); // NM starts before weapons do
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
        foreach (var nob in toRemove)
        {
            nob.PrefabId = 0;
            nob.SpawnableCollectionId = 0;
        }

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

public static class DecalSwap
{
    public static void SwapDecals()
    {
        var vanillaPhysGrenade = SpawnerManager.NameToWeaponDict["HandGrenade"].GetComponent<PhysicsGrenade>();
        foreach (var go in KokiWeaponsPlugin.SharedBundle.LoadAllAssets<GameObject>())
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