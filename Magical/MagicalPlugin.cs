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

namespace Magical
{
	[BepInPlugin(ModId, "Magical", "1.0.2.0")]
	[BepInProcess("valheim.exe")]
	[BepInProcess("valheim_server.exe")]
	public class MagicalPlugin : BaseUnityPlugin
	{
		public const string ModId = "dev.crystal.magical";

		public static ConfigEntry<float> BaseStamina;
		public static ConfigEntry<float> BaseEitr;
		public static ConfigEntry<float> BaseHealth;

		public static ConfigEntry<float> BaseStaminaRegen;
		public static ConfigEntry<float> BaseEitrRegen;
		public static ConfigEntry<float> BaseHealthRegen;
		public static ConfigEntry<float> StaminaRegenDelay;
		public static ConfigEntry<float> EitrRegenDelay;
		public static ConfigEntry<float> HealthRegenTickRate;

		public static ConfigEntry<float> SkillStaminaReduction;
		public static ConfigEntry<float> SkillEitrReduction;
		public static ConfigEntry<float> SkillHealthReduction;

		private static Harmony sPlayerTrackingHarmony;
		private static Harmony sPlayerHarmony;
		private static Harmony sAttackHarmony;

		private static List<Player> sPlayers;

		static MagicalPlugin()
		{
			sPlayers = new List<Player>();
		}

		private void Awake()
		{
			BaseStamina = Config.Bind("Base", nameof(BaseStamina), 50.0f, "Maximum stamina before any food modifiers are applied. Game default 50.");
			BaseStamina.SettingChanged += PlayerVariable_SettingChanged;

			BaseEitr = Config.Bind("Base", nameof(BaseEitr), 0.0f, "Maximum eitr before any food modifiers are applied. Game default 0.");
			BaseEitr.SettingChanged += PlayerConstant_SettingChanged;

			BaseHealth = Config.Bind("Base", nameof(BaseHealth), 25.0f, "Maximum health before any food modifiers are applied. Game default 25.");
			BaseHealth.SettingChanged += PlayerVariable_SettingChanged;

			BaseStaminaRegen = Config.Bind("Regen", nameof(BaseStaminaRegen), 6.0f, "The base rate of stamina regen per second, before any modifiers are applied. Game default 6.");
			BaseStaminaRegen.SettingChanged += PlayerVariable_SettingChanged;

			BaseEitrRegen = Config.Bind("Regen", nameof(BaseEitrRegen), 2.0f, "The base rate of eitr regen per second, before any modifiers are applied. Game default 2.");
			BaseEitrRegen.SettingChanged += PlayerVariable_SettingChanged;

			BaseHealthRegen = Config.Bind("Regen", nameof(BaseHealthRegen), 0.0f, "The base rate of health regen per health regen tick, before any modifiers are applied. Game default 0.");
			BaseHealthRegen.SettingChanged += PlayerConstant_SettingChanged;

			StaminaRegenDelay = Config.Bind("Regen", nameof(StaminaRegenDelay), 1.0f, "The number of seconds after using stamina before it starts to regenerate. Game default 1.");
			StaminaRegenDelay.SettingChanged += PlayerVariable_SettingChanged;

			EitrRegenDelay = Config.Bind("Regen", nameof(EitrRegenDelay), 1.0f, "The number of seconds after using eitr before it starts to regenerate. Game default 1.");
			EitrRegenDelay.SettingChanged += PlayerVariable_SettingChanged;

			HealthRegenTickRate = Config.Bind("Regen", nameof(HealthRegenTickRate), 10.0f, "The number of seconds between ticks of health regeneration. Game default 10.");
			HealthRegenTickRate.SettingChanged += PlayerConstant_SettingChanged;

			SkillStaminaReduction = Config.Bind("Skill", nameof(SkillStaminaReduction), 0.33f, "Stamina cost reduction multiplier for actions based on player skill. Value represents reduction with 100 skill and will scale down at lower skill levels. Game default 0.33.");
			SkillStaminaReduction.SettingChanged += Attack_SettingChanged;

			SkillEitrReduction = Config.Bind("Skill", nameof(SkillEitrReduction), 0.33f, "Eitr cost reduction multiplier for actions based on player skill. Value represents reduction with 100 skill and will scale down at lower skill levels. Game default 0.33.");
			SkillEitrReduction.SettingChanged += Attack_SettingChanged;

			SkillHealthReduction = Config.Bind("Skill", nameof(SkillHealthReduction), 0.33f, "Health cost reduction multiplier for actions based on player skill. Value represents reduction with 100 skill and will scale down at lower skill levels. Game default 0.33.");
			SkillHealthReduction.SettingChanged += Attack_SettingChanged;

			sPlayerTrackingHarmony = new Harmony(ModId + "_Player_Tracking");
			sPlayerHarmony = new Harmony(ModId + "_Player");
			sAttackHarmony = new Harmony(ModId + "_Attack");

			ClampConfig();

			sPlayerTrackingHarmony.PatchAll(typeof(Player_Tracking_Patches));
			sPlayerHarmony.PatchAll(typeof(Player_Patches));
			sAttackHarmony.PatchAll(typeof(Attack_Patches));
		}

		private void OnDestroy()
		{
			sPlayerTrackingHarmony.UnpatchSelf();
			sPlayerHarmony.UnpatchSelf();
			sAttackHarmony.UnpatchSelf();
			sPlayers.Clear();
		}

		private static void ClampConfig()
		{
			if (BaseStamina.Value < 0.0f) BaseStamina.Value = 0.0f;
			if (BaseStamina.Value > 1000.0f) BaseStamina.Value = 1000.0f;

			if (BaseEitr.Value < 0.0f) BaseEitr.Value = 0.0f;
			if (BaseEitr.Value > 1000.0f) BaseEitr.Value = 1000.0f;

			if (BaseHealth.Value < 0.0f) BaseHealth.Value = 0.0f;
			if (BaseHealth.Value > 1000.0f) BaseHealth.Value = 1000.0f;

			if (BaseStaminaRegen.Value < 0.1f) BaseStaminaRegen.Value = 0.1f;
			if (BaseStaminaRegen.Value > 1000.0f) BaseStaminaRegen.Value = 1000.0f;

			if (BaseEitrRegen.Value < 0.1f) BaseEitrRegen.Value = 0.1f;
			if (BaseEitrRegen.Value > 1000.0f) BaseEitrRegen.Value = 1000.0f;

			if (BaseHealthRegen.Value < 0.0f) BaseHealthRegen.Value = 0.0f;
			if (BaseHealthRegen.Value > 1000.0f) BaseHealthRegen.Value = 1000.0f;

			if (StaminaRegenDelay.Value < 0.1f) StaminaRegenDelay.Value = 0.1f;
			if (StaminaRegenDelay.Value > 3600.0f) StaminaRegenDelay.Value = 3600.0f;

			if (EitrRegenDelay.Value < 0.1f) EitrRegenDelay.Value = 0.1f;
			if (EitrRegenDelay.Value > 3600.0f) EitrRegenDelay.Value = 3600.0f;

			if (HealthRegenTickRate.Value < 0.1f) HealthRegenTickRate.Value = 0.1f;
			if (HealthRegenTickRate.Value > 3600.0f) HealthRegenTickRate.Value = 3600.0f;

			if (SkillStaminaReduction.Value < 0.0f) SkillStaminaReduction.Value = 0.0f;
			if (SkillStaminaReduction.Value > 1.0f) SkillStaminaReduction.Value = 1.0f;

			if (SkillEitrReduction.Value < 0.0f) SkillEitrReduction.Value = 0.0f;
			if (SkillEitrReduction.Value > 1.0f) SkillEitrReduction.Value = 1.0f;

			if (SkillHealthReduction.Value < 0.0f) SkillHealthReduction.Value = 0.0f;
			if (SkillHealthReduction.Value > 1.0f) SkillHealthReduction.Value = 1.0f;
		}

		private void PlayerVariable_SettingChanged(object sender, EventArgs e)
		{
			ClampConfig();

			foreach (Player player in sPlayers)
			{
				SetPlayerValues(player);
			}
		}

		private void Attack_SettingChanged(object sender, EventArgs e)
		{
			ClampConfig();

			sAttackHarmony.UnpatchSelf();
			sAttackHarmony.PatchAll(typeof(Attack_Patches));
		}

		private void PlayerConstant_SettingChanged(object sender, EventArgs e)
		{
			ClampConfig();

			sPlayerHarmony.UnpatchSelf();
			sPlayerHarmony.PatchAll(typeof(Player_Patches));
		}

		private static void SetPlayerValues(Player player)
		{
			player.m_baseStamina = BaseStamina.Value;
			player.m_baseHP = BaseHealth.Value;
			player.m_staminaRegen = BaseStaminaRegen.Value;
			player.m_eiterRegen = BaseEitrRegen.Value;
			player.m_staminaRegenDelay = StaminaRegenDelay.Value;
			player.m_eitrRegenDelay = EitrRegenDelay.Value;
		}

		[HarmonyPatch(typeof(Player))]
		private static class Player_Tracking_Patches
		{
			[HarmonyPatch("Awake"), HarmonyPostfix]
			private static void Awake_Prefix(Player __instance)
			{
				SetPlayerValues(__instance);
				sPlayers.Add(__instance);
			}

			[HarmonyPatch("OnDestroy"), HarmonyPrefix]
			private static void OnDestroy_Prefix(Player __instance)
			{
				sPlayers.Remove(__instance);
			}
		}

		[HarmonyPatch(typeof(Player))]
		private static class Player_Patches
		{
			private enum TranspilerState
			{
				Searching,
				Checking,
				Replacing,
				Searching2,
				Checking2,
				Replacing2,
				Finishing
			}

			[HarmonyPatch("UpdateFood"), HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> UpdateFood_Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				TranspilerState state = TranspilerState.Searching;

				CodeInstruction previousInstruction = null;

				foreach (CodeInstruction instruction in instructions)
				{
					switch (state)
					{
						case TranspilerState.Searching:
							if (instruction.opcode == OpCodes.Ldfld && ((FieldInfo)instruction.operand).Name.Equals("m_foodRegenTimer"))
							{
								state = TranspilerState.Checking;
							}
							yield return instruction;
							break;
						case TranspilerState.Checking:
							if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 10.0f)
							{
								state = TranspilerState.Replacing;
							}
							else
							{
								yield return instruction;
								state = TranspilerState.Searching;
							}
							break;
						case TranspilerState.Replacing:
							yield return new CodeInstruction(OpCodes.Ldc_R4, HealthRegenTickRate.Value);
							yield return instruction;
							state = TranspilerState.Searching2;
							break;
						case TranspilerState.Searching2:
							if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.0f)
							{
								previousInstruction = instruction;
								state = TranspilerState.Checking2;
							}
							else
							{
								yield return instruction;
							}
							break;
						case TranspilerState.Checking2:
							if (instruction.opcode == OpCodes.Stloc_S)
							{
								previousInstruction = instruction;
								state = TranspilerState.Replacing2;
							}
							else
							{
								yield return previousInstruction;
								yield return instruction;
								state = TranspilerState.Searching2;
							}
							break;
						case TranspilerState.Replacing2:
							yield return new CodeInstruction(OpCodes.Ldc_R4, BaseHealthRegen.Value);
							yield return previousInstruction;
							yield return instruction;
							state = TranspilerState.Finishing;
							break;
						case TranspilerState.Finishing:
							yield return instruction;
							break;
					}
				}
			}

			[HarmonyPatch("GetTotalFoodValue"), HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> GetTotalFoodValue_Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				TranspilerState state = TranspilerState.Searching;

				foreach (CodeInstruction instruction in instructions)
				{
					switch (state)
					{
						case TranspilerState.Searching:
							if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.0f)
							{
								state = TranspilerState.Replacing;
							}
							else
							{
								yield return instruction;
							}
							break;
						case TranspilerState.Replacing:
							yield return new CodeInstruction(OpCodes.Ldc_R4, BaseEitr.Value);
							yield return instruction;
							state = TranspilerState.Finishing;
							break;
						case TranspilerState.Finishing:
							yield return instruction;
							break;
					}
				}
			}
		}

		[HarmonyPatch(typeof(Attack))]
		private static class Attack_Patches
		{
			private enum TranspilerState
			{
				Searching,
				Replacing,
				Finishing
			}

			[HarmonyPatch("GetAttackStamina"), HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> GetAttackStamina_Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				return ReplaceSkillModifier(instructions, SkillStaminaReduction.Value);
			}

			[HarmonyPatch("GetAttackEitr"), HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> GetAttackEitr_Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				return ReplaceSkillModifier(instructions, SkillEitrReduction.Value);
			}

			[HarmonyPatch("GetAttackHealth"), HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> GetAttackHealth_Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				return ReplaceSkillModifier(instructions, SkillHealthReduction.Value);
			}

			private static IEnumerable<CodeInstruction> ReplaceSkillModifier(IEnumerable<CodeInstruction> instructions, float newModifier)
			{
				TranspilerState state = TranspilerState.Searching;

				foreach (CodeInstruction instruction in instructions)
				{
					switch (state)
					{
						case TranspilerState.Searching:
							if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 0.33f)
							{
								state = TranspilerState.Replacing;
							}
							else
							{
								yield return instruction;
							}
							break;
						case TranspilerState.Replacing:
							yield return new CodeInstruction(OpCodes.Ldc_R4, newModifier);
							yield return instruction;
							state = TranspilerState.Finishing;
							break;
						case TranspilerState.Finishing:
							yield return instruction;
							break;
					}
				}
			}
		}
	}
}
