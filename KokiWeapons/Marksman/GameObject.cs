using System.Linq;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class Marksman
{
    public static GameObject TemplateGameObject;
    public static GameObject Mesh;
    public static GameObject PhysMesh;
    public static string name = "Marksman";

    public static void LoadBundleAssets(AssetBundle bundle)
    {
        // Mesh = bundle.LoadAsset<GameObject>("RepulsorGrenade");
        // PhysMesh = bundle.LoadAsset<GameObject>("PhysRepulsorGrenade");
    }

    [HarmonyPatch(typeof(SpawnerManager), "PopulateAllWeapons")]
    [HarmonyPostfix]
    public static void RegisterWeapon()
    {
        if (SpawnerManager.NameToWeaponDict.ContainsKey(name)) return;
        InitTemplate();
        GameObject TPTrap = TemplateGameObject;
        System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length + 1);
        SpawnerManager.AllWeapons[^1] = TPTrap;

        SpawnerManager.NameToWeaponDict.Add(TPTrap.name, TPTrap);
        SpawnerManager.NameToIndexDict.Add(TPTrap.name, SpawnerManager.AllWeapons.Length - 1);
    }

    public static void InitTemplate()
    {
        GameObject template = UnityEngine.Object.Instantiate(SpawnerManager.NameToWeaponDict["Gun"]);
        GunToMarksman(template, false);
        TemplateGameObject = template;
        TemplateGameObject.SetActive(false);
        Object.DontDestroyOnLoad(TemplateGameObject);
    }

    public static void GunToMarksman(GameObject go, bool isClientVisual)
    {
        go.name = name;
        go.GetComponent<ItemBehaviour>().weaponName = name;

        // Transform meshParent = go.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        // meshParent.Find("SM_StunGrenade_00").gameObject.SetActive(false);
        // Object.Instantiate(Mesh).transform.SetParent(meshParent);
        if (isClientVisual)
        {
            // mesh.transform.localPosition = new Vector3(0, -0.2f, 0);
            // mesh.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            // mesh.transform.localRotation = Quaternion.identity;
        }
    }

    public static bool GetIsMarksman(GameObject go)
    {
        if (!KokiWeaponsPlugin.Debug)
            return go.GetComponent<IsMarksman>();

        foreach (var c in go.GetComponents<Component>())
        {
            if (c.GetType().Name == "IsMarksman") return c;
        }
        return false;
    }
}

public class IsMarksman : MonoBehaviour
{

}