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
using System.Collections.Generic;

namespace DeathPenalty
{
    [BepInPlugin(ModId, "Death Penalty", "1.0.4.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class DeathPenaltyPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.deathpenalty";

        public static ConfigEntry<float> SkillLossPercent;
        public static ConfigEntry<float> MercyEffectDuration;
        public static ConfigEntry<float> SafetyEffectDuration;

        private static Harmony sSkillsHarmony;
        private static Harmony sPlayerHarmony;
        private static Harmony sTombStoneHarmony;

        private static readonly List<Player> sPlayers;
        private static readonly List<TombStone> sTombStones;

        static DeathPenaltyPlugin()
        {
            sPlayers = new List<Player>();
            sTombStones = new List<TombStone>();
        }

        private void Awake()
        {
            SkillLossPercent = Config.Bind("Death", nameof(SkillLossPercent), 5.0f, "The percent loss suffered to all skills when the player dies. Range 0-100. 0 disables skill loss. 50 reduces all skills by half. 100 resets all skills to 0. Resulting loss is effectively rounded by the game up to the next full level. Game default is 5.");
            SkillLossPercent.SettingChanged += SkillLossPercent_SettingChanged;

            MercyEffectDuration = Config.Bind("Death", nameof(MercyEffectDuration), 600.0f, "The duration, in seconds, of the \"No Skill Loss\" status effect that is granted on death which prevents further loss of skills via subsequent deaths. Game default is 600.");
            MercyEffectDuration.SettingChanged += MercyEffectDuration_SettingChanged;

            SafetyEffectDuration = Config.Bind("Death", nameof(SafetyEffectDuration), 50.0f, "The duration, in seconds, of the \"Corpse Run\" status effect that is granted upon looting a tombstone which boosts regen and other stats. Game default is 50.");
            SafetyEffectDuration.SettingChanged += SafetyEffectDuration_SettingChanged;

            ClampConfig();

            sSkillsHarmony = new Harmony(ModId + "_Skills");
            sPlayerHarmony = new Harmony(ModId + "_Player");
            sTombStoneHarmony = new Harmony(ModId + "_TombStone");

            sSkillsHarmony.PatchAll(typeof(Skills_Patches));
            sPlayerHarmony.PatchAll(typeof(Player_Patches));
            sTombStoneHarmony.PatchAll(typeof(TombStone_Patches));
        }

        private void OnDestroy()
        {
            sSkillsHarmony.UnpatchSelf();
            sPlayerHarmony.UnpatchSelf();
            sTombStoneHarmony.UnpatchSelf();
        }

        private void SkillLossPercent_SettingChanged(object sender, System.EventArgs e)
        {
            ClampConfig();
            foreach (Player player in sPlayers)
            {
                player.GetSkills().m_DeathLowerFactor = SkillLossPercent.Value * 0.01f;
            }
        }

        private void MercyEffectDuration_SettingChanged(object sender, System.EventArgs e)
        {
            ClampConfig();
            foreach (Player player in sPlayers)
            {
                player.m_hardDeathCooldown = MercyEffectDuration.Value;
            }
        }

        private void SafetyEffectDuration_SettingChanged(object sender, System.EventArgs e)
        {
            ClampConfig();
            foreach (TombStone tombstone in sTombStones)
            {
                tombstone.m_lootStatusEffect.m_ttl = SafetyEffectDuration.Value;
            }
        }

        private static void ClampConfig()
        {
            if (SkillLossPercent.Value < 0.0f) SkillLossPercent.Value = 0.0f;
            if (SkillLossPercent.Value > 100.0f) SkillLossPercent.Value = 100.0f;

            if (MercyEffectDuration.Value < 0.0f) MercyEffectDuration.Value = 0.0f;
            if (float.IsPositiveInfinity(MercyEffectDuration.Value)) MercyEffectDuration.Value = float.MaxValue;

            if (SafetyEffectDuration.Value < 0.0f) SafetyEffectDuration.Value = 0.0f;
            if (float.IsPositiveInfinity(SafetyEffectDuration.Value)) SafetyEffectDuration.Value = float.MaxValue;
        }

        [HarmonyPatch(typeof(Skills))]
        private static class Skills_Patches
        {
            [HarmonyPatch("Awake"), HarmonyPostfix]
            private static void Awake_Postfix(Skills __instance)
            {
                __instance.m_DeathLowerFactor = SkillLossPercent.Value * 0.01f;
            }
        }

        [HarmonyPatch(typeof(Player))]
        private static class Player_Patches
        {
            [HarmonyPatch("Awake"), HarmonyPostfix]
            private static void Awake_Postfix(Player __instance)
            {
                __instance.m_hardDeathCooldown = MercyEffectDuration.Value;
                sPlayers.Add(__instance);
            }

            [HarmonyPatch("OnDestroy"), HarmonyPrefix]
            private static void OnDestroy_Prefix(Player __instance)
            {
                sPlayers.Remove(__instance);
            }
        }

        [HarmonyPatch(typeof(TombStone))]
        private static class TombStone_Patches
        {
            [HarmonyPatch("Awake"), HarmonyPostfix]
            private static void Awake_Postfix(TombStone __instance)
            {
                // m_lootStatusEffect is a buffed up version of SE_Stats
                __instance.m_lootStatusEffect.m_ttl = SafetyEffectDuration.Value;
                sTombStones.Add(__instance);
            }

            [HarmonyPatch("UpdateDespawn"), HarmonyPostfix]
            private static void UpdateDespawn_Postfix(TombStone __instance)
            {
                sTombStones.Remove(__instance);
            }
        }
    }
}
