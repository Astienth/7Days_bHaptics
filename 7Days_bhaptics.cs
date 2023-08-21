using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using MyBhapticsTactsuit;

namespace Days_bhaptics
{
    [BepInPlugin("org.bepinex.plugins.7Days_bhaptics", "7Days_bhaptics integration", "1.4")]
    public class Plugin : BaseUnityPlugin
    {
#pragma warning disable CS0109 // Remove unnecessary warning
        internal static new ManualLogSource Log;
#pragma warning restore CS0109
        public static TactsuitVR tactsuitVr;

        private void Awake()
        {
            // Make my own logger so it can be accessed from the Tactsuit class
            Log = base.Logger;
            // Plugin startup logic
            Logger.LogMessage("Plugin 7Days_bhaptics is loaded!");
            tactsuitVr = new TactsuitVR();
            // one startup heartbeat so you know the vest works correctly
            tactsuitVr.PlaybackHaptics("HeartBeat");
            // patch all functions
            var harmony = new Harmony("bhaptics.patch.7days");
            harmony.PatchAll();
        }
    }

    /*
    EntityPlayerLocal :
        swimming
    */

    
    [HarmonyPatch(typeof(EntityPlayerLocal), "DamageEntity")]
    public class bhaptics_OnDamage
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayer __instance, int __result)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            if ( __result <= 0 || Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Impact");
            Plugin.tactsuitVr.PlaybackHaptics("hurtvisor");
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "OnFired")]
    public class bhaptics_OnFired
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayer __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            if (Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_R");
            Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_R");
        }
    }
    
    [HarmonyPatch(typeof(EntityPlayerLocal), "OnEntityDeath")]
    public class bhaptics_OnEntityDeath
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayer __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            if (Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }
            Plugin.tactsuitVr.PlaybackHaptics("Death");
        }
    }
    
    [HarmonyPatch(typeof(EntityPlayerLocal), "OnUpdateEntity")]
    public class bhaptics_OnUpdateEntity
    {
        public static bool startedHeart = false;

        [HarmonyPostfix]
        public static void Postfix(EntityPlayer __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            if (Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }
            if (Traverse.Create(__instance).Field("oldHealth").GetValue<float>() < 15 
                && !startedHeart)
            {
                startedHeart = true;
                Plugin.tactsuitVr.StartHeartBeat();
            }
            else
            {
                startedHeart = false;
                Plugin.tactsuitVr.StopHeartBeat();
            }
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "FireEvent")]
    public class bhaptics_OnFireEvent
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayer __instance, MinEventTypes _eventType)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            if (Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }

            switch(_eventType)
            {
                case MinEventTypes.onSelfItemActivate:
                    Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_R", true, 0.5f); 
                    Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_R", true, 0.5f);
                    break;
                    
                case MinEventTypes.onSelfJump:
                    Plugin.tactsuitVr.PlaybackHaptics("OnJump");
                    break;

                case MinEventTypes.onSelfLandJump:
                    Plugin.tactsuitVr.PlaybackHaptics("OnJump");
                    break;

                case MinEventTypes.onSelfHealedSelf:
                    Plugin.tactsuitVr.PlaybackHaptics("Heal");
                    break; 

                case MinEventTypes.onOtherHealedSelf:
                    Plugin.tactsuitVr.PlaybackHaptics("Heal");
                    break;


                /*
                case onSelfSwimStart,
                      onSelfSwimStop,
                      onSelfSwimRun,
                      onSelfSwimIdle,
                onSelfItemCrafted,
                  onSelfItemRepaired,
                  onSelfItemLooted,
                  onSelfItemLost,
                  onSelfItemGained,
                  onSelfItemSold,
                  onSelfItemBought,
                  onSelfItemActivate,
                  onSelfItemDeactivate,

                onSelfRespawn,   => Stop all bhaptics
                onSelfLeaveGame,=> Stop all bhaptics

                */
                default: break;
            }
        }
    }
}

