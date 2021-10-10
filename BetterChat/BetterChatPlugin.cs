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
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace BetterChat
{
    [BepInPlugin(ModId, "Better Chat", "1.4.2.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class BetterChatPlugin : BaseUnityPlugin
    {
        public const string ModId = "dev.crystal.betterchat";

        public static ConfigEntry<bool> AlwaysVisible;
        public static ConfigEntry<float> HideDelay;
        public static ConfigEntry<bool> ForceCase;
        public static ConfigEntry<bool> SlashOpensChat;
        public static ConfigEntry<bool> DefaultShout;
        public static ConfigEntry<bool> ShowShoutPings;
        public static ConfigEntry<float> TalkDistance;
        public static ConfigEntry<float> WhisperDistance;

        private static Harmony sChatAwakeHarmony;
        private static Harmony sPlayerHarmony;
        private static Harmony sChatShowHarmony;
        private static Harmony sChatAlwaysShowHarmony;
        private static Harmony sChatMixedCaseHarmony;
        private static Harmony sChatShoutHarmony;
        private static Harmony sMinimapHarmony;
        private static Harmony sChatSlashHarmony;

        private static Chat sChat;
        private static List<Talker> sTalkers;

        private static readonly FieldInfo sHideTimerField;

        private static bool sMoveCaretToEnd = false;

        static BetterChatPlugin()
        {
            sTalkers = new List<Talker>();
            sHideTimerField = typeof(Chat).GetField("m_hideTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private void Awake()
        {
            AlwaysVisible = Config.Bind("Chat", nameof(AlwaysVisible), false, "If True, the chat window will remain visible at all times. If False, the chat window will appear when new messages are received.");
            AlwaysVisible.SettingChanged += AlwaysVisible_SettingChanged;

            HideDelay = Config.Bind("Chat", nameof(HideDelay), 10.0f, "The time, in seconds, to keep the chat window visible after sending or receiving a message. Minimum is 0.5. Has no effect if AlwaysVisible=true.");
            HideDelay.SettingChanged += HideDelay_SettingChanged;

            ForceCase = Config.Bind("Chat", nameof(ForceCase), false, "If True, shout will be in all caps and whisper will be in all lowercase (game default). If False, messages will appear as they were originally entered.");
            ForceCase.SettingChanged += ForceCase_SettingChanged;

            SlashOpensChat = Config.Bind("Chat", nameof(SlashOpensChat), true, "If True, pressing the slash key (/) will open the chat window and start a message.");
            SlashOpensChat.SettingChanged += SlashOpensChat_SettingChanged;

            DefaultShout = Config.Bind("Chat", nameof(DefaultShout), false, "If True, text entered will shout by default - type /s for talk. If False, chat will be talk by default - type /s for shout.");
            DefaultShout.SettingChanged += DefaultShout_SettingChanged;

            ShowShoutPings = Config.Bind("Chat", nameof(ShowShoutPings), true, "If True, pings will show on your map when players shout (game default). If False, the pings will not show. (Other players can still see your shout pings.)");
            ShowShoutPings.SettingChanged += ShowShoutPings_SettingChanged;

            TalkDistance = Config.Bind("Chat", nameof(TalkDistance), 15.0f, "The maximum distance from a player at which you will receive their normal chat messages (not whisper or shout). Game default is 15. Acceptable range is 1-100.");
            TalkDistance.SettingChanged += Distance_SettingChanged;

            WhisperDistance = Config.Bind("Chat", nameof(WhisperDistance), 4.0f, "The maximum distance from a player at which you will receive their whispered chat messages. Game default is 4. Acceptable range is 1-20");
            WhisperDistance.SettingChanged += Distance_SettingChanged;

            ClampConfig();

            sChatAwakeHarmony = new Harmony(ModId + "_ChatAwake");
            sPlayerHarmony = new Harmony(ModId + "_Player");
            sChatShowHarmony = new Harmony(ModId + "_ChatShow");
            sChatAlwaysShowHarmony = new Harmony(ModId + "_ChatAlwaysShow");
            sChatMixedCaseHarmony = new Harmony(ModId + "_ChatMixedCase");
            sChatShoutHarmony = new Harmony(ModId + "_ChatShout");
            sMinimapHarmony = new Harmony(ModId + "_Minimap");
            sChatSlashHarmony = new Harmony(ModId + "_ChatSlash");

            sChatAwakeHarmony.PatchAll(typeof(Chat_Patches));
            sPlayerHarmony.PatchAll(typeof(Player_Patches));
            if (AlwaysVisible.Value)
            {
                sChatAlwaysShowHarmony.PatchAll(typeof(Chat_AlwaysShow_Patch));
            }
            else
            {
                sChatShowHarmony.PatchAll(typeof(Chat_Show_Patch));
            }
            if (!ForceCase.Value)
            {
                sChatMixedCaseHarmony.PatchAll(typeof(Chat_MixedCase_Patch));
            }
            if (DefaultShout.Value)
            {
                sChatShoutHarmony.PatchAll(typeof(Chat_Shout_Patch));
            }
            if (!ShowShoutPings.Value)
            {
                sMinimapHarmony.PatchAll(typeof(Minimap_Patches));
            }
            if (SlashOpensChat.Value)
            {
                sChatSlashHarmony.PatchAll(typeof(Chat_Slash_Patches));
            }
        }

        private void OnDestroy()
        {
            sChatAwakeHarmony.UnpatchSelf();
            sPlayerHarmony.UnpatchSelf();
            sChatShowHarmony.UnpatchSelf();
            sChatAlwaysShowHarmony.UnpatchSelf();
            sChatMixedCaseHarmony.UnpatchSelf();
            sChatShoutHarmony.UnpatchSelf();
            sMinimapHarmony.UnpatchSelf();
            sChatSlashHarmony.UnpatchSelf();
        }

        private static void ClampConfig()
        {
            // Minimum delay prevents issues like flickering or permanently hidden chat window
            if (HideDelay.Value < 0.5f) HideDelay.Value = 0.5f;
            if (HideDelay.Value > 3600.0f) HideDelay.Value = 3600.0f;

            // Distance values are clamped primarily for privacy concerns
            if (TalkDistance.Value < 1.0f) TalkDistance.Value = 1.0f;
            if (TalkDistance.Value > 100.0f) TalkDistance.Value = 100.0f;

            if (WhisperDistance.Value < 1.0f) WhisperDistance.Value = 1.0f;
            if (WhisperDistance.Value > 20.0f) WhisperDistance.Value = 20.0f;
        }

        private void AlwaysVisible_SettingChanged(object sender, EventArgs e)
        {
            if (AlwaysVisible.Value)
            {
                sChatShowHarmony.UnpatchSelf();
                sChatAlwaysShowHarmony.PatchAll(typeof(Chat_AlwaysShow_Patch));
            }
            else
            {
                sChatAlwaysShowHarmony.UnpatchSelf();
                sChatShowHarmony.PatchAll(typeof(Chat_Show_Patch));
            }
        }

        private void HideDelay_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();

            if (sChat != null)
            {
                sChat.m_hideDelay = HideDelay.Value;
            }
        }

        private void ForceCase_SettingChanged(object sender, EventArgs e)
        {
            if (ForceCase.Value)
            {
                sChatMixedCaseHarmony.UnpatchSelf();
            }
            else
            {
                sChatMixedCaseHarmony.PatchAll(typeof(Chat_MixedCase_Patch));
            }
        }

        private void SlashOpensChat_SettingChanged(object sender, EventArgs e)
        {
            if (SlashOpensChat.Value)
            {
                sChatSlashHarmony.PatchAll(typeof(Chat_Slash_Patches));
            }
            else
            {
                sChatSlashHarmony.UnpatchSelf();
            }
        }

        private void DefaultShout_SettingChanged(object sender, EventArgs e)
        {
            if (DefaultShout.Value)
            {
                sChatShoutHarmony.PatchAll(typeof(Chat_Shout_Patch));
            }
            else
            {
                sChatShoutHarmony.UnpatchSelf();
            }
        }

        private void ShowShoutPings_SettingChanged(object sender, EventArgs e)
        {
            if (ShowShoutPings.Value)
            {
                sMinimapHarmony.UnpatchSelf();
            }
            else
            {
                sMinimapHarmony.PatchAll(typeof(Minimap_Patches));
            }
        }

        private void Distance_SettingChanged(object sender, EventArgs e)
        {
            ClampConfig();

            foreach (Talker talker in sTalkers)
            {
                talker.m_visperDistance = WhisperDistance.Value;
                talker.m_normalDistance = TalkDistance.Value;
            }
        }

        [HarmonyPatch(typeof(Chat))]
        private static class Chat_Patches
        {
            [HarmonyPatch("Awake"), HarmonyPostfix]
            private static void Awake_Postfix(Chat __instance)
            {
                __instance.m_hideDelay = HideDelay.Value;
                sChat = __instance;

                // Make the chat window click-through so that it is still possible to interact with UI behind it like the map or crafting menu
                Graphic[] graphics = __instance.m_chatWindow.GetComponentsInChildren<Graphic>();
                foreach (Graphic graphic in graphics)
                {
                    graphic.raycastTarget = false;
                }
            }
        }

        [HarmonyPatch(typeof(Player))]
        private static class Player_Patches
        {
            [HarmonyPatch("Awake"), HarmonyPostfix]
            private static void Awake_Postfix(Player __instance)
            {
                Talker talker = __instance.GetComponent<Talker>();
                talker.m_visperDistance = WhisperDistance.Value;
                talker.m_normalDistance = TalkDistance.Value;
                sTalkers.Add(talker);
            }

            [HarmonyPatch("OnDestroy"), HarmonyPrefix]
            private static void OnDestroy_Prefix(Player __instance)
            {
                sTalkers.Remove(__instance.GetComponent<Talker>());
            }
        }

        [HarmonyPatch(typeof(Chat))]
        private static class Chat_AlwaysShow_Patch
        {
            [HarmonyPatch("Update"), HarmonyPrefix]
            private static bool Update_Prefix(Chat __instance)
            {
                // Resetting this to 0 restarts the window hide timer (and makes the window visible)
                sHideTimerField.SetValue(__instance, 0.0f);
                return true;
            }
        }

        [HarmonyPatch(typeof(Chat))]
        private static class Chat_Slash_Patches
        {
            private enum TranspilerState
            {
                Searching,
                Inserting,
                Labeling,
                Searching2,
                Labeling2,
                Finishing
            }

            [HarmonyPatch("Update"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                LocalBuilder isSlashPressed = generator.DeclareLocal(typeof(bool));
                isSlashPressed.SetLocalSymInfo(nameof(isSlashPressed));

                Label label1 = generator.DefineLabel();
                Label label2 = generator.DefineLabel();

                yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                yield return new CodeInstruction(OpCodes.Stloc, isSlashPressed.LocalIndex);

                TranspilerState state = TranspilerState.Searching;

                foreach (CodeInstruction instruction in instructions)
                {
                    if (state == TranspilerState.Inserting)
                    {
                        if (instruction.opcode != OpCodes.Brfalse) throw new InvalidOperationException($"[BetterChat] {nameof(Chat_Slash_Patches)} encountered unexpected IL code. Unable to patch. This is most likely due to a game update changing the target code. Disable {nameof(SlashOpensChat)} in the config as a workaround until the mod can be fixed.");

                        // Previous instruction was checking if enter is pressed. If so, skip the slash key check (boolean OR).
                        yield return new CodeInstruction(OpCodes.Brtrue, label1);

                        // Check for slash key if enter is not pressed. Also store the result of the check.
                        yield return new CodeInstruction(OpCodes.Ldc_I4, (int)KeyCode.Slash);
                        yield return new CodeInstruction(OpCodes.Call, typeof(Input).GetMethod(nameof(Input.GetKeyDown), new[] { typeof(KeyCode) }));
                        yield return new CodeInstruction(OpCodes.Stloc, isSlashPressed.LocalIndex);
                        yield return new CodeInstruction(OpCodes.Ldloc, isSlashPressed.LocalIndex);

                        state = TranspilerState.Labeling;
                    }
                    else if (state == TranspilerState.Labeling)
                    {
                        // Label this instruction as the one to jump to if the enter key is pressed (to skip the slash check).
                        instruction.labels.Add(label1);
                        state = TranspilerState.Searching2;
                    }
                    else if (state == TranspilerState.Labeling2)
                    {
                        // Label this instruction as the one to jump to when skipping the slash insertion code.
                        instruction.labels.Add(label2);
                        state = TranspilerState.Finishing;
                    }

                    yield return instruction;

                    if (state == TranspilerState.Searching && instruction.opcode == OpCodes.Call)
                    {
                        MethodBase method = (MethodBase)instruction.operand;
                        if (method.Name == nameof(Input.GetKeyDown))
                        {
                            state = TranspilerState.Inserting;
                        }
                    }
                    else if (state == TranspilerState.Searching2 && instruction.opcode == OpCodes.Callvirt)
                    {
                        MethodBase method = (MethodBase)instruction.operand;
                        if (method.Name == nameof(InputField.ActivateInputField))
                        {
                            // If slash was not pressed (meaning enter was), then skip the block below
                            yield return new CodeInstruction(OpCodes.Ldloc, isSlashPressed.LocalIndex);
                            yield return new CodeInstruction(OpCodes.Brfalse, label2);

                            // If slash was pressed, replace current chat input string with a / character
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Ldfld, typeof(Chat).GetField(nameof(Chat.m_input)));
                            yield return new CodeInstruction(OpCodes.Ldstr, "/");
                            yield return new CodeInstruction(OpCodes.Callvirt, typeof(InputField).GetMethod("set_text"));

                            // Move caret to end (after slash) in LateUpdate
                            yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                            yield return new CodeInstruction(OpCodes.Stsfld, typeof(BetterChatPlugin).GetField(nameof(sMoveCaretToEnd), BindingFlags.Static | BindingFlags.NonPublic));

                            state = TranspilerState.Labeling2;
                        }
                    }
                }
            }

            [HarmonyPatch("LateUpdate"), HarmonyPostfix]
            private static void LateUpdate_Postfix(Chat __instance)
            {
                if (sMoveCaretToEnd)
                {
                    __instance.m_input.MoveTextEnd(false);
                    sMoveCaretToEnd = false;
                }
            }
        }

        [HarmonyPatch(typeof(Chat))]
        private static class Chat_Show_Patch
        {
            [HarmonyPatch(nameof(Chat.OnNewChatMessage)), HarmonyPostfix]
            private static void OnNewChatMessage_Postfix(Chat __instance, GameObject go, long senderID, Vector3 pos, Talker.Type type, string user, string text)
            {
                // Resetting this to 0 restarts the window hide timer (and makes the window visible)
                sHideTimerField.SetValue(__instance, 0.0f);
            }
        }

        [HarmonyPatch(typeof(Chat))]
        private static class Chat_MixedCase_Patch
        {
            [HarmonyPatch("AddString", new[] { typeof(string), typeof(string), typeof(Talker.Type) }), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> AddString_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return StripForcedCase(instructions);
            }

            [HarmonyPatch("AddInworldText"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> AddInworldText_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return StripForcedCase(instructions);
            }
        }

        [HarmonyPatch(typeof(Chat))]
        private static class Chat_Shout_Patch
        {
            [HarmonyPatch("InputText"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> InputText_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ldstr && instruction.operand.Equals("say "))
                    {
                        instruction.operand = "s ";
                    }
                    yield return instruction;
                }
            }
        }

        [HarmonyPatch(typeof(Minimap))]
        private static class Minimap_Patches
        {
            [HarmonyPatch("UpdateDynamicPins"), HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // Using a list to make looking ahead simpler
                List<CodeInstruction> modified = new List<CodeInstruction>(instructions);

                for (int i = 0; i < modified.Count - 1; ++i)
                {
                    if (modified[i + 1].opcode == OpCodes.Call)
                    {
                        MethodBase method = (MethodBase)modified[i + 1].operand;
                        if (method.Name == "UpdateShoutPins")
                        {
                            modified.RemoveRange(i, 2);
                            break;
                        }
                    }
                }

                return modified;
            }
        }

        private static IEnumerable<CodeInstruction> StripForcedCase(IEnumerable<CodeInstruction> instructions)
        {
            // Using a list to make looking ahead simpler
            List<CodeInstruction> modified = new List<CodeInstruction>(instructions);

            for (int i = 0; i < modified.Count; ++i)
            {
                if (modified[i].opcode == OpCodes.Callvirt)
                {
                    MethodBase method = modified[i].operand as MethodBase;
                    if (method != null)
                    {
                        if (method.Name == nameof(string.ToLowerInvariant) || method.Name == nameof(string.ToUpper))
                        {
                            // Remove
                            //   text = text.ToLowerInvariant()
                            // Or
                            //   text = text.ToUpper()
                            // ldarg.2, callvirt, starg.s
                            modified.RemoveRange(i - 1, 3);
                            i -= 2;
                        }
                    }
                }
            }

            return modified;
        }
    }
}
