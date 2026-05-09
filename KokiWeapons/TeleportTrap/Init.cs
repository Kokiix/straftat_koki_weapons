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
        if (SpawnerManager.NameToWeaponDict.ContainsKey("Teleport Mine")) return;
        TPTrapNetworking.Init();
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
        TemplateGameObject.name = "Teleport Mine";
        Object.DontDestroyOnLoad(TemplateGameObject);

        ItemBehaviour ib = TemplateGameObject.GetComponent<ItemBehaviour>();
        ib.weaponName = "teleport mine";

        WeaponHandSpawner spawner = TemplateGameObject.GetComponent<WeaponHandSpawner>();
        spawner.currentAmmo = 2;

        Transform meshParent = TemplateGameObject.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        meshParent.Find("PF_APMine_00").gameObject.SetActive(false);
        MineMeshInstance.transform.SetParent(meshParent);

        // Physics GO (used as "objToSpawn" in hand spawner)
        TemplatePhysGameObject = Object.Instantiate(spawner.objToSpawn);
        TemplatePhysGameObject.SetActive(false);
        TemplatePhysGameObject.name = "Physics Teleport Mine";
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