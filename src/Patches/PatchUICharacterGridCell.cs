using BoomerangFoo.Settings;
using HarmonyLib;
using UnityEngine;

namespace BoomerangFoo.Patches
{
    [HarmonyPatch(typeof(UICharacterGridCell), nameof(UICharacterGridCell.BootAllOtherPlayersExceptMe))]
    class UICharacterGridCellBootAllOtherPlayersExceptMePatch
    {
        static bool Prefix()
        {
            // if duplicate characters allowed, stop the blocker from running
            return !_CustomSettings.EnableDuplicatedCharacters;
        }
    }

    [HarmonyPatch(typeof(UICharacterGridCell), nameof(UICharacterGridCell.SetupCharacter))]
    class UICharacterGridCellSetupCharacterPatch
    {
        static void Postfix(UICharacterGridCell __instance)
        {
            UIMenuLobby uIMenuLobby = Singleton<UIManager>.Instance.GetMenu(UIMenuBase.MenuType.Lobby) as UIMenuLobby;
            if (uIMenuLobby != null)
            {
                // Add player icons up to maximum players
                for (int i = 6; i < ModSettings.Instance.MaxPlayers; i++)
                {
                    UICharacterGridPlayerIcon uICharacterGridPlayerIcon = Object.Instantiate(uIMenuLobby.characterGridPlayerIconPrefab, __instance.container, worldPositionStays: false);
                    uICharacterGridPlayerIcon.SetPlayer(i, PlayerType.Human);
                    __instance.playerIconPool.Add(uICharacterGridPlayerIcon.playerID, uICharacterGridPlayerIcon);
                    uICharacterGridPlayerIcon.SetActive(setActive: false);
                }
            }
            __instance.CheckAltCharacter();
            __instance.CheckLockedState();
        }
    }
}
