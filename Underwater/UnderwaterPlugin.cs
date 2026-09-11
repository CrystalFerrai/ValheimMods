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
using CrystalLib;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.InputSystem;

namespace Underwater
{
	[BepInPlugin(ModId, ModName, ModVersion)]
	[BepInDependency("_shudnal.ConditionalConfigSync", BepInDependency.DependencyFlags.HardDependency)]
	[BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class UnderwaterPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.underwater";
		public const string ModName = "Underwater";
		public const string ModVersion = "1.2.1.0";

		internal static readonly ConfigSync ConfigSync = new ConfigSync(ModId)
		{
			DisplayName = ModName,
			CurrentVersion = ModVersion,
			MinimumRequiredVersion = ModVersion,
			ModRequired = true
		};

		public static ConfigEntry<bool> ModUseAllowed;
		public static ConfigEntry<bool> PlayerSwims;
        public static ConfigEntry<bool> CameraIgnoreWater;
        public static ConfigEntry<Key> ToggleSwimKey;

        private static CrystalLib.InputBinding sToggleSwimBinding;

		private static Harmony sCharacterHarmony;
        private static Harmony sGameCameraHarmony;

        private void Awake()
        {
			ModUseAllowed = Config.Bind("Underwater", nameof(ModUseAllowed), true, "Whether this mod is allowed to be used. Intended for server owners who wish to disable the mod on their server. [The value will be enforced on a server.]");
			ModUseAllowed.SettingChanged += ModUseAllowed_SettingChanged;
			ConfigSync.AddConfigEntry(ModUseAllowed, ConfigSyncMode.AlwaysServerControlled);

			PlayerSwims = Config.Bind("Underwater", nameof(PlayerSwims), true, "Whether players should swim or ignore water, walking along the terrain beneath it. Can also be toggled by pressing the toggle swim key (default Backspace). Game default true.");
			PlayerSwims.SettingChanged += IgnoreWater_SettingChanged;
			ConfigSync.AddConfigEntry(PlayerSwims, ConfigSyncMode.AlwaysClientControlled);

			CameraIgnoreWater = Config.Bind("Underwater", nameof(CameraIgnoreWater), false, "Whether the camera should ignore water, allowing it to move beneath the surface. This setting is implied true if PlayerSwims is false. Game default false.");
            CameraIgnoreWater.SettingChanged += IgnoreWater_SettingChanged;
			ConfigSync.AddConfigEntry(CameraIgnoreWater, ConfigSyncMode.AlwaysClientControlled);

			ToggleSwimKey = Config.Bind("Underwater", nameof(ToggleSwimKey), Key.Backspace, "Binds a shortcut key for toggling the PlayerSwims option.");
			ConfigSync.AddConfigEntry(ToggleSwimKey, ConfigSyncMode.AlwaysClientControlled);

			sToggleSwimBinding = new CrystalLib.InputBinding("ToggleSwim", ToggleSwimKey);
			sToggleSwimBinding.InputPressed += ToggleSwimBinding_InputPressed;

			sCharacterHarmony = new Harmony(ModId + "_Character");
            sGameCameraHarmony = new Harmony(ModId + "_GameCamera");

            sCharacterHarmony.PatchAll(typeof(Character_Patches));

            if (!PlayerSwims.Value || CameraIgnoreWater.Value)
            {
                sGameCameraHarmony.PatchAll(typeof(GameCamera_Patches));
            }
        }

		private void OnDestroy()
        {
            sCharacterHarmony.UnpatchSelf();
            sGameCameraHarmony.UnpatchSelf();

            sToggleSwimBinding.Dispose();
        }

		private void ModUseAllowed_SettingChanged(object sender, EventArgs e)
		{
			if (ModUseAllowed.Value)
			{
				sCharacterHarmony.PatchAll(typeof(Character_Patches));
				if (!PlayerSwims.Value || CameraIgnoreWater.Value)
				{
					sGameCameraHarmony.PatchAll(typeof(GameCamera_Patches));
				}
			}
			else
			{
				sCharacterHarmony.UnpatchSelf();
				sGameCameraHarmony.UnpatchSelf();
			}
		}

		private void IgnoreWater_SettingChanged(object sender, EventArgs e)
        {
            sGameCameraHarmony.UnpatchSelf();
            if (ModUseAllowed.Value && (!PlayerSwims.Value || CameraIgnoreWater.Value))
            {
                sGameCameraHarmony.PatchAll(typeof(GameCamera_Patches));
            }
        }

        private void ToggleSwimBinding_InputPressed(object sender, InputEventArgs e)
        {
			if (ModUseAllowed.Value)
			{
				PlayerSwims.Value = !PlayerSwims.Value;
				e.Player.Message(MessageHud.MessageType.TopLeft, PlayerSwims.Value ? "Swimming On" : "Swimming Off");
			}
			else
			{
				e.Player.Message(MessageHud.MessageType.TopLeft, "Underwater mod use disallowed by server or local config");
			}
        }

        [HarmonyPatch(typeof(Character))]
        private static class Character_Patches
        {
            [HarmonyPatch("InLiquidDepth"), HarmonyPrefix]
            private static bool InLiquidDepth_Prefix(Character __instance, ref float __result)
            {
                __result = 0.0f;
                return !__instance.IsPlayer() || PlayerSwims.Value;
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

				if (state != TranspilerState.Finishing)
				{
					throw new InvalidOperationException("[Underwater] Failed to apply camera patch");
				}
			}
		}
    }
}
