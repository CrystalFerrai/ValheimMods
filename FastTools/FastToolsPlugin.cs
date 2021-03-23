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
using UnityEngine;

namespace FastTools
{
    [BepInPlugin("dev.crystal.fasttools", "Fast Tools", "1.0.2.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class FastToolsPlugin : BaseUnityPlugin
    {
        private static readonly List<Player> sPlayers;

        public static ConfigEntry<float> ToolUseDelay;

        static FastToolsPlugin()
        {
            sPlayers = new List<Player>();
        }

        private void Awake()
        {
            ToolUseDelay = Config.Bind("Tools", nameof(ToolUseDelay), 0.25f, "The delay time for placement tools, in seconds. Game default is 0.5.");
            ToolUseDelay.SettingChanged += ToolUseDelay_SettingChanged;

            Harmony.CreateAndPatchAll(typeof(Player_Patches));
        }

        private void ToolUseDelay_SettingChanged(object sender, EventArgs e)
        {
            foreach (Player player in sPlayers)
            {
                player.m_toolUseDelay = ToolUseDelay.Value;
            }
        }

        [HarmonyPatch(typeof(Player))]
        private static class Player_Patches
        {
            [HarmonyPatch("Awake"), HarmonyPostfix]
            private static void Awake_Postfix(Player __instance)
            {
                __instance.m_toolUseDelay = ToolUseDelay.Value;
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
