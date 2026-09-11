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
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace ProperPortals
{
	[BepInPlugin(ModId, ModName, ModVersion)]
	[BepInDependency("_shudnal.ConditionalConfigSync", BepInDependency.DependencyFlags.HardDependency)]
	[BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class ProperPortalsPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.properportals";
        public const string ModName = "Proper Portals";
        public const string ModVersion = "1.4.1.0";

		internal static readonly ConfigSync ConfigSync = new ConfigSync(ModId)
		{
			DisplayName = ModName,
			CurrentVersion = ModVersion,
			MinimumRequiredVersion = ModVersion
		};

		public static ConfigEntry<bool> ModRequired;

		public static ConfigEntry<bool> CarryAnything;
        public static ConfigEntry<float> FadeTime;
        public static ConfigEntry<float> MinPortalTime;
        public static ConfigEntry<float> ActivationRange;

        private static Harmony sInventoryHarmony;
        private static Harmony sHudHarmony;
        private static Harmony sPlayerHarmony;
        private static Harmony sTeleportWorldHarmony;
        private static Harmony sZDOManHarmony;

        private static Dictionary<ZDOID, TeleportWorld> sPortals;

        static ProperPortalsPlugin()
        {
            sPortals = new Dictionary<ZDOID, TeleportWorld>();
        }

        private void Awake()
		{
			ModRequired = Config.Bind("ServerSync", nameof(ModRequired), true, "If true on server, clients connecting will be rejected if they do not have the mod. If false, clients may connect without the mod. If true on client, cannot join a server unless it is running the mod. Clients without the mod may cause it to not function reliably for others. It is recommended to keep this true if possible.");
			ConfigSync.ModRequired = ModRequired.Value;
			ModRequired.SettingChanged += ModRequired_SettingChanged;
			ConfigSync.AddConfigEntry(ModRequired, ConfigSyncMode.AlwaysServerControlled);

			ModRequired = Config.Bind("ServerSync", nameof(ModRequired), true, "If true on server, clients connecting will be rejected if they do not have the mod. If false, clients may connect without the mod. If true on client, cannot join a server unless it is running the mod. Clients without the mod may cause it to not function reliably for others. It is recommended to keep this true if possible.");
			ConfigSync.ModRequired = ModRequired.Value;
			ModRequired.SettingChanged += ModRequired_SettingChanged;
			ConfigSync.AddConfigEntry(ModRequired, ConfigSyncMode.AlwaysServerControlled);

			CarryAnything = Config.Bind("Portal", nameof(CarryAnything), true, "Whether to allow using portals while carrying portal restricted items such as metals. Set false to use world setting. Set true to force allow. [The value will be enforced on a server.]");
            CarryAnything.SettingChanged += CarryAnything_SettingChanged;
			ConfigSync.AddConfigEntry(CarryAnything, ConfigSyncMode.AlwaysServerControlled);

			FadeTime = Config.Bind("Portal", nameof(FadeTime), 0.5f, "The time it takes to fade the screen before teleporting. Teleporting does not start until after the screen fade completes. Game default is 1. [The value may be enforced on a server via sync policy.]");
            FadeTime.SettingChanged += PortalTime_SettingChanged;
			ConfigSync.AddConfigEntry(FadeTime, ConfigSyncMode.Conditional, serverControlledByDefault: true);

			MinPortalTime = Config.Bind("Portal", nameof(MinPortalTime), 0.0f, "The minimum time to wait for a teleport to complete, in seconds. It can take longer if the target location needs to be loaded. Increase this if you have the issue of dropping in before loading completes. Game default is 8. [The value may be enforced on a server via sync policy.]");
            MinPortalTime.SettingChanged += PortalTime_SettingChanged;
			ConfigSync.AddConfigEntry(MinPortalTime, ConfigSyncMode.Conditional, serverControlledByDefault: true);

			ActivationRange = Config.Bind("Portal", nameof(ActivationRange), 2.0f, "The distance at which a portal will start glowing and making noise when a player approaches it. Maximum accepted value is 10. Setting to 0 prevents portals from glowing or making noise at all. Game default is 3. [The value will be enforced on a server.]");
            ActivationRange.SettingChanged += ActivationRange_SettingChanged;
			ConfigSync.AddConfigEntry(ActivationRange, ConfigSyncMode.AlwaysServerControlled);

			ClampConfig();

            sInventoryHarmony = new Harmony(ModId + "_Inventory");
            sHudHarmony = new Harmony(ModId + "_Hud");
            sPlayerHarmony = new Harmony(ModId + "_Player");
            sTeleportWorldHarmony = new Harmony(ModId + "_TeleportWorld");
            sZDOManHarmony = new Harmony(ModId + "_ZDOMan");

            if (CarryAnything.Value)
            {
                sInventoryHarmony.PatchAll(typeof(Inventory_Patches));
            }
            sHudHarmony.PatchAll(typeof(Hud_Patches));
            sPlayerHarmony.PatchAll(typeof(Player_Patches));
            sTeleportWorldHarmony.PatchAll(typeof(TeleportWorld_Patches));
            sZDOManHarmony.PatchAll(typeof(ZDOMan_Patches));
        }

        private void OnDestroy()
        {
            sInventoryHarmony.UnpatchSelf();
            sHudHarmony.UnpatchSelf();
            sPlayerHarmony.UnpatchSelf();
            sTeleportWorldHarmony.UnpatchSelf();
            sZDOManHarmony.UnpatchSelf();

            sPortals.Clear();
        }

        private static void ClampConfig()
        {
            if (FadeTime.Value < 0.0f) FadeTime.Value = 0.0f;
            if (FadeTime.Value > 10.0) FadeTime.Value = 10.0f;

            if (MinPortalTime.Value < 0.0f) MinPortalTime.Value = 0.0f;
            if (MinPortalTime.Value > 60.0) MinPortalTime.Value = 60.0f;

            if (ActivationRange.Value < 0.0f) ActivationRange.Value = 0.0f;
            if (ActivationRange.Value > 10.0) ActivationRange.Value = 10.0f;
        }

		private void ModRequired_SettingChanged(object sender, EventArgs e)
		{
			ConfigSync.ModRequired = ModRequired.Value;
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

        private void ActivationRange_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();
            foreach (TeleportWorld portal in sPortals.Values)
            {
                UpdateActivationRange(portal);
            }
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

        [HarmonyPatch(typeof(TeleportWorld))]
        private static class TeleportWorld_Patches
        {
            [HarmonyPatch("Awake"), HarmonyPostfix]
            private static void Awake_Postfix(TeleportWorld __instance)
            {
                // If enabled == false, this is probably a build placement ghost rather than an actual portal. It will not have a ZDO.
                if (__instance.enabled)
                {
                    UpdateActivationRange(__instance);

                    // Every time a portal is loaded (like by a player loading the area it is in), a new instance is created.
                    // However, the same portal will always have the same ZDOID. So, we can just replace the instance.
                    sPortals[__instance.GetComponent<ZNetView>().GetZDO().m_uid] = __instance;
                }
            }

            // Note: It is not possible to patch TeleportWorld.OnDestroyed because it is not defined.
        }

        [HarmonyPatch(typeof(ZDOMan))]
        private static class ZDOMan_Patches
        {
            [HarmonyPatch("HandleDestroyedZDO"), HarmonyPrefix]
            private static void HandleDestroyedZDO_Prefix(ZDOMan __instance, ZDOID uid)
            {
                // This will happen when a portal is actually destroyed, not when it is simply unloaded. In this case,
                // remove the entry from our dictionary because it is never coming back.
                // Note: Every ZDO getting destroyed will call this function. We only care about ZDOs we are tracking.
                sPortals.Remove(uid);
            }
        }

        private static void UpdateActivationRange(TeleportWorld portal)
        {
            // This if check is to see if the portal instance is actually loaded. If it isn't then we don't care. Next time
            // it is laoded, a new instance will be created.
            if (portal)
            {
                portal.m_activationRange = ActivationRange.Value;

                // Activation is triggered when a player is within a radius of the proximity root. The root needs to be moved as
                // the range changes so that the defined circle is in front of the portal object with its edge meeting the portal.
                // Note: Because the check uses a circle, the activation zone gets weirder as the range gets larger.
                Transform root = portal.m_proximityRoot;
                root.localPosition = new Vector3(root.localPosition.x, root.localPosition.y, ActivationRange.Value * 0.5f + 0.25f);
            }
        }
    }
}
