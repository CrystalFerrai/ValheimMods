// Copyright 2023 Crystal Ferrai
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

// WARNING: This mod is experimental and not intended to be released in its current state.
// While it does change the sea level, it causes some unintended side effects with map
// generation that need to be investigated. It also does not adjust the position of the
// distant water visual effect, so things look a bit broken.

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace SeaLevel
{
	[BepInPlugin(ModId, "SeaLevel", "1.0.0.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class SeaLevel : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.sealevel";

        public static ConfigEntry<float> WaterLevel;
        public static ConfigEntry<bool> IgnoreWater;

        private static Harmony sZoneSystemHarmony;
        private static Harmony sWaterVolumeHarmony;

        private void Awake()
        {
            WaterLevel = Config.Bind("SeaLevel", nameof(WaterLevel), 30.0f, "The current level of the water, in meters. Game default 30.");
            WaterLevel.SettingChanged += WaterLevel_SettingChanged;

            IgnoreWater = Config.Bind("Seafloor", nameof(IgnoreWater), false, "Whether players should ignore water, walking along the terrain beneath it. Game default false.");

            ClampConfig();
            sZoneSystemHarmony = new Harmony(ModId + "_ZoneSystem");
            sWaterVolumeHarmony = new Harmony(ModId + "_WaterVolume");

            sZoneSystemHarmony.PatchAll(typeof(ZoneSystem_Patches));
            sWaterVolumeHarmony.PatchAll(typeof(WaterVolume_Patches));
        }

		private void OnDestroy()
        {
            sZoneSystemHarmony.UnpatchSelf();
            sWaterVolumeHarmony.UnpatchSelf();
        }

        private void ClampConfig()
        {
            if (WaterLevel.Value < 0.0f) WaterLevel.Value = 0.0f;
            if (WaterLevel.Value > 1000.0f) WaterLevel.Value = 1000.0f;
        }

        private void WaterLevel_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();
            if (ZoneSystem.instance != null)
            {
                ZoneSystem.instance.m_waterLevel = WaterLevel.Value;
            }
            if (WaterVolume.Instances != null)
            {
                foreach (WaterVolume volume in WaterVolume.Instances)
                {
                    SetWaterVolumeBounds(volume);
                }
            }
        }

        private static void SetWaterVolumeBounds(WaterVolume volume)
        {
            Vector3 position = volume.transform.position;
            position.y = WaterLevel.Value;
            volume.transform.position = position;

            position = volume.m_waterSurface.transform.position;
            position.y = WaterLevel.Value;
            volume.m_waterSurface.transform.position = position;
        }

        [HarmonyPatch(typeof(ZoneSystem))]
        private static class ZoneSystem_Patches
        {
            [HarmonyPatch("Awake"), HarmonyPrefix]
            private static void Awake_Prefix(ZoneSystem __instance)
            {
                __instance.m_waterLevel = WaterLevel.Value;
            }
        }

        [HarmonyPatch(typeof(WaterVolume))]
        private static class WaterVolume_Patches
        {
            [HarmonyPatch("Awake"), HarmonyPostfix]
            private static void Awake_Postfix(WaterVolume __instance)
            {
                SetWaterVolumeBounds(__instance);
            }
        }
    }
}
