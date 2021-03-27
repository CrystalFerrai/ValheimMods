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
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ProperPortals
{
    [BepInPlugin(ModId, "Proper Portals", "1.0.2.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class ProperPortalsPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.properportals";

        public static ConfigEntry<bool> CarryAnything;
        public static ConfigEntry<float> MinPortalTime;

        private static Harmony sInventoryHarmony;
        private static Harmony sPlayerHarmony;

        private void Awake()
        {
            CarryAnything = Config.Bind("Portal", nameof(CarryAnything), true, "Whether to allow using portals while carrying portal restricted items such as metals. Game default is false.");
            CarryAnything.SettingChanged += CarryAnything_SettingChanged;

            MinPortalTime = Config.Bind("Portal", nameof(MinPortalTime), 1.0f, "The minimum time to wait for a teleport to complete, in seconds, including an initial 1 second fade out. It can take longer if the target location needs to be loaded. Increase this if you have the issue of dropping in before loading completes. Game default is 8.");
            MinPortalTime.SettingChanged += MinPortalTime_SettingChanged;

            ClampConfig();

            sInventoryHarmony = new Harmony(ModId + "_Inventory");
            sPlayerHarmony = new Harmony(ModId + "_Player");

            if (CarryAnything.Value)
            {
                sInventoryHarmony.PatchAll(typeof(Inventory_Patches));
            }
            sPlayerHarmony.PatchAll(typeof(Player_Patches));
        }

        private void OnDestroy()
        {
            sInventoryHarmony.UnpatchSelf();
            sPlayerHarmony.UnpatchSelf();
        }

        private static void ClampConfig()
        {
            if (MinPortalTime.Value < 1.0f) MinPortalTime.Value = 1.0f;
            if (MinPortalTime.Value > 60.0) MinPortalTime.Value = 60.0f;
        }

        private void CarryAnything_SettingChanged(object sender, EventArgs e)
        {
            if (CarryAnything.Value)
            {
                sInventoryHarmony.PatchAll(typeof(Inventory_Patches));
            }
            else
            {
                sInventoryHarmony.UnpatchSelf();
            }
        }

        private void MinPortalTime_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();
            sPlayerHarmony.UnpatchSelf();
            sPlayerHarmony.PatchAll(typeof(Player_Patches));
        }

        [HarmonyPatch(typeof(Inventory))]
        private static class Inventory_Patches
        {
            [HarmonyPatch(nameof(Inventory.IsTeleportable)), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> IsTeleportable_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // return true
                yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                yield return new CodeInstruction(OpCodes.Ret);
            }
        }

        [HarmonyPatch(typeof(Player))]
        private static class Player_Patches
        {
            [HarmonyPatch("UpdateTeleport"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> UpdateTeleport_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ldc_R4)
                    {
                        float value = (float)instruction.operand;
                        if (value == 2.0f)
                        {
                            // Pre-teleport delay allowing time for load screen to fade in
                            instruction.operand = 1.0f;
                        }
                        else if (value == 8.0f)
                        {
                            // Teleport minimum wait time which includes the pre-teleport delay
                            instruction.operand = MinPortalTime.Value;
                        }
                    }
                    yield return instruction;
                }
            }
        }
    }
}
