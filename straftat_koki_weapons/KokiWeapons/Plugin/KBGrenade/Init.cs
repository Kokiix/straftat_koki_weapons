using BepInEx.Configuration;
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

        KBGrenadeMechanics.GrenadeDamage = KokiWeaponsPlugin.Instance.Config.Bind("KB Grenade", "Damage", 0,
        new ConfigDescription("How much damage the KB Grenade should deal on impact, range from 0-100.",
        new AcceptableValueRange<int>(0, 100)));

        // TODO: some NRE appears if you let grenade explode in your hand; doesn't appear to actually break anything tho

        // TODO: replace with actual working system
        var spin = physGrenade.transform.Find("meshScale").Find("RepulsorGrenadeMerged").gameObject.AddComponent<SpinUntilHit>();
        spin.axis = new Vector3(1, 0, 0);
        spin.rotateSpeed = 250;
        spin.collisionRadius = 0.5f;
    }
}