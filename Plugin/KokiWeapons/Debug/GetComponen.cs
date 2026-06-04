using System.Linq;
using HarmonyLib;
using UnityEngine;

static class GameObjectExtension
{
    public static T GetComponen<T>(this GameObject go) where T : Component
    {
        if (go.TryGetComponent(out T newComponent)) return newComponent;
        else if (KokiWeaponsPlugin.KWDebug)
        {
            // I have no idea why but the regular GetComponent(string typename) doesn't work :(
            Component oldComponent = go.GetComponents<Component>().FirstOrDefault(c => c.GetType().Name == typeof(T).Name);
            if (oldComponent)
            {
                Object.Destroy(oldComponent);
                return go.AddComponent<T>();
            }
        }
        return null;
    }

    public static bool TryGetComponen<T>(this GameObject go, out T component) where T : Component
    {
        if (go.TryGetComponent(out T newComponent))
        {
            component = newComponent;
            return true;
        }
        else if (KokiWeaponsPlugin.KWDebug)
        {
            Component oldComponent = go.GetComponent(typeof(T).Name);
            if (oldComponent)
            {
                Object.Destroy(oldComponent);
                component = go.AddComponent<T>();
                return true;
            }
        }

        component = default(T);
        return false;
    }
}