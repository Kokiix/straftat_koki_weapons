using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using ComputerysModdingUtilities;
using FishNet;
using HarmonyLib;
using UnityEngine;
using FishNet.Object;
using HeathenEngineering.PhysKit;
using System;
using FishNet.Managing;

[assembly: StraftatMod(isVanillaCompatible: false)]

[BepInDependency(MyceliumNetworking.MyPluginInfo.PLUGIN_GUID)]
[BepInPlugin("com.koki.weapons", "Koki Weapons", "1.1.0")]
public class KokiWeaponsPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static Harmony Harmony;
    internal static AssetBundle Bundle;

    internal static bool Debug = true;

    internal static List<GameObject> CustomWeapons = new List<GameObject>();
    internal static List<GameObject> NetworkObjects = new List<GameObject>();

    internal static ushort FishNetCollectionID = (ushort)("com.koki.weapons".GetHashCode() & 0xFFFF);
    internal static uint MyceliumID = 932828;

    internal static KokiWeaponsPlugin Instance;

    private void Awake()
    {
        Instance = this;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        Logger = base.Logger;
        Harmony = new Harmony("com.koki.weapons");
        Harmony.PatchAll();
        if (!Debug)
            Harmony.Unpatch(typeof(Settings).GetMethod(nameof(Settings.IncreaseTauntsAmount)), HarmonyPatchType.Prefix, "com.koki.weapons");

        // For hot reload
        foreach (var existingBundle in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (existingBundle.name == "kokiweaponsbundle")
                existingBundle.Unload(true);
        }
        string bundlePath = Debug ? Path.Combine(Paths.PluginPath, "KokiWeapons") : Path.GetDirectoryName(Info.Location);
        var mainBundle = AssetBundle.LoadFromFile(Path.Combine(bundlePath, "kokiWeaponsBundle"));
        if (!mainBundle)
        {
            Logger.LogError("Bundle for KokiWeapons not found! Plugin will not load.");
            return;
        }
        foreach (var go in mainBundle.LoadAllAssets<GameObject>())
        {
            if (go.GetComponent<ItemBehaviour>())
                CustomWeapons.Add(go);
            if (go.GetComponent<NetworkObject>())
                NetworkObjects.Add(go);
        }

        foreach (var material in mainBundle.LoadAllAssets<Material>())
            material.shader = Shader.Find(material.shader.name);
        Bundle = mainBundle;

        if (Debug)
        {
            if (InstanceFinder.NetworkManager)
                RegisterWeapons.Postfix();
        }

        TPTrapNetworking netw = this.gameObject.AddComponent<TPTrapNetworking>();
    }

    public void OnDestroy()
    {
        foreach (GameObject weapon in SpawnWeaponOnTaunt.weapons)
        {
            if (weapon)
                InstanceFinder.ServerManager.Despawn(weapon);
        }

        this.gameObject.GetComponent<TPTrapNetworking>().Deregister();
        Harmony.UnpatchSelf();
        if (!RegisteredWeapons) return;
        System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length - CustomWeapons.Count);
        DeregisterFishnet();
    }

    // Runs once at game start
    internal static bool RegisteredWeapons = false;
    [HarmonyPatch(typeof(SpawnerManager), "PopulateAllWeapons")]
    public static class RegisterWeapons
    {
        public static void Postfix()
        {
            if (SpawnerManager.AllWeapons == null) return;

            var weaponIdx = SpawnerManager.AllWeapons.Length;

            // For hot reload
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

            KBGrenade.Init.Run();
            TeleportTrap.Init.Run();
            RegisteredWeapons = true;
            RegisterFishnet.Postfix(); // NM starts before weapons do
        }
    }

    // Runs each time the player quits to title screen
    [HarmonyPatch(typeof(NetworkManager), "Start")]
    public static class RegisterFishnet
    {
        public static void Postfix()
        {
            if (!RegisteredWeapons) return;
            KDBG.Log("REGISTERED FISHNET STUFF");
            var nm = InstanceFinder.NetworkManager;
            var collection = nm.SpawnablePrefabs;

            foreach (var weapon in NetworkObjects)
            {
                if (!weapon) continue;
                weapon.TryGetComponent(out NetworkObject nob);
                collection.AddObject(nob);
            }
        }
    }

    public static void DeregisterFishnet()
    {

        var toRemove = new HashSet<NetworkObject>();

        foreach (var weapon in NetworkObjects)
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
            // GetObject(bool asServer, int id) 
            // In FishNet, 'id' corresponds to the index/PrefabId
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

    // [HarmonyPatch(typeof(PhysicsGrenade), "Update")]
    // public static class DisableExplosionForInspection
    // {
    //     public static bool Prefix()
    //     {
    //         return false;
    //     }
    // }
}