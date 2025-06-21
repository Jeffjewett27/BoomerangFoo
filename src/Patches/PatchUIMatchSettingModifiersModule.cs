using HarmonyLib;
using System;

using BoomerangFoo.GameModes;
using BoomerangFoo.UI;

namespace BoomerangFoo.Patches
{
    [HarmonyPatch(typeof(UIMatchSettingModifiersModule), nameof(UIMatchSettingModifiersModule.LoadValuesFromDisk))]
    class UIMatchSettingModifiersModuleLoadValuesFromDiskPatch
    {
        static void Postfix()
        {
            // Load custom settings after game settings
            Modifiers.LoadSettings();
        }
    }
}