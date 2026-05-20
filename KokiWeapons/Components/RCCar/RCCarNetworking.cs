using FishNet;
using FishNet.Object;
using MyceliumNetworking;
using UnityEngine;
using Steamworks;

public class RCCarNetworking : MonoBehaviour
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
    public static void TargetedRPC(CSteamID steamID, string methodname, object[] parameters)
    {
        MyceliumNetwork.RPCTarget(
            ID,
            methodname,
            steamID,
            ReliableType.Reliable,
            parameters
        );
    }

    [CustomRPC]
    public void LinkCarToCarItem(int carItemNob, int carNob)
    {
        NetworkObject nob1, nob2 = null;
        if (!InstanceFinder.IsServer)
        {
            InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(carItemNob, out nob1);
            InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(carNob, out nob2);
        }
        else
        {
            InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(carItemNob, out nob1);
            InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(carNob, out nob2);
        }

        var carItem = nob1.gameObject;
        var car = nob2.gameObject.GetComponent<RCCar>();

        carItem.GetComponent<RCCarItem>().car = car;
        car.rcCarItem = carItem;
    }
}