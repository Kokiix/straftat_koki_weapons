using UnityEngine;


public static class TeleportTrap
{
    private static GameObject _gameObject;
    public static GameObject BaseGrenadeMesh;
    public static GameObject PhysGrenadeMesh;
    public static GameObject GameObject()
    {
        if (_gameObject) return _gameObject;

        _gameObject = SpawnerManager.NameToWeaponDict["APMine"];

        // Swap visuals
        // Transform baseVisualParent = _gameObject.transform.Find("ElbowPivotPoint").Find("AimStrafePivot");
        // baseVisualParent.Find("SM_Grenadino_01").gameObject.SetActive(false);
        // BaseGrenadeMesh.transform.SetParent(baseVisualParent);

        // Transform physicsObj = _gameObject.GetComponent<DualLauncher>().trickShot.template.gameObject.transform;
        // physicsObj.Find("Graph").gameObject.SetActive(false);
        // PhysGrenadeMesh.name = "Graph";
        // PhysGrenadeMesh.transform.SetParent(physicsObj);
        // foreach (var x in _gameObject.GetComponents<Component>())
        // {
        //     KokiWeaponsPlugin.Logger.LogError(x.GetType().Name);
        // }

        return _gameObject;
    }
}