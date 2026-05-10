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

    public const uint MyceliumID = 932828;

    private void Awake()
    {
        Logger = base.Logger;
        Harmony = new Harmony("com.koki.weapons");
        Harmony.PatchAll();

        // Networking
        MyceliumNetwork.RegisterNetworkObject(this, MyceliumID);

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

        TeleportTrap.TemplateGameObject = null;
        TeleportTrap.TemplatePhysGameObject = null;
        TeleportTrap.MineMesh = null;
        TeleportTrap.PhysMineMesh = null;

        MyceliumNetwork.DeregisterNetworkObject(this, MyceliumID);
        Harmony.UnpatchSelf();
    }

    [CustomRPC]
    public void APMineToTPTrap(int nobID)
    {
        if (InstanceFinder.IsServer) return;

        KokiDebug.Log("starting delay thingy");
        StartCoroutine(DelayedConvertToTPMine(nobID));
    }

    private IEnumerator DelayedConvertToTPMine(int nobid)
    {
        NetworkObject nob;
        do
        {
            yield return new WaitForSeconds(0.5f);
        } while (!InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(nobid, out nob) || !nob.gameObject.GetComponent<ItemBehaviour>());

        TeleportTrap.ConvertAPToTPTrap(nob.gameObject);
    }
}