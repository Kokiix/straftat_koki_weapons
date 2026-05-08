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

    public static GameObject MineMesh;
    public static GameObject PhysMineMesh;

    public static GameObject MineMeshInstance;
    public static GameObject PhysMineMeshInstance;

    public static GameObject GetTemplateGameObject()
    {
        if (TemplateGameObject) return TemplateGameObject;

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

        // Also create template for physics game obj which is stored in the hand spawner component
        Transform physMine = spawner.objToSpawn.transform;
        physMine.Find("PF_APMine_00").gameObject.SetActive(false);
        PhysMineMeshInstance.transform.SetParent(physMine);

        if (physMine.transform.Find("radius(Clone)"))
            Object.Destroy(physMine.transform.Find("radius(Clone)").gameObject);
        GameObject proxMineRadius = Object.Instantiate(Resources.FindObjectsOfTypeAll<GameObject>().First(go => go.name == "ProximityMine" && go.transform.Find("radius")).transform.Find("radius").gameObject);
        proxMineRadius.transform.SetParent(physMine);
        proxMineRadius.transform.localScale = new Vector3(1.8584f, 1.8584f, 1.8584f);
        // proxMineRadius.transform.position = new Vector3(-0.11f, 0, 0.175f); // pretty sure this is just because the model is off center
        proxMineRadius.SetActive(false);

        Object.Destroy(GetTrapLink(physMine.gameObject));
        physMine.gameObject.AddComponent<TrapLink>();

        BoxCollider collider = physMine.GetComponent<BoxCollider>();
        collider.size = new Vector3(1.6f, 0.81f, 1.6f);

        return TemplateGameObject;
    }

    [HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
    [HarmonyPrefix]
    static bool PlaceMinePrefix(WeaponHandSpawner __instance, GameObject obj, Vector3 position, Quaternion rotation)
    {
        TrapLink connector = __instance.gameObject.GetComponent<TrapLink>();
        if (PauseManager.BetweenRounds || !connector) return true;

        GameObject physMine = UnityEngine.Object.Instantiate(obj, position, rotation);
        InstanceFinder.ServerManager.Spawn(physMine);

        physMine.GetComponent<ProximityMine>().sync___set_value__rootObject(__instance.rootObject, true);

        if (connector.otherTrap)
        {
            GameObject otherTrap = connector.otherTrap.gameObject;

            NetworkObject otherNob = otherTrap.GetComponent<NetworkObject>();
            NetworkObject thisNob = physMine.GetComponent<NetworkObject>();
            __instance.damage = otherNob.ObjectId;

            Weapon otherTrapWeapon = otherTrap.GetComponent<ProximityMine>().sync___get_value_weapon();
            otherTrapWeapon.bulletsAmount = thisNob.ObjectId;
            otherTrap.GetComponent<ProximityMine>().sync___set_value_weapon(otherTrapWeapon, true);

            otherTrap.transform.Find("radius(Clone)").gameObject.SetActive(true);
            physMine.transform.Find("radius(Clone)").gameObject.SetActive(true);
        }
        else
        {
            connector.otherTrap = physMine;
        }
        physMine.GetComponent<ProximityMine>().sync___set_value_weapon(__instance, true);
        return false;
    }

    [HarmonyPatch(typeof(ProximityMine), "HandleExplosion")]
    [HarmonyPrefix]
    static bool ExplodePrefix(ProximityMine __instance)
    {
        if (!GetTrapLink(__instance.gameObject)) return true;
        TrapLink isTPTrap = (TrapLink)GetTrapLink(__instance.gameObject);
        if (!isTPTrap) return true;
        if (!InstanceFinder.IsServer) return false;

        Weapon sharedWeapon = __instance.sync___get_value_weapon();
        int otherTrapID;
        if (sharedWeapon.damage == __instance.gameObject.GetComponent<NetworkObject>().ObjectId)
            otherTrapID = sharedWeapon.bulletsAmount;
        else
            otherTrapID = (int)sharedWeapon.damage;

        if (!InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(otherTrapID, out NetworkObject otherNob))
            return false;
        ProximityMine otherTrap = otherNob.gameObject.GetComponent<ProximityMine>();

        Collider[] colliders = Physics.OverlapSphere(__instance.transform.position, __instance.explosionRadius, __instance.bodyLayer);
        if (colliders.Length != 0)
        {
            foreach (Collider c in colliders)
            {
                FirstPersonController fpc = c.GetComponent<FirstPersonController>();
                if (fpc)
                {
                    if (!otherTrap.sync___get_value_detonated())
                    {
                        otherTrap.HandleExplosion();
                    }
                    fpc.Teleport(otherTrap.transform.position, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
                }
            }
        }
        __instance.ExplodeServer();
        return false;
    }

    [HarmonyPatch]
    public class RegisterWeapon
    {
        [HarmonyPatch(typeof(SpawnerManager), "PopulateAllWeapons")]
        public static void Postfix()
        {
            if (SpawnerManager.NameToWeaponDict.ContainsKey("Teleport Trap")) return;
            GameObject TPTrap = TeleportTrap.GetTemplateGameObject();
            System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length + 1);
            SpawnerManager.AllWeapons[^1] = TPTrap;
            SpawnerManager.NameToWeaponDict[TPTrap.name] = TPTrap;
            SpawnerManager.NameToIndexDict[TPTrap.name] = SpawnerManager.AllWeapons.Length - 1;
        }
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