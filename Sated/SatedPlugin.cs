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
    [BepInPlugin(ModId, "Sated", "1.1.2.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class SatedPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.sated";

        public static ConfigEntry<bool> ShowFoodTimerBars;
        public static ConfigEntry<float> HealthCurveExponent;
        public static ConfigEntry<float> StaminaCurveExponent;

        private static Harmony sPlayerHarmony;
        private static Harmony sHudHarmony;
        private static Harmony sHudHealthHarmony;
        private static Harmony sHudFoodHarmony;

        private static readonly FieldInfo sPlayerFoodsField;
        private static readonly GameObject sProgressBarPrefab;

        private static GuiBar[] sFoodProgressBars;

        static SatedPlugin()
        {
            sPlayerFoodsField = typeof(Player).GetField("m_foods", BindingFlags.Instance | BindingFlags.NonPublic);

            AssetBundle progresBarAssetBundle = LoadAssetBundle("progress_bar");
            sProgressBarPrefab = progresBarAssetBundle.LoadAsset<GameObject>("Assets/ProgressBar/ProgressBarElement.prefab");
        }

        private void Awake()
        {
            ShowFoodTimerBars = Config.Bind("Food", nameof(ShowFoodTimerBars), true, "Whether to show timer bars below food icons on the HUD.");
            ShowFoodTimerBars.SettingChanged += ShowFoodTimerBars_SettingChanged;

            HealthCurveExponent = Config.Bind("Food", nameof(HealthCurveExponent), 8.0f, "The value of the exponent 'e' used in the food curve formula 'y = 1 - x^e' for calculating added health. Valid range 0.1 - 100. Higher values make you full longer, but also drop off more suddenly. A value of 1 indicates a linear decline (vanilla behavior). Values less than 1 invert the curve, causing a faster initial decline which gradually slows down.");
            HealthCurveExponent.SettingChanged += CurveExponent_SettingChanged;

            StaminaCurveExponent = Config.Bind("Food", nameof(StaminaCurveExponent), 8.0f, "The value of the exponent 'e' used in the food curve formula 'y = 1 - x^e' for calculating added stamina. Valid range 0.1 - 100. Higher values make you full longer, but also drop off more suddenly. A value of 1 indicates a linear decline (vanilla behavior). Values less than 1 invert the curve, causing a faster initial decline which gradually slows down.");
            StaminaCurveExponent.SettingChanged += CurveExponent_SettingChanged;

            ClampConfig();

            sPlayerHarmony = new Harmony(ModId + "_Player");
            sHudHarmony = new Harmony(ModId + "_Hud");
            sHudHealthHarmony = new Harmony(ModId + "_HudHealth");
            sHudFoodHarmony = new Harmony(ModId + "_HudFood");

            sPlayerHarmony.PatchAll(typeof(Player_Patches));
            sHudHarmony.PatchAll(typeof(Hud_Patches));
            sHudHealthHarmony.PatchAll(typeof(Hud_Health_Patch));
            if (ShowFoodTimerBars.Value)
            {
                sHudFoodHarmony.PatchAll(typeof(Hud_Food_Patch));
            }
        }

        private void OnDestroy()
        {
            sPlayerHarmony.UnpatchSelf();
            sHudHealthHarmony.UnpatchSelf();
            sHudFoodHarmony.UnpatchSelf();
        }

        private static void ClampConfig()
        {
            if (HealthCurveExponent.Value < 0.1f) HealthCurveExponent.Value = 0.1f;
            if (HealthCurveExponent.Value > 100.0f) HealthCurveExponent.Value = 100.0f;

            if (StaminaCurveExponent.Value < 0.1f) StaminaCurveExponent.Value = 0.1f;
            if (StaminaCurveExponent.Value > 100.0f) StaminaCurveExponent.Value = 100.0f;
        }

        private void CurveExponent_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();
            sHudHealthHarmony.UnpatchSelf();
            sHudHealthHarmony.PatchAll(typeof(Hud_Health_Patch));
        }

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

        [HarmonyPatch(typeof(Player))]
        private static class Player_Patches
        {
            [HarmonyPatch("GetTotalFoodValue"), HarmonyPrefix]
            private static bool GetTotalFoodValue_Prefix(Player __instance, out float hp, out float stamina)
            {
                var a = new[] { 0f, 7 };
                hp = 25.0f;
                stamina = 75.0f;
                foreach (Player.Food food in (List<Player.Food>)sPlayerFoodsField.GetValue(__instance))
                {
                    // y = 1 - x^8
                    hp += (1.0f - Mathf.Pow(1.0f - food.m_health / food.m_item.m_shared.m_food, HealthCurveExponent.Value)) * food.m_item.m_shared.m_food;
                    stamina += (1.0f - Mathf.Pow(1.0f - food.m_stamina / food.m_item.m_shared.m_foodStamina, StaminaCurveExponent.Value)) * food.m_item.m_shared.m_food;
                }
                return false;
            }
        }

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

        [HarmonyPatch(typeof(Hud))]
        private static class Hud_Health_Patch
        {
            [HarmonyPatch("UpdateFood"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> UpdateFood_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool done = false;
                CodeInstruction previous = null;

                foreach (CodeInstruction instruction in instructions)
                {
                    if (done)
                    {
                        yield return instruction;
                        continue;
                    }

                    if (instruction.opcode == OpCodes.Ldfld && ((FieldInfo)instruction.operand).Name == nameof(Player.Food.m_health) && previous != null)
                    {
                        // (1.0f - Mathf.Pow(1.0f - food.m_health / food.m_item.m_shared.m_food, CurveExponent.Value)) * food.m_item.m_shared.m_food
                        yield return new CodeInstruction(OpCodes.Ldc_R4, 1.0f);
                        yield return new CodeInstruction(OpCodes.Ldc_R4, 1.0f);
                        yield return previous;
                        yield return instruction;
                        yield return previous.Clone();
                        yield return new CodeInstruction(OpCodes.Ldfld, typeof(Player.Food).GetField(nameof(Player.Food.m_item)));
                        yield return new CodeInstruction(OpCodes.Ldfld, typeof(ItemDrop.ItemData).GetField(nameof(ItemDrop.ItemData.m_shared)));
                        yield return new CodeInstruction(OpCodes.Ldfld, typeof(ItemDrop.ItemData.SharedData).GetField(nameof(ItemDrop.ItemData.SharedData.m_food)));
                        yield return new CodeInstruction(OpCodes.Div);
                        yield return new CodeInstruction(OpCodes.Sub);
                        yield return new CodeInstruction(OpCodes.Ldc_R4, HealthCurveExponent.Value);
                        yield return new CodeInstruction(OpCodes.Call, typeof(Mathf).GetMethod(nameof(Mathf.Pow)));
                        yield return new CodeInstruction(OpCodes.Sub);
                        yield return previous.Clone();
                        yield return new CodeInstruction(OpCodes.Ldfld, typeof(Player.Food).GetField(nameof(Player.Food.m_item)));
                        yield return new CodeInstruction(OpCodes.Ldfld, typeof(ItemDrop.ItemData).GetField(nameof(ItemDrop.ItemData.m_shared)));
                        yield return new CodeInstruction(OpCodes.Ldfld, typeof(ItemDrop.ItemData.SharedData).GetField(nameof(ItemDrop.ItemData.SharedData.m_food)));
                        yield return new CodeInstruction(OpCodes.Mul);

                        done = true;
                    }
                    else
                    {
                        if (previous != null)
                        {
                            yield return previous;
                            previous = null;
                        }

                        if (instruction.opcode == OpCodes.Ldloc_S)
                        {
                            previous = instruction;
                        }
                        else
                        {
                            yield return instruction;
                        }
                    }
                }
            }
        }

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

        private static AssetBundle LoadAssetBundle(string name)
        {
            Assembly assembly = Assembly.GetCallingAssembly();
            return AssetBundle.LoadFromStream(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{name}"));
        }
    }
}
