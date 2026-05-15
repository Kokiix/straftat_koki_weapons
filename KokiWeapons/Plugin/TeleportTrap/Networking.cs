using FishNet;
using FishNet.Object;
using MyceliumNetworking;
using UnityEngine;

namespace TeleportTrap;

public class Networking : MonoBehaviour
{
    public void Awake()
    {
        MyceliumNetwork.RegisterNetworkObject(this, KokiWeaponsPlugin.MyceliumID);
    }

    public void Deregister()
    {
        MyceliumNetwork.DeregisterNetworkObject(this, KokiWeaponsPlugin.MyceliumID);
    }

    public static void RPC(string methodname, object[] parameters)
    {
        MyceliumNetwork.RPC(
            KokiWeaponsPlugin.MyceliumID,
            methodname,
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
            go1.GetComponent<TPTrap>().Activate(go2);
            go2.GetComponent<TPTrap>().Activate(go1);
        }
        else if (InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(nobID1, out NetworkObject nob3)
            && InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(nobID2, out NetworkObject nob4))
        {
            var go1 = nob3.gameObject;
            var go2 = nob4.gameObject;
            go1.GetComponent<TPTrap>().Activate(go2);
            go2.GetComponent<TPTrap>().Activate(go1);
        }

    }
}