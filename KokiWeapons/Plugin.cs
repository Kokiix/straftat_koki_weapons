using System.Collections;
using BepInEx;
using BepInEx.Logging;
using ComputerysModdingUtilities;
using HarmonyLib;
using UnityEngine;

[assembly: StraftatMod(isVanillaCompatible: true)]

namespace KokiWeapons;

[BepInPlugin("com.koki.weapons", "Koki Weapons", "1.0.0")]
public class KokiWeaponsPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static Harmony harmony;
    private void Awake()
    {
        Logger = base.Logger;
        harmony = new Harmony("com.koki.weapons");
        harmony.PatchAll();
    }

    private void OnDestroy()
    {
        harmony.UnpatchSelf();
    }
}