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
        Init();
        GameObject TPTrap = TemplateGameObject;
        System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length + 1);
        SpawnerManager.AllWeapons[^1] = TPTrap;

        SpawnerManager.NameToWeaponDict[TPTrap.name] = TPTrap;
        SpawnerManager.NameToIndexDict[TPTrap.name] = SpawnerManager.AllWeapons.Length - 1;
    }

    public static void Init()
    {
        MineMeshInstance = Object.Instantiate(MineMesh);
        PhysMineMeshInstance = Object.Instantiate(PhysMineMesh);
        GameObject templateAPMine = UnityEngine.Object.Instantiate(SpawnerManager.NameToWeaponDict["APMine"]);
        ConvertAPToTPTrap(templateAPMine);
        TemplateGameObject = templateAPMine;
        TemplateGameObject.SetActive(false);
        Object.DontDestroyOnLoad(TemplateGameObject);
    }

    public static GameObject ConvertAPToTPTrap(GameObject go)
    {
        KokiDebug.Log("start conversion");
        go.AddComponent<TrapLink>();
        go.name = "Teleport Mine";
        KokiDebug.Log("step 1");

        ItemBehaviour ib = go.GetComponent<ItemBehaviour>();
        ib.weaponName = "teleport mine";
        KokiDebug.Log("step 2");

        WeaponHandSpawner spawner = go.GetComponent<WeaponHandSpawner>();
        spawner.currentAmmo = 2;
        KokiDebug.Log("step 3");

        Transform meshParent = go.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        meshParent.Find("PF_APMine_00").gameObject.SetActive(false);
        MineMeshInstance.transform.SetParent(meshParent);
        KokiDebug.Log("step 4");

        if (!TemplatePhysGameObject)
            InitTemplatePhysGO(originalPhysMine: spawner.objToSpawn);
        KokiDebug.Log("step 5");

        spawner.objToSpawn = TemplatePhysGameObject;
        KokiDebug.Log("step 6");

        return go;
    }

    public static void InitTemplatePhysGO(GameObject originalPhysMine)
    {
        TemplatePhysGameObject = Object.Instantiate(originalPhysMine);
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