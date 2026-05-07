using System;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class TeleportTrap
{
    private static GameObject _gameObject;
    public static GameObject BaseMineMesh;
    public static GameObject PhysGrenadeMesh;
    public static GameObject GameObject()
    {
        if (_gameObject) return _gameObject;

        _gameObject = UnityEngine.Object.Instantiate(SpawnerManager.NameToWeaponDict["APMine"]);

        ItemBehaviour ib = _gameObject.GetComponent<ItemBehaviour>();
        ib.name = "teleport trap";

        WeaponHandSpawner spawner = _gameObject.GetComponent<WeaponHandSpawner>();
        spawner.currentAmmo = 2;
        // Communicate to patch by flagging both at once
        spawner.proximityMine = true;
        spawner.apmine = true;

        // Swap visuals
        Transform baseVisualParent = _gameObject.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        baseVisualParent.Find("PF_APMine_00").gameObject.SetActive(false);
        BaseMineMesh.transform.SetParent(baseVisualParent);

        return _gameObject;
    }

    [HarmonyPatch(typeof(ProximityMine), "HandleExplosion")]
    [HarmonyPrefix]
    static void ExplodePrefix()
    {
        KokiWeaponsPlugin.Logger.LogError("sldfkj");
    }

    [HarmonyPatch(typeof(WeaponHandSpawner), "RpcLogic___SpawnObject_2587446063")]
    [HarmonyPrefix]
    static void PlaceObjectPrefix(WeaponHandSpawner __instance, GameObject obj)
    {
        if (__instance.proximityMine && __instance.apmine)
        {
            KokiDebug.Log("teleport trap!");
        }
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(Component), "transform", MethodType.Getter)]
    public static Transform ItemTransform() =>
        // its a stub so it has no initial content
        throw new NotImplementedException("It's a stub");

    [HarmonyPatch(typeof(ItemBehaviour), "Start")]
    [HarmonyPrefix]
    static bool ItemBehavStart(ItemBehaviour __instance)
    {
        __instance.maxPivot = 0f - __instance.maxPivot;
        __instance.weaponScript = __instance.GetComponent<Weapon>();
        ItemTransform().localScale = new Vector3(2f, 2f, 2f);
        __instance.audio = __instance.GetComponent<AudioSource>();
        // __instance.initialLocalPosition = base.transform.localPosition;
        __instance.hoveredObjectRenderer = __instance.GetComponentsInChildren<MeshRenderer>();
        __instance.hoveredObjectMat.Clear();
        for (int i = 0; i < __instance.hoveredObjectRenderer.Length; i++)
        {
            Material[] materials = __instance.hoveredObjectRenderer[i].materials;
            foreach (Material item in materials)
            {
                __instance.hoveredObjectMat.Add(item);
            }
        }
        __instance.col = __instance.GetComponent<Collider>();
        __instance.gripRight = __instance.GetComponentsInChildren<Grip>()[0].transform;
        __instance.gripLeft = __instance.GetComponentsInChildren<Grip>()[1].transform;
        // if (!dispenserStart && base.gameObject.name != "Pig Held Item")
        // {
        //     groundMov = base.transform.DOLocalMove(base.transform.localPosition + base.transform.parent.up / 2f, 1.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        // }
        if (__instance.GetComponentInChildren<AimStrafePivot>() != null)
        {
            __instance.aimStrafePivot = __instance.GetComponentInChildren<AimStrafePivot>().transform;
        }
        return false;
    }
}