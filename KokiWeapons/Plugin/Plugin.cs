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

[assembly: StraftatMod(isVanillaCompatible: false)]

[BepInDependency(MyceliumNetworking.MyPluginInfo.PLUGIN_GUID)]
[BepInPlugin("com.koki.weapons", "Koki Weapons", "2.0.1")]
public class KokiWeaponsPlugin : BaseUnityPlugin
{
    internal static Harmony Harmony;

    internal static bool Debug = true;

    internal static List<GameObject> CustomWeapons = new List<GameObject>();
    internal static List<GameObject> NetworkObjects = new List<GameObject>();

    internal static uint MyceliumID = 932828;

    internal static KokiWeaponsPlugin Instance;

    internal static bool RegisteredWeapons = false;

    internal static readonly string[] BundleNames = ["shared", "tptrap", "repulsiongrenade", "rccar"];

    private void Awake()
    {
        Instance = this;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        // Remove loaded bundle if hot reloading
        foreach (var existingBundle in AssetBundle.GetAllLoadedAssetBundles())
        {
            if (BundleNames.Contains(existingBundle.name))
                existingBundle.Unload(true);
        }
        LoadBundle();
        this.gameObject.AddComponent<TPTrapNetworking>();

        // Also for hot reload
        if (InstanceFinder.NetworkManager)
            RegisterWeapons.Postfix();

        Harmony = new Harmony("com.koki.weapons");
        Harmony.PatchAll();
        if (!Debug)
        {
            Harmony.Unpatch(typeof(Settings).GetMethod(nameof(Settings.IncreaseTauntsAmount)), HarmonyPatchType.Prefix, "com.koki.weapons");
        }
    }

    private void OnDestroy()
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
        RegisterFishnet.DeregisterFishnet();
    }

    private void LoadBundle()
    {
        string bundlePath = Debug ? Path.Combine(Paths.PluginPath, "KokiWeapons") : Path.GetDirectoryName(Info.Location);

        foreach (var filePath in Directory.GetFiles(bundlePath))
        {
            var fileName = Path.GetFileName(filePath);
            if (fileName.EndsWith(".dll")) continue;

            var bundle = AssetBundle.LoadFromFile(Path.Combine(bundlePath, fileName));
            if (fileName == "shared")
            {
                foreach (var material in bundle.LoadAllAssets<Material>())
                    material.shader = Shader.Find(material.shader.name);
            }
            else
            {
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
}