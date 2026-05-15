using FishNet;
using FishNet.Object;
using MyceliumNetworking;
using UnityEngine;

namespace TeleportTrap;

public class Networking : MonoBehaviour
{
    void Awake()
    {
        MyceliumNetwork.RegisterNetworkObject(this, KokiWeaponsPlugin.MyceliumID);
    }

    public void Deregister()
    {
        MyceliumNetwork.DeregisterNetworkObject(this, KokiWeaponsPlugin.MyceliumID);
    }

    [CustomRPC]
    public void TeleportClient(Vector3 destination)
    {
        if (InstanceFinder.IsHost) return;
        FirstPersonController.instance.Teleport(destination, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
    }

    [CustomRPC]
    public void ToggleRadius(int nobID)
    {
        if (InstanceFinder.IsHost) return;
        InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(nobID, out NetworkObject nob);
        var radius = nob.transform.Find("radius").gameObject;
        radius.SetActive(!radius.activeSelf);
    }
}