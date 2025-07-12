using BoomerangFoo.Settings;
using HarmonyLib;
using System;
using System.Reflection;

namespace BoomerangFoo.Patches
{
    [HarmonyPatch(typeof(UIMenuLobby), nameof(UIMenuLobby.GetClosestAvailableGridCell))]
    class UIMenuLobbyGetClosestAvailableGridCellPatch
    {
        static UICharacterGridCell.SelectionState[] selections;
        static void Prefix(UIMenuLobby __instance)
        {
            if (_CustomSettings.EnableDuplicatedCharacters)
            {
                if (selections == null || selections.Length < __instance.characterGridCells.Length)
                {
                    selections = new UICharacterGridCell.SelectionState[__instance.characterGridCells.Length];
                }
                for (int i = 0; i < __instance.characterGridCells.Length; i++)
                {
                    var cell = __instance.characterGridCells[i];
                    selections[i] = cell.selectionState;
                    cell.selectionState = UICharacterGridCell.SelectionState.Deselected;
                }
            }
        }

        static void Postfix(UIMenuLobby __instance)
        {
            if (_CustomSettings.EnableDuplicatedCharacters)
            {
                for (int i = 0; i < __instance.characterGridCells.Length; i++)
                {
                    var cell = __instance.characterGridCells[i];
                    cell.selectionState = selections[i];
                }
            }
        }
    }

    [HarmonyPatch(typeof(UIMenuLobby), "IsGridCellAvailable")]
    class UIMenuLobbyIsGridCellAvailablePatch
    {
        static bool Prefix(ref bool __result)
        {
            // if duplicated characters enabled, then don't block
            if (_CustomSettings.EnableDuplicatedCharacters)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(UIMenuLobby), nameof(UIMenuLobby.Init))]
    class UIMenuLobbyInitPatch
    {
        static void Postfix(UIMenuLobby __instance)
        {
            // Add impossible diffulty option
            __instance.botDifficultySlider.SetMaxValue((int)SettingsManager.BotDifficulty.impossible);
            var difficultyNames = __instance.botDifficultySlider.stringMap;
            Array.Resize(ref __instance.botDifficultySlider.stringMap, __instance.botDifficultySlider.stringMap.Length + 1);
            __instance.botDifficultySlider.stringMap[__instance.botDifficultySlider.stringMap.Length - 1] = "Impossible";

            // Increase maximum number of bots
            // First change the slider
            int prevBots = __instance.numBotsSlider.maxValue;
            int numExtra = ModSettings.Instance.MaxPlayers - prevBots - 1;
            int prevLen = __instance.numBotsSlider.stringMap.Length;
            __instance.numBotsSlider.SetMaxValue(prevBots + numExtra);
            Array.Resize(ref __instance.numBotsSlider.stringMap, prevLen + numExtra);
            for (int i = prevLen; i < prevLen + numExtra; i++)
            {
                __instance.numBotsSlider.stringMap[i] = $"{i} (experimental)";
            }
            
            // Next clone UILobbyPlayer objects
            prevLen = __instance.uiPlayers.Length;
            Array.Resize(ref __instance.uiPlayers, ModSettings.Instance.MaxPlayers);
            UILobbyPlayer lastPlayer = __instance.uiPlayers[prevLen - 1];

            var cloneMethod = typeof(object)
                .GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = prevLen; i < __instance.uiPlayers.Length; i++)
            {
                __instance.uiPlayers[i] = (UILobbyPlayer)cloneMethod!.Invoke(lastPlayer, null)!;
                __instance.uiPlayers[i].SetupUIPlayer(i);
                __instance.uiPlayers[i].playerType = PlayerType.Bot;
                __instance.uiPlayers[i].inputDevice = UILobbyPlayer.InputDevice.None;
            }

        }
    }
}
