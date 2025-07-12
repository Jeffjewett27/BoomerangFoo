using HarmonyLib;
using System;
using Rewired;
using System.Reflection;
using RewiredConsts;

namespace BoomerangFoo.Patches
{
    [HarmonyPatch(typeof(UILobbyPlayer), nameof(UILobbyPlayer.AssignInputDevice))]
    class UILobbyPlayerAssignInputDevicePatch
    {

        static void Prefix(UILobbyPlayer __instance)
        {

            if (__instance.rewiredPlayer == null)
            {
                // Cannot add ReInput at runtime.
                // Just copy player 5 input. This is for a bot anyway. It just needs to not be null.
                Rewired.Player newPlayer = ReInput.players.GetPlayer(5);
                __instance.rewiredPlayer = newPlayer;
            }
        }
    }

}
