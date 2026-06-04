using System.Linq;
using UnityEngine;

static class GameObjectExtension
{
    public static T GetOrReload<T>(this GameObject go) where T : Component
    {
        if (go.TryGetComponent(out T newComponent)) return newComponent;
        else if (KokiWeaponsPlugin.KWDebug)
        {
            // I have no idea why but the regular GetComponent(string typename) doesn't work :(
            Component oldComponent = go.GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == typeof(T).Name);
            if (oldComponent)
            {
                newComponent = go.AddComponent<T>();
                CopyComponentData(oldComponent, newComponent);
                Object.Destroy(oldComponent);
                return newComponent;
            }
        }
        return null;
    }

    public static bool TryGetOrReload<T>(this GameObject go, out T component) where T : Component
    {
        if (go.TryGetComponent(out component))
            return true;
        else if (KokiWeaponsPlugin.KWDebug)
        {
            Component oldComponent = go.GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == typeof(T).Name);
            if (oldComponent)
            {
                component = go.AddComponent<T>();
                CopyComponentData(oldComponent, component);
                Object.Destroy(oldComponent);
                return true;
            }
        }

        component = default(T);
        return false;
    }

    static void CopyComponentData(Component source, Component destination)
    {
        try
        {
            string json = JsonUtility.ToJson(source);
            JsonUtility.FromJsonOverwrite(json, destination);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[HotReload] Failed to copy data via JsonUtility: {ex.Message}");
        }
    }
}