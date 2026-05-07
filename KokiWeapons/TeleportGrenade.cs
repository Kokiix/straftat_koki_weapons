using UnityEngine;


public static class TeleportGrenade
{
    private static GameObject _gameObject;
    public static GameObject VisualsGameObject;
    public static GameObject GameObject()
    {
        if (_gameObject) return _gameObject;

        _gameObject = SpawnerManager.NameToWeaponDict["HandGrenade"];
        Transform parent = _gameObject.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        parent.Find("SM_Grenadino_01").gameObject.SetActive(false);
        VisualsGameObject.transform.SetParent(parent);
        return _gameObject;
    }
}