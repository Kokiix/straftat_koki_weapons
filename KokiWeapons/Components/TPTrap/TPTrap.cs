using FishNet;
using FishNet.Object;
using UnityEngine;

public class TPTrap : MonoBehaviour
{
    private GameObject otherTrap;

    private void Awake()
    {
        PauseManager.OnBeforeSpawn += Despawn;

        // Play animation
        var anim = transform.Find("TeleTrapPhysMesh").Find("trap_010").GetComponent<Animation>();
        anim["tpmineSphere"].layer = 0;
        anim.Play("tpmineSphere");
        anim["tpmineTorus"].layer = 1;
        anim.Play("tpmineTorus");
    }

    private void OnTriggerStay(Collider col)
    {
        var fpc = FirstPersonController.instance;
        if (!otherTrap || col.gameObject != fpc.gameObject) return;
        fpc.Teleport(otherTrap.transform.position, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: false);

        var nob1 = this.gameObject.GetComponent<NetworkObject>().ObjectId;
        var nob2 = otherTrap.GetComponent<NetworkObject>().ObjectId;
        TPTrapNetworking.RPC("DestroyTrapPair", [nob1, nob2]);
    }

    public void Activate(GameObject other)
    {
        otherTrap = other;
        transform.Find("radius").gameObject.SetActive(true);
    }

    private void Despawn()
    {
        if (InstanceFinder.IsHost)
        {
            this.gameObject.GetComponent<NetworkObject>().Despawn();
        }
    }
}