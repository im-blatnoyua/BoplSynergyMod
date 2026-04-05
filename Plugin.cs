using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;

namespace BoplSynergyMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; } = null!;
        public static ManualLogSource Log { get; private set; } = null!;

        void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo($"[BoplSynergyMod] Loading v{PluginInfo.PLUGIN_VERSION}...");

            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            try
            {
                harmony.PatchAll();
                Log.LogInfo("[BoplSynergyMod] All patches applied successfully.");
                Log.LogInfo("[BoplSynergyMod] Synergy system initialized!");
            }
            catch (Exception ex)
            {
                Log.LogError($"[BoplSynergyMod] Patch failed: {ex.Message}");
                Log.LogError($"[BoplSynergyMod] Stack trace: {ex.StackTrace}");
            }

            Log.LogInfo("[BoplSynergyMod] Loaded successfully!");
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.boplsynergy.mod";
        public const string PLUGIN_NAME = "Bopl Synergy Mod";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}
