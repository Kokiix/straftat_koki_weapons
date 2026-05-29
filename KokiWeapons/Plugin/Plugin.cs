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
using System.Linq;
using HarmonyLib.Tools;
using KokiWeapons;

[assembly: StraftatMod(isVanillaCompatible: false)]

[BepInDependency(MyceliumNetworking.MyPluginInfo.PLUGIN_GUID)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class KokiWeaponsPlugin : BaseUnityPlugin
{
    internal static Harmony Harmony;

    internal static List<GameObject> CustomWeapons = new List<GameObject>();
    internal static List<GameObject> NetworkObjects = new List<GameObject>();

    internal static readonly uint MyceliumID = 932828;

    internal static KokiWeaponsPlugin Instance;

    internal static bool RegisteredWeapons = false;

    internal static readonly string[] BundleNames = ["shared", "tptrap", "repulsiongrenade", "rccar"];

    internal static AssetBundle SharedBundle;

    private void Awake()
    {
        Instance = this;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        Harmony.PatchAll();

        // Remove loaded bundle if hot reloading
        foreach (var existingBundle in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (BundleNames.Contains(existingBundle.name))
                existingBundle.Unload(true);
        }
        LoadBundles();
        this.gameObject.AddComponent<TPTrapNetworking>();
        this.gameObject.AddComponent<RCCarNetworking>();

        // Also for hot reload
        if (InstanceFinder.NetworkManager)
            SpawnerManager.PopulateAllWeapons();

        UpdateTrapLinkOnPlace.Init();
        // RCCarLink.Init();
    }

    private void OnDestroy()
    {
        foreach (GameObject weapon in SpawnWeaponOnTaunt.weapons)
        {
            if (weapon)
                InstanceFinder.ServerManager.Despawn(weapon);
        }

        this.gameObject.GetComponent<TPTrapNetworking>().Deregister();
        this.gameObject.AddComponent<RCCarNetworking>().Deregister();
        Harmony.UnpatchSelf();

        if (!RegisteredWeapons) return;
        System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length - CustomWeapons.Count);
        RegisterFishnet.DeregisterFishnet();
    }

    private void LoadBundles()
    {
        string bundlePath = Path.GetDirectoryName(Info.Location);
        if (bundlePath.IsNullOrWhiteSpace())
        {
            bundlePath = Path.Combine(Paths.PluginPath, "DEVELOPMENT-BUILD-Koki Weapons");
            Harmony.Unpatch(typeof(Settings).GetMethod(nameof(Settings.IncreaseTauntsAmount)), HarmonyPatchType.Prefix, MyPluginInfo.PLUGIN_GUID);
        }

        var sharedAssets = AssetBundle.LoadFromFile(Path.Combine(bundlePath, "shared"));
        foreach (var material in sharedAssets.LoadAllAssets<Material>())
            material.shader = Shader.Find(material.shader.name);
        SharedBundle = sharedAssets;
        foreach (var filePath in Directory.GetFiles(bundlePath))
        {
            var fileName = Path.GetFileName(filePath);
            if (!BundleNames.Contains(fileName) || fileName == "shared") continue;

            var bundle = AssetBundle.LoadFromFile(Path.Combine(bundlePath, fileName));
            foreach (var go in bundle.LoadAllAssets<GameObject>())
            {
                if (go.GetComponent<ItemBehaviour>())
                    CustomWeapons.Add(go);
                if (go.GetComponent<NetworkObject>())
                    NetworkObjects.Add(go);
            }
        }
    }
}