using UnityEngine;


public static class TeleportGrenade
{
    public static GameObject GameObject;
    public static void InitGameObject(GameObject visuals)
    {
        GameObject = SpawnerManager.NameToWeaponDict["HandGrenade"];
        Transform handle = GameObject.transform.Find("SM_Grenadino_00_Low.001");
        KokiWeaponsPlugin.Logger.LogError(handle);
    }
}