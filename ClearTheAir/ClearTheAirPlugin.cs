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

namespace ClearTheAir
{
	[BepInPlugin(ModId, "ClearTheAir", "1.0.4.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class ClearTheAirPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.cleartheair";

        public static ConfigEntry<float> MistClearRadiusMultiplier;

        // Notes on related game types
        // Mister: emits mist within a radius
        // Demister: blocks mist within a radius
        // SE_Demister: Wisplight player effect which controls a demister
        // MistEmitter: unsure

        private static Harmony sDemisterHarmony;

        private static readonly FieldInfo sAllDemistersField;

        // Copy of MistClearRadiusMultiplier so we can reference the old value after it changes
        private float mMistClearRadiusMultiplier;

        static ClearTheAirPlugin()
        {
            sAllDemistersField = typeof(Demister).GetField("m_instances", BindingFlags.Static | BindingFlags.NonPublic);
        }

        private void Awake()
        {
            MistClearRadiusMultiplier = Config.Bind("Mist", nameof(MistClearRadiusMultiplier), 1.0f, "Multiplier to apply to the for clear radius of all items which can clear mist. Game default 1.");
            MistClearRadiusMultiplier.SettingChanged += MistClearRadiusMultiplier_SettingChanged;

            ClampConfig();
            mMistClearRadiusMultiplier = MistClearRadiusMultiplier.Value;

            sDemisterHarmony = new Harmony(ModId + "_Demister");

            sDemisterHarmony.PatchAll(typeof(Demister_Patches));
        }

        private void OnDestroy()
        {
            sDemisterHarmony.UnpatchSelf();
        }

        private void MistClearRadiusMultiplier_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();

            List<Demister> allDemisters = (List<Demister>)sAllDemistersField.GetValue(null);
            foreach (Demister demister in allDemisters)
            {
                SetMistClearRadius(demister, mMistClearRadiusMultiplier, MistClearRadiusMultiplier.Value);
            }

            mMistClearRadiusMultiplier = MistClearRadiusMultiplier.Value;
        }

        private void ClampConfig()
        {
            if (MistClearRadiusMultiplier.Value < 0.1f) MistClearRadiusMultiplier.Value = 0.1f;
            if (MistClearRadiusMultiplier.Value > 100.0f) MistClearRadiusMultiplier.Value = 100.0f;
        }

        private static void SetMistClearRadius(Demister demister, float oldMultiplier, float newMultiplier)
        {
            float radius = demister.m_forceField.endRange / oldMultiplier * newMultiplier;
            demister.m_forceField.endRange = radius;
        }

        [HarmonyPatch(typeof(Demister))]
        private static class Demister_Patches
        {
            [HarmonyPatch("Awake"), HarmonyPostfix]
            private static void Awake_Postfix(Demister __instance)
            {
                SetMistClearRadius(__instance, 1.0f, MistClearRadiusMultiplier.Value);
            }
        }
    }
}
