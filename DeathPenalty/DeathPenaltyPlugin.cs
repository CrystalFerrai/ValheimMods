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
using System.Reflection.Emit;

namespace DeathPenalty
{
    [BepInPlugin(ModId, "Death Penalty", "1.1.4.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class DeathPenaltyPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.deathpenalty";

        public static ConfigEntry<float> SkillLossPercent;
        public static ConfigEntry<bool> ResetLevelProgress;
        public static ConfigEntry<float> MercyEffectDuration;
        public static ConfigEntry<float> SafetyEffectDuration;

        private static Harmony sSkillsLevelHarmony;
        private static Harmony sSkillsAccumulatorHarmony;
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
            SkillLossPercent = Config.Bind("Death", nameof(SkillLossPercent), 5.0f, "The percent loss suffered to the level of all skills when the player dies. Range 0-100. 0 disables skill loss. 50 reduces all skills by half. 100 resets all skills to 0. Game default is 5.");
            SkillLossPercent.SettingChanged += SkillLossPercent_SettingChanged;

            ResetLevelProgress = Config.Bind("Death", nameof(ResetLevelProgress), true, "Whether to reset progress towards the next level for all skills when the player dies. This is independent of the loss of skill levels. Game default is true.");
            ResetLevelProgress.SettingChanged += ResetLevelProgress_SettingChanged;

            MercyEffectDuration = Config.Bind("Death", nameof(MercyEffectDuration), 600.0f, "The duration, in seconds, of the \"No Skill Loss\" status effect that is granted on death which prevents further loss of skills via subsequent deaths. Game default is 600.");
            MercyEffectDuration.SettingChanged += MercyEffectDuration_SettingChanged;

            SafetyEffectDuration = Config.Bind("Death", nameof(SafetyEffectDuration), 50.0f, "The duration, in seconds, of the \"Corpse Run\" status effect that is granted upon looting a tombstone which boosts regen and other stats. Game default is 50.");
            SafetyEffectDuration.SettingChanged += SafetyEffectDuration_SettingChanged;

            ClampConfig();

            sSkillsLevelHarmony = new Harmony(ModId + "_Skills_Level");
            sSkillsAccumulatorHarmony = new Harmony(ModId + "_Skills_Accumulator");
            sPlayerHarmony = new Harmony(ModId + "_Player");
            sTombStoneHarmony = new Harmony(ModId + "_TombStone");

            sSkillsLevelHarmony.PatchAll(typeof(Skills_Level_Patches));
            sPlayerHarmony.PatchAll(typeof(Player_Patches));
            sTombStoneHarmony.PatchAll(typeof(TombStone_Patches));

            if (!ResetLevelProgress.Value)
			{
                sSkillsAccumulatorHarmony.PatchAll(typeof(Skills_Accumulator_Patches));
            }
        }

        private void OnDestroy()
        {
            sSkillsLevelHarmony.UnpatchSelf();
            sSkillsAccumulatorHarmony.UnpatchSelf();
            sPlayerHarmony.UnpatchSelf();
            sTombStoneHarmony.UnpatchSelf();
        }

        private void SkillLossPercent_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();
            foreach (Player player in sPlayers)
            {
                player.GetSkills().m_DeathLowerFactor = SkillLossPercent.Value * 0.01f;
            }
        }

        private void ResetLevelProgress_SettingChanged(object sender, EventArgs e)
        {
            if (ResetLevelProgress.Value)
			{
                sSkillsAccumulatorHarmony.UnpatchSelf();
			}
            else
            {
                sSkillsAccumulatorHarmony.PatchAll(typeof(Skills_Accumulator_Patches));
            }
        }

        private void MercyEffectDuration_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();
            foreach (Player player in sPlayers)
            {
                player.m_hardDeathCooldown = MercyEffectDuration.Value;
            }
        }

        private void SafetyEffectDuration_SettingChanged(object sender, EventArgs e)
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
        private static class Skills_Level_Patches
        {
            [HarmonyPatch("Awake"), HarmonyPostfix]
            private static void Awake_Postfix(Skills __instance)
            {
                __instance.m_DeathLowerFactor = SkillLossPercent.Value * 0.01f;
            }
        }

        [HarmonyPatch(typeof(Skills))]
        private static class Skills_Accumulator_Patches
        {
            private enum TranspilerState
            {
                Searching,
                Updating,
                Finishing
            }

            [HarmonyPatch("LowerAllSkills"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> LowerAllSkills_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                TranspilerState state = TranspilerState.Searching;

                CodeInstruction previousInstruction = null;

                foreach (CodeInstruction instruction in instructions)
				{
					switch (state)
					{
						case TranspilerState.Searching:
                            if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.0f)
                            {
                                previousInstruction = instruction;
                                state = TranspilerState.Updating;
                            }
                            else
							{
                                yield return instruction;
							}
							break;
						case TranspilerState.Updating:
                            if (instruction.opcode == OpCodes.Stfld && ((FieldInfo)instruction.operand).Name == nameof(Skills.Skill.m_accumulator))
							{
                                // Omit the instructions which set m_accumulator to 0
                                state = TranspilerState.Finishing;
							}
                            else
							{
                                yield return previousInstruction;
                                yield return instruction;
                                state = TranspilerState.Searching;
							}
                            previousInstruction = null;
                            break;
						case TranspilerState.Finishing:
                            yield return instruction;
							break;
					}
				}
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
