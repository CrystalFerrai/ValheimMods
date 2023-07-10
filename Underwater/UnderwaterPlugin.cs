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
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Underwater
{
	[BepInPlugin(ModId, "Underwater", "1.0.0.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class UnderwaterPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.underwater";

        public static ConfigEntry<bool> PlayerIgnoreWater;
        public static ConfigEntry<bool> CameraIgnoreWater;

        private static Harmony sCharacterHarmony;
        private static Harmony sGameCameraHarmony;
        private static Harmony sZInputHarmony;
        private static Harmony sPlayerControllerHarmony;

        private static readonly MethodInfo sTakeInputMethod;
        private static readonly FieldInfo sCharacterField;

        static UnderwaterPlugin()
		{
            sTakeInputMethod = typeof(PlayerController).GetMethod("TakeInput", BindingFlags.NonPublic | BindingFlags.Instance);
            sCharacterField = typeof(PlayerController).GetField("m_character", BindingFlags.NonPublic | BindingFlags.Instance);
		}

        private void Awake()
        {
            PlayerIgnoreWater = Config.Bind("Underwater", nameof(PlayerIgnoreWater), false, "Whether players should ignore water, walking along the terrain beneath it. Can also be toggled by pressing Backspace. Game default false.");
			PlayerIgnoreWater.SettingChanged += IgnoreWater_SettingChanged;

            CameraIgnoreWater = Config.Bind("Underwater", nameof(CameraIgnoreWater), false, "Whether the camera should ignore water, allowing it to move beneath the surface. This setting is implied true if PlayerIgnoreWater is true. Game default false.");
            CameraIgnoreWater.SettingChanged += IgnoreWater_SettingChanged;

            sCharacterHarmony = new Harmony(ModId + "_Character");
            sGameCameraHarmony = new Harmony(ModId + "_GameCamera");
            sZInputHarmony = new Harmony(ModId + "_ZInput");
            sPlayerControllerHarmony = new Harmony(ModId + "_PlayerController");

            sCharacterHarmony.PatchAll(typeof(Character_Patches));
            sZInputHarmony.PatchAll(typeof(ZInput_Patches));
            sPlayerControllerHarmony.PatchAll(typeof(PlayerController_Patches));

            if (PlayerIgnoreWater.Value || CameraIgnoreWater.Value)
            {
                sGameCameraHarmony.PatchAll(typeof(GameCamera_Patches));
            }
        }

		private void OnDestroy()
        {
            sCharacterHarmony.UnpatchSelf();
            sGameCameraHarmony.UnpatchSelf();
            sZInputHarmony.UnpatchSelf();
            sPlayerControllerHarmony.UnpatchSelf();
        }

        private void IgnoreWater_SettingChanged(object sender, System.EventArgs e)
        {
            if (PlayerIgnoreWater.Value || CameraIgnoreWater.Value)
            {
                sGameCameraHarmony.PatchAll(typeof(GameCamera_Patches));
            }
            else
            {
                sGameCameraHarmony.UnpatchSelf();
            }
        }

        [HarmonyPatch(typeof(Character))]
        private static class Character_Patches
        {
            [HarmonyPatch("InLiquidDepth"), HarmonyPrefix]
            private static bool InLiquidDepth_Prefix(Character __instance, ref float __result)
            {
                __result = 0.0f;
                return !__instance.IsPlayer() || !PlayerIgnoreWater.Value;
            }
        }

        [HarmonyPatch(typeof(GameCamera))]
        private static class GameCamera_Patches
        {
            private enum TranspilerState
            {
                Searching,
                Checking,
                Searching2,
                Checking2,
                Updating,
                Finishing
            }

            [HarmonyPatch("GetCameraPosition"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> GetCameraPosition_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // This transpiler removes code that prevents the game camera from going underwater

                TranspilerState state = TranspilerState.Searching;

                CodeInstruction instruction1 = null;
                CodeInstruction instruction2 = null;
                CodeInstruction instruction3 = null;

                foreach (CodeInstruction instruction in instructions)
				{
					switch (state)
					{
						case TranspilerState.Searching:
                            if (instruction.opcode == OpCodes.Ldloc_S)
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
                            if (instruction3 != null && instruction.opcode == OpCodes.Call && ((MethodInfo)instruction.operand).Name.Equals("GetLiquidLevel"))
							{
                                instruction1 = instruction2 = instruction3 = null;
                                state = TranspilerState.Searching2;
							}
                            else if (instruction2 != null && instruction.opcode == OpCodes.Ldc_I4_S && (sbyte)instruction.operand == 10)
							{
                                instruction3 = instruction;
							}
                            else if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 1.0f)
							{
                                instruction2 = instruction;
							}
                            else
							{
                                yield return instruction1; instruction1 = null;
                                if (instruction2 != null) { yield return instruction2; instruction2 = null; }
                                if (instruction3 != null) { yield return instruction3; instruction3 = null; }
                                yield return instruction;
                                state = TranspilerState.Searching;
							}
							break;
                        case TranspilerState.Searching2:
                            if (instruction.opcode == OpCodes.Ldarg_0)
							{
                                instruction1 = instruction;
                                state = TranspilerState.Checking2;
							}
                            break;
                        case TranspilerState.Checking2:
                            if (instruction.opcode == OpCodes.Ldc_I4_0)
							{
                                instruction2 = instruction;
                                state = TranspilerState.Updating;
							}
                            else
							{
                                instruction1 = null;
                                state = TranspilerState.Searching2;
							}
                            break;
                        case TranspilerState.Updating:
                            yield return instruction1;
                            yield return instruction2;
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

        [HarmonyPatch(typeof(PlayerController))]
        private static class PlayerController_Patches
        {
            [HarmonyPatch("FixedUpdate"), HarmonyPostfix]
            private static void FixedUpdate_Postfix(PlayerController __instance)
            {
                if ((bool)sTakeInputMethod.Invoke(__instance, null))
				{
                    if (ZInput.GetButtonDown("ToggleSwim"))
					{
                        PlayerIgnoreWater.Value = !PlayerIgnoreWater.Value;

                        Player player = (Player)sCharacterField.GetValue(__instance);
                        player.Message(MessageHud.MessageType.TopLeft, PlayerIgnoreWater.Value ? "Swimming Off" : "Swimming On");
                    }
				}
            }
        }

        [HarmonyPatch(typeof(ZInput))]
        private static class ZInput_Patches
        {
            [HarmonyPatch("Reset"), HarmonyPostfix]
            private static void Reset_Postfix(ZInput __instance)
            {
                __instance.AddButton("ToggleSwim", KeyCode.Backspace);
            }
        }
    }
}
