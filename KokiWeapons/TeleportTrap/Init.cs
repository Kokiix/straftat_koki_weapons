using System.Linq;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using HarmonyLib;
using MyceliumNetworking;
using UnityEngine;

[HarmonyPatch]
public static class TeleportTrap
{
    public static GameObject TemplateGameObject;
    public static GameObject TemplatePhysGameObject;

    public static GameObject MineMesh;
    public static GameObject PhysMineMesh;

    [HarmonyPatch(typeof(SpawnerManager), "PopulateAllWeapons")]
    [HarmonyPostfix]
    public static void RegisterWeapon()
    {
        if (SpawnerManager.NameToWeaponDict.ContainsKey("Teleport Mine")) return;
        Init();
        GameObject TPTrap = TemplateGameObject;
        System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length + 1);
        SpawnerManager.AllWeapons[^1] = TPTrap;

        SpawnerManager.NameToWeaponDict[TPTrap.name] = TPTrap;
        SpawnerManager.NameToIndexDict[TPTrap.name] = SpawnerManager.AllWeapons.Length - 1;
    }

    public static void Init()
    {
        GameObject templateAPMine = UnityEngine.Object.Instantiate(SpawnerManager.NameToWeaponDict["APMine"]);
        ConvertAPToTPTrap(templateAPMine);
        TemplateGameObject = templateAPMine;
        TemplateGameObject.SetActive(false);
        Object.DontDestroyOnLoad(TemplateGameObject);
    }

    public static GameObject ConvertAPToTPTrap(GameObject go)
    {
        go.AddComponent<TrapLink>();
        go.name = "Teleport Mine";

        // KokiDebug.PrintComponents(go);
        ItemBehaviour ib = go.GetComponent<ItemBehaviour>();
        ib.weaponName = "teleport mine";

        WeaponHandSpawner spawner = go.GetComponent<WeaponHandSpawner>();
        spawner.currentAmmo = 2;

        Transform meshParent = go.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        meshParent.Find("PF_APMine_00").gameObject.SetActive(false);
        GameObject mesh = Object.Instantiate(MineMesh);
        mesh.transform.SetParent(meshParent);
        mesh.transform.localPosition = new Vector3(0, -0.25f, 0);
        mesh.SetActive(true);

        if (!TemplatePhysGameObject)
            InitTemplatePhysGO(originalPhysMine: spawner.objToSpawn);

        spawner.objToSpawn = TemplatePhysGameObject;

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
        Object.Instantiate(PhysMineMesh).transform.SetParent(physGOTransform);

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