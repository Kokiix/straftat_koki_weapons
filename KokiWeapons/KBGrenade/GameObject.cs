using System.Linq;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class KBGrenade
{
    public static GameObject TemplateGameObject;
    public static GameObject TemplatePhysGameObject;

    public static GameObject Mesh;
    public static GameObject PhysMesh;

    public static void LoadBundleAssets(AssetBundle bundle)
    {
        // Mesh = bundle.LoadAsset<GameObject>("TeleTrapMesh");
        // PhysMesh = bundle.LoadAsset<GameObject>("TeleTrapPhysMesh");
    }

    [HarmonyPatch(typeof(SpawnerManager), "PopulateAllWeapons")]
    [HarmonyPostfix]
    public static void RegisterWeapon()
    {
        if (SpawnerManager.NameToWeaponDict.ContainsKey("Repulsion Grenade")) return;
        InitTemplate();
        GameObject TPTrap = TemplateGameObject;
        System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length + 1);
        SpawnerManager.AllWeapons[^1] = TPTrap;

        SpawnerManager.NameToWeaponDict.Add(TPTrap.name, TPTrap);
        SpawnerManager.NameToIndexDict.Add(TPTrap.name, SpawnerManager.AllWeapons.Length - 1);
    }

    public static void InitTemplate()
    {
        GameObject template = UnityEngine.Object.Instantiate(SpawnerManager.NameToWeaponDict["StunGrenade"]);
        ToKBGrenade(template, false);
        TemplateGameObject = template;
        TemplateGameObject.SetActive(false);
        Object.DontDestroyOnLoad(TemplateGameObject);
    }

    public static void ToKBGrenade(GameObject go, bool isClientVisual)
    {
        go.name = "Repulsion Grenade";
        ItemBehaviour ib = go.GetComponent<ItemBehaviour>();
        ib.weaponName = "repulsion grenade";

        // go.AddComponent<IsKBGrenade>();
        go.GetComponent<DualLauncher>().trickShot.template.gameObject.AddComponent<IsKBGrenade>();

        // Transform meshParent = go.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        // meshParent.Find("PF_APMine_00").gameObject.SetActive(false);
        // GameObject mesh = Object.Instantiate(Mesh);
        // mesh.transform.SetParent(meshParent);
        if (isClientVisual)
        {
            // mesh.transform.localPosition = new Vector3(0, -0.2f, 0);
            // mesh.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            // mesh.transform.localRotation = Quaternion.identity;
        }
        // mesh.SetActive(true);

        // BoxCollider coll = go.GetComponent<BoxCollider>();
        // coll.center = new Vector3(0, 0.09f, 0f);
        // coll.size = new Vector3(0.21f, 0.5f, 0.16f);

        if (!TemplatePhysGameObject)
        {
            // GameObject physGO = Object.Instantiate(go.GetComponent<WeaponHandSpawner>().objToSpawn);
            // ConvertToPhysTPTrap(physGO, false);
            // TemplatePhysGameObject = physGO;
        }
    }

    public static void ConvertToPhysTPTrap(GameObject go, bool isClientVisual)
    {
        go.name = "Physics Repulsion Grenade";
        Object.DontDestroyOnLoad(go);

        // Replace mesh
        // Transform physGOTransform = go.transform;
        // physGOTransform.Find("PF_APMine_00").gameObject.SetActive(false);
        // GameObject mesh = Object.Instantiate(PhysMesh);
        // mesh.transform.SetParent(physGOTransform);

        if (isClientVisual)
        {
            // mesh.transform.localPosition = new Vector3(0f, 0f, 0f);
            // anim["sphere"].layer = 0;
            // anim.Play("sphere");
            // anim["torus"].layer = 1;
            // anim["torus"].weight = 1;
            // anim["torus"].enabled = true;
            // anim.Play("torus");
        }

        // Adjust radius for player detection
        // BoxCollider collider = physGOTransform.GetComponent<BoxCollider>();
        // collider.size = new Vector3(1.6f, 1.6f, 1.6f);

        // Remove bullet collider since comes with one
        // Object.Destroy(physGOTransform.transform.Find("Cube").gameObject);
    }

    public static bool GetIsKBGrenade(GameObject go)
    {
        if (!KokiWeaponsPlugin.Debug)
            return go.GetComponent<IsKBGrenade>();

        foreach (var c in go.GetComponents<Component>())
        {
            if (c.GetType().Name == "IsKBGrenade") return true;
        }
        return false;
    }
}

public class IsKBGrenade : MonoBehaviour
{

}