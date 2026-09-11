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

namespace Magni
{
	[BepInPlugin(ModId, ModName, ModVersion)]
	[BepInDependency("_shudnal.ConditionalConfigSync", BepInDependency.DependencyFlags.HardDependency)]
	[BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class MagniPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.magni";
        public const string ModName = "Magni";
        public const string ModVersion = "1.2.1.0";

		internal static readonly ConfigSync ConfigSync = new ConfigSync(ModId)
		{
			DisplayName = ModName,
			CurrentVersion = ModVersion,
			MinimumRequiredVersion = ModVersion
		};

		public static ConfigEntry<bool> ModRequired;

		public static ConfigEntry<float> CarryCapacityMultiplier;

        private static Harmony sPlayerHarmony;

        private void Awake()
		{
			ModRequired = Config.Bind("ServerSync", nameof(ModRequired), true, "If true on server, clients connecting will be rejected if they do not have the mod. If false, clients may connect without the mod. If true on client, cannot join a server unless it is running the mod. Clients without the mod may cause it to not function reliably for others. It is recommended to keep this true if possible.");
			ConfigSync.ModRequired = ModRequired.Value;
			ModRequired.SettingChanged += ModRequired_SettingChanged;
			ConfigSync.AddConfigEntry(ModRequired, ConfigSyncMode.AlwaysServerControlled);

			CarryCapacityMultiplier = Config.Bind("Weight", nameof(CarryCapacityMultiplier), 2.0f, "Multiplier to apply to max carry weight capacity. Game default = 1.0. Mod default = 2.0. [The value will be enforced on a server.]");
            CarryCapacityMultiplier.SettingChanged += CarryCapacity_SettingChanged;
			ConfigSync.AddConfigEntry(CarryCapacityMultiplier, ConfigSyncMode.AlwaysServerControlled);

			ClampConfig();

            sPlayerHarmony = new Harmony(ModId + "_Player");
            sPlayerHarmony.PatchAll(typeof(Player_Patches));
        }

        private void OnDestroy()
        {
            sPlayerHarmony.UnpatchSelf();
        }

        private static void ClampConfig()
        {
            if (CarryCapacityMultiplier.Value < 0.0f) CarryCapacityMultiplier.Value = 0.0f;
            if (CarryCapacityMultiplier.Value > 1000.0f) CarryCapacityMultiplier.Value = 1000.0f;
        }

		private void ModRequired_SettingChanged(object sender, EventArgs e)
		{
			ConfigSync.ModRequired = ModRequired.Value;
		}

		private void CarryCapacity_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();
        }

        [HarmonyPatch(typeof(Player))]
        private static class Player_Patches
        {
            [HarmonyPatch(nameof(Player.GetMaxCarryWeight)), HarmonyPostfix]
            private static void GetMaxCarryWeight_Postfix(Player __instance, ref float __result)
			{
                __result *= CarryCapacityMultiplier.Value;
			}
        }
    }
}
