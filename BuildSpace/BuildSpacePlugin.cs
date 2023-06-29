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

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BuildSpace
{
	[BepInPlugin(ModId, "BuildSpace", "1.0.1.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class BuildSpacePlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.buildspace";

        public static ConfigEntry<float> BuildRadiusMultiplier;

        private static Harmony sCraftingStationHarmony;

        private static readonly FieldInfo sAllStationsField;

        // Copy of BuildRadiusMultiplier so we can reference the old value after it changes
        private float mBuildRadiusMultiplier;

        static BuildSpacePlugin()
		{
            sAllStationsField = typeof(CraftingStation).GetField("m_allStations", BindingFlags.Static | BindingFlags.NonPublic);
		}

        private void Awake()
        {
            BuildRadiusMultiplier = Config.Bind("Build", nameof(BuildRadiusMultiplier), 1.0f, "Multiplier to apply to the build radius of crafting stations. Game default 1.");
            BuildRadiusMultiplier.SettingChanged += BuildRadiusMultiplier_SettingChanged;

            ClampConfig();
            mBuildRadiusMultiplier = BuildRadiusMultiplier.Value;

            sCraftingStationHarmony = new Harmony(ModId + "_CraftingStation");

            sCraftingStationHarmony.PatchAll(typeof(CraftingStation_Patches));
        }

        private void OnDestroy()
		{
            sCraftingStationHarmony.UnpatchSelf();
		}

        private void BuildRadiusMultiplier_SettingChanged(object sender, EventArgs e)
		{
            ClampConfig();

            List<CraftingStation> allStations = (List<CraftingStation>)sAllStationsField.GetValue(null);
            foreach (CraftingStation station in allStations)
			{
                SetBuildRadius(station, mBuildRadiusMultiplier, BuildRadiusMultiplier.Value);
            }

            mBuildRadiusMultiplier = BuildRadiusMultiplier.Value;
		}

		private void ClampConfig()
        {
            if (BuildRadiusMultiplier.Value < 0.1f) BuildRadiusMultiplier.Value = 0.1f;
            if (BuildRadiusMultiplier.Value > 100.0f) BuildRadiusMultiplier.Value = 100.0f;
        }

        private static void SetBuildRadius(CraftingStation station, float oldMultiplier, float newMultiplier)
		{
            float radius = station.m_rangeBuild / oldMultiplier * newMultiplier;
            station.m_rangeBuild = radius;

            CircleProjector projector = station.m_areaMarker?.GetComponent<CircleProjector>();
            if (projector != null)
			{
                projector.m_radius = radius;
                projector.m_nrOfSegments = (int)(radius * 4.0f);
			}
        }

        [HarmonyPatch(typeof(CraftingStation))]
        private static class CraftingStation_Patches
        {
            [HarmonyPatch("Start"), HarmonyPostfix]
            private static void Start_Postfix(CraftingStation __instance)
            {
                SetBuildRadius(__instance, 1.0f, BuildRadiusMultiplier.Value);
            }
        }
    }
}
