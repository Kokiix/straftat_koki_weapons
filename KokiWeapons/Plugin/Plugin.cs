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
[BepInPlugin("com.koki.weapons", "Koki Weapons", "2.0.1")]
public class KokiWeaponsPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static Harmony Harmony;
    internal static AssetBundle Bundle;

    internal static bool Debug = false;

    internal static List<GameObject> CustomWeapons = new List<GameObject>();
    internal static List<GameObject> NetworkObjects = new List<GameObject>();

    internal static uint MyceliumID = 932828;

    internal static KokiWeaponsPlugin Instance;

    internal static bool RegisteredWeapons = false;

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
            {
                CustomWeapons.Add(go);
                NetworkObjects.Add(go);
            }
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
        RegisterFishnet.DeregisterFishnet();
    }
}