using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Serializing;
using HarmonyLib;
using MyceliumNetworking;
using UnityEngine;

[HarmonyPatch]
public class CustomWeaponNetworkManager : MonoBehaviour
{
    public const uint MyceliumID = 932828;
    public void Awake()
    {
        MyceliumNetwork.DeregisterNetworkObject(this.gameObject.GetComponent<CustomWeaponNetworkManager>(), CustomWeaponNetworkManager.MyceliumID);
        MyceliumNetwork.RegisterNetworkObject(this, MyceliumID);
    }

    [HarmonyPatch(typeof(ServerManager), "Spawn", new Type[] { typeof(NetworkObject), typeof(NetworkConnection) })]
    [HarmonyPostfix]
    public static void SendClientWeaponVisuals(NetworkObject nob)
    {
        GameObject go = nob.gameObject;
        if (TeleportTrap.GetTrapLink(go))
            MyceliumNetwork.RPC(MyceliumID, nameof(DisplayClientVisual), ReliableType.Reliable, nob.ObjectId, nameof(TeleportTrap.ConvertToTPTrap), true);
    }

    [CustomRPC]
    public void ChangeRadiusVisibility(GameObject trap, bool state)
    {
        trap.transform.Find("radius(Clone)").gameObject.SetActive(state);
    }

    [CustomRPC]
    public void TeleportClient(Vector3 pos)
    {
        FirstPersonController.instance.Teleport(pos, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
    }

    [CustomRPC]
    public void DisplayClientVisual(int nobID, string callbackName, bool waitForIBheavior)
    {
        if (InstanceFinder.IsServer) return;

        Dictionary<string, Action<GameObject, bool>> callbackNameToMethod = new() {
            { nameof(TeleportTrap.ConvertToTPTrap), TeleportTrap.ConvertToTPTrap },
            { nameof(TeleportTrap.ConvertToPhysTPTrap), TeleportTrap.ConvertToPhysTPTrap }
        };

        StartCoroutine(DelayedApplyVisuals(nobID, callbackNameToMethod[callbackName], waitForIBheavior));
    }

    private IEnumerator DelayedApplyVisuals(int nobid, Action<GameObject, bool> callback, bool waitForIBheavior)
    {
        NetworkObject nob = null;
        do
        {
            yield return new WaitForSeconds(0.25f);
            if (waitForIBheavior && nob)
                waitForIBheavior = !nob.gameObject.GetComponent<ItemBehaviour>();
        } while (!InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(nobid, out nob) || waitForIBheavior);

        callback(nob.gameObject, true);
    }
}