using System.Linq;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class TeleportTrap
{
    public static GameObject TemplateGameObject;
    public static GameObject TemplatePhysGameObject;

    public static GameObject MineMesh;
    public static GameObject PhysMineMesh;

    public static GameObject MineMeshInstance;
    public static GameObject PhysMineMeshInstance;

    [HarmonyPatch(typeof(SpawnerManager), "PopulateAllWeapons")]
    [HarmonyPostfix]
    public static void RegisterWeapon()
    {
        if (SpawnerManager.NameToWeaponDict.ContainsKey("Teleport Trap")) return;
        CreateGOTemplates();
        GameObject TPTrap = TemplateGameObject;
        System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length + 1);
        SpawnerManager.AllWeapons[^1] = TPTrap;

        SpawnerManager.NameToWeaponDict[TPTrap.name] = TPTrap;
        SpawnerManager.NameToIndexDict[TPTrap.name] = SpawnerManager.AllWeapons.Length - 1;
    }

    public static void CreateGOTemplates()
    {
        // Non Physics GO (used only in registration)
        MineMeshInstance = Object.Instantiate(MineMesh);
        PhysMineMeshInstance = Object.Instantiate(PhysMineMesh);

        TemplateGameObject = UnityEngine.Object.Instantiate(SpawnerManager.NameToWeaponDict["APMine"]);
        TemplateGameObject.SetActive(false);
        TemplateGameObject.AddComponent<TrapLink>();
        TemplateGameObject.name = "Teleport Trap";
        Object.DontDestroyOnLoad(TemplateGameObject);

        ItemBehaviour ib = TemplateGameObject.GetComponent<ItemBehaviour>();
        ib.weaponName = "teleport trap";

        WeaponHandSpawner spawner = TemplateGameObject.GetComponent<WeaponHandSpawner>();
        spawner.currentAmmo = 2;

        Transform meshParent = TemplateGameObject.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        meshParent.Find("PF_APMine_00").gameObject.SetActive(false);
        MineMeshInstance.transform.SetParent(meshParent);

        // Physics GO (used as "objToSpawn" in hand spawner)
        TemplatePhysGameObject = Object.Instantiate(spawner.objToSpawn);
        TemplatePhysGameObject.SetActive(false);
        TemplatePhysGameObject.name = "Physics Teleport Trap";
        Object.DontDestroyOnLoad(TemplatePhysGameObject);

        ProximityMine mine = TemplatePhysGameObject.GetComponent<ProximityMine>();
        mine.instantExplode = false;

        // Replace mesh
        Transform physGOTransform = TemplatePhysGameObject.transform;
        physGOTransform.Find("PF_APMine_00").gameObject.SetActive(false);
        PhysMineMeshInstance.transform.SetParent(physGOTransform);

        // Insert radius GO from prox mine
        if (physGOTransform.Find("radius(Clone)"))
            Object.Destroy(physGOTransform.Find("radius(Clone)").gameObject);
        GameObject proxMineRadius = Object.Instantiate(Resources.FindObjectsOfTypeAll<GameObject>().First(go => go.name == "ProximityMine" && go.transform.Find("radius")).transform.Find("radius").gameObject);
        proxMineRadius.transform.SetParent(physGOTransform);
        proxMineRadius.transform.localScale = new Vector3(1.8584f, 1.8584f, 1.8584f);
        proxMineRadius.SetActive(false);

        // Add behavior to flag as TP trap (does nothing else)
        Object.Destroy(GetTrapLink(TemplatePhysGameObject));
        TemplatePhysGameObject.AddComponent<TrapLink>();

        BoxCollider collider = physGOTransform.GetComponent<BoxCollider>();
        collider.size = new Vector3(1.6f, 0.81f, 1.6f);

        spawner.objToSpawn = TemplatePhysGameObject;
    }

    [HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
    [HarmonyPrefix]
    static bool PlaceMinePrefix(WeaponHandSpawner __instance, GameObject obj, Vector3 position, Quaternion rotation)
    {
        TrapLink connector = __instance.gameObject.GetComponent<TrapLink>();
        if (PauseManager.BetweenRounds || !connector) return true;

        GameObject newTrap = UnityEngine.Object.Instantiate(TemplatePhysGameObject, position, rotation);
        newTrap.SetActive(true);

        ProximityMine mine = newTrap.GetComponent<ProximityMine>();
        mine.activated = false;

        InstanceFinder.ServerManager.Spawn(newTrap);

        newTrap.GetComponent<ProximityMine>().sync___set_value__rootObject(__instance.rootObject, true);

        if (connector.otherTrap)
        {
            GameObject otherTrap = connector.otherTrap.gameObject;

            NetworkObject otherNob = otherTrap.GetComponent<NetworkObject>();
            NetworkObject thisNob = newTrap.GetComponent<NetworkObject>();
            __instance.damage = otherNob.ObjectId;

            Weapon otherTrapWeapon = otherTrap.GetComponent<ProximityMine>().sync___get_value_weapon();
            otherTrapWeapon.bulletsAmount = thisNob.ObjectId;
            otherTrap.GetComponent<ProximityMine>().sync___set_value_weapon(otherTrapWeapon, true);

            otherTrap.transform.Find("radius(Clone)").gameObject.SetActive(true);
            newTrap.transform.Find("radius(Clone)").gameObject.SetActive(true);
        }
        else
        {
            __instance.damage = -1;
            __instance.bulletsAmount = -1;
            connector.otherTrap = newTrap;
        }

        newTrap.GetComponent<ProximityMine>().sync___set_value_weapon(__instance, true);
        return false;
    }

    [HarmonyPatch(typeof(ProximityMine), "HandleExplosion")]
    [HarmonyPrefix]
    static bool ExplodePrefix(ProximityMine __instance)
    {

        if (!GetTrapLink(__instance.gameObject)) return true;

        Weapon sharedWeapon = __instance.sync___get_value_weapon();
        int otherTrapID;
        if (sharedWeapon.damage == __instance.gameObject.GetComponent<NetworkObject>().ObjectId)
            otherTrapID = sharedWeapon.bulletsAmount;
        else
            otherTrapID = (int)sharedWeapon.damage;

        InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(otherTrapID, out NetworkObject otherNob);
        ProximityMine otherTrap = otherNob.gameObject.GetComponent<ProximityMine>();
        __instance.sync___set_value_detonated(true, false);
        Collider[] colliders = Physics.OverlapSphere(__instance.transform.position, __instance.explosionRadius, __instance.bodyLayer);

        if (!otherTrap.sync___get_value_detonated())
        {
            otherTrap.HandleExplosion();
        }

        if (colliders.Length != 0)
        {
            foreach (Collider c in colliders)
            {
                FirstPersonController fpc = c.GetComponent<FirstPersonController>();
                if (fpc)
                {
                    fpc.Teleport(otherTrap.transform.position, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
                }
            }
        }
        __instance.ExplodeServer();
        return false;
    }


    [HarmonyPatch(typeof(ProximityMine), "OnTriggerStay")]
    [HarmonyPrefix]
    static bool DetectExplosion(ProximityMine __instance)
    {
        if (!GetTrapLink(__instance.gameObject)) return true;
        if (!InstanceFinder.IsServer) return false;

        Weapon sharedWeapon = __instance.sync___get_value_weapon();
        int otherTrapID;
        if (sharedWeapon.bulletsAmount == -1 || sharedWeapon.damage == -1f) return false;
        if (sharedWeapon.damage == __instance.gameObject.GetComponent<NetworkObject>().ObjectId)
            otherTrapID = sharedWeapon.bulletsAmount;
        else
            otherTrapID = (int)sharedWeapon.damage;

        if (InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(otherTrapID, out NetworkObject otherNob))
        {
            __instance.ChangeState();
            __instance.HandleExplosion();
        }
        return false;
    }


    [HarmonyPatch(typeof(ItemSpawner), "StartNewRound")]
    [HarmonyPrefix]
    static bool Test(ItemSpawner __instance)
    {
        KokiDebug.Log(__instance.itemToSpawn.name);

        return true;
    }

    // Required because of hot reload BS
    public static Component GetTrapLink(GameObject go)
    {
        foreach (var c in go.GetComponents<Component>())
        {
            if (c.GetType().Name == "TrapLink") return c;
        }
        return null;
    }
}

public class TrapLink : MonoBehaviour
{
    public GameObject otherTrap;
}