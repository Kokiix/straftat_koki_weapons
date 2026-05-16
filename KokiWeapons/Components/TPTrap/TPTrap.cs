using System.Linq;
using FishNet;
using FishNet.Object;
using HarmonyLib;
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

    public void Prime(GameObject other)
    {
        otherTrap = other;
        transform.Find("radius").gameObject.SetActive(true);
    }

    private void OnTriggerStay(Collider col)
    {
        if (!otherTrap || detonated
        || !col.CompareTag("Player")
        || !InstanceFinder.IsServer) return;

        detonated = true;
        var playerIDs = GetPlayersToTeleport();
        otherTrap.TryGetComponent(out TPTrap otherTrapComponent);
        if (!otherTrapComponent.detonated)
        {
            otherTrapComponent.detonated = true;
            var otherPlayerIDs = otherTrapComponent.GetPlayersToTeleport();

            playerIDs.Do(ID => TPTrapNetworking.TargetedRPC(ID, "TPPlayer", [otherTrap.transform.position]));
            otherPlayerIDs.Do(ID => TPTrapNetworking.TargetedRPC(ID, "TPPlayer", [this.transform.position]));

            var nob1 = this.gameObject.GetComponent<NetworkObject>().ObjectId;
            var nob2 = otherTrap.GetComponent<NetworkObject>().ObjectId;
            TPTrapNetworking.RPC("DestroyTrapPair", [nob1, nob2]);
        }
        else
        {
            Debug.LogError("UHHHHHHHHHHHHHHHH OH");
        }
    }

    private ulong[] GetPlayersToTeleport()
    {
        var boxExtents = new Vector3(3.2f, 1.631936f, 3.2f);
        var layerMask = 1 << 11 | 1 << 16;
        return Physics.OverlapBox(this.transform.position, boxExtents, Quaternion.identity, layerMask)
            .Select(col => col.transform.root.gameObject)
            .Where(go => go.CompareTag("Player"))
            .Select(go => ulong.Parse(go.GetComponent<NetworkObject>().Owner.GetAddress()))
            .Distinct()
            .ToArray();
    }

    public GameObject explosionVfx;
    public AudioClip explosionAudio;
    public void Explode()
    {
        if (otherTrap)
            otherTrap.transform.Find("radius").gameObject.SetActive(false);
        Object.Destroy(this.gameObject);
        Object.Instantiate(explosionVfx, transform.position, Quaternion.identity);
        SoundManager.Instance.PlaySound(explosionAudio);
    }

    private void Despawn()
    {
        if (InstanceFinder.IsServer && this && gameObject)
        {
            InstanceFinder.ServerManager.Despawn(this.gameObject);
        }
    }
}