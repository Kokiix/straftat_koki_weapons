using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using ComputerysModdingUtilities;
using FishNet;
using HarmonyLib;
using UnityEngine;
using FishNet.Object;
using System;
using FishNet.Managing;
using System.Linq;
using HarmonyLib.Tools;
using KokiWeapons;

[assembly: StraftatMod(isVanillaCompatible: false)]

[BepInDependency(MyceliumNetworking.MyPluginInfo.PLUGIN_GUID)]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
class KokiWeaponsPlugin : BaseUnityPlugin
{
    internal static KokiWeaponsPlugin Instance;
    static Harmony Harmony;

    static readonly string[] BundleNames = ["kokiweapons_shared", "tptrap", "repulsiongrenade", "rccar"];
    internal static List<GameObject> CustomWeapons = new List<GameObject>();
    internal static List<GameObject> NetworkObjects = new List<GameObject>();

    internal static bool RegisteredWeapons = false;

    void Awake()
    {
        Instance = this;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        Harmony.PatchAll();

        // Remove loaded bundle if hot reloading
        foreach (var existingBundle in AssetBundle.GetAllLoadedAssetBundles())
            if (BundleNames.Contains(existingBundle.name))
                existingBundle.Unload(true);
        LoadBundles();

        // Networking components
        this.gameObject.AddComponent<TPTrapNetworking>();
        // this.gameObject.AddComponent<RCCarNetworking>();

        // Event subscriptions
        UpdateTrapLinkOnPlace.Init();
        // RCCarLink.Init();
    }

    void OnDestroy()
    {
        Harmony.UnpatchSelf();

        foreach (GameObject weapon in SpawnWeaponOnTaunt.weapons)
            if (weapon)
                InstanceFinder.ServerManager.Despawn(weapon);

        this.gameObject.GetComponent<TPTrapNetworking>().Deregister();
        // this.gameObject.AddComponent<RCCarNetworking>().Deregister();

        if (!RegisteredWeapons) return;
        System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length - CustomWeapons.Count);
        RegisterFishnet.DeregisterFishnet();
    }

    internal static bool KWDebug = false;
    void LoadBundles()
    {
        string bundlePath = Path.GetDirectoryName(Info.Location);
        if (bundlePath.IsNullOrWhiteSpace())
        {
            KWDebug = true;
            Debug.LogError("DEBUG MODE ACTIVE");
            bundlePath = Path.Combine(Paths.PluginPath, "DEVELOPMENT-BUILD-Koki Weapons");
        }
        else
            Harmony.Unpatch(typeof(Settings).GetMethod(nameof(Settings.IncreaseTauntsAmount)), HarmonyPatchType.Prefix, MyPluginInfo.PLUGIN_GUID);

        // Shader swap
        var sharedAssets = AssetBundle.LoadFromFile(Path.Combine(bundlePath, "kokiweapons_shared"));
        foreach (var material in sharedAssets.LoadAllAssets<Material>())
            material.shader = Shader.Find(material.shader.name);
        PostWeaponRegistration.SharedAssets = sharedAssets;

        // Load weapons
        foreach (var filePath in Directory.GetFiles(bundlePath))
        {
            var fileName = Path.GetFileName(filePath);
            if (!BundleNames.Contains(fileName) || fileName == "kokiweapons_shared") continue;

            var bundle = AssetBundle.LoadFromFile(Path.Combine(bundlePath, fileName));
            foreach (var go in bundle.LoadAllAssets<GameObject>())
            {
                if (go.GetComponent<ItemBehaviour>())
                    CustomWeapons.Add(go);
                if (go.GetComponent<NetworkObject>())
                    NetworkObjects.Add(go);
            }
        }

        // Manually register loaded weapons if hot reload
        if (KWDebug && InstanceFinder.NetworkManager)
        {
            SpawnerManager.AllWeapons = null;
            SpawnerManager.PopulateAllWeapons();

            Type[] componentTypes = [];
            Dictionary<string, Type> componNameToType = componentTypes
            .Zip(componentTypes.Select(type => type.Name), (k, v) => new { k, v })
            .ToDictionary(x => x.v, x => x.k);

            foreach (var obj in NetworkObjects)
            {
                foreach (var oldComponent in obj.GetComponents<Component>())
                {
                    if (componNameToType.ContainsKey(oldComponent.GetType().Name))
                    {
                        var newComponent = obj.AddComponent(componNameToType[oldComponent.GetType().Name]);
                        CopyComponentData(oldComponent, newComponent);
                        UnityEngine.Object.Destroy(oldComponent);
                    }
                }
            }
        }
    }

    static void CopyComponentData(Component source, Component destination)
    {
        try
        {
            string json = JsonUtility.ToJson(source);
            JsonUtility.FromJsonOverwrite(json, destination);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[HotReload] Failed to copy data via JsonUtility: {ex.Message}");
        }
    }
}