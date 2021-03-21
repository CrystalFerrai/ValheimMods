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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Pathfinder
{
    [BepInPlugin("dev.crystal.pathfinder", "Pathfinder", "1.0.1.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class PathfinderPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> LandExploreRadius;
        public static ConfigEntry<float> SeaExploreRadius;
        public static ConfigEntry<float> AltitudeRadiusBonus;
        public static ConfigEntry<float> DaylightRadiusBonus;
        public static ConfigEntry<float> ForestRadiusPenalty;

        private static float sAltitudeBonus;
        private static float sDaylightLandBonus;
        private static float sDaylightSeaBonus;
        private static float sForestPenalty;

        private void Awake()
        {
            LandExploreRadius = Config.Bind("Base", nameof(LandExploreRadius), 200.0f, "The radius around the player to uncover while travelling on land near sea level. Higher values may cause performance issues. Max allowed is 2000. Game default is 100.");
            if (LandExploreRadius.Value < 0.0f) LandExploreRadius.Value = 0.0f;
            if (LandExploreRadius.Value > 2000.0f) LandExploreRadius.Value = 2000.0f;

            SeaExploreRadius = Config.Bind("Base", nameof(SeaExploreRadius), 300.0f, "The radius around the player to uncover while travelling on a boat. Higher values may cause performance issues. Max allowed is 2000. Game default is 100.");
            if (SeaExploreRadius.Value < 0.0f) SeaExploreRadius.Value = 0.0f;
            if (SeaExploreRadius.Value > 2000.0f) SeaExploreRadius.Value = 2000.0f;

            AltitudeRadiusBonus = Config.Bind("Multipliers", nameof(AltitudeRadiusBonus), 0.5f, "Bonus multiplier to apply to land exploration radius based on altitude. For every 100 units above sea level (smooth scale), add this value multiplied by LandExploreRadius to the total. For example, with a radius of 200 and a multiplier of 0.5, radius is 200 at sea level, 250 at 50 altitude, 300 at 100 altitude, 400 at 200 altitude, etc. For reference, a typical mountain peak is around 170 altitude. This will not increase total radius beyond 2000. Accepted range 0-10. Set to 0 to disable.");
            if (AltitudeRadiusBonus.Value < 0.0f) AltitudeRadiusBonus.Value = 0.0f;
            if (AltitudeRadiusBonus.Value > 10.0f) AltitudeRadiusBonus.Value = 10.0f;

            DaylightRadiusBonus = Config.Bind("Multipliers", nameof(DaylightRadiusBonus), 0.2f, "Bonus multiplier to apply to land exploration radius when it is daylight (meaning day time and not in a permanent night biome). This value is multiplied by the base exploration radius (land or sea) and added to the total. This will not increase total radius beyond 2000. Accepted range 0-10. Set to 0 to disable.");
            if (DaylightRadiusBonus.Value < 0.0f) DaylightRadiusBonus.Value = 0.0f;
            if (DaylightRadiusBonus.Value > 10.0f) DaylightRadiusBonus.Value = 10.0f;

            ForestRadiusPenalty = Config.Bind("Multipliers", nameof(ForestRadiusPenalty), 0.1f, "Penalty to apply to land exploration radius when in a forest (black forest, mistlands, forested parts of meadows and plains). This value is multiplied by the base exploration radius (land or sea) and subtraced from the total. Accepted range 0-1. Set to 0 to disable.");
            if (ForestRadiusPenalty.Value < 0.0f) ForestRadiusPenalty.Value = 0.0f;
            if (ForestRadiusPenalty.Value > 1.0f) ForestRadiusPenalty.Value = 1.0f;

            sAltitudeBonus = LandExploreRadius.Value * AltitudeRadiusBonus.Value;
            sDaylightLandBonus = LandExploreRadius.Value * DaylightRadiusBonus.Value;
            sDaylightSeaBonus = SeaExploreRadius.Value * DaylightRadiusBonus.Value;
            sForestPenalty = LandExploreRadius.Value * ForestRadiusPenalty.Value;

            Harmony.CreateAndPatchAll(typeof(Minimap_UpdateExplore_Patch));
        }

        [HarmonyPatch(typeof(Minimap), "UpdateExplore")]
        private static class Minimap_UpdateExplore_Patch
        {
            private enum TranspilerState
            {
                Searching,
                Checking,
                Finishing
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                TranspilerState state = TranspilerState.Searching;

                CodeInstruction previous = null;

                foreach (CodeInstruction instruction in instructions)
                {
                    switch (state)
                    {
                        case TranspilerState.Searching:
                            if (instruction.opcode == OpCodes.Ldarg_0)
                            {
                                previous = instruction;
                                state = TranspilerState.Checking;
                            }
                            else
                            {
                                yield return instruction;
                            }
                            break;
                        case TranspilerState.Checking:
                            if (instruction.opcode == OpCodes.Ldfld && ((FieldInfo)instruction.operand).Name == nameof(Minimap.m_exploreRadius))
                            {
                                yield return new CodeInstruction(OpCodes.Ldarg_2); // player
                                yield return new CodeInstruction(OpCodes.Call, typeof(Minimap_UpdateExplore_Patch).GetMethod(nameof(GetExploreRadius), BindingFlags.Static | BindingFlags.NonPublic));
                                state = TranspilerState.Finishing;
                            }
                            else
                            {
                                yield return previous;
                                yield return instruction;
                                state = TranspilerState.Searching;
                            }
                            previous = null;
                            break;
                        case TranspilerState.Finishing:
                            yield return instruction;
                            break;
                    }
                }
            }

            private static float GetExploreRadius(Player player)
            {
                // Player may not be the one piloting a boat, but should still get the sea radius if they are riding in one that has a pilot.
                // A longship is about 20 units long. 19 is about as far as you could possibly get from a pilot and still be on the boat.
                List<Player> players = new List<Player>();
                Player.GetPlayersInRange(player.transform.position, 21.0f, players);
                if (players.Any(p => p.GetShipControl() != null))
                {
                    if (EnvMan.instance.IsDaylight())
                    {
                        return Mathf.Clamp(SeaExploreRadius.Value + sDaylightSeaBonus, 10.0f, 2000.0f);
                    }
                }

                if (player.InInterior())
                {
                    // In a dungeon. Dungeons are way up high and we dont want to reveal a huge section of the map when entering one.
                    // We actually want to reduce the radius siunce it doesnt make sense to be able to explore the map while in a dungeon
                    return LandExploreRadius.Value * 0.5f;
                }

                // Sea level = 30, tallest mountains (not including the rare super mountains) seem to be around 220. Stop adding altitude bonus after 400
                return Mathf.Max(
                    Mathf.Clamp
                    (
                        LandExploreRadius.Value + (Mathf.Min(player.transform.position.y, 400.0f) - ZoneSystem.instance.m_waterLevel) / 100.0f * sAltitudeBonus + (EnvMan.instance.IsDaylight() ? sDaylightLandBonus : 0.0f),
                        LandExploreRadius.Value,
                        2000.0f
                    ) - GetForestPenalty(player),
                    10.0f);
            }

            private static float GetForestPenalty(Player player)
            {
                // Based roughly on logic found in MiniMap.GetMaskColor
                Heightmap.Biome biome = player.GetCurrentBiome();
                switch (player.GetCurrentBiome())
                {
                    case Heightmap.Biome.BlackForest:
                    case Heightmap.Biome.Mistlands:
                    case Heightmap.Biome.Swamp:
                        return sForestPenalty;
                    case Heightmap.Biome.Meadows:
                        return WorldGenerator.InForest(player.transform.position) ? sForestPenalty : 0.0f;
                    case Heightmap.Biome.Plains:
                        return WorldGenerator.GetForestFactor(player.transform.position) < 0.8f ? sForestPenalty : 0.0f;
                    default:
                        return 0.0f;
                }
            }
        }
    }
}
