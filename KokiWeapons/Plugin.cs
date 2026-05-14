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
            if (existingBundle.name == "kokiweaponsbundle" || existingBundle.name == "weaponmaterials")
                existingBundle.Unload(true);
        }

        string bundlePath = Debug ? Path.Combine(Paths.PluginPath, "KokiWeapons") : Path.GetDirectoryName(Info.Location);
        var mainBundle = AssetBundle.LoadFromFile(Path.Combine(bundlePath, "kokiWeaponsBundle"));
        var weaponMaterials = AssetBundle.LoadFromFile(Path.Combine(bundlePath, "weaponmaterials"));
        if (!mainBundle)
        {
            Logger.LogError("Bundle for KokiWeapons not found! Plugin will not load.");
            return;
        }

        // Old system
        // TPTrap.LoadBundleAssets(bundle);
        // this.gameObject.AddComponent<TPTrapNetworking>();

        // New system
        CustomWeapons = mainBundle.LoadAllAssets<GameObject>();

        if (Debug)
            RegisterWeapons.Postfix();

        LoadShaders(mainBundle, weaponMaterials);
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

            SpawnerManager.NameToWeaponDict["Repulsion Grenade"]
            .GetComponent<TrickShot>().template.gameObject
            .GetComponent<PhysicsGrenade>().explosionDecal =
            SpawnerManager.NameToWeaponDict["StunGrenade"]
            .GetComponent<TrickShot>().template.gameObject
            .GetComponent<PhysicsGrenade>().explosionDecal;
            RegisteredWeapons = true;
        }
    }

    public static void LoadShaders(AssetBundle main, AssetBundle weaponMaterials)
    {
        weaponMaterials.LoadAllAssets<Material>().Do(m => m.shader = Shader.Find("S_WeaponOutline_00"));

        main.LoadAsset<Material>("M_StunGrenade_Radius_00 1").shader = Shader.Find("S_HandGrenadeRadius_00");
        main.LoadAsset<Material>("M_Taser_Sphere_00").shader = Shader.Find("S_DoubleSided_Emissive_00");
        main.LoadAsset<Material>("WFX_M_SmokeScroll SoftMult").shader = Shader.Find("WFX/Scroll/Multiply Soft Tint");
        main.LoadAsset<Material>("WFX_M_SmallDots Add").shader = Shader.Find("WFX/Additive Alpha8");
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