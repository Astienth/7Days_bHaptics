using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using MyBhapticsTactsuit;
using System.Collections.Generic;

namespace Days_bhaptics
{
    [BepInPlugin("org.bepinex.plugins.7Days_bhaptics", "7Days_bhaptics integration", "1.4")]
    public class Plugin : BaseUnityPlugin
    {
#pragma warning disable CS0109 // Remove unnecessary warning
        internal static new ManualLogSource Log;
#pragma warning restore CS0109
        public static TactsuitVR tactsuitVr;
        public static bool startedHeart = false;

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
        public static void Postfix(EntityPlayerLocal __instance, DamageSource _damageSource)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            if (Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }

            if (__instance.IsDead())
            {
                return;
            }

            if (_damageSource.damageSource == EnumDamageSource.External)
            {
                KeyValuePair<float, float> coord = TactsuitVR.getAngleAndShift(__instance.transform, _damageSource.getDirection(), 180f);
                Plugin.tactsuitVr.PlayBackHit("Impact", coord.Key, coord.Value);
            }
            else
            {
                Plugin.tactsuitVr.PlaybackHaptics("Impact");
                Plugin.tactsuitVr.PlaybackHaptics("hurtvisor");
            }
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "OnFired")]
    public class bhaptics_OnFired
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance)
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
        public static void Postfix(EntityPlayerLocal __instance)
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
            Plugin.tactsuitVr.StopThreads();
            Plugin.startedHeart = false;
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "OnUpdateEntity")]
    public class bhaptics_OnUpdateEntity
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            if (__instance.IsDead() || Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }

            if (Traverse.Create(__instance).Field("oldHealth").GetValue<float>() < 15)
            {
                if (!Plugin.startedHeart)
                {
                    Plugin.startedHeart = true;
                    Plugin.tactsuitVr.StartHeartBeat();
                }
            }
            else
            {
                if (Plugin.startedHeart)
                {
                    Plugin.startedHeart = false;
                    Plugin.tactsuitVr.StopHeartBeat();
                }
            }
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "FireEvent")]
    public class bhaptics_OnFireEvent
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance, MinEventTypes _eventType)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            if (Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }

            switch (_eventType)
            {
                case MinEventTypes.onSelfJump:
                    Plugin.tactsuitVr.PlaybackHaptics("OnJump");
                    break;

                case MinEventTypes.onSelfRespawn:
                    Plugin.tactsuitVr.StopThreads();
                    Plugin.startedHeart = false;
                    break;

                case MinEventTypes.onSelfFirstSpawn:
                    Plugin.tactsuitVr.StopThreads();
                    Plugin.startedHeart = false;
                    break;

                default: break;
            }
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "SwimModeTick")]
    public class bhaptics_OnSwimModeTick
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            if (Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }

            int swimMode = Traverse.Create(__instance).Field("swimMode").GetValue<int>();
            if (swimMode > 0)
            {
                Plugin.tactsuitVr.StartSwimming();
            }
            else
            {
                Plugin.tactsuitVr.StopSwimming();
            }
        }
    }

    /*
    FireEvent from other classes
    

                //Event not used, find healing somewhere else ?
                case MinEventTypes.onSelfHealedSelf:
                    Plugin.tactsuitVr.PlaybackHaptics("Heal");
                    break; 
    */

    [HarmonyPatch(typeof(EntityAlive), "FireEvent")]
    public class bhaptics_OnFireEventEntityAlive
    {
        [HarmonyPostfix]
        public static void Postfix(EntityAlive __instance, MinEventTypes _eventType)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            if (__instance is EntityPlayerLocal &&
                !Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {

                switch (_eventType)
                {
                    case MinEventTypes.onOtherHealedSelf:
                        Plugin.tactsuitVr.PlaybackHaptics("Heal");
                        break;


                    case MinEventTypes.onSelfWaterSubmerge:
                        Plugin.tactsuitVr.PlaybackHaptics("EnterWater_Arms");
                        Plugin.tactsuitVr.PlaybackHaptics("EnterWater_Vest");
                        TactsuitVR.headUnderwater = true;
                        break;

                    case MinEventTypes.onSelfWaterSurface:
                        Plugin.tactsuitVr.PlaybackHaptics("ExitWater_Arms");
                        Plugin.tactsuitVr.PlaybackHaptics("ExitWater_Vest");
                        TactsuitVR.headUnderwater = false;
                        break;

                    case MinEventTypes.onSelfPrimaryActionRayHit:
                        Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_R", true, 0.5f);
                        Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_R", true, 0.5f);
                        break;

                    case MinEventTypes.onSelfSecondaryActionRayHit:
                        Plugin.tactsuitVr.PlaybackHaptics("RecoilArm_R", true, 0.5f);
                        Plugin.tactsuitVr.PlaybackHaptics("RecoilVest_R", true, 0.5f);
                        break;

                    default: break;
                }
            }
        }
    }



    [HarmonyPatch(typeof(EntityPlayerLocal), "FallImpact")]
    public class bhaptics_OnFallImpact
    {
        [HarmonyPrefix]
        public static void Prefix(EntityPlayerLocal __instance, float speed)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            if (__instance.IsDead() || Traverse.Create(__instance).Field("isSpectator").GetValue<bool>())
            {
                return;
            }

            if (speed > 0.02f)
            {
                Plugin.tactsuitVr.PlaybackHaptics("LandAfterJump");
            }
        }
    }

    [HarmonyPatch(typeof(ItemActionEat), "ExecuteAction")]
    public class bhaptics_OnEatAndDrink
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            Plugin.tactsuitVr.PlaybackHaptics("eatingvisor");
            Plugin.tactsuitVr.PlaybackHaptics("Eating");
        }
    }
    
    [HarmonyPatch(typeof(ItemActionEat), "ExecuteInstantAction")]
    public class bhaptics_OnDrinkAndDrink
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            Plugin.tactsuitVr.PlaybackHaptics("eatingvisor");
            Plugin.tactsuitVr.PlaybackHaptics("Eating");
        }
    }

    [HarmonyPatch(typeof(GameManager), "OnApplicationQuit")]
    public class bhaptics_OnAppQuit
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            Plugin.tactsuitVr.StopAllHapticFeedback();
            Plugin.startedHeart = false;
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "LateUpdate")]
    public class bhaptics_OnLateUpdate
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }
            if (Traverse.Create(__instance).Field("swimMode").GetValue<int>() < 0)
            {
                Plugin.tactsuitVr.StopSwimming();
            }
        }
    }

    [HarmonyPatch(typeof(EntityPlayerLocal), "OnEntityUnload")]
    public class bhaptics_OnDestroy
    {
        [HarmonyPostfix]
        public static void Postfix(EntityPlayerLocal __instance)
        {
            if (Plugin.tactsuitVr.suitDisabled)
            {
                return;
            }

            Plugin.tactsuitVr.StopAllHapticFeedback();
        }
    }
}

