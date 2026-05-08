using FishNet;
using FishNet.Object;
using HarmonyLib;
using UnityEngine;

[HarmonyPatch]
public static class TeleportTrap
{
    public static GameObject NonPhysGO;

    public static GameObject BaseMineMesh;
    public static GameObject PhysMineMesh;

    public static GameObject GetNonPhysGO()
    {
        if (NonPhysGO) return NonPhysGO;

        BaseMineMesh = Object.Instantiate(BaseMineMesh);
        PhysMineMesh = Object.Instantiate(PhysMineMesh);

        NonPhysGO = UnityEngine.Object.Instantiate(SpawnerManager.NameToWeaponDict["APMine"]);
        NonPhysGO.SetActive(false);
        NonPhysGO.AddComponent<TrapPair>();

        ItemBehaviour ib = NonPhysGO.GetComponent<ItemBehaviour>();
        ib.weaponName = "teleport trap";

        WeaponHandSpawner spawner = NonPhysGO.GetComponent<WeaponHandSpawner>();
        spawner.currentAmmo = 2;

        Transform baseVisualParent = NonPhysGO.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        baseVisualParent.Find("PF_APMine_00").gameObject.SetActive(false);
        BaseMineMesh.transform.SetParent(baseVisualParent);

        Transform physObjMine = spawner.objToSpawn.transform;
        physObjMine.Find("PF_APMine_00").gameObject.SetActive(false);
        PhysMineMesh.transform.SetParent(physObjMine);

        Object.Destroy(GetTrapLink(physObjMine.gameObject));
        physObjMine.gameObject.AddComponent<TrapLink>();

        return NonPhysGO;
    }

    [HarmonyPatch(typeof(ProximityMine), "HandleExplosion")]
    [HarmonyPrefix]
    static bool ExplodePrefix(ProximityMine __instance)
    {
        TrapLink trap_data = (TrapLink)GetTrapLink(__instance.gameObject);
        if (!trap_data.otherTrap)
            return false;

        Collider[] colliders = Physics.OverlapSphere(__instance.transform.position, __instance.explosionRadius, __instance.bodyLayer);
        if (colliders.Length != 0)
        {
            foreach (Collider c in colliders)
            {
                FirstPersonController fpc = c.GetComponent<FirstPersonController>();
                if (fpc)
                {
                    // KokiDebug.Log(trap_data.otherTrap.transform.position);
                    ProximityMine otherMine = trap_data.otherTrap.GetComponent<ProximityMine>();
                    if (!otherMine.sync___get_value_detonated())
                    {
                        otherMine.HandleExplosion();
                    }
                    fpc.Teleport(trap_data.otherTrap.transform.position, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
                    break;
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
            new_trap.otherTrap = connector.origTrap;
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

