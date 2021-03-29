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
    [BepInPlugin(ModId, "Proper Portals", "1.1.0.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class ProperPortalsPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.properportals";

        public static ConfigEntry<bool> CarryAnything;
        public static ConfigEntry<float> FadeTime;
        public static ConfigEntry<float> MinPortalTime;

        private static Harmony sInventoryHarmony;
        private static Harmony sHudHarmony;
        private static Harmony sPlayerHarmony;

        private void Awake()
        {
            CarryAnything = Config.Bind("Portal", nameof(CarryAnything), true, "Whether to allow using portals while carrying portal restricted items such as metals. Game default is false.");
            CarryAnything.SettingChanged += CarryAnything_SettingChanged;

            FadeTime = Config.Bind("Portal", nameof(FadeTime), 0.5f, "The time it takes to fade the screen before teleporting. Teleporting does not start until after the screen fade completes. Game default is 1.");
            FadeTime.SettingChanged += PortalTime_SettingChanged;

            MinPortalTime = Config.Bind("Portal", nameof(MinPortalTime), 0.0f, "The minimum time to wait for a teleport to complete, in seconds. It can take longer if the target location needs to be loaded. Increase this if you have the issue of dropping in before loading completes. Game default is 8.");
            MinPortalTime.SettingChanged += PortalTime_SettingChanged;

            ClampConfig();

            sInventoryHarmony = new Harmony(ModId + "_Inventory");
            sHudHarmony = new Harmony(ModId + "_Hud");
            sPlayerHarmony = new Harmony(ModId + "_Player");

            if (CarryAnything.Value)
            {
                sInventoryHarmony.PatchAll(typeof(Inventory_Patches));
            }
            sHudHarmony.PatchAll(typeof(Hud_Patches));
            sPlayerHarmony.PatchAll(typeof(Player_Patches));
        }

        private void OnDestroy()
        {
            sInventoryHarmony.UnpatchSelf();
            sHudHarmony.UnpatchSelf();
            sPlayerHarmony.UnpatchSelf();
        }

        private static void ClampConfig()
        {
            if (FadeTime.Value < 0.0f) FadeTime.Value = 0.0f;
            if (FadeTime.Value > 10.0) FadeTime.Value = 10.0f;

            if (MinPortalTime.Value < 0.0f) MinPortalTime.Value = 0.0f;
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

        private void PortalTime_SettingChanged(object sender, EventArgs e)
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

        [HarmonyPatch(typeof(Hud))]
        private static class Hud_Patches
        {
            [HarmonyPatch("GetFadeDuration"), HarmonyPrefix]
            private static bool GetFadeDuration_Prefix(Hud __instance, ref float __result, Player player)
            {
                if (player?.IsTeleporting() ?? false)
                {
                    __result = FadeTime.Value;
                    return false;
                }
                return true;
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
                            instruction.operand = FadeTime.Value;
                        }
                        else if (value == 8.0f)
                        {
                            // Teleport minimum wait time which includes the pre-teleport delay
                            instruction.operand = FadeTime.Value + MinPortalTime.Value;
                        }
                        else if (value == 15.0f)
                        {
                            // Time at which to set player on ground if ground not found at player position
                            instruction.operand = FadeTime.Value + MinPortalTime.Value + 0.5f;
                        }
                    }
                    yield return instruction;
                }
            }
        }
    }
}
