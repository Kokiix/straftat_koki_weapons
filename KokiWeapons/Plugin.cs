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

[assembly: StraftatMod(isVanillaCompatible: false)]

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
        Harmony.PatchAll();

        // For hot reload
        var loadedBundles = AssetBundle.GetAllLoadedAssetBundles();
        var existingBundle = loadedBundles.FirstOrDefault(b => b.name == "kokiweaponsbundle");
        if (existingBundle)
            existingBundle.Unload(true);

        string bundlePath = Path.Combine(Paths.PluginPath, "KokiWeapons/kokiWeaponsBundle");
        Bundle = AssetBundle.LoadFromFile(bundlePath);

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

        if (TeleportTrap.PhysMineMeshInstance)
            TeleportTrap.PhysMineMeshInstance.transform.SetParent(null);
        TeleportTrap.TemplateGameObject = null;

        Harmony.UnpatchSelf();
    }
}