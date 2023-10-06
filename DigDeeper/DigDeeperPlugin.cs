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
using System.Reflection.Emit;

namespace DigDeeper
{
    [BepInPlugin(ModId, "Dig Deeper", "1.1.4.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class DigDeeperPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.digdeeper";

        public static ConfigEntry<float> MaximumDepth;
        public static ConfigEntry<float> MaximumHeight;

        private static Harmony sHeightmapHarmony;
        private static Harmony sTerrainCompHarmony;

        private void Awake()
        {
            MaximumDepth = Config.Bind("Digging", nameof(MaximumDepth), 20.0f, "The maximum depth you can dig below the terrain surface. Range 0-128. Game default is 8.");
            MaximumDepth.SettingChanged += Config_SettingChanged;

            MaximumHeight = Config.Bind("Digging", nameof(MaximumHeight), 8.0f, "The maximum height you can raise the terrain. Range 0-128. Game default is 8.");
            MaximumHeight.SettingChanged += Config_SettingChanged;

            ClampConfig();

            sHeightmapHarmony = new Harmony(ModId + "_Heightmap");
            sHeightmapHarmony.PatchAll(typeof(Heightmap_Patches));

            sTerrainCompHarmony = new Harmony(ModId + "_TerrainComp");
            sTerrainCompHarmony.PatchAll(typeof(TerrainComp_Patches));
        }

        private void OnDestroy()
        {
            sHeightmapHarmony.UnpatchSelf();
        }

        private static void ClampConfig()
        {
            if (MaximumDepth.Value < 0.0f) MaximumDepth.Value = 0.0f;
            if (MaximumDepth.Value > 128.0f) MaximumDepth.Value = 128.0f;

            if (MaximumHeight.Value < 0.0f) MaximumHeight.Value = 0.0f;
            if (MaximumHeight.Value > 128.0f) MaximumHeight.Value = 128.0f;
        }

        private static void Config_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();

            sHeightmapHarmony.UnpatchSelf();
            sHeightmapHarmony.PatchAll(typeof(Heightmap_Patches));

            sTerrainCompHarmony.UnpatchSelf();
            sTerrainCompHarmony.PatchAll(typeof(TerrainComp_Patches));
        }

        // NOTE: This patch is for the old terrain system and can probably be removed soon.
        [HarmonyPatch(typeof(Heightmap))]
        private static class Heightmap_Patches
        {
            private enum TranspilerState
            {
                Searching,
                Replacing
            }

            [HarmonyPatch("LevelTerrain"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> LevelTerrain_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                TranspilerState state = TranspilerState.Searching;

                CodeInstruction valueInstruction = null;

                foreach (CodeInstruction instruction in instructions)
                {
                    switch (state)
                    {
                        case TranspilerState.Searching:
                            if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 8.0f)
                            {
                                valueInstruction = instruction;
                                state = TranspilerState.Replacing;
                            }
                            else
                            {
                                yield return instruction;
                            }
                            break;
                        case TranspilerState.Replacing:
                            if (instruction.opcode == OpCodes.Sub)
                            {
                                valueInstruction.operand = MaximumDepth.Value;
                            }
                            else if (instruction.opcode == OpCodes.Add)
                            {
                                valueInstruction.operand = MaximumHeight.Value;
                            }
                            yield return valueInstruction;
                            yield return instruction;
                            valueInstruction = null;
                            state = TranspilerState.Searching;
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
                Replacing
            }

            [HarmonyPatch(nameof(TerrainComp.ApplyToHeightmap)), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> ApplyToHeightmap_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                TranspilerState state = TranspilerState.Searching;

                CodeInstruction valueInstruction = null;

                foreach (CodeInstruction instruction in instructions)
                {
                    switch (state)
                    {
                        case TranspilerState.Searching:
                            if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 8.0f)
                            {
                                valueInstruction = instruction;
                                state = TranspilerState.Replacing;
                            }
                            else
                            {
                                yield return instruction;
                            }
                            break;
                        case TranspilerState.Replacing:
                            if (instruction.opcode == OpCodes.Sub)
                            {
                                valueInstruction.operand = MaximumDepth.Value;
                            }
                            else if (instruction.opcode == OpCodes.Add)
                            {
                                valueInstruction.operand = MaximumHeight.Value;
                            }
                            yield return valueInstruction;
                            yield return instruction;
                            valueInstruction = null;
                            state = TranspilerState.Searching;
                            break;
                    }
                }
            }

            [HarmonyPatch("LevelTerrain"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> LevelTerrain_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ldc_R4)
                    {
                        if ((float)instruction.operand == 8.0f)
                        {
                            instruction.operand = MaximumHeight.Value;
                        }
                        else if ((float)instruction.operand == -8.0f)
                        {
                            instruction.operand = -MaximumDepth.Value;
                        }
                    }
                    yield return instruction;
                }
            }

            [HarmonyPatch("RaiseTerrain"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> RaiseTerrain_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ldc_R4)
                    {
                        if ((float)instruction.operand == 8.0f)
                        {
                            instruction.operand = MaximumHeight.Value;
                        }
                        else if ((float)instruction.operand == -8.0f)
                        {
                            instruction.operand = -MaximumDepth.Value;
                        }
                    }
                    yield return instruction;
                }
            }
        }
    }
}
