using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

public class KBGrenade : PhysicsGrenade
{
    internal static ConfigEntry<int> grenadeDamage = KokiWeaponsPlugin.Instance.Config.Bind("KB Grenade", "Damage", 0,
        new ConfigDescription("How much damage the KB Grenade should deal on impact, range from 0-100.",
        new AcceptableValueRange<int>(0, 100)));

    bool _spin = true;

    internal void KBExplosion(Vector3 position)
    {
        transform.position = position;
        if (isOwner)
        {
            var colliders = Physics.OverlapSphere(position, explosionRadius, bodyLayer);

            Rigidbody rb = null;
            colliders.DoIf(c => c && c.gameObject.TryGetComponent(out rb),
            item =>
            {
                Vector3 force = item.transform.position - transform.position;
                force.y = 0;
                force.Normalize();
                force *= 10;
                force.y = 2;

                item.transform.position += new Vector3(0, 2.5f, 0);
                rb.AddForce(force, ForceMode.Impulse);
            });

            colliders
            .Select(c => c.GetComponentInParent<PlayerHealth>())
            .Distinct()
            .Do(ph =>
            {
                if (!ph || ph.sync___get_value_isKilled()) return;
                Vector3 force = ph.controller.transform.position - transform.position;
                if (force.y < 0)
                    force.y = 0;
                force.Normalize();

                if (ph.controller.isGrounded)
                    ph.controller.transform.position += new Vector3(0, 2.5f, 0);
                ph.controller.CustomAddForce(force, 120);
                if (grenadeDamage.Value > 0)
                    ph.RemoveHealth(grenadeDamage.Value / 25);
            });
        }

        UnityEngine.Object.Destroy(base.gameObject, 3f);
        base.enabled = false;
        graph.gameObject.SetActive(value: false);
        if (!fragGrenade)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate(explosionVfx, base.transform.position, Quaternion.identity);
            if (gameObject.transform.Find("ball") != null)
            {
                gameObject.transform.Find("ball").localScale = vfxScale;
            }
        }
        UnityEngine.Object.Instantiate(explosionDecal, base.transform.position, Quaternion.identity);
        audio.Play();
    }

    internal void Spin()
    {
        graph.Rotate(rotateAxis * rotateSpeed * Time.deltaTime);
    }
}

[HarmonyPatch(typeof(PhysicsGrenade))]
class KBGrenadeOverrides
{
    [HarmonyPatch("RpcLogic___HandleExplosion_4276783012"), HarmonyPrefix]
    static bool RedirectHandleExplosion(PhysicsGrenade __instance, Vector3 position)
    {
        if (__instance is KBGrenade kbg)
        {
            kbg.KBExplosion(position);
            return false;
        }
        return true;
    }

    [HarmonyPatch("Update"), HarmonyPostfix]
    static void StartSpin(PhysicsGrenade __instance)
    {
        if (__instance is KBGrenade kbg)
        {
            kbg.Spin();
        }
    }
}