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
public class TPTrapNetworking : MonoBehaviour
{
    public const uint MyceliumID = 932828;
    public void Awake()
    {
        MyceliumNetwork.DeregisterNetworkObject(this.gameObject.GetComponent<TPTrapNetworking>(), TPTrapNetworking.MyceliumID);
        MyceliumNetwork.RegisterNetworkObject(this, MyceliumID);
    }

    [HarmonyPatch(typeof(ServerManager), "Spawn", new Type[] { typeof(NetworkObject), typeof(NetworkConnection) })]
    [HarmonyPostfix]
    public static void SendClientWeaponVisuals(NetworkObject nob)
    {
        GameObject go = nob.gameObject;
        if (TPTrap.GetTrapLink(go))
            MyceliumNetwork.RPC(MyceliumID, nameof(DisplayClientVisual), ReliableType.Reliable, nob.ObjectId, nameof(TPTrap.ConvertToTPTrap), true);
    }

    [CustomRPC]
    public void DisplayClientVisual(int nobID, string callbackName, bool waitForIbehavior)
    {
        if (InstanceFinder.IsServer) return;

        Dictionary<string, Action<GameObject, bool>> callbackNameToMethod = new() {
            { nameof(TPTrap.ConvertToTPTrap), TPTrap.ConvertToTPTrap },
            { nameof(TPTrap.ConvertToPhysTPTrap), TPTrap.ConvertToPhysTPTrap },
            { nameof(ToggleRadius), ToggleRadius }
        };

        StartCoroutine(CallOnObj(nobID, callbackNameToMethod[callbackName], waitForIbehavior));
    }

    private IEnumerator CallOnObj(int nobid, Action<GameObject, bool> callback, bool waitForIBehavior)
    {
        NetworkObject nob = null;
        do
        {
            yield return new WaitForSeconds(0.15f);
            if (waitForIBehavior && nob)
                waitForIBehavior = !nob.gameObject.GetComponent<ItemBehaviour>();
        } while (!InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(nobid, out nob) || waitForIBehavior);

        callback(nob.gameObject, true);
    }

    public static void ToggleRadius(GameObject go, bool _)
    {
        GameObject radius = go.transform.Find("radius(Clone)").gameObject;
        radius.transform.localPosition = Vector3.zero;
        radius.SetActive(!radius.activeSelf);
    }

    [CustomRPC]
    public void TeleportClient(Vector3 pos)
    {
        FirstPersonController.instance.Teleport(pos, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
    }

    [CustomRPC]
    public void ExplodeMineFromClient(int nobID)
    {
        if (!InstanceFinder.IsHost) return;
        InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(nobID, out NetworkObject nob);
        TPTrapMechanics.ExplodeTPTrapOnHit(null, nob.gameObject);
    }

}