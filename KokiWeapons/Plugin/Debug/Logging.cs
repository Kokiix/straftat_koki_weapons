using UnityEngine;

public static class KDBG
{
    public static void PrintComponents(GameObject obj)
    {
        foreach (var x in obj.GetComponents<Component>())
        {
            Debug.LogError(x.GetType().Name);
        }
    }

}