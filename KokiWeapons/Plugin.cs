using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using ComputerysModdingUtilities;
using FishNet;
using HarmonyLib;
using UnityEngine;
using MyceliumNetworking;
using FishNet.Object;
using HarmonyLib.Tools;
using FishNet.Managing.Object;
using HeathenEngineering.PhysKit;

[assembly: StraftatMod(isVanillaCompatible: false)]

[BepInDependency(MyceliumNetworking.MyPluginInfo.PLUGIN_GUID)]
[BepInPlugin("com.koki.weapons", "Koki Weapons", "1.1.0")]
public class KokiWeaponsPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static Harmony Harmony;
    internal static bool Debug = true;
    internal static GameObject[] CustomWeapons;
    internal static ushort FishNetCollectionID = (ushort)("com.koki.weapons".GetHashCode() & 0xFFFF);

    private void Awake()
    {
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
        foreach (var material in mainBundle.LoadAllAssets<Material>())
            material.shader = Shader.Find(material.shader.name);

        // Old system
        // TPTrap.LoadBundleAssets(bundle);
        // this.gameObject.AddComponent<TPTrapNetworking>();

        // New system
        CustomWeapons = mainBundle.LoadAllAssets<GameObject>();

        if (Debug)
            RegisterWeapons.Postfix();

    }

    public void OnDestroy()
    {
        foreach (GameObject weapon in SpawnWeaponOnTaunt.weapons)
        {
            if (weapon)
                InstanceFinder.ServerManager.Despawn(weapon);
        }

        Harmony.UnpatchSelf();
        if (!RegisteredWeapons) return;
        System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length - CustomWeapons.Length);
        InstanceFinder.NetworkManager._runtimeSpawnablePrefabs.Remove(FishNetCollectionID);
        foreach (var weapon in CustomWeapons)
        {
            SpawnerManager.NameToWeaponDict.Remove(weapon.name);
            SpawnerManager.NameToIndexDict.Remove(weapon.name);
        }
    }

    internal static bool RegisteredWeapons = false;
    [HarmonyPatch(typeof(SpawnerManager), "PopulateAllWeapons")]
    public static class RegisterWeapons
    {
        public static void Postfix()
        {
            if (SpawnerManager.AllWeapons == null) return;
            var allWeaponIdx = SpawnerManager.AllWeapons.Length;
            System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length + CustomWeapons.Length);
            var nm = InstanceFinder.NetworkManager;
            var collection = (SinglePrefabObjects)nm.GetPrefabObjects<SinglePrefabObjects>(FishNetCollectionID, createIfMissing: true);
            var weaponIdx = collection.GetObjectCount() - 1;
            foreach (var weapon in CustomWeapons)
            {
                SpawnerManager.AllWeapons[allWeaponIdx++] = weapon;
                SpawnerManager.NameToWeaponDict.Add(weapon.name, weapon);
                SpawnerManager.NameToIndexDict.Add(weapon.name, SpawnerManager.AllWeapons.Length - 1);

                collection.AddObject(weapon.GetComponent<NetworkObject>());
                ManagedObjects.InitializePrefab(weapon.GetComponent<NetworkObject>(), weaponIdx++, FishNetCollectionID);
            }

            var physGrenade = SpawnerManager.NameToWeaponDict["Repulsion Grenade"]
            .GetComponent<TrickShot>().template.gameObject;
            physGrenade.GetComponent<PhysicsGrenade>().explosionDecal =
            SpawnerManager.NameToWeaponDict["StunGrenade"]
            .GetComponent<TrickShot>().template.gameObject
            .GetComponent<PhysicsGrenade>().explosionDecal;
            RegisteredWeapons = true;
            collection.AddObject(physGrenade.GetComponent<NetworkObject>());
            ManagedObjects.InitializePrefab(physGrenade.GetComponent<NetworkObject>(), weaponIdx++, FishNetCollectionID);
        }
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