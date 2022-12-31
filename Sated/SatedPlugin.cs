// Copyright 2021 Crystal Ferrai
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// Disabled because it messes up the UI after Hearth and Home update.
// Food timer bars need to be reworked, but not a priority because the
// UI now shows the timer as text.
//#define FEATURE_FOOD_BARS

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Sated
{
    [BepInPlugin(ModId, "Sated", "1.1.7.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class SatedPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.sated";

#if FEATURE_FOOD_BARS
        public static ConfigEntry<bool> ShowFoodTimerBars;
#endif
        public static ConfigEntry<float> HealthCurveExponent;
        public static ConfigEntry<float> StaminaCurveExponent;
        public static ConfigEntry<float> EitrCurveExponent;

        private static Harmony sPlayerHarmony;
#if FEATURE_FOOD_BARS
        private static Harmony sHudHarmony;
        private static Harmony sHudFoodHarmony;

        private static readonly GameObject sProgressBarPrefab;
        private static GuiBar[] sFoodProgressBars;
#endif

        private static readonly FieldInfo sPlayerFoodsField;

        static SatedPlugin()
        {
            sPlayerFoodsField = typeof(Player).GetField("m_foods", BindingFlags.Instance | BindingFlags.NonPublic);

#if FEATURE_FOOD_BARS
            AssetBundle progresBarAssetBundle = LoadAssetBundle("progress_bar");
            sProgressBarPrefab = progresBarAssetBundle.LoadAsset<GameObject>("Assets/ProgressBar/ProgressBarElement.prefab");
#endif
        }

        private void Awake()
        {
#if FEATURE_FOOD_BARS
            ShowFoodTimerBars = Config.Bind("Food", nameof(ShowFoodTimerBars), true, "Whether to show timer bars below food icons on the HUD.");
            ShowFoodTimerBars.SettingChanged += ShowFoodTimerBars_SettingChanged;
#endif

            HealthCurveExponent = Config.Bind("Food", nameof(HealthCurveExponent), 8.0f, "The value of the exponent 'e' used in the food curve formula 'y = 1 - x^e' for calculating added health. Valid range 0.1 - 100. Higher values make you full longer, but also drop off more suddenly. A value of 1 indicates a linear decline. Values less than 1 invert the curve, causing a faster initial decline which gradually slows down.");
            HealthCurveExponent.SettingChanged += CurveExponent_SettingChanged;

            StaminaCurveExponent = Config.Bind("Food", nameof(StaminaCurveExponent), 8.0f, "The value of the exponent 'e' used in the food curve formula 'y = 1 - x^e' for calculating added stamina. Valid range 0.1 - 100. Higher values make you full longer, but also drop off more suddenly. A value of 1 indicates a linear decline. Values less than 1 invert the curve, causing a faster initial decline which gradually slows down.");
            StaminaCurveExponent.SettingChanged += CurveExponent_SettingChanged;

            EitrCurveExponent = Config.Bind("Food", nameof(EitrCurveExponent), 8.0f, "The value of the exponent 'e' used in the food curve formula 'y = 1 - x^e' for calculating added eitr. Valid range 0.1 - 100. Higher values make you full longer, but also drop off more suddenly. A value of 1 indicates a linear decline. Values less than 1 invert the curve, causing a faster initial decline which gradually slows down.");
            EitrCurveExponent.SettingChanged += CurveExponent_SettingChanged;

            ClampConfig();

            sPlayerHarmony = new Harmony(ModId + "_Player");
            sPlayerHarmony.PatchAll(typeof(Player_Patches));

#if FEATURE_FOOD_BARS
            sHudHarmony = new Harmony(ModId + "_Hud");
            sHudHarmony.PatchAll(typeof(Hud_Patches));
            
            sHudFoodHarmony = new Harmony(ModId + "_HudFood");
            if (ShowFoodTimerBars.Value)
            {
                sHudFoodHarmony.PatchAll(typeof(Hud_Food_Patch));
            }
#endif
        }

        private void OnDestroy()
        {
            sPlayerHarmony.UnpatchSelf();
#if FEATURE_FOOD_BARS
            sHudHarmony.UnpatchSelf();
            sHudFoodHarmony.UnpatchSelf();
#endif
        }

        private static void ClampConfig()
        {
            if (HealthCurveExponent.Value < 0.1f) HealthCurveExponent.Value = 0.1f;
            if (HealthCurveExponent.Value > 100.0f) HealthCurveExponent.Value = 100.0f;

            if (StaminaCurveExponent.Value < 0.1f) StaminaCurveExponent.Value = 0.1f;
            if (StaminaCurveExponent.Value > 100.0f) StaminaCurveExponent.Value = 100.0f;

            if (EitrCurveExponent.Value < 0.1f) EitrCurveExponent.Value = 0.1f;
            if (EitrCurveExponent.Value > 100.0f) EitrCurveExponent.Value = 100.0f;
        }

        private void CurveExponent_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();
        }

#if FEATURE_FOOD_BARS
        private void ShowFoodTimerBars_SettingChanged(object sender, EventArgs e)
        {
            if (ShowFoodTimerBars.Value)
            {
                ShowFoodBars(true);
                sHudFoodHarmony.PatchAll(typeof(Hud_Food_Patch));
            }
            else
            {
                sHudFoodHarmony.UnpatchSelf();
                ShowFoodBars(false);
            }
        }
#endif

        [HarmonyPatch(typeof(Player))]
        private static class Player_Patches
        {
            [HarmonyPatch("GetTotalFoodValue"), HarmonyPrefix]
            private static bool GetTotalFoodValue_Prefix(Player __instance, out float hp, out float stamina, out float eitr)
            {
                hp = __instance.m_baseHP;
                stamina = __instance.m_baseStamina;
                eitr = 0.0f;
                foreach (Player.Food food in (List<Player.Food>)sPlayerFoodsField.GetValue(__instance))
                {
                    // y = 1 - x^8
                    float time = 1.0f - food.m_time / food.m_item.m_shared.m_foodBurnTime;
                    hp += (1.0f - Mathf.Pow(time, HealthCurveExponent.Value)) * food.m_item.m_shared.m_food;
                    stamina += (1.0f - Mathf.Pow(time, StaminaCurveExponent.Value)) * food.m_item.m_shared.m_foodStamina;
                    eitr += (1.0f - Mathf.Pow(time, EitrCurveExponent.Value)) * food.m_item.m_shared.m_foodEitr;
                }
                return false;
            }
        }

#if FEATURE_FOOD_BARS
        [HarmonyPatch(typeof(Hud))]
        private static class Hud_Patches
        {
            [HarmonyPatch("Awake"), HarmonyPostfix]
            private static void Awake_Postfix(Hud __instance)
            {
                if (ShowFoodTimerBars.Value)
                {
                    ShowFoodBars(true);
                }
            }

            [HarmonyPatch("OnDestroy"), HarmonyPrefix]
            private static void OnDestroy_Prefix(Hud __instance)
            {
                sFoodProgressBars = null;
            }
        }
#endif

#if FEATURE_FOOD_BARS
        [HarmonyPatch(typeof(Hud))]
        private static class Hud_Food_Patch
        {
            [HarmonyPatch("UpdateFood"), HarmonyPostfix]
            private static void UpdateFood_Postfix(Hud __instance, Player player)
            {
                List<Player.Food> foods = player.GetFoods();
                for (int i = __instance.m_foodIcons.Length - 1; i >= 0; --i)
                {
                    if (i >= foods.Count)
                    {
                        sFoodProgressBars[i].gameObject.SetActive(false);
                        continue;
                    }

                    sFoodProgressBars[i].gameObject.SetActive(true);
                    sFoodProgressBars[i].SetColor(foods[i].m_item.m_shared.m_foodColor);
                    sFoodProgressBars[i].SetMaxValue(foods[i].m_item.m_shared.m_food);
                    sFoodProgressBars[i].SetValue(foods[i].m_health);
                }
            }
        }

        // Don't call this unless you are sure the 'show' bool has flipped since the last call, else the food slots will offset farther
        private static void ShowFoodBars(bool show)
        {
            Hud hud = Hud.instance;
            if (hud == null)
            {
                sFoodProgressBars = null;
                return;
            }

            if (show)
            {
                sFoodProgressBars = new GuiBar[hud.m_foodIcons.Length];
            }

            for (int i = 0; i < hud.m_foodIcons.Length; ++i)
            {
                // Scale may be different whe creating the UI than it is when altering it later, so use the live scale factor
                float scale = hud.m_foodIcons[i].canvas.scaleFactor;
                float space = (6 * i + 4) * scale;
                RectTransform icon = hud.m_foodIcons[i].GetComponentInParent<RectTransform>();
                RectTransform slot = icon.parent.gameObject.GetComponentInParent<RectTransform>();
                slot.position = new Vector3(slot.position.x, slot.position.y + (show ? space : -space), slot.position.z);

                if (show)
                {
                    float barOffset = 16.0f * scale;
                    GameObject barObject = Instantiate(sProgressBarPrefab);
                    RectTransform bar = barObject.GetComponent<RectTransform>();
                    bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 32.0f);
                    bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 4.0f);
                    ((RectTransform)bar.GetChild(0)).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 4.0f);
                    bar.SetParent(slot, false);
                    bar.position = new Vector3(slot.position.x - barOffset, slot.position.y - barOffset, slot.position.z);

                    sFoodProgressBars[i] = barObject.GetComponent<GuiBar>();
                }
                else
                {
                    Destroy(sFoodProgressBars[i].gameObject);
                }
            }

            if (!show)
            {
                sFoodProgressBars = null;
            }
        }
#endif

        private static AssetBundle LoadAssetBundle(string name)
        {
            Assembly assembly = Assembly.GetCallingAssembly();
            return AssetBundle.LoadFromStream(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{name}"));
        }
    }
}
