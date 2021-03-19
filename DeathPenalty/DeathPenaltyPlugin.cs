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

namespace DeathPenalty
{
    [BepInPlugin("dev.crystal.deathpenalty", "Death Penalty", "1.0.1.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class DeathPenaltyPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> SkillLossPercent;
        public static ConfigEntry<float> MercyEffectDuration;
        public static ConfigEntry<float> SafetyEffectDuration;

        private void Awake()
        {
            SkillLossPercent = Config.Bind("Death", nameof(SkillLossPercent), 5.0f, "The percent loss suffered to all skills when the player dies. Range 0-100. 0 disables skill loss. 50 reduces all skills by half. 100 resets all skills to 0. Resulting loss is effectively rounded by the game up to the next full level. Game default is 5.");
            if (SkillLossPercent.Value < 0.0f) SkillLossPercent.Value = 0.0f;
            if (SkillLossPercent.Value > 100.0f) SkillLossPercent.Value = 100.0f;

            MercyEffectDuration = Config.Bind("Death", nameof(MercyEffectDuration), 600.0f, "The duration, in seconds, of the \"No Skill Loss\" status effect that is granted on death which prevents further loss of skills via subsequent deaths. Game default is 600.");
            if (MercyEffectDuration.Value < 0.0f) MercyEffectDuration.Value = 0.0f;
            if (float.IsPositiveInfinity(MercyEffectDuration.Value)) MercyEffectDuration.Value = float.MaxValue;

            SafetyEffectDuration = Config.Bind("Death", nameof(SafetyEffectDuration), 50.0f, "The duration, in seconds, of the \"Corpse Run\" status effect that is granted upon looting a tombstone which boosts regen and other stats. Game default is 50.");
            if (SafetyEffectDuration.Value < 0.0f) SafetyEffectDuration.Value = 0.0f;
            if (float.IsPositiveInfinity(SafetyEffectDuration.Value)) SafetyEffectDuration.Value = float.MaxValue;

            Harmony.CreateAndPatchAll(typeof(Skills_Awake_Patch));
            Harmony.CreateAndPatchAll(typeof(Player_Awake_Patch));
            Harmony.CreateAndPatchAll(typeof(Tombstone_Awake_Patch));
        }

        [HarmonyPatch(typeof(Skills), "Awake")]
        private static class Skills_Awake_Patch
        {
            private static void Postfix(Skills __instance)
            {
                __instance.m_DeathLowerFactor = SkillLossPercent.Value * 0.01f;
            }
        }

        [HarmonyPatch(typeof(Player), "Awake")]
        private static class Player_Awake_Patch
        {
            private static void Postfix(Player __instance)
            {
                __instance.m_hardDeathCooldown = MercyEffectDuration.Value;
            }
        }

        [HarmonyPatch(typeof(TombStone), "Awake")]
        private static class Tombstone_Awake_Patch
        {
            private static void Postfix(TombStone __instance)
            {
                // m_lootStatusEffect is a buffed up version of SE_Stats
                __instance.m_lootStatusEffect.m_ttl = SafetyEffectDuration.Value;
            }
        }
    }
}
