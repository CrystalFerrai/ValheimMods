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
using System.Reflection.Emit;

namespace GracefulLanding
{
    [BepInPlugin("dev.crystal.gracefullanding", "Graceful Landing", "1.0.0.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class GracefulLandingPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> MinDamageHeight;
        public static ConfigEntry<float> MaxDamageHeight;
        public static ConfigEntry<float> MaxDamageAmount;

        private void Awake()
        {
            MinDamageHeight = Config.Bind("Falling", nameof(MinDamageHeight), 8.0f, "The minimum distance you must fall to receive any fall damage. Allowed range 1-10000. Game default 4.");
            if (MinDamageHeight.Value < 1.0f) MinDamageHeight.Value = 1.0f;
            if (MinDamageHeight.Value > 10000.0f) MinDamageHeight.Value = 10000.0f;

            MaxDamageHeight = Config.Bind("Falling", nameof(MaxDamageHeight), 64.0f, $"The minimum distance you must to receive maximum fall damage. Allowed range 1-10000. Must be equal to or higher than {nameof(MinDamageHeight)}. Game default 16.");
            if (MaxDamageHeight.Value < MinDamageHeight.Value) MaxDamageHeight.Value = MinDamageHeight.Value;
            if (MaxDamageHeight.Value > 10000.0f) MaxDamageHeight.Value = 10000.0f;

            MaxDamageAmount = Config.Bind("Falling", nameof(MaxDamageAmount), 100.0f, "The maximum fall damage that can be received. Allowed range 0-10000. Game default 100.");
            if (MaxDamageAmount.Value < 0.0f) MaxDamageAmount.Value = 0.0f;
            if (MaxDamageAmount.Value > 10000.0f) MaxDamageAmount.Value = 10000.0f;

            Harmony.CreateAndPatchAll(typeof(Character_UpdateGroundContact_Patch));
        }

        [HarmonyPatch(typeof(Character), "UpdateGroundContact")]
        private static class Character_UpdateGroundContact_Patch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ldc_R4)
                    {
                        switch ((float)instruction.operand)
                        {
                            case 4.0f:
                                instruction.operand = MinDamageHeight.Value;
                                break;
                            case 16.0f:
                                instruction.operand = MaxDamageHeight.Value;
                                break;
                            case 100.0f:
                                instruction.operand = MaxDamageAmount.Value;
                                break;
                        }
                    }
                    yield return instruction;
                }
            }
        }
    }
}
