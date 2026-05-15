using FishNet;
using FishNet.Object;
using UnityEngine;

public class TPTrap : MonoBehaviour
{
    private GameObject otherTrap;
    private bool detonated = false;

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
        if (!otherTrap || col.gameObject != fpc.gameObject || detonated) return;
        Debug.Log("boom");
        detonated = true;
        if (!otherTrap.GetComponent<TPTrap>().detonated)
        {
            var nob1 = this.gameObject.GetComponent<NetworkObject>().ObjectId;
            var nob2 = otherTrap.GetComponent<NetworkObject>().ObjectId;
            InstanceFinder.ServerManager.Despawn(this.gameObject);
            InstanceFinder.ServerManager.Despawn(otherTrap);
            TPTrapNetworking.RPC("DestroyTrapPair", [nob1, nob2]);
        }
        fpc.Teleport(otherTrap.transform.position, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
    }

    public void Activate(GameObject other)
    {
        otherTrap = other;
        transform.Find("radius").gameObject.SetActive(true);
    }

    public GameObject explosionVfx;
    public AudioClip explosionAudio;
    public void Explode()
    {
        if (otherTrap)
            otherTrap.transform.Find("radius").gameObject.SetActive(false);
        InstanceFinder.ServerManager.Despawn(this.gameObject);
        Object.Instantiate(explosionVfx, transform.position, Quaternion.identity);
        SoundManager.Instance.PlaySound(explosionAudio);
    }

    private void Despawn()
    {
        if (InstanceFinder.IsServer)
        {
            InstanceFinder.ServerManager.Despawn(this.gameObject);
        }
    }
}