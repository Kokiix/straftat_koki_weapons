using System.Linq;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class KBGrenade
{
    public static GameObject TemplateGameObject;
    public static GameObject Mesh;
    public static GameObject PhysMesh;

    public static void LoadBundleAssets(AssetBundle bundle)
    {
        Mesh = bundle.LoadAsset<GameObject>("RepulsorGrenade");
        PhysMesh = bundle.LoadAsset<GameObject>("PhysRepulsorGrenade");
    }

    [HarmonyPatch(typeof(SpawnerManager), "PopulateAllWeapons")]
    [HarmonyPostfix]
    public static void RegisterWeapon()
    {
        if (SpawnerManager.NameToWeaponDict.ContainsKey("Repulsion Grenade")) return;
        InitTemplate();
        GameObject KBGrenade = TemplateGameObject;
        System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length + 1);
        SpawnerManager.AllWeapons[^1] = KBGrenade;

        SpawnerManager.NameToWeaponDict.Add(KBGrenade.name, KBGrenade);
        SpawnerManager.NameToIndexDict.Add(KBGrenade.name, SpawnerManager.AllWeapons.Length - 1);
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

        GameObject physGrenade = go.GetComponent<DualLauncher>().trickShot.template.gameObject;
        if (!GetIsKBGrenade(physGrenade))
        {
            physGrenade.AddComponent<IsKBGrenade>();
            ToPhysKBGrenade(physGrenade, false);
        }

        Transform meshParent = go.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        meshParent.Find("SM_StunGrenade_00").gameObject.SetActive(false);
        Object.Instantiate(Mesh).transform.SetParent(meshParent);
        if (isClientVisual)
        {
            // mesh.transform.localPosition = new Vector3(0, -0.2f, 0);
            // mesh.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            // mesh.transform.localRotation = Quaternion.identity;
        }
    }

    public static void ToPhysKBGrenade(GameObject go, bool isClientVisual)
    {
        go.name = "Physics Repulsion Grenade";

        // Replace mesh
        var newMeshTransform = Object.Instantiate(PhysMesh).transform;
        newMeshTransform.SetParent(go.transform);
        Transform graph = go.transform.Find("Graph");
        graph.Find("Trail").SetParent(newMeshTransform);
        graph.Find("Sphere (1)").SetParent(newMeshTransform);
        graph.gameObject.SetActive(false);

        go.GetComponent<PhysicsGrenade>().graph = newMeshTransform;

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
    }

    public static bool GetIsKBGrenade(GameObject go)
    {
        if (!KokiWeaponsPlugin.Debug)
            return go.GetComponent<IsKBGrenade>();

        foreach (var c in go.GetComponents<Component>())
        {
            if (c.GetType().Name == "IsKBGrenade") return c;
        }
        return false;
    }
}

public class IsKBGrenade : MonoBehaviour
{

}