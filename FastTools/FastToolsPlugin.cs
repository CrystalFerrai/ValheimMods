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

namespace FastTools
{
	[BepInPlugin(ModId, "Fast Tools", "1.2.2.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class FastToolsPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.fasttools";

        public static ConfigEntry<float> PlaceDelay;
        public static ConfigEntry<float> RemoveDelay;
        public static ConfigEntry<float> StaminaUseMultiplier;

        private static Harmony sPlayerHarmony;
        private static Harmony sPlayerPlacementHarmony;

        private static readonly List<Player> sPlayers;

        static FastToolsPlugin()
        {
            sPlayers = new List<Player>();
        }

        private void Awake()
        {
            PlaceDelay = Config.Bind("Tools", nameof(PlaceDelay), 0.25f, "The delay time for placing items, in seconds. Allowed range 0-10. Game default is 0.4.");
            PlaceDelay.SettingChanged += Delay_SettingChanged;

            RemoveDelay = Config.Bind("Tools", nameof(RemoveDelay), 0.15f, "The delay time for removing items, in seconds. Allowed range 0-10. Game default is 0.25.");
            RemoveDelay.SettingChanged += Delay_SettingChanged;

			StaminaUseMultiplier = Config.Bind("Tools", nameof(StaminaUseMultiplier), 1.0f, "Multiplier to apply to the stamina cost of using a placement tool (hammer, hoe, cultivator). Game default is 1.0.");
			StaminaUseMultiplier.SettingChanged += StaminaUseMultiplier_SettingChanged;

			ClampConfig();

            sPlayerHarmony = new Harmony(ModId + "_Player");
            sPlayerPlacementHarmony = new Harmony(ModId + "_Player_Placement");

            sPlayerHarmony.PatchAll(typeof(Player_Patches));
            sPlayerPlacementHarmony.PatchAll(typeof(Player_Placement_Patches));
        }

		private void OnDestroy()
        {
            sPlayerHarmony.UnpatchSelf();
            sPlayerPlacementHarmony.UnpatchSelf();
            sPlayers.Clear();
        }

        private static void ClampConfig()
        {
            // There is no feedback when delay is active aside from tools simply not working, so don't allow really long delays.

            if (PlaceDelay.Value < 0.0f) PlaceDelay.Value = 0.0f;
            if (PlaceDelay.Value > 10.0f) PlaceDelay.Value = 10.0f;

            if (RemoveDelay.Value < 0.0f) RemoveDelay.Value = 0.0f;
            if (RemoveDelay.Value > 10.0f) RemoveDelay.Value = 10.0f;

			if (StaminaUseMultiplier.Value < 0.0f) StaminaUseMultiplier.Value = 0.0f;
			if (StaminaUseMultiplier.Value > 10.0f) StaminaUseMultiplier.Value = 10.0f;
		}

        private void Delay_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();
            foreach (Player player in sPlayers)
            {
                player.m_placeDelay = PlaceDelay.Value;
                player.m_removeDelay = RemoveDelay.Value;
            }
        }

		private void StaminaUseMultiplier_SettingChanged(object sender, EventArgs e)
		{
			sPlayerPlacementHarmony.UnpatchSelf();
			sPlayerPlacementHarmony.PatchAll(typeof(Player_Placement_Patches));
		}

		[HarmonyPatch(typeof(Player))]
        private static class Player_Patches
        {
            [HarmonyPatch("Awake"), HarmonyPostfix]
            private static void Awake_Postfix(Player __instance)
            {
                __instance.m_placeDelay = PlaceDelay.Value;
                __instance.m_removeDelay = RemoveDelay.Value;
                sPlayers.Add(__instance);
            }

            [HarmonyPatch("OnDestroy"), HarmonyPrefix]
            private static void OnDestroy_Prefix(Player __instance)
            {
                sPlayers.Remove(__instance);
            }
		}

		[HarmonyPatch(typeof(Player))]
		private static class Player_Placement_Patches
		{
			private enum TranspilerState
			{
				Searching,
                Calculating,
                Searching2,
                Checking,
                Checking2,
                Checking3,
				Replacing
			}

			[HarmonyPatch("UpdatePlacement"), HarmonyTranspiler]
			private static IEnumerable<CodeInstruction> UpdatePlacement_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
			{
				LocalBuilder stamina = generator.DeclareLocal(typeof(float));
				stamina.SetLocalSymInfo(nameof(stamina));

				yield return new CodeInstruction(OpCodes.Ldc_R4, 0.0f);
				yield return new CodeInstruction(OpCodes.Stloc, stamina.LocalIndex);

                FieldInfo sharedField = typeof(ItemDrop.ItemData).GetField(nameof(ItemDrop.ItemData.m_shared));
                FieldInfo attackField = typeof(ItemDrop.ItemData.SharedData).GetField(nameof(ItemDrop.ItemData.SharedData.m_attack));
                FieldInfo attackStaminaField = typeof(Attack).GetField(nameof(Attack.m_attackStamina));

				TranspilerState state = TranspilerState.Searching;

                CodeInstruction instruction1 = null;
                CodeInstruction instruction2 = null;
                CodeInstruction instruction3 = null;

				foreach (CodeInstruction instruction in instructions)
				{
					switch (state)
					{
						case TranspilerState.Searching:
                            if (instruction.opcode == OpCodes.Call && ((MethodInfo)instruction.operand).Name.Equals("GetRightItem"))
                            {
                                state = TranspilerState.Calculating;
							}
							yield return instruction;
							break;

                        case TranspilerState.Calculating:
                            yield return instruction; // stloc.0

                            // Get the stamina cost of the tool, multiply it by the mod's multiplier, store the result in 'stamina' local variable.
                            yield return new CodeInstruction(OpCodes.Ldloc_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, sharedField);
                            yield return new CodeInstruction(OpCodes.Ldfld, attackField);
                            yield return new CodeInstruction(OpCodes.Ldfld, attackStaminaField);
                            yield return new CodeInstruction(OpCodes.Ldc_R4, StaminaUseMultiplier.Value);
                            yield return new CodeInstruction(OpCodes.Mul);
                            yield return new CodeInstruction(OpCodes.Stloc, stamina.LocalIndex);

                            state = TranspilerState.Searching2;
                            break;

                        case TranspilerState.Searching2:
                            if (instruction.opcode == OpCodes.Ldloc_0)
                            {
                                instruction1 = instruction;
                                state = TranspilerState.Checking;
                            }
                            else
                            {
                                yield return instruction;
                            }
                            break;

                        case TranspilerState.Checking:
                            if (instruction.opcode == OpCodes.Ldfld && ((FieldInfo)instruction.operand).Name == sharedField.Name)
                            {
                                instruction2 = instruction;
                                state = TranspilerState.Checking2;
                            }
                            else
							{
								yield return instruction1;
								yield return instruction;
								state = TranspilerState.Searching2;
                            }
                            break;

                        case TranspilerState.Checking2:
							if (instruction.opcode == OpCodes.Ldfld && ((FieldInfo)instruction.operand).Name == attackField.Name)
                            {
                                instruction3 = instruction;
                                state = TranspilerState.Checking3;
							}
							else
							{
                                yield return instruction1;
                                yield return instruction2;
                                yield return instruction;
								state = TranspilerState.Searching2;
							}
							break;

                        case TranspilerState.Checking3:
							if (instruction.opcode == OpCodes.Ldfld && ((FieldInfo)instruction.operand).Name == attackStaminaField.Name)
							{
								state = TranspilerState.Replacing;
							}
							else
							{
								yield return instruction1;
								yield return instruction2;
								yield return instruction3;
								yield return instruction;
								state = TranspilerState.Searching2;
							}
							break;

						case TranspilerState.Replacing:
							// We found a reference to rightItem.m_shared.m_attack.m_attackStamina. Replace it with our 'stamina' variable
							yield return new CodeInstruction(OpCodes.Ldloc, stamina.LocalIndex);
                            yield return instruction;

                            // Keep searching. There is more than one occurrence.
                            state = TranspilerState.Searching2;
							break;
					}
				}
			}
		}
	}
}
