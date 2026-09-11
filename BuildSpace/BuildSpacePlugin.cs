// Copyright 2026 Crystal Ferrai
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
using ConditionalConfigSync;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BuildSpace
{
	[BepInPlugin(ModId, ModName, ModVersion)]
	[BepInDependency("_shudnal.ConditionalConfigSync", BepInDependency.DependencyFlags.HardDependency)]
	[BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class BuildSpacePlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.buildspace";
        public const string ModName = "Build Space";
        public const string ModVersion = "1.2.1.0";

		internal static readonly ConfigSync ConfigSync = new ConfigSync(ModId)
		{
			DisplayName = ModName,
			CurrentVersion = ModVersion,
			MinimumRequiredVersion = ModVersion
		};

		public static ConfigEntry<bool> ModRequired;

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
			ModRequired = Config.Bind("ServerSync", nameof(ModRequired), true, "If true on server, clients connecting will be rejected if they do not have the mod. If false, clients may connect without the mod. If true on client, cannot join a server unless it is running the mod. Clients without the mod may cause it to not function reliably for others. It is recommended to keep this true if possible.");
			ConfigSync.ModRequired = ModRequired.Value;
			ModRequired.SettingChanged += ModRequired_SettingChanged;
			ConfigSync.AddConfigEntry(ModRequired, ConfigSyncMode.AlwaysServerControlled);

			BuildRadiusMultiplier = Config.Bind("Build", nameof(BuildRadiusMultiplier), 1.0f, "Multiplier to apply to the build radius of crafting stations. Game default 1. [The value will be enforced on a server.]");
            BuildRadiusMultiplier.SettingChanged += BuildRadiusMultiplier_SettingChanged;
            ConfigSync.AddConfigEntry(BuildRadiusMultiplier, ConfigSyncMode.AlwaysServerControlled);

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

		private void ModRequired_SettingChanged(object sender, EventArgs e)
		{
			ConfigSync.ModRequired = ModRequired.Value;
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
