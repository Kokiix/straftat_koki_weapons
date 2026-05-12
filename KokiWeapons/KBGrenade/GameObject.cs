using System.Linq;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using HarmonyLib;
using MyceliumNetworking;
using UnityEngine;

[HarmonyPatch]
public static class KBGrenade
{
    public static GameObject TemplateGameObject;
    public static GameObject TemplatePhysGameObject;

    public static GameObject Mesh;
    public static GameObject PhysMesh;

    public static void Init()
    {
        GameObject template = UnityEngine.Object.Instantiate(SpawnerManager.NameToWeaponDict["APMine"]);
        ConvertToTPTrap(template, false);
        TemplateGameObject = template;
        TemplateGameObject.SetActive(false);
        Object.DontDestroyOnLoad(TemplateGameObject);
    }

    [HarmonyPatch(typeof(SpawnerManager), "PopulateAllWeapons")]
    [HarmonyPostfix]
    public static void RegisterWeapon()
    {
        if (SpawnerManager.NameToWeaponDict.ContainsKey("Teleport Mine")) return;
        Init();
        GameObject TPTrap = TemplateGameObject;
        System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length + 1);
        SpawnerManager.AllWeapons[^1] = TPTrap;

        SpawnerManager.NameToWeaponDict.Add(TPTrap.name, TPTrap);
        SpawnerManager.NameToIndexDict.Add(TPTrap.name, SpawnerManager.AllWeapons.Length - 1);
    }

    public static void ConvertToTPTrap(GameObject go, bool isClientVisual)
    {
        go.AddComponent<TrapLink>();
        go.name = "Teleport Mine";

        ItemBehaviour ib = go.GetComponent<ItemBehaviour>();
        ib.weaponName = "teleport mine";

        WeaponHandSpawner spawner = go.GetComponent<WeaponHandSpawner>();
        spawner.currentAmmo = 2;

        Transform meshParent = go.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        meshParent.Find("PF_APMine_00").gameObject.SetActive(false);
        GameObject mesh = Object.Instantiate(Mesh);
        mesh.transform.SetParent(meshParent);
        if (isClientVisual)
        {
            mesh.transform.localPosition = new Vector3(0, -0.2f, 0);
            mesh.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            mesh.transform.localRotation = Quaternion.identity;
        }
        mesh.SetActive(true);

        BoxCollider coll = go.GetComponent<BoxCollider>();
        coll.center = new Vector3(0, 0.09f, 0f);
        coll.size = new Vector3(0.21f, 0.5f, 0.16f);

        if (!TemplatePhysGameObject)
        {
            GameObject physGO = Object.Instantiate(spawner.objToSpawn);
            ConvertToPhysTPTrap(physGO, false);
            TemplatePhysGameObject = physGO;
        }

        // ..does this even do anything if I'm patching the spawn method anyways?
        spawner.objToSpawn = TemplatePhysGameObject;
    }

    public static void ConvertToPhysTPTrap(GameObject go, bool isClientVisual)
    {
        go.name = "Physics Teleport Mine";
        Object.DontDestroyOnLoad(go);

        ProximityMine mine = go.GetComponent<ProximityMine>();
        mine.canActivate = false;

        // Replace mesh
        Transform physGOTransform = go.transform;
        physGOTransform.Find("PF_APMine_00").gameObject.SetActive(false);
        GameObject mesh = Object.Instantiate(PhysMesh);
        mesh.transform.SetParent(physGOTransform);

        var anim = go.transform.Find("TeleTrapPhysMesh(Clone)").Find("trap_010").gameObject.AddComponent<Animation>();
        anim.AddClip(SphereAnim, "sphere");
        anim.AddClip(TorusAnim, "torus");

        if (isClientVisual)
        {
            mesh.transform.localPosition = new Vector3(0f, 0f, 0f);
            anim["sphere"].layer = 0;
            anim.Play("sphere");
            anim["torus"].layer = 1;
            anim["torus"].weight = 1;
            anim["torus"].enabled = true;
            anim.Play("torus");
        }

        // Insert radius GO from prox mine
        if (physGOTransform.Find("radius(Clone)"))
            Object.Destroy(physGOTransform.Find("radius(Clone)").gameObject);
        GameObject proxMineRadius = Object.Instantiate(Resources.FindObjectsOfTypeAll<GameObject>().First(go => go.name == "ProximityMine" && go.transform.Find("radius")).transform.Find("radius").gameObject);
        proxMineRadius.transform.SetParent(physGOTransform);
        proxMineRadius.transform.localScale = new Vector3(1.858382f, 1.858382f, 1.858382f);
        proxMineRadius.SetActive(false);

        // Add behavior to flag as TP trap (does nothing else)
        Object.Destroy(GetTrapLink(go));
        go.AddComponent<TrapLink>();

        // Adjust radius for player detection
        BoxCollider collider = physGOTransform.GetComponent<BoxCollider>();
        collider.size = new Vector3(1.6f, 1.6f, 1.6f);

        // Remove bullet collider since comes with one
        Object.Destroy(physGOTransform.transform.Find("Cube").gameObject);
    }

    // Required because of hot reload BS
    public static TrapLink GetTrapLink(GameObject go)
    {
        // return go.GetComponent<TrapLink>();

        // Debug
        foreach (var c in go.GetComponents<Component>())
        {
            if (c.GetType().Name == "TrapLink") return (TrapLink)c;
        }
        return null;
    }
}