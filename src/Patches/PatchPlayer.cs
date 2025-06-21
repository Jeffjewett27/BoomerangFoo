using BoomerangFoo.GameModes;
using BoomerangFoo.Powerups;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using static Player;

namespace BoomerangFoo.Patches
{
    class PatchPlayer
    {
        public static event Action<Player> OnPostGetReady;
        public static void InvokePostGetReady(Player player) { OnPostGetReady?.Invoke(player); }

        public static event Action<Player> OnPostSpawnIn;
        public static void InvokePostSpawnIn(Player player) { OnPostSpawnIn?.Invoke(player); }

        public static event Action<Player> OnPreInit;
        public static void InvokePreInit(Player player) { OnPreInit?.Invoke(player); }

        public static event Action<Player> OnPreUpdate;
        public static void InvokePreUpdate(Player player) { OnPreUpdate?.Invoke(player); }

        public static event Action<Player> OnPostUpdate;
        public static void InvokePostUpdate(Player player) { OnPostUpdate?.Invoke(player); }

        public static event Action<Player> OnPreStartShield;
        public static void InvokePreStartShield(Player player) { OnPreStartShield?.Invoke(player); }

        public static event Action<Player> OnPostCreateDecoy;
        public static void InvokePostCreateDecoy(Player player) { OnPostCreateDecoy?.Invoke(player); }

        public static event Action<Player> OnPostRunGoldenDiscTimer;
        public static void InvokePostRunGoldenDiscTimer(Player player) { OnPostRunGoldenDiscTimer?.Invoke(player); }

        public static event Action<Player> OnPreDie;
        public static void InvokePreDie(Player player) { OnPreDie?.Invoke(player); }

        public static event Action<Player> OnPostDie;
        public static void InvokePostDie(Player player) { OnPostDie?.Invoke(player); }

        public static Func<Player, bool> PreStartFall;

        public static Predicate<Player> DoToggleGoldenDiscPFX;

        public static Func<Player, Player.HoldingGoldenDisc, Player.HoldingGoldenDisc, float> GoldenDiscPenalty;

        public static event Action<Player, PowerupType> OnPreStartPowerup;
        public static void InvokePreStartPowerup(Player player, PowerupType powerupType) { OnPreStartPowerup?.Invoke(player, powerupType); }

        public static event Action<Player, PowerupType> OnPostStartPowerup;
        public static void InvokePostStartPowerup(Player player, PowerupType powerupType) { OnPostStartPowerup?.Invoke(player, powerupType); }

        public static event Action<Player> OnPreStartFall;
        public static void InvokePreStartFall(Player player) { OnPreStartFall?.Invoke(player); }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetReady))]
    class PlayerGetReadyPatch
    {
        static void Postfix(Player __instance)
        {
            PatchPlayer.InvokePostGetReady(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), "SpawnIn")]
    class PlayerSpawnInPatch
    {
        static void Postfix(Player __instance)
        {
            PatchPlayer.InvokePostSpawnIn(__instance);
            PowerupType startupPowers = GameMode.selected.gameSettings.StartupPowerUps;
            if (startupPowers != 0 && Singleton<GameManager>.Instance.roundNumber == 1)
            {
                CommonFunctions.GetEnumPowerUpValues(startupPowers).ForEach(delegate (PowerupType i)
                {
                    __instance.StartPowerup(i);
                });
            }
        }
    }

    [HarmonyPatch(typeof(Player), "Start")]
    class PlayerStartPatch
    {

        static void Postfix(Player __instance)
        {
            __instance.gameObject.AddComponent<PlayerState>();
        }
    }

    [HarmonyPatch(typeof(Player), "Init")]
    class PlayerInitPatch
    {
        static readonly FieldInfo activePowerups = typeof(Player).GetField("maxActivePowerups", BindingFlags.NonPublic | BindingFlags.Instance);

        static void Prefix(Player __instance)
        {
            PatchPlayer.InvokePreInit(__instance);
            PowerupManager.powerupHistories[__instance.powerupHistory] = __instance;
            activePowerups.SetValue(__instance, GameMode.selected.gameSettings.MaxPowerups);
        }
    }

    [HarmonyPatch(typeof(Player), "Update")]
    class PlayerUpdatePatch
    {
        static void Prefix(Player __instance)
        {
            PatchPlayer.InvokePreUpdate(__instance);
        }

        static void Postfix(Player __instance)
        {
            PatchPlayer.InvokePostUpdate(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), "StartReverseInputsTimer")]
    class PlayerStartReverseInputsTimerPatch
    {
        static void Prefix(Player __instance, ref float duration)
        {
            duration = BamboozlePowerup.StartReverseInputs(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), "RunReverseInputsTimer")]
    class PlayerRunReverseInputsTimerPatch
    {
        static void Prefix(Player __instance)
        {
            BamboozlePowerup.StopReverseInputs(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.StartShield))]
    class PlayerStartShieldPatch
    {
        static void Prefix(Player __instance)
        {
            PatchPlayer.InvokePreStartShield(__instance);
        }

        static void Postfix(Player __instance)
        {
            __instance.shieldHitsLeft = CommonFunctions.GetPlayerState(__instance)?.shieldHits ?? 1;
        }
    }

    [HarmonyPatch(typeof(Player), "StartShieldHitCounter")]
    class PlayerStartStartShieldHitCounterPatch
    {
        static void Postfix(Player __instance)
        {
            __instance.shieldHitsLeft = CommonFunctions.GetPlayerState(__instance)?.shieldHits ?? 1;
        }
    }

    [HarmonyPatch(typeof(Player), "CreateDecoy")]
    class PlayerCreateDecoyPatch
    {
        static void Postfix(Player __instance)
        {
            PatchPlayer.InvokePostCreateDecoy(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), "RunGoldenDiscTimer")]
    class PlayerRunGoldenDiscTimerPatch
    {
        static void Postfix(Player __instance)
        {
            PatchPlayer.InvokePostRunGoldenDiscTimer(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), "HasGoldenDisc", MethodType.Setter)]
    class PlayerSetHasGoldenDiscPatch
    {
        static void Prefix(Player __instance, Player.HoldingGoldenDisc value)
        {
            if (PatchPlayer.GoldenDiscPenalty != null)
            {
                Player.HoldingGoldenDisc oldHasGolden = __instance.HasGoldenDisc;
                float penalty = 0;
                if (oldHasGolden != value && (oldHasGolden == HoldingGoldenDisc.No || oldHasGolden == HoldingGoldenDisc.IsDropped))
                {
                    // counteract the default penalty
                    penalty -= Singleton<GameManager>.Instance.goldenGoalScore * 0.05f;
                }
                penalty += PatchPlayer.GoldenDiscPenalty(__instance, oldHasGolden, value);
                __instance.goldenHoldTime -= penalty;
            }
        }

        static void Postfix(Player __instance)
        {
            __instance.goldenHoldTime = Math.Max(__instance.goldenHoldTime, 0);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnPickupColliderTriggered))]
    class PlayerOnPickupColliderTriggeredPatch
    {
        private static bool? canCatchTeammates = null;

        static void Prefix(Player __instance, Collider2D collider)
        {
            if (!collider.CompareTag("Disc"))
            {
                return;
            }
            Disc disc = collider.GetComponent<Disc>();
            if (disc.IsGoldenDisc)
            {
                canCatchTeammates = Singleton<SettingsManager>.Instance.MatchSettings.canCatchTeammatesDiscs;
                Singleton<SettingsManager>.Instance.MatchSettings.canCatchTeammatesDiscs = true;
            }
        }

        static void Postfix(Player __instance)
        {
            if (canCatchTeammates != null)
            {
                Singleton<SettingsManager>.Instance.MatchSettings.canCatchTeammatesDiscs = (bool)canCatchTeammates;
                canCatchTeammates = null;
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.PickUpDisc))]
    class PlayerPickUpDiscPatch
    {
        static bool Prefix(Player __instance)
        {
            if (GameMode.selected.gameSettings.NoBoomerangs)
            {
                return false; // don't pick up boomerangs
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.RespawnDisc))]
    class PlayerRespawnDiscPatch
    {
        static bool Prefix(Player __instance)
        {
            if (GameMode.selected.gameSettings.NoBoomerangs)
            {
                return false; // don't pick up boomerangs
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Die))]
    class PlayerDiePatch
    {
        public static bool stopDie = false;

        static bool Prefix(Player __instance)
        {
            GameManagerAddPlayerKillPatch.killedPlayer = __instance;
            PatchPlayer.InvokePreDie(__instance);

            if (stopDie)
            {
                stopDie = false;
                return false;
            }
            return true;
        }

        static void Postfix(Player __instance)
        {
            PatchPlayer.InvokePostDie(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.StartFall))]
    class PlayerStartFallPatch
    {
        static bool Prefix(Player __instance)
        {
            if (PatchPlayer.PreStartFall != null)
            {
                bool doOriginal = PatchPlayer.PreStartFall.Invoke(__instance);
                return doOriginal;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Player), "ToggleGoldenDiscPFX")]
    class PlayerToggleGoldenDiscPFXPatch
    {
        static bool Prefix(Player __instance)
        {
            if (PatchPlayer.DoToggleGoldenDiscPFX == null) return true;
            return PatchPlayer.DoToggleGoldenDiscPFX.Invoke(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.StartPowerup))]
    class PlayerStartPowerupPatch
    {
        static void Prefix(Player __instance, PowerupType newPowerup)
        {
            PatchPlayer.InvokePreStartPowerup(__instance, newPowerup);
        }

        static void Postfix(Player __instance, PowerupType newPowerup)
        {
            PatchPlayer.InvokePostStartPowerup(__instance, newPowerup);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ClearPowerups))]
    class PlayerClearPowerupsPatch
    {
        static void Prefix(Player __instance)
        {
            PowerupManager.InvokeRemovePowerup(__instance, __instance.powerupHistory, PowerupType.None);
        }
    }
}
