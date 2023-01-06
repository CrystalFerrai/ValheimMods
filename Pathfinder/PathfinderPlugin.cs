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

// Uncomment this to show debugging information on the in-game Hud
// Warning: Do not release mod with this uncommented
//#define DEBUG_SHOW_OVERLAY

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

#if DEBUG_SHOW_OVERLAY
using System.Text;
#endif

namespace Pathfinder
{
    [BepInPlugin(ModId, "Pathfinder", "2.0.7.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class PathfinderPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.pathfinder";

        public static ConfigEntry<float> LandExploreRadius;
        public static ConfigEntry<float> SeaExploreRadius;
        public static ConfigEntry<float> AltitudeRadiusBonus;
        public static ConfigEntry<float> ForestRadiusPenalty;
        public static ConfigEntry<float> DaylightRadiusScale;
        public static ConfigEntry<float> WeatherRadiusScale;
        public static ConfigEntry<bool> DisplayCurrentRadiusValue;
        public static ConfigEntry<bool> DisplayVariables;

        private static Harmony sMinimapHarmony;
        private static Harmony sHudHarmony;

        private static Text sRadiusHudText;
        private static Text sVariablesHudText;

#if DEBUG_SHOW_OVERLAY
        private static Harmony sDebugHarmony;
        private static StringBuilder sDebugTextBuilder;
        private static Text sDebugText;
#endif

        private void Awake()
        {
            LandExploreRadius = Config.Bind("Base", nameof(LandExploreRadius), 200.0f, "The radius around the player to uncover while travelling on land near sea level. Higher values may cause performance issues. Max allowed is 2000. Game default is 100.");
            LandExploreRadius.SettingChanged += Config_SettingChanged;

            SeaExploreRadius = Config.Bind("Base", nameof(SeaExploreRadius), 300.0f, "The radius around the player to uncover while travelling on a boat. Higher values may cause performance issues. Max allowed is 2000. Game default is 100.");
            SeaExploreRadius.SettingChanged += Config_SettingChanged;

            AltitudeRadiusBonus = Config.Bind("Multipliers", nameof(AltitudeRadiusBonus), 0.5f, "Bonus multiplier to apply to land exploration radius based on altitude. For every 100 units above sea level (smooth scale), add this value multiplied by LandExploreRadius to the total. For example, with a radius of 200 and a multiplier of 0.5, radius is 200 at sea level, 250 at 50 altitude, 300 at 100 altitude, 400 at 200 altitude, etc. For reference, a typical mountain peak is around 170 altitude. Accepted range 0-2. Set to 0 to disable.");
            AltitudeRadiusBonus.SettingChanged += Config_SettingChanged;

            ForestRadiusPenalty = Config.Bind("Multipliers", nameof(ForestRadiusPenalty), 0.3f, "Penalty to apply to land exploration radius when in a forest (black forest, forested parts of meadows and plains). This value is multiplied by the base land exploration radius and subtraced from the total. Accepted range 0-1. Set to 0 to disable.");
            ForestRadiusPenalty.SettingChanged += Config_SettingChanged;

            DaylightRadiusScale = Config.Bind("Multipliers", nameof(DaylightRadiusScale), 0.2f, "Influences how much daylight (directional and ambient light) affects exploration radius. This value is multiplied by the base land or sea exploration radius and added to the total. Accepted range 0-1. Set to 0 to disable.");
            DaylightRadiusScale.SettingChanged += Config_SettingChanged;

            WeatherRadiusScale = Config.Bind("Multipliers", nameof(WeatherRadiusScale), 0.5f, "Influences how much the current weather affects exploration radius. This value is multiplied by the base land or sea exploration radius and added to the total. Accepted range 0-1. Set to 0 to disable.");
            WeatherRadiusScale.SettingChanged += Config_SettingChanged;

            DisplayCurrentRadiusValue = Config.Bind("Miscellaneous", nameof(DisplayCurrentRadiusValue), false, "Enabling this will display the currently computed exploration radius in the bottom left of the in-game Hud. Useful if you are trying to tweak config values and want to see the result.");
            DisplayCurrentRadiusValue.SettingChanged += DisplayRadiusValue_SettingChanged;

            DisplayVariables = Config.Bind("Miscellaneous", nameof(DisplayVariables), false, "Enabling this will display on the Hud the values of various variables that go into calculating the exploration radius. Mostly useful for debugging and tweaking the config.");
            DisplayVariables.SettingChanged += DisplayVariablesValue_SettingChanged;

            ClampConfig();

            sMinimapHarmony = new Harmony(ModId + "_Minimap");
            sHudHarmony = new Harmony(ModId + "_Hud");

            sMinimapHarmony.PatchAll(typeof(Minimap_Patches));
            sHudHarmony.PatchAll(typeof(Hud_Patches));

#if DEBUG_SHOW_OVERLAY
            sDebugTextBuilder = new StringBuilder();
            sDebugHarmony = new Harmony(ModId + "_Hud_Debug");
            sDebugHarmony.PatchAll(typeof(Hud_Debug_Patch));
#endif
        }

        private void Config_SettingChanged(object sender, System.EventArgs e)
        {
            ClampConfig();
        }

        private void DisplayRadiusValue_SettingChanged(object sender, EventArgs e)
        {
            sRadiusHudText.gameObject.SetActive(DisplayCurrentRadiusValue.Value);
            if (!DisplayCurrentRadiusValue.Value)
            {
                sRadiusHudText.text = string.Empty;
			}
        }

        private void DisplayVariablesValue_SettingChanged(object sender, EventArgs e)
        {
            sVariablesHudText.gameObject.SetActive(DisplayVariables.Value);
            if (!DisplayVariables.Value)
            {
                sVariablesHudText.text = string.Empty;
            }
        }

        private void OnDestroy()
        {
            sMinimapHarmony.UnpatchSelf();
            sHudHarmony.UnpatchSelf();
#if DEBUG_SHOW_OVERLAY
            sDebugHarmony.UnpatchSelf();
#endif
        }

        private static void ClampConfig()
        {
            if (LandExploreRadius.Value < 0.0f) LandExploreRadius.Value = 0.0f;
            if (LandExploreRadius.Value > 2000.0f) LandExploreRadius.Value = 2000.0f;

            if (SeaExploreRadius.Value < 0.0f) SeaExploreRadius.Value = 0.0f;
            if (SeaExploreRadius.Value > 2000.0f) SeaExploreRadius.Value = 2000.0f;

            if (AltitudeRadiusBonus.Value < 0.0f) AltitudeRadiusBonus.Value = 0.0f;
            if (AltitudeRadiusBonus.Value > 2.0f) AltitudeRadiusBonus.Value = 2.0f;

            if (ForestRadiusPenalty.Value < 0.0f) ForestRadiusPenalty.Value = 0.0f;
            if (ForestRadiusPenalty.Value > 1.0f) ForestRadiusPenalty.Value = 1.0f;

            if (DaylightRadiusScale.Value < 0.0f) DaylightRadiusScale.Value = 0.0f;
            if (DaylightRadiusScale.Value > 1.0f) DaylightRadiusScale.Value = 1.0f;

            if (WeatherRadiusScale.Value < 0.0f) WeatherRadiusScale.Value = 0.0f;
            if (WeatherRadiusScale.Value > 1.0f) WeatherRadiusScale.Value = 1.0f;
        }

        [HarmonyPatch(typeof(Minimap))]
        private static class Minimap_Patches
        {
            private enum TranspilerState
            {
                Searching,
                Checking,
                Finishing
            }

            [HarmonyPatch("UpdateExplore"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> UpdateExplore_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                TranspilerState state = TranspilerState.Searching;

                CodeInstruction previous = null;

                foreach (CodeInstruction instruction in instructions)
                {
                    switch (state)
                    {
                        case TranspilerState.Searching:
                            if (instruction.opcode == OpCodes.Ldarg_0)
                            {
                                previous = instruction;
                                state = TranspilerState.Checking;
                            }
                            else
                            {
                                yield return instruction;
                            }
                            break;
                        case TranspilerState.Checking:
                            if (instruction.opcode == OpCodes.Ldfld && ((FieldInfo)instruction.operand).Name == nameof(Minimap.m_exploreRadius))
                            {
                                yield return new CodeInstruction(OpCodes.Ldarg_2); // player
                                yield return new CodeInstruction(OpCodes.Call, typeof(Minimap_Patches).GetMethod(nameof(GetExploreRadius), BindingFlags.Static | BindingFlags.NonPublic));
                                state = TranspilerState.Finishing;
                            }
                            else
                            {
                                yield return previous;
                                yield return instruction;
                                state = TranspilerState.Searching;
                            }
                            previous = null;
                            break;
                        case TranspilerState.Finishing:
                            yield return instruction;
                            break;
                    }
                }
            }

            private static float GetExploreRadius(Player player)
            {
                float result;

                if (player.InInterior())
                {
                    // In a dungeon. Dungeons are way up high and we dont want to reveal a huge section of the map when entering one.
                    // We actually want to reduce the radius since it doesnt make sense to be able to explore the map while in a dungeon
                    result = Mathf.Max(LandExploreRadius.Value * 0.2f, 10.0f);

                    sRadiusHudText.text = $"Pathfinder: radius={result:0.0}";

                    return result;
                }

                float baseRadius;
                float multiplier = 1.0f;

                // Player may not be the one piloting a boat, but should still get the sea radius if they are riding in one that has a pilot.
                // A longship is about 20 units long. 19 is about as far as you could possibly get from a pilot and still be on the boat.
                List<Player> players = new List<Player>();
                Player.GetPlayersInRange(player.transform.position, 21.0f, players);
                if (players.Any(p => p.IsAttachedToShip()))
                {
                    baseRadius = SeaExploreRadius.Value;
                }
                else
                {
                    baseRadius = LandExploreRadius.Value;
                }

                // Take the higher of directional or ambient light, subtract 1 to turn this into a value we can add to our multiplier
                float light = Mathf.Max(GetColorMagnitude(EnvMan.instance.m_dirLight.color * EnvMan.instance.m_dirLight.intensity), GetColorMagnitude(RenderSettings.ambientLight));
                multiplier += (light - 1.0f) * DaylightRadiusScale.Value;

                // Account for weather
                float particles = 0.0f;
                foreach (GameObject particleSystem in EnvMan.instance.GetCurrentEnvironment().m_psystems)
                {
                    // Certain particle systems heavily obstruct view
                    if (particleSystem.name.Equals("Mist", StringComparison.InvariantCultureIgnoreCase))
                    {
                        particles += 0.5f;
                    }
                    if (particleSystem.name.Equals("SnowStorm", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Snow storm lowers visibility during the day more than at night
                        particles += 0.7f * light;
                    }
                }

                // Fog density range seems to be 0.001 to 0.15 based on environment data. Multiply by 10 to get a more meaningful range.
                float fog = Mathf.Clamp(RenderSettings.fogDensity * 10.0f + particles, 0.0f, 1.5f);
                multiplier -= fog * WeatherRadiusScale.Value;

                // Sea level = 30, tallest mountains (not including the rare super mountains) seem to be around 220. Stop adding altitude bonus after 400
                float altitude = Mathf.Clamp(player.transform.position.y - ZoneSystem.instance.m_waterLevel, 0.0f, 400.0f);
                float adjustedAltitude = altitude / 100.0f * Mathf.Max(0.05f, 1.0f - particles);
                multiplier += adjustedAltitude * AltitudeRadiusBonus.Value;

                // Make adjustments based on biome
                float location = GetLocationModifier(player, adjustedAltitude);
                multiplier += location;

                if (multiplier > 5.0f) multiplier = 5.0f;
                if (multiplier < 0.2f) multiplier = 0.2f;

#if DEBUG_SHOW_OVERLAY
                {
                    Light dirLight = EnvMan.instance.m_dirLight;

                    float time = (float)((long)ZNet.instance.GetTimeSeconds() % EnvMan.instance.m_dayLengthSec) / (float)EnvMan.instance.m_dayLengthSec;
                    int hours = Mathf.FloorToInt(time * 24.0f);
                    int minutes = Mathf.FloorToInt((time * 24.0f - hours) * 60.0f);

                    sDebugTextBuilder.Clear();

                    sDebugTextBuilder.AppendLine($"env={EnvMan.instance.GetCurrentEnvironment().m_name} ({Localization.instance.Localize(string.Concat("$biome_", EnvMan.instance.GetCurrentBiome().ToString().ToLower()))})");
                    sDebugTextBuilder.AppendLine($"time={hours:00}:{minutes:00} ({time:0.000})");
                    sDebugTextBuilder.AppendLine($"altitude={player.transform.position.y - ZoneSystem.instance.m_waterLevel:0.00}");
                    sDebugTextBuilder.AppendLine($"light={light:0.000} (dir={GetColorMagnitude(EnvMan.instance.m_dirLight.color * EnvMan.instance.m_dirLight.intensity):0.000}, amb={GetColorMagnitude(RenderSettings.ambientLight):0.000})");
                    sDebugTextBuilder.AppendLine($"fog={fog:0.000} (raw={RenderSettings.fogDensity:0.0000})");
                    sDebugTextBuilder.AppendLine($"particle={particles:0.000}");
                    sDebugTextBuilder.AppendLine($"radius={baseRadius * multiplier:0.0} ({baseRadius:0.0} * {multiplier:0.000})");

                    sDebugText.text = sDebugTextBuilder.ToString();
                }
#endif

                result = Mathf.Clamp(baseRadius * multiplier, 20.0f, 2000.0f);

                if (DisplayVariables.Value)
                {
                    const string fmt = "+0.000;-0.000;0.000";
                    sVariablesHudText.text = $"Pathfinder Variables\nRadius: {result:0.0}\nBase: {baseRadius:0.#}\nMultiplier: {multiplier:0.000}\n\nLight: {((light - 1.0f) * DaylightRadiusScale.Value).ToString(fmt)}\nWeather: {(-fog * WeatherRadiusScale.Value).ToString(fmt)}\nAltitude: {(adjustedAltitude * AltitudeRadiusBonus.Value).ToString(fmt)}\nLocation: {location.ToString(fmt)}";
                }

                if (DisplayCurrentRadiusValue.Value)
				{
                    sRadiusHudText.text = $"Pathfinder: radius={result:0.0}";
                }

                return result;
            }

            private static float GetColorMagnitude(Color color)
            {
                // Intentionally ignoring alpha here
                return Mathf.Sqrt(color.r * color.r + color.g * color.g + color.b * color.b);
            }

            private static float GetLocationModifier(Player player, float altitude)
            {
                // Forest thresholds based on logic found in MiniMap.GetMaskColor

                float forestPenalty = ForestRadiusPenalty.Value + altitude * AltitudeRadiusBonus.Value * ForestRadiusPenalty.Value;
                switch (player.GetCurrentBiome())
                {
                    case Heightmap.Biome.BlackForest:
                        // Small extra penalty to account for high daylight values in black forest
                        return -forestPenalty - 0.25f * DaylightRadiusScale.Value;
                    case Heightmap.Biome.Meadows:
                        return WorldGenerator.InForest(player.transform.position) ? -forestPenalty : 0.0f;
                    case Heightmap.Biome.Plains:
                        // Small extra bonus to account for low daylight values in plains
                        return (WorldGenerator.GetForestFactor(player.transform.position) < 0.8f ? -forestPenalty : 0.0f) + 0.1f * DaylightRadiusScale.Value;
                    default:
                        return 0.0f;
                }
            }
        }

        [HarmonyPatch(typeof(Hud))]
        private static class Hud_Patches
        {
            [HarmonyPatch("Awake"), Harmony, HarmonyPostfix]
            private static void Awake_Postfix(Hud __instance)
            {
                {
                    GameObject textObject = new GameObject("Pathfinder_RadiusText");
                    textObject.AddComponent<CanvasRenderer>();
                    textObject.transform.localPosition = Vector3.zero;

                    RectTransform transform = textObject.AddComponent<RectTransform>();
                    transform.SetParent(__instance.m_rootObject.transform);
                    transform.pivot = transform.anchorMin = transform.anchorMax = new Vector2(0.0f, 0.0f);
                    transform.offsetMin = new Vector2(10.0f, 5.0f);
                    transform.offsetMax = new Vector2(210.0f, 165.0f);

                    sRadiusHudText = textObject.AddComponent<Text>();
                    sRadiusHudText.raycastTarget = false;
                    sRadiusHudText.font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Helvetica", "Arial" }, 12);
                    sRadiusHudText.fontStyle = FontStyle.Bold;
                    sRadiusHudText.color = Color.white;
                    sRadiusHudText.fontSize = 12;
                    sRadiusHudText.alignment = TextAnchor.LowerLeft;

                    Outline textOutline = textObject.AddComponent<Outline>();
                    textOutline.effectColor = Color.black;

                    textObject.SetActive(DisplayCurrentRadiusValue.Value);
                }

                {
                    GameObject textObject = new GameObject("Pathfinder_VariableText");
                    textObject.AddComponent<CanvasRenderer>();
                    textObject.transform.localPosition = Vector3.zero;

                    RectTransform transform = textObject.AddComponent<RectTransform>();
                    transform.SetParent(__instance.m_rootObject.transform);
                    transform.pivot = transform.anchorMin = transform.anchorMax = new Vector2(0.0f, 0.0f);
                    transform.offsetMin = new Vector2(240.0f, 5.0f);
                    transform.offsetMax = new Vector2(440.0f, 165.0f);

                    sVariablesHudText = textObject.AddComponent<Text>();
                    sVariablesHudText.raycastTarget = false;
                    sVariablesHudText.font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Helvetica", "Arial" }, 12);
                    sVariablesHudText.fontStyle = FontStyle.Bold;
                    sVariablesHudText.color = Color.white;
                    sVariablesHudText.fontSize = 12;
                    sVariablesHudText.alignment = TextAnchor.LowerLeft;

                    Outline textOutline = textObject.AddComponent<Outline>();
                    textOutline.effectColor = Color.black;

                    textObject.SetActive(DisplayVariables.Value);
                }
            }
        }

#if DEBUG_SHOW_OVERLAY
        [HarmonyPatch(typeof(Hud))]
        private static class Hud_Debug_Patch
        {
            [HarmonyPatch("Awake"), Harmony, HarmonyPostfix]
            private static void Awake_Postfix(Hud __instance)
            {
                GameObject debugTextObject = new GameObject("DebugText");
                debugTextObject.AddComponent<CanvasRenderer>();
                debugTextObject.transform.localPosition = Vector3.zero;

                RectTransform transform = debugTextObject.AddComponent<RectTransform>();
                transform.SetParent(__instance.m_rootObject.transform);
                transform.pivot = transform.anchorMin = transform.anchorMax = new Vector2(0.5f, 1.0f);
                transform.offsetMin = new Vector2(-300.0f, -500.0f);
                transform.offsetMax = new Vector2(300.0f, -100.0f);

                sDebugText = debugTextObject.AddComponent<Text>();
                sDebugText.raycastTarget = false;
                sDebugText.font = Font.CreateDynamicFontFromOSFont("Courier New", 14);
                sDebugText.fontStyle = FontStyle.Bold;
                sDebugText.color = Color.white;
                sDebugText.fontSize = 14;
                sDebugText.alignment = TextAnchor.UpperLeft;

                Outline debugTextOutline = debugTextObject.AddComponent<Outline>();
                debugTextOutline.effectColor = Color.black;
            }
        }
#endif
    }
}
