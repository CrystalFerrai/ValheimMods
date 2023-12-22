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

namespace Comfortable
{
	[BepInPlugin(ModId, "Comfortable", "1.0.3.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class ComfortablePlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.comfortable";

        public static ConfigEntry<float> BaseRestTime;
        public static ConfigEntry<float> RestTimePerComfort;
        public static ConfigEntry<float> ComfortItemRadius;
        public static ConfigEntry<float> FireItemRadiusMultiplier;

        private static Harmony sFejdStartupHarmony;
        private static Harmony sPlayerHarmony;
        private static Harmony sSERestedHarmony;
        private static Harmony sEffectAreaHarmony;

        private static readonly List<Player> sPlayers;

        private static readonly int sStatusEffectRested;
        private static readonly float sComfortRadiusConstant;

        // Copy of FireItemRadiusMultiplier so we can reference the old value after it changes
        private float mFireRadiusMultiplier;

        static ComfortablePlugin()
		{
            sPlayers = new List<Player>();
            sStatusEffectRested = (int)typeof(Player).GetField("s_statusEffectRested", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            sComfortRadiusConstant = (float)typeof(SE_Rested).GetField("c_ComfortRadius", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
		}

        private void Awake()
        {
            BaseRestTime = Config.Bind("Comfort", nameof(BaseRestTime), 480.0f, "The base time of the rested buff, in seconds. Game default 480.");
            BaseRestTime.SettingChanged += BaseRestTime_SettingChanged;

            RestTimePerComfort = Config.Bind("Comfort", nameof(RestTimePerComfort), 60.0f, "The time to add to the rested buff, in seconds, for each comfort level beyond 1. Game default 60.");
            RestTimePerComfort.SettingChanged += RestTimePerComfort_SettingChanged;

            ComfortItemRadius = Config.Bind("Comfort", nameof(ComfortItemRadius), 10.0f, "The range at which comforting items will affect the comfort level of players. Game default 10.");
            ComfortItemRadius.SettingChanged += ComfortItemRadius_SettingChanged;

            FireItemRadiusMultiplier = Config.Bind("Comfort", nameof(FireItemRadiusMultiplier), 1.0f, "A multiplier to apply to the range at which items provide the \"Fire\" buff. Game default 1. Very high values may cause performance issues.");
            FireItemRadiusMultiplier.SettingChanged += FireItemRadiusMultiplier_SettingChanged;

            ClampConfig();
            mFireRadiusMultiplier = FireItemRadiusMultiplier.Value;

            sFejdStartupHarmony = new Harmony(ModId + "_FejdStartup");
            sPlayerHarmony = new Harmony(ModId + "_Player");
            sSERestedHarmony = new Harmony(ModId + "_SE_Rested");
            sEffectAreaHarmony = new Harmony(ModId + "_EffectArea");

            sFejdStartupHarmony.PatchAll(typeof(FejdStartup_Patches));
            sPlayerHarmony.PatchAll(typeof(Player_Patches));
            sSERestedHarmony.PatchAll(typeof(SE_Rested_Patches));
            sEffectAreaHarmony.PatchAll(typeof(EffectArea_Patches));

            // We must wait for the ObjectDB to be setup before we can apply the config values. See OnObjectDBSetup method.
        }

        private void OnDestroy()
        {
            sFejdStartupHarmony.UnpatchSelf();
            sPlayerHarmony.UnpatchSelf();
            sSERestedHarmony.UnpatchSelf();
            sEffectAreaHarmony.UnpatchSelf();
        }

        private void BaseRestTime_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();

            foreach (SE_Rested effect in GetAllRestedEffects())
            {
                effect.m_baseTTL = BaseRestTime.Value;
            }
		}

        private void RestTimePerComfort_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();

            foreach (SE_Rested effect in GetAllRestedEffects())
            {
                effect.m_TTLPerComfortLevel = RestTimePerComfort.Value;
            }
        }

        private void ComfortItemRadius_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();

            sSERestedHarmony.UnpatchSelf();
            sSERestedHarmony.PatchAll(typeof(SE_Rested_Patches));
        }

        private void FireItemRadiusMultiplier_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();

            foreach (EffectArea area in EffectArea.GetAllAreas())
			{
                UpdateFireEffectRadius(area, mFireRadiusMultiplier, FireItemRadiusMultiplier.Value);
			}
            mFireRadiusMultiplier = FireItemRadiusMultiplier.Value;
        }

