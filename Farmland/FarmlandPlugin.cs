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
using UnityEngine;

namespace Farmland
{
	[BepInPlugin(ModId, "Farmland", "1.0.1.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class FarmlandPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.farmland";

        public static ConfigEntry<float> VegetationThreshold;

        private static Harmony sPlayerHarmony;
        private static Harmony sTerrainCompHarmony;

        private void Awake()
        {
            VegetationThreshold = Config.Bind("Land", nameof(VegetationThreshold), 0.0f, "The amount of vegetation land must support to allow cultivation. Lower values provide more farmable land. Range 0.0 to 1.0. Game default 0.25. Mod default 0.0.");
            VegetationThreshold.SettingChanged += VegetationThreshold_SettingChanged;

            ClampConfig();

            sPlayerHarmony = new Harmony(ModId + "_Player");
            sPlayerHarmony.PatchAll(typeof(Player_Patches));

            sTerrainCompHarmony = new Harmony(ModId + "_TerrainComp");
            sTerrainCompHarmony.PatchAll(typeof(TerrainComp_Patches));
        }

        private void OnDestroy()
        {
            sPlayerHarmony.UnpatchSelf();
            sTerrainCompHarmony.UnpatchSelf();
        }

        private static void ClampConfig()
        {
            if (VegetationThreshold.Value < 0.0f) VegetationThreshold.Value = 0.0f;
            if (VegetationThreshold.Value > 1.0f) VegetationThreshold.Value = 1.0f;
        }

        private void VegetationThreshold_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();

            sPlayerHarmony.UnpatchSelf();
            sPlayerHarmony.PatchAll(typeof(Player_Patches));
        }

        [HarmonyPatch(typeof(Player))]
        private static class Player_Patches
        {
            private enum TranspilerState
            {
                Searching,
                Updating,
                Finishing
            }

            // This patch changes the vegetation threshold at which the player can cultivate land.
            [HarmonyPatch("UpdatePlacementGhost"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> UpdatePlacementGhost_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                TranspilerState state = TranspilerState.Searching;

                foreach (CodeInstruction instruction in instructions)
                {
                    switch (state)
                    {
                        case TranspilerState.Searching:
                            if (instruction.opcode == OpCodes.Callvirt)
                            {
                                state = TranspilerState.Updating;
                            }
                            yield return instruction;
                            break;
                        case TranspilerState.Updating:
                            if (instruction.opcode == OpCodes.Ldc_R4 && ((float)instruction.operand) == 0.25f)
                            {
                                yield return new CodeInstruction(OpCodes.Ldc_R4, VegetationThreshold.Value);
                                state = TranspilerState.Finishing;
                            }
                            else
                            {
                                yield return instruction;
                                state = TranspilerState.Searching;
                            }
                            break;
                        case TranspilerState.Finishing:
                            yield return instruction;
                            break;
                    }
                }
            }
        }


        [HarmonyPatch(typeof(TerrainComp))]
        private static class TerrainComp_Patches
        {
            private enum TranspilerState
            {
                Searching,
                Updating,
                Labeling,
                Finishing
            }

            // The purpose of this patch is to update the terrain visuals when cultivating terrain below the default vegetation threshold.
            // Without this, land below the threshold that has been been cultivated will appear unchanged (not cultivated).
            [HarmonyPatch("PaintCleared"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> PaintCleared_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                Label label1 = generator.DefineLabel();

                TranspilerState state = TranspilerState.Searching;

                foreach (CodeInstruction instruction in instructions)
                {
                    switch (state)
                    {
                        case TranspilerState.Searching:
                            if (instruction.opcode == OpCodes.Ldfld && (FieldInfo)instruction.operand == typeof(Color).GetField(nameof(Color.a)))
                            {
                                state = TranspilerState.Updating;
                            }
                            yield return instruction;
                            break;
                        case TranspilerState.Updating:
                            if (instruction.opcode == OpCodes.Stloc_S)
                            {
                                yield return instruction;

                                // If vegetation value (alpha channel) is already greater than 0.25, skip the value coercion
                                yield return new CodeInstruction(OpCodes.Ldloc_S, instruction.operand);
                                yield return new CodeInstruction(OpCodes.Ldc_R4, 0.25f);
                                yield return new CodeInstruction(OpCodes.Bge_Un_S, label1);

                                // If this is not a cultivate operation, skip the value coercion
                                yield return new CodeInstruction(OpCodes.Ldarg_3);
                                yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                                yield return new CodeInstruction(OpCodes.Bne_Un_S, label1);

                                // Vegetation value is less than 0.25, set it to 0.25
                                yield return new CodeInstruction(OpCodes.Ldc_R4, 0.25f);
                                yield return new CodeInstruction(OpCodes.Stloc_S, instruction.operand);

                                state = TranspilerState.Labeling;
                            }
                            else
                            {
                                yield return instruction;
                                state = TranspilerState.Searching;
                            }
                            break;
                        case TranspilerState.Labeling:
                            instruction.labels.Add(label1);
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
