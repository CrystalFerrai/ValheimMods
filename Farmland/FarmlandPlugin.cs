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
using System.Reflection.Emit;
using UnityEngine;

namespace Farmland
{
	[BepInPlugin(ModId, ModName, ModVersion)]
	[BepInDependency("_shudnal.ConditionalConfigSync", BepInDependency.DependencyFlags.HardDependency)]
	[BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class FarmlandPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.farmland";
        public const string ModName = "Farmland";
        public const string ModVersion = "1.2.1.0";

		internal static readonly ConfigSync ConfigSync = new ConfigSync(ModId)
		{
			DisplayName = ModName,
			CurrentVersion = ModVersion,
			MinimumRequiredVersion = ModVersion
		};

		public static ConfigEntry<bool> ModRequired;

		public static ConfigEntry<float> VegetationThreshold;

        private static Harmony sPlayerHarmony;
        private static Harmony sTerrainCompHarmony;

        private void Awake()
		{
			ModRequired = Config.Bind("ServerSync", nameof(ModRequired), true, "If true on server, clients connecting will be rejected if they do not have the mod. If false, clients may connect without the mod. If true on client, cannot join a server unless it is running the mod. Clients without the mod may cause it to not function reliably for others. It is recommended to keep this true if possible.");
			ConfigSync.ModRequired = ModRequired.Value;
			ModRequired.SettingChanged += ModRequired_SettingChanged;
			ConfigSync.AddConfigEntry(ModRequired, ConfigSyncMode.AlwaysServerControlled);

			VegetationThreshold = Config.Bind("Land", nameof(VegetationThreshold), 0.0f, "The amount of vegetation land must support to allow cultivation. Lower values provide more farmable land. Range 0.0 to 1.0. Game default 0.25. Mod default 0.0. [The value will be enforced on a server.]");
            VegetationThreshold.SettingChanged += VegetationThreshold_SettingChanged;
			ConfigSync.AddConfigEntry(VegetationThreshold, ConfigSyncMode.AlwaysServerControlled);

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

		private void ModRequired_SettingChanged(object sender, EventArgs e)
		{
			ConfigSync.ModRequired = ModRequired.Value;
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
            // This patch changes the vegetation threshold at which the player can cultivate land.
            [HarmonyPatch("UpdatePlacementGhost"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> UpdatePlacementGhost_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool found = false;
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ldc_R4 && ((float)instruction.operand) == 0.25f)
                    {
                        if (found)
                        {
                            Debug.LogWarning("[Farmland] Found multiple possible candidates for vegetation threshold in Player.UpdatePlacementGhost. Mod may need to be updated.");
						}
                        found = true;
                        yield return new CodeInstruction(OpCodes.Ldc_R4, VegetationThreshold.Value);
                    }
                    else
                    {
                        yield return instruction;
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

                MethodInfo findHeightMap = typeof(Heightmap).GetMethod(nameof(Heightmap.FindHeightmap), BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Vector3) }, null);
                MethodInfo getBiome = typeof(Heightmap).GetMethod(nameof(Heightmap.GetBiome), BindingFlags.Instance | BindingFlags.Public);

				FieldInfo paintTypeField = typeof(TerrainOp.Settings).GetField(nameof(TerrainOp.Settings.m_paintType));

				LocalBuilder isAshlands = generator.DeclareLocal(typeof(bool));
                isAshlands.SetLocalSymInfo(nameof(isAshlands));

                // Check if within the Ashlands biome
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Call, findHeightMap);
				yield return new CodeInstruction(OpCodes.Ldarg_1);
				yield return new CodeInstruction(OpCodes.Ldc_R4, 0.02f);
				yield return new CodeInstruction(OpCodes.Ldc_I4_0);
				yield return new CodeInstruction(OpCodes.Callvirt, getBiome);
				yield return new CodeInstruction(OpCodes.Ldc_I4_S, 32); // 32 = Biome.AshLands
				yield return new CodeInstruction(OpCodes.Ceq);
				yield return new CodeInstruction(OpCodes.Stloc, isAshlands.LocalIndex);

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

                                // If biome is Ashlands, skip value coercion
                                yield return new CodeInstruction(OpCodes.Ldloc, isAshlands.LocalIndex);
                                yield return new CodeInstruction(OpCodes.Brtrue, label1);

                                // If vegetation value (alpha channel) is already greater than 0.25, skip the value coercion
                                yield return new CodeInstruction(OpCodes.Ldloc_S, instruction.operand);
                                yield return new CodeInstruction(OpCodes.Ldc_R4, 0.25f);
                                yield return new CodeInstruction(OpCodes.Bge_Un_S, label1);

                                // If this is not a cultivate operation, skip the value coercion
                                yield return new CodeInstruction(OpCodes.Ldarg_3);
                                yield return new CodeInstruction(OpCodes.Ldfld, paintTypeField);
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

                if (state != TranspilerState.Finishing)
				{
					throw new InvalidOperationException("[Farmland] Unable to patch code. This may be due to a game update that the mod has not yet updated for. Details: Invalid patch state");
				}
            }
		}
    }
}
