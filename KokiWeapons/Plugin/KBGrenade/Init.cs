using HeathenEngineering.PhysKit;
using UnityEngine;

namespace KBGrenade;

public class Init
{
    public static void Run()
    {
        var physGrenade = SpawnerManager.NameToWeaponDict["Repulsion Grenade"]
        .GetComponent<TrickShot>().template.gameObject;
        // physGrenade.GetComponent<PhysicsGrenade>().explosionDecal =
        // SpawnerManager.NameToWeaponDict["StunGrenade"]
        // .GetComponent<TrickShot>().template.gameObject
        // .GetComponent<PhysicsGrenade>().explosionDecal;

        if (physGrenade.GetComponent<RepulsionGrenade>())
        {
            Object.Destroy(physGrenade.GetComponent<RepulsionGrenade>());
            Object.Destroy(physGrenade.GetComponent<SpinUntilHit>());
        }

        physGrenade.AddComponent<RepulsionGrenade>();
        var spin = physGrenade.transform.Find("meshScale").Find("RepulsorGrenadeMerged").gameObject.AddComponent<SpinUntilHit>();
        spin.axis = new Vector3(1, 0, 0);
        spin.rotateSpeed = 250;
        spin.collisionRadius = 0.5f;
    }
}