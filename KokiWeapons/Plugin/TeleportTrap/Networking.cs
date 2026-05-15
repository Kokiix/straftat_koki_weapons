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
        KDBG.Log("link!");
    }
}