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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastTools
{
    [BepInPlugin("dev.crystal.fasttools", "Fast Tools", "1.0.0.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class FastToolsPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> ToolUseDelay;

        private void Awake()
        {
            ToolUseDelay = Config.Bind("Tools", nameof(ToolUseDelay), 0.0f, "The delay time for placement tools, in seconds. Game default is 0.25.");

            Harmony.CreateAndPatchAll(typeof(Player_Awake_Patch));
        }

        [HarmonyPatch(typeof(Player), "Awake")]
        private static class Player_Awake_Patch
        {
            private static void Postfix(Player __instance)
            {
                __instance.m_toolUseDelay = ToolUseDelay.Value;
            }
        }
    }
}
