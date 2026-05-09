using UnityEngine;

public static class KokiDebug
{
    public static void Log(object s)
    {
        KokiWeaponsPlugin.Logger.LogError(s);
    }

    public static void PrintComponents(GameObject obj)
    {
        foreach (var x in obj.GetComponents<Component>())
        {
            KokiWeaponsPlugin.Logger.LogError(x.GetType().Name);
        }
    }

}