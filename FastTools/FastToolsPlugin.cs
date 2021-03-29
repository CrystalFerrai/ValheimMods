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

namespace FastTools
{
    [BepInPlugin(ModId, "Fast Tools", "1.1.0.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class FastToolsPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.fasttools";

        public static ConfigEntry<float> PlaceDelay;
        public static ConfigEntry<float> RemoveDelay;

        private static Harmony sPlayerHarmony;
        private static readonly List<Player> sPlayers;

        static FastToolsPlugin()
        {
            sPlayers = new List<Player>();
        }

        private void Awake()
        {
            PlaceDelay = Config.Bind("Tools", nameof(PlaceDelay), 0.25f, "The delay time for placing items, in seconds. Allowed range 0-10. Game default is 0.4.");
            PlaceDelay.SettingChanged += Delay_SettingChanged;

            RemoveDelay = Config.Bind("Tools", nameof(RemoveDelay), 0.25f, "The delay time for removing items, in seconds. Allowed range 0-10. Game default is 0.25.");
            RemoveDelay.SettingChanged += Delay_SettingChanged;

            ClampConfig();

            sPlayerHarmony = new Harmony(ModId + "_Player");
            sPlayerHarmony.PatchAll(typeof(Player_Patches));
        }

        private void OnDestroy()
        {
            sPlayerHarmony.UnpatchSelf();
            sPlayers.Clear();
        }

        private static void ClampConfig()
        {
            // There is no feedback when delay is active aside from tools simply not working, so don't allow really long delays.

            if (PlaceDelay.Value < 0.0f) PlaceDelay.Value = 0.0f;
            if (PlaceDelay.Value > 10.0f) PlaceDelay.Value = 10.0f;

            if (RemoveDelay.Value < 0.0f) RemoveDelay.Value = 0.0f;
            if (RemoveDelay.Value > 10.0f) RemoveDelay.Value = 10.0f;
        }

        private void Delay_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();
            foreach (Player player in sPlayers)
            {
                player.m_placeDelay = PlaceDelay.Value;
                player.m_removeDelay = PlaceDelay.Value;
            }
        }

        [HarmonyPatch(typeof(Player))]
        private static class Player_Patches
        {
            [HarmonyPatch("Awake"), HarmonyPostfix]
            private static void Awake_Postfix(Player __instance)
            {
                UnityEngine.Debug.Log($"[FastTools] place={__instance.m_placeDelay}, remove={__instance.m_removeDelay}");
                __instance.m_placeDelay = PlaceDelay.Value;
                __instance.m_removeDelay = PlaceDelay.Value;
                sPlayers.Add(__instance);
            }

            [HarmonyPatch("OnDestroy"), HarmonyPrefix]
            private static void OnDestroy_Prefix(Player __instance)
            {
                sPlayers.Remove(__instance);
            }
        }
    }
}
