using System.Linq;
using FishNet;
using FishNet.Object;
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

    public static GameObject CreateTemplateGameObject()
    {
        if (TemplateGameObject) return TemplateGameObject;

        MineMeshInstance = Object.Instantiate(MineMesh);
        PhysMineMeshInstance = Object.Instantiate(PhysMineMesh);

        TemplateGameObject = UnityEngine.Object.Instantiate(SpawnerManager.NameToWeaponDict["APMine"]);
        TemplateGameObject.SetActive(false);
        TemplateGameObject.AddComponent<TrapPair>();

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

        Object.Destroy(physMine.transform.Find("radius(Clone)").gameObject);
        GameObject proxMineRadius = Object.Instantiate(Resources.FindObjectsOfTypeAll<GameObject>().First(go => go.name == "ProximityMine" && go.transform.Find("radius")).transform.Find("radius").gameObject);
        proxMineRadius.transform.SetParent(physMine);
        proxMineRadius.transform.localScale = new Vector3(1.8584f, 1.8584f, 1.8584f);
        proxMineRadius.transform.position = new Vector3(-0.11f, 0, 0.175f); // pretty sure this is just because the model is off center
        proxMineRadius.SetActive(false);

        Object.Destroy(GetTrapLink(physMine.gameObject));
        physMine.gameObject.AddComponent<TrapLink>();

        BoxCollider collider = physMine.GetComponent<BoxCollider>();
        collider.size = new Vector3(1.5f, 1.5f, 1.5f);

        return TemplateGameObject;
    }

    [HarmonyPatch(typeof(ProximityMine), "HandleExplosion")]
    [HarmonyPrefix]
    static bool ExplodePrefix(ProximityMine __instance)
    {
        TrapLink trap_data = (TrapLink)GetTrapLink(__instance.gameObject);
        if (!trap_data) return true;
        if (!trap_data.otherTrap) return false;

        Collider[] colliders = Physics.OverlapSphere(__instance.transform.position, __instance.explosionRadius, __instance.bodyLayer);
        if (colliders.Length != 0)
        {
            foreach (Collider c in colliders)
            {
                FirstPersonController fpc = c.GetComponent<FirstPersonController>();
                if (fpc)
                {
                    ProximityMine otherMine = trap_data.otherTrap.GetComponent<ProximityMine>();
                    if (!otherMine.sync___get_value_detonated())
                    {
                        otherMine.HandleExplosion();
                    }
                    fpc.Teleport(trap_data.otherTrap.transform.position, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
                }
            }
        }
        __instance.ExplodeServer();
        return false;
    }

    [HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
    [HarmonyPrefix]
    static bool MinePlacementPre(WeaponHandSpawner __instance, GameObject obj, Vector3 position, Quaternion rotation)
    {
        TrapPair connector = __instance.gameObject.GetComponent<TrapPair>();
        if (PauseManager.BetweenRounds || !connector) return true;

        GameObject physMine = UnityEngine.Object.Instantiate(obj, position, rotation);
        InstanceFinder.ServerManager.Spawn(physMine);
        physMine.GetComponent<ProximityMine>().sync___set_value__rootObject(__instance.rootObject, true);
        physMine.GetComponent<ProximityMine>().sync___set_value_weapon((Weapon)__instance, true);

        TrapLink new_trap = (TrapLink)GetTrapLink(physMine);
        if (connector.origTrap)
        {
            connector.origTrap.GetComponent<TrapLink>().otherTrap = physMine;
            connector.origTrap.transform.Find("radius(Clone)").gameObject.SetActive(true);
            new_trap.otherTrap = connector.origTrap;
            physMine.transform.Find("radius(Clone)").gameObject.SetActive(true);
        }
        else
        {
            connector.origTrap = physMine;
        }
        return false;
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

public class TrapPair : MonoBehaviour
{
    public GameObject origTrap;
}

public class TrapLink : MonoBehaviour
{
    public GameObject otherTrap;
}

