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

[assembly: StraftatMod(isVanillaCompatible: false)]

[BepInDependency(MyceliumNetworking.MyPluginInfo.PLUGIN_GUID)]
[BepInPlugin("com.koki.weapons", "Koki Weapons", "1.0.0")]
public class KokiWeaponsPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static Harmony Harmony;
    internal static AssetBundle Bundle;

    private void Awake()
    {
        Logger = base.Logger;
        Harmony = new Harmony("com.koki.weapons");

        // For hot reload
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        var loadedBundles = AssetBundle.GetAllLoadedAssetBundles();
        var existingBundle = loadedBundles.FirstOrDefault(b => b.name == "kokiweaponsbundle");
        if (existingBundle)
            existingBundle.Unload(true);

        string bundlePath = Path.Combine(Paths.PluginPath, "KokiWeapons/kokiWeaponsBundle");
        string altBundlePath = Path.Combine(Paths.PluginPath, "kokiWeaponsBundle");
        Bundle = AssetBundle.LoadFromFile(bundlePath);
        if (!Bundle)
            Bundle = AssetBundle.LoadFromFile(altBundlePath);
        if (!Bundle)
        {
            Logger.LogError("Bundle for KokiWeapons not found! Plugin will not load.");
            return;
        }

        // Networking
        this.gameObject.AddComponent<CustomWeaponNetworkManager>();

        Harmony.PatchAll();
        TeleportTrap.MineMesh = Bundle.LoadAsset<GameObject>("TeleTrapMesh");
        TeleportTrap.PhysMineMesh = Bundle.LoadAsset<GameObject>("TeleTrapPhysMesh");
    }

    public void OnDestroy()
    {
        // Cleanup stuff spawned by Debug
        foreach (GameObject weapon in SpawnWeaponOnTaunt.weapons)
        {
            if (weapon)
            {
                InstanceFinder.ServerManager.Despawn(weapon);
            }
        }

        TeleportTrap.TemplateGameObject = null;
        TeleportTrap.TemplatePhysGameObject = null;
        TeleportTrap.MineMesh = null;
        TeleportTrap.PhysMineMesh = null;

        Harmony.UnpatchSelf();
    }
}