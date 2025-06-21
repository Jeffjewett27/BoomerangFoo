using HarmonyLib;
using UnityEngine;

using BoomerangFoo.GameModes;

namespace BoomerangFoo.Patches
{
    [HarmonyPatch(typeof(Actor), nameof(Actor.GetDisarmed))]
    class ActorGetDisarmedPatch
    {
        static void Prefix(Actor __instance, ref Vector3 disarmDirection)
        {
            disarmDirection = disarmDirection * GameMode.selected.gameSettings.KnockbackFactor;
        }
    }
}