        private static void ClampConfig()
        {
            if (BaseRestTime.Value < 0.0f) BaseRestTime.Value = 0.0f;
            if (BaseRestTime.Value > 36000.0f) BaseRestTime.Value = 36000.0f;

            if (RestTimePerComfort.Value < 0.0f) RestTimePerComfort.Value = 0.0f;
            if (RestTimePerComfort.Value > 3600.0f) RestTimePerComfort.Value = 3600.0f;

            if (ComfortItemRadius.Value < 0.0f) ComfortItemRadius.Value = 0.0f;
            if (ComfortItemRadius.Value > 1000.0f) ComfortItemRadius.Value = 1000.0f;

            if (FireItemRadiusMultiplier.Value < 0.01f) FireItemRadiusMultiplier.Value = 0.01f;
            if (FireItemRadiusMultiplier.Value > 10.0f) FireItemRadiusMultiplier.Value = 10.0f;
        }

        // Called any time a new ObjectDB has been setup
        private static void OnObjectDBSetup()
        {
            SE_Rested effect = GetRestedEffect();
            if (effect != null)
            {
                // Updating the primary effect instance means all new copies of the effect will have these values
                effect.m_baseTTL = BaseRestTime.Value;
                effect.m_TTLPerComfortLevel = RestTimePerComfort.Value;
            }
            else
            {
                Debug.LogError("Comfortable: Could not locate SE_Rested effect object. This mod will not function properly.");
            }
        }

        private static IEnumerable<SE_Rested> GetAllRestedEffects()
        {
            // This is the primary instance of the effect which gets copied whenever a new instance is needed by a player
            SE_Rested effect = GetRestedEffect();
            if (effect != null) yield return effect;

            foreach(Player player in sPlayers)
            {
                // This is a copy of the effect currently in use by a player
                SE_Rested playerEffect = GetRestedEffect(player);
                if (playerEffect != null)
                {
                    yield return playerEffect;
                }
            }
        }

        private static SE_Rested GetRestedEffect()
		{
            return (SE_Rested)ObjectDB.instance?.GetStatusEffect(sStatusEffectRested);
        }

        private static SE_Rested GetRestedEffect(Player player)
        {
            return (SE_Rested)player.GetSEMan().GetStatusEffect(sStatusEffectRested);
        }

        private static void UpdateFireEffectRadius(EffectArea area, float oldMultiplier, float newMultiplier)
        {
            if ((area.m_type & EffectArea.Type.Heat) == 0) return;

            SphereCollider collider = area.GetComponent<Collider>() as SphereCollider;
            if (collider != null)
            {
                collider.radius = collider.radius / oldMultiplier * newMultiplier;

				// DEBUG: prints the new radius of the area along with the name of the associated object
				//Debug.Log($"DEBUG: {area.GetComponentInParent<Piece>()?.m_name ?? "[Unknown]"} - heat radius = {collider.radius}");
			}
        }

        [HarmonyPatch(typeof(FejdStartup))]
        private static class FejdStartup_Patches
        {
            [HarmonyPatch("SetupObjectDB"), HarmonyPostfix]
            private static void SetupObjectDB_Postfix()
            {
                OnObjectDBSetup();
            }
        }

        [HarmonyPatch(typeof(Player))]
		private static class Player_Patches
		{
			[HarmonyPatch("Awake"), HarmonyPostfix]
			private static void Awake_Postfix(Player __instance)
			{
				sPlayers.Add(__instance);
			}

			[HarmonyPatch("OnDestroy"), HarmonyPrefix]
			private static void OnDestroy_Prefix(Player __instance)
			{
				sPlayers.Remove(__instance);
			}
		}

        [HarmonyPatch(typeof(SE_Rested))]
        private static class SE_Rested_Patches
        {
            private enum TranspilerState
            {
                Searching,
                Finishing
            }

            [HarmonyPatch("GetNearbyComfortPieces"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> GetNearbyComfortPieces_Transpiler(IEnumerable<CodeInstruction> instructions)
			{
                // The comfort redius constant gets inlined, so we need to update the inlined value rather than the constant itself.

                TranspilerState state = TranspilerState.Searching;

                foreach (CodeInstruction instruction in instructions)
				{
					switch (state)
					{
						case TranspilerState.Searching:
                            if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == sComfortRadiusConstant)
							{
                                yield return new CodeInstruction(OpCodes.Ldc_R4, ComfortItemRadius.Value);
                                state = TranspilerState.Finishing;
							}
                            else
							{
                                yield return instruction;
							}
							break;
						case TranspilerState.Finishing:
                            yield return instruction;
							break;
					}
				}
			}
        }

        [HarmonyPatch(typeof(EffectArea))]
        private static class EffectArea_Patches
        {
            [HarmonyPatch("Awake"), HarmonyPostfix]
            private static void Awake_Postfix(EffectArea __instance)
			{
                UpdateFireEffectRadius(__instance, 1.0f, FireItemRadiusMultiplier.Value);
			}
        }
    }
}
