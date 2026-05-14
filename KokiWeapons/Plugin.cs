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

[assembly: StraftatMod(isVanillaCompatible: false)]

[BepInDependency(MyceliumNetworking.MyPluginInfo.PLUGIN_GUID)]
[BepInPlugin("com.koki.weapons", "Koki Weapons", "1.1.0")]
public class KokiWeaponsPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static Harmony Harmony;
    internal static bool Debug = true;
    internal static GameObject[] CustomWeapons;

    private void Awake()
    {
        Logger = base.Logger;
        Harmony = new Harmony("com.koki.weapons");
        Harmony.PatchAll();
        if (!Debug)
            Harmony.Unpatch(typeof(Settings).GetMethod(nameof(Settings.IncreaseTauntsAmount)), HarmonyPatchType.Prefix, "com.koki.weapons");

        // For hot reload
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        var loadedBundles = AssetBundle.GetAllLoadedAssetBundles();
        var existingBundle = loadedBundles.FirstOrDefault(b => b.name == "kokiweaponsbundle");
        if (existingBundle)
            existingBundle.Unload(true);

        string bundlePath = Debug ? Path.Combine(Paths.PluginPath, "KokiWeapons/kokiWeaponsBundle") : Path.Combine(Path.GetDirectoryName(Info.Location), "kokiWeaponsBundle");
        var bundle = AssetBundle.LoadFromFile(bundlePath);
        if (!bundle)
        {
            Logger.LogError("Bundle for KokiWeapons not found! Plugin will not load.");
            return;
        }

        // Old system
        // TPTrap.LoadBundleAssets(bundle);
        // this.gameObject.AddComponent<TPTrapNetworking>();

        // New system
        CustomWeapons = bundle.LoadAllAssets<GameObject>();
    }

    public void OnDestroy()
    {
        if (Debug)
            foreach (GameObject weapon in SpawnWeaponOnTaunt.weapons)
            {
                if (weapon)
                    InstanceFinder.ServerManager.Despawn(weapon);
            }
        Harmony.UnpatchSelf();
    }

    [HarmonyPatch(typeof(SpawnerManager), "PopulateAllWeapons")]
    public static class RegisterWeapons
    {
        public static void Postfix()
        {
            foreach (var weapon in CustomWeapons)
            {
                if (SpawnerManager.NameToWeaponDict.ContainsKey(weapon.name)) return;

                System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length + 1);
                SpawnerManager.AllWeapons[^1] = weapon;

                SpawnerManager.NameToWeaponDict.Add(weapon.name, weapon);
                SpawnerManager.NameToIndexDict.Add(weapon.name, SpawnerManager.AllWeapons.Length - 1);

                var nm = InstanceFinder.NetworkManager;
                var collectionID = (ushort)("com.koki.weapons".GetHashCode() & 0xFFFF);
                var collection = (SinglePrefabObjects)nm.GetPrefabObjects<SinglePrefabObjects>(collectionID, createIfMissing: true);
                collection.AddObject(weapon.GetComponent<NetworkObject>());

                var weaponIdx = collection.GetObjectCount() - 1;
                ManagedObjects.InitializePrefab(weapon.GetComponent<NetworkObject>(), weaponIdx, collectionID);
            }
        }
    }
}