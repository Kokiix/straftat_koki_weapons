using System.Linq;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class Marksman
{
    public static GameObject TemplateGameObject;
    public static GameObject TemplateCoin;
    public static GameObject Mesh;
    public static GameObject PhysMesh;
    public static string name = "Marksman";

    public static float CoinDamageBoost = 1.2f;

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
        InitTemplates();
        GameObject TPTrap = TemplateGameObject;
        System.Array.Resize(ref SpawnerManager.AllWeapons, SpawnerManager.AllWeapons.Length + 1);
        SpawnerManager.AllWeapons[^1] = TPTrap;

        SpawnerManager.NameToWeaponDict.Add(TPTrap.name, TPTrap);
        SpawnerManager.NameToIndexDict.Add(TPTrap.name, SpawnerManager.AllWeapons.Length - 1);
    }

    public static void InitTemplates()
    {
        GameObject template = UnityEngine.Object.Instantiate(SpawnerManager.NameToWeaponDict["Gun"]);
        GunToMarksman(template, false);
        TemplateGameObject = template;
        TemplateGameObject.SetActive(false);
        Object.DontDestroyOnLoad(TemplateGameObject);

        TemplateCoin = InitCoin();
        TemplateCoin.SetActive(false);
        Object.DontDestroyOnLoad(TemplateCoin);
    }

    public static void GunToMarksman(GameObject go, bool isClientVisual)
    {
        go.name = name;
        var ib = go.GetComponent<ItemBehaviour>();
        ib.weaponName = name;
        ib.aimWeapon = false;
        ib.aimCrosshair = null;

        var gun = go.GetComponent<Gun>();
        gun.requireBothHands = true;

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

    public static GameObject InitCoin()
    {
        var coin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        coin.AddComponent<Rigidbody>();
        coin.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        return coin;
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