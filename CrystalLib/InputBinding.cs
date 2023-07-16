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

using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CrystalLib
{
    /// <summary>
    /// Helper to register and manage a player input binding
    /// </summary>
	public class InputBinding : IDisposable
    {
        /// <summary>
        /// The name to use when registering the input binding
        /// </summary>
        /// <remarks>
        /// The name of the binding should be unique across the entire game. For a list of built-in bindings,
        /// see the method ZInput.Reset in assembly_utils which ships with the game.
        /// </remarks>
        public string Name { get; }

        /// <summary>
        /// The config entry for the key code used for the binding
        /// </summary>
		public ConfigEntry<KeyCode> ConfigEntry { get; }

        /// <summary>
        /// Fires when a player activates the bound input
        /// </summary>
        public event EventHandler<InputEventArgs> InputPressed;

        private static readonly  List<InputBinding> sInstances;

		private static readonly Harmony sZInputHarmony;
		private static readonly Harmony sPlayerControllerHarmony;

		private static readonly MethodInfo sTakeInputMethod;
		private static readonly FieldInfo sCharacterField;
		private static readonly FieldInfo sViewField;

		static InputBinding()
		{
            sInstances = new List<InputBinding>();

			sTakeInputMethod = typeof(PlayerController).GetMethod("TakeInput", BindingFlags.NonPublic | BindingFlags.Instance);
			sCharacterField = typeof(PlayerController).GetField("m_character", BindingFlags.NonPublic | BindingFlags.Instance);
			sViewField = typeof(PlayerController).GetField("m_nview", BindingFlags.NonPublic | BindingFlags.Instance);

			sZInputHarmony = new Harmony("CrystalLib_KeyBind_ZInput");
			sPlayerControllerHarmony = new Harmony("CrystalLib_KeyBind_PlayerController");

			sZInputHarmony.PatchAll(typeof(ZInput_Patches));
			sPlayerControllerHarmony.PatchAll(typeof(PlayerController_Patches));
		}

        /// <summary>
        /// Creates and registers an input binding
        /// </summary>
        /// <param name="name">The name of the binding. Should be globally unique.</param>
        /// <param name="configEntry">A config entry for the key code to use for the binding</param>
        public InputBinding(string name, ConfigEntry<KeyCode> configEntry)
		{
            Name = name;
			ConfigEntry = configEntry;

			ConfigEntry.SettingChanged += ConfigEntry_SettingChanged;

            sInstances.Add(this);

            if (ZInput.instance != null)
			{
                ZInput.instance.AddButton(Name, ConfigEntry.Value);
			}
		}

		~InputBinding()
		{
            Dispose(false);
		}

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public void Dispose()
		{
            GC.SuppressFinalize(this);
            Dispose(true);
		}

        private void Dispose(bool disposing)
		{
            if (disposing)
            {
                ConfigEntry.SettingChanged -= ConfigEntry_SettingChanged;
                sInstances.Remove(this);
            }
        }

        private void ConfigEntry_SettingChanged(object sender, EventArgs e)
        {
            if (ZInput.instance == null) return;

            ZInput.instance.Setbutton(Name, ConfigEntry.Value);
        }

        [HarmonyPatch(typeof(PlayerController))]
        private static class PlayerController_Patches
        {
            [HarmonyPatch("FixedUpdate"), HarmonyPostfix]
            private static void FixedUpdate_Postfix(PlayerController __instance)
            {
                ZNetView view = (ZNetView)sViewField.GetValue(__instance);
                if (view && !view.IsOwner())
                {
                    return;
                }
                if (!(bool)sTakeInputMethod.Invoke(__instance, null))
                {
                    return;
                }

                foreach (InputBinding instance in sInstances)
				{
                    if (ZInput.GetButtonDown(instance.Name))
                    {
                        Player player = (Player)sCharacterField.GetValue(__instance);
                        instance.InputPressed?.Invoke(instance, new InputEventArgs(player));
					}
				}
            }
        }

        [HarmonyPatch(typeof(ZInput))]
        private static class ZInput_Patches
        {
            [HarmonyPatch("Reset"), HarmonyPostfix]
            private static void Reset_Postfix(ZInput __instance)
            {
                foreach (InputBinding instance in sInstances)
                {
                    __instance.AddButton(instance.Name, instance.ConfigEntry.Value);
                }
            }
        }
    }

	public class InputEventArgs : EventArgs
	{
        public Player Player { get; }

		public InputEventArgs(Player player)
		{
			Player = player;
		}
	}
}
