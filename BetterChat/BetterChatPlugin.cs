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
    [BepInPlugin("dev.crystal.betterchat", "Better Chat", "1.4.0.0")]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")]
    public class BetterChatPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> AlwaysVisible;
        public static ConfigEntry<float> HideDelay;
        public static ConfigEntry<bool> ForceCase;
        public static ConfigEntry<bool> SlashOpensChat;
        public static ConfigEntry<bool> DefaultShout;
        public static ConfigEntry<bool> ShowShoutPings;
        public static ConfigEntry<float> TalkDistance;
        public static ConfigEntry<float> WhisperDistance;

        private static readonly FieldInfo sHideTimerField;

        private static bool sMoveCaretToEnd = false;

        static BetterChatPlugin()
        {
            sHideTimerField = typeof(Chat).GetField("m_hideTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private void Awake()
        {
            AlwaysVisible = Config.Bind("Chat", nameof(AlwaysVisible), false, "If True, the chat window will remain visible at all times. If False, the chat window will appear when new messages are received.");
            HideDelay = Config.Bind("Chat", nameof(HideDelay), 10.0f, "The time, in seconds, to keep the chat window visible after sending or receiving a message. Minimum is 0.5. Has no effect if AlwaysVisible=true.");
            ForceCase = Config.Bind("Chat", nameof(ForceCase), false, "If True, shout will be in all caps and whisper will be in all lowercase (game default). If False, messages will appear as they were originally entered.");
            SlashOpensChat = Config.Bind("Chat", nameof(SlashOpensChat), true, "If True, pressing the slash key (/) will open the chat window and start a message.");
            DefaultShout = Config.Bind("Chat", nameof(DefaultShout), false, "If True, text entered will shout by default - type /s for talk. If False, chat will be talk by default - type /s for shout.");
            ShowShoutPings = Config.Bind("Chat", nameof(ShowShoutPings), true, "If True, pings will show on your map when players shout (game default). If False, the pings will not show. (Other players can still see your shout pings.)");
            TalkDistance = Config.Bind("Chat", nameof(TalkDistance), 15.0f, "The maximum distance from a player at which you will receive their normal chat messages (not whisper or shout). Game default is 15. Acceptable range is 1-100.");
            WhisperDistance = Config.Bind("Chat", nameof(WhisperDistance), 4.0f, "The maximum distance from a player at which you will receive their whispered chat messages. Game default is 4. Acceptable range is 1-20");
            
            Harmony.CreateAndPatchAll(typeof(Chat_Awake_Patch));
            Harmony.CreateAndPatchAll(typeof(Talker_Awake_Patch));
            if (AlwaysVisible.Value)
            {
                Harmony.CreateAndPatchAll(typeof(Chat_Update_Patch_AlwaysVisible));
            }
            else
            {
                Harmony.CreateAndPatchAll(typeof(Chat_OnNewChatMessage_Patch));
            }
            if (!ForceCase.Value)
            {
                Harmony.CreateAndPatchAll(typeof(Chat_AddString_Patch));
                Harmony.CreateAndPatchAll(typeof(Chat_AddInWorldText_Patch));
            }
            if (DefaultShout.Value)
            {
                Harmony.CreateAndPatchAll(typeof(Chat_InputText_Patch));
            }
            if (!ShowShoutPings.Value)
            {
                Harmony.CreateAndPatchAll(typeof(Minimap_UpdateDynamicPins_Patch));
            }
            if (SlashOpensChat.Value)
            {
                Harmony.CreateAndPatchAll(typeof(Chat_Update_Patch_SlashOpensChat));
                Harmony.CreateAndPatchAll(typeof(Chat_LateUpdate_Patch_SlashOpensChat));
            }
        }

        [HarmonyPatch(typeof(Chat), "Awake")]
        private static class Chat_Awake_Patch
        {
            private static void Postfix(Chat __instance)
            {
                // Minimum delay prevents issues like flickering or permanently hidden chat window
                __instance.m_hideDelay = Mathf.Max(0.5f, HideDelay.Value);

                // Make the chat window click-through so that it is still possible to interact with UI behind it like the map or crafting menu
                Graphic[] graphics = __instance.m_chatWindow.GetComponentsInChildren<Graphic>();
                foreach (Graphic graphic in graphics)
                {
                    graphic.raycastTarget = false;
                }
            }
        }

        [HarmonyPatch(typeof(Talker), "Awake")]
        private static class Talker_Awake_Patch
        {
            private static void Postfix(Talker __instance)
            {
                // Values are clamped primarily for privacy concerns
                __instance.m_visperDistance = Mathf.Clamp(WhisperDistance.Value, 1.0f, 20.0f);
                __instance.m_normalDistance = Mathf.Clamp(TalkDistance.Value, 1.0f, 100.0f);
            }
        }

        [HarmonyPatch(typeof(Chat), "Update")]
        private static class Chat_Update_Patch_AlwaysVisible
        {
            private static bool Prefix(Chat __instance)
            {
                // Resetting this to 0 restarts the window hide timer (and makes the window visible)
                sHideTimerField.SetValue(__instance, 0.0f);
                return true;
            }
        }

        [HarmonyPatch(typeof(Chat), "Update")]
        private static class Chat_Update_Patch_SlashOpensChat
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

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
                        if (instruction.opcode != OpCodes.Brfalse) throw new InvalidOperationException($"[BetterChat] {nameof(Chat_Update_Patch_SlashOpensChat)} encountered unexpected IL code. Unable to patch. This is most likely due to a game update changing the target code. Disable {nameof(SlashOpensChat)} in the config as a workaround until the mod can be fixed.");

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
        }

        [HarmonyPatch(typeof(Chat), "LateUpdate")]
        private static class Chat_LateUpdate_Patch_SlashOpensChat
        {
            private static void Postfix(Chat __instance)
            {
                if (sMoveCaretToEnd)
                {
                    __instance.m_input.MoveTextEnd(false);
                    sMoveCaretToEnd = false;
                }
            }
        }

        [HarmonyPatch(typeof(Chat), nameof(Chat.OnNewChatMessage))]
        private static class Chat_OnNewChatMessage_Patch
        {
            private static void Postfix(Chat __instance, GameObject go, long senderID, Vector3 pos, Talker.Type type, string user, string text)
            {
                // Resetting this to 0 restarts the window hide timer (and makes the window visible)
                sHideTimerField.SetValue(__instance, 0.0f);
            }
        }

        [HarmonyPatch(typeof(Chat), "AddString", new[] { typeof(string), typeof(string), typeof(Talker.Type) })]
        private static class Chat_AddString_Patch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return StripForcedCase(instructions);
            }
        }

        [HarmonyPatch(typeof(Chat), "AddInworldText")]
        private static class Chat_AddInWorldText_Patch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return StripForcedCase(instructions);
            }
        }

        [HarmonyPatch(typeof(Chat), "InputText")]
        private static class Chat_InputText_Patch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // Using a list to make looking ahead simpler
                List<CodeInstruction> modified = new List<CodeInstruction>(instructions);

                for (int i = 0; i < modified.Count - 1; ++i)
                {
                    if (modified[i + 1].opcode == OpCodes.Stloc_1)
                    {
                        if (modified[i].opcode == OpCodes.Ldc_I4_1)
                        {
                            // Replace
                            //   Talker.Type type = Talker.Type.Normal
                            // With
                            //   Talker.Type type = Talker.Type.Shout
                            modified[i].opcode = OpCodes.Ldc_I4_2;
                        }
                        else if (modified[i].opcode == OpCodes.Ldc_I4_2)
                        {
                            // Replace
                            //   type = Talker.Type.Shout
                            // With
                            //   type = Talker.Type.Normal
                            modified[i].opcode = OpCodes.Ldc_I4_1;
                        }
                    }
                }

                return modified;
            }
        }

        [HarmonyPatch(typeof(Minimap), "UpdateDynamicPins")]
        private static class Minimap_UpdateDynamicPins_Patch
        {
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
