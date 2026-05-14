using HeathenEngineering.PhysKit;

namespace KBGrenade;

public class Init
{
    public static void Run()
    {
        var physGrenade = SpawnerManager.NameToWeaponDict["Repulsion Grenade"]
        .GetComponent<TrickShot>().template.gameObject;
        physGrenade.GetComponent<PhysicsGrenade>().explosionDecal =
        SpawnerManager.NameToWeaponDict["StunGrenade"]
        .GetComponent<TrickShot>().template.gameObject
        .GetComponent<PhysicsGrenade>().explosionDecal;

        if (KokiWeaponsPlugin.DebugGetComponent(physGrenade, typeof(RepulsionGrenade)) == null)
            physGrenade.AddComponent<RepulsionGrenade>();
    }
}