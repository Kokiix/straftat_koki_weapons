using System;
using System.Linq;
using FishNet;
using FishNet.Object;
using HarmonyLib;
using Steamworks;
using UnityEngine;

public class TPTrap : MonoBehaviour
{
    [NonSerialized] public Transform owner;
    GameObject _otherTrap;
    bool _detonated = false;

    void Awake()
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
        _otherTrap = other;
        transform.Find("radius").gameObject.SetActive(true);
    }

    void OnTriggerStay(Collider col)
    {
        if (!_otherTrap || _detonated
        || !col.CompareTag("Player")
        || !InstanceFinder.IsServer) return;

        _detonated = true;
        var players = GetPlayers();
        _otherTrap.TryGetComponent(out TPTrap otherTrapComponent);
        if (!otherTrapComponent._detonated)
        {
            otherTrapComponent._detonated = true;
            var otherPlayers = otherTrapComponent.GetPlayers();

            // this is SO poorly written
            var playerIDs = players.Where(player => TeleportIfHost(player, _otherTrap.transform.position))
            .Select(go => GOToCSteamID(go));
            var otherPlayerIDs = otherPlayers.Where(player => TeleportIfHost(player, this.transform.position))
            .Select(go => GOToCSteamID(go));

            playerIDs.Do(ID => TPTrapNetworking.TargetedRPC(ID, "TPPlayer", [_otherTrap.transform.position, this]));
            otherPlayerIDs.Do(ID => TPTrapNetworking.TargetedRPC(ID, "TPPlayer", [this.transform.position, this]));

            var nob1 = this.gameObject.GetComponent<NetworkObject>().ObjectId;
            var nob2 = _otherTrap.GetComponent<NetworkObject>().ObjectId;
            TPTrapNetworking.RPC("DestroyTrapPair", [nob1, nob2]);
        }
        else
        {
            Debug.LogError("UHHHHHHHHHHHHHHHH OH");
        }
    }

    private CSteamID GOToCSteamID(GameObject go)
    {
        return (CSteamID)ulong.Parse(go
                .GetComponent<NetworkObject>().Owner.GetAddress());
    }

    private bool TeleportIfHost(GameObject potentialHost, Vector3 pos)
    {
        var ID = GOToCSteamID(potentialHost);
        if (ID == SteamUser.GetSteamID())
        {
            potentialHost.GetComponent<PlayerHealth>().sync___set_value_killer(owner, true);
            potentialHost.GetComponent<FirstPersonController>().Teleport(pos, angle: 0f, boost: false, cac: null, power: 0, decel: 0, dontTranslateRotation: true);
            return false;
        }
        return true;
    }

    private GameObject[] GetPlayers()
    {
        var boxExtents = new Vector3(3.2f, 1.631936f, 3.2f);
        var layerMask = 1 << 11 | 1 << 16;
        var colliders = Physics.OverlapBox(this.transform.position, boxExtents, Quaternion.identity, layerMask);
        return colliders
            .Select(col => col.transform.root.gameObject)
            .Where(go => go.CompareTag("Player"))
            .Distinct()
            .ToArray();
    }

    public GameObject explosionVfx;
    public AudioClip explosionAudio;
    public void Explode()
    {
        if (_otherTrap)
            _otherTrap.transform.Find("radius").gameObject.SetActive(false);
        UnityEngine.Object.Destroy(this.gameObject);
        UnityEngine.Object.Instantiate(explosionVfx, transform.position, Quaternion.identity);
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