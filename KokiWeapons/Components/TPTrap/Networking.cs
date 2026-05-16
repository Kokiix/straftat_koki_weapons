using FishNet;
using FishNet.Object;
using MyceliumNetworking;
using UnityEngine;
using Steamworks;

public class TPTrapNetworking : MonoBehaviour
{
    private static readonly uint ID = 932828;
    public void Awake()
    {
        MyceliumNetwork.RegisterNetworkObject(this, ID);
    }

    public void Deregister()
    {
        MyceliumNetwork.DeregisterNetworkObject(this, ID);
    }

    public static void RPC(string methodname, object[] parameters)
    {
        MyceliumNetwork.RPC(
            ID,
            methodname,
            ReliableType.Reliable,
            parameters
        );
    }
    public static void TargetedRPC(ulong steamID, string methodname, object[] parameters)
    {
        Debug.Log(steamID);
        MyceliumNetwork.RPCTarget(
            ID,
            methodname,
            (CSteamID)steamID,
            ReliableType.Reliable,
            parameters
        );
    }

    [CustomRPC]
    public void LinkMines(int nobID1, int nobID2)
    {
        if (!InstanceFinder.IsServer
        && InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(nobID1, out NetworkObject nob1)
        && InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(nobID2, out NetworkObject nob2))
        {
            var go1 = nob1.gameObject;
            var go2 = nob2.gameObject;
            go1.GetComponent<TPTrap>().Prime(go2);
            go2.GetComponent<TPTrap>().Prime(go1);
        }
        else if (InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(nobID1, out NetworkObject nob3)
            && InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(nobID2, out NetworkObject nob4))
        {
            var go1 = nob3.gameObject;
            var go2 = nob4.gameObject;
            go1.GetComponent<TPTrap>().Prime(go2);
            go2.GetComponent<TPTrap>().Prime(go1);
        }
    }

    // Ripped straight from ProximityMine.ExplodeObservers lol
    [CustomRPC]
    public void DestroyTrapPair(int nobID1, int nobID2)
    {
        foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (go != null)
            {
                float distanceToExplo = Vector3.Distance(base.transform.position, go.transform.position);
                float maxFXDistance = 40;
                // TODO: store these hard coded values somewhere else
                go.GetComponent<PlayerHealth>().LocalScreenshake(
                    duration: 0.3f,
                    strength: Mathf.Lerp(0.5f, 13, Mathf.Clamp(distanceToExplo / maxFXDistance, 0f, 1f)),
                    vibrato: 20,
                    randomness: 20,
                    shakeEase: DG.Tweening.Ease.Linear);
            }
        }

        NetworkObject nob1, nob2;
        if (!InstanceFinder.IsServer)
        {
            InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(nobID1, out nob1);
            InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(nobID2, out nob2);
        }
        else
        {
            InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(nobID1, out nob1);
            InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(nobID2, out nob2);
        }

        if (nob1)
            nob1.gameObject.GetComponent<TPTrap>().Explode();
        if (nob2)
            nob2.gameObject.GetComponent<TPTrap>().Explode();
    }

    [CustomRPC]
    public void TPPlayer(Vector3 position)
    {
        FirstPersonController.instance.Teleport(position, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
    }
}