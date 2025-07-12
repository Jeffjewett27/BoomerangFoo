using System;
using HarmonyLib;
using BoomerangFoo.GameModes;
using System.Collections;
using UnityEngine;
using BoomerangFoo.Settings;

namespace BoomerangFoo.Patches
{
    public class PatchGameManager
    {
        //public delegate void OnPostUpdateHandler(GameManager gameManager);
        //public static event OnPostUpdateHandler OnPostUpdate;
        public static event Action<GameManager> OnPostUpdate;
        public static void InvokePostUpdate(GameManager gameManager) { OnPostUpdate?.Invoke(gameManager); }

        public static event Action<GameManager> OnPostPrepareRound;
        public static void InvokePostPrepareRound(GameManager gameManager) { OnPostPrepareRound?.Invoke(gameManager); }

        public static event Action<GameManager> OnPreStartMatch;
        public static void InvokePreStartMatch(GameManager gameManager) { OnPreStartMatch?.Invoke(gameManager); }

        public static event Action<GameManager> OnPreStartRoundSequence;
        public static void InvokePreStartRoundSequence(GameManager gameManager) { OnPreStartRoundSequence?.Invoke(gameManager); }

        public static bool NumPlayerCheckDisabled = false;

        public static Func<GameManager, int> OpponentsLeftStandingNow;

        public static event Action<GameManager, Player, Player> OnPreAddPlayerKill;
        public static void InvokePreAddPlayerKill(GameManager gameManager, Player killer, Player killed) { OnPreAddPlayerKill?.Invoke(gameManager, killer, killed); }

        public static Func<GameManager, Player, Player, GameMode.Relationship> PlayerRelationship;

        public static Func<GameManager, Player, float> GoldenDiscPlayerScore;

    }

    [HarmonyPatch(typeof(GameManager), "Update")]
    class GameManagerUpdatePatch
    {

        static void Postfix(GameManager __instance)
        {
            PatchGameManager.InvokePostUpdate(__instance);
        }
    }

    [HarmonyPatch(typeof(GameManager), "PrepareRound")]
    class GameManagerPrepareRoundPatch
    {

        static void Postfix(GameManager __instance)
        {
            PatchGameManager.InvokePostPrepareRound(__instance);
            if (_CustomSettings.SuddenDeathTimeLimit > 0)
            {
                Singleton<SettingsManager>.Instance.MatchSettings.suddenDeathTime = _CustomSettings.SuddenDeathTimeLimit;
            }
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartMatch))]
    class GameManagerStartMatchPatch
    {

        static void Prefix(GameManager __instance)
        {
            PatchGameManager.InvokePreStartMatch(__instance);
        }
    }

    [HarmonyPatch(typeof(GameManager), "StartRoundSequence")]
    class GameManagerStartRoundSequencePatch
    {

        static void Prefix(GameManager __instance)
        {
            PatchGameManager.InvokePreStartRoundSequence(__instance);
        }
    }

    [HarmonyPatch(typeof(GameManager), "CheckPlayersLeftStanding")]
    class GameManagerCheckPlayersLeftStandingPatch
    {

        static bool Prefix(GameManager __instance, ref IEnumerator __result)
        {
            // If player check is disabled, don't run coroutine
            if (PatchGameManager.NumPlayerCheckDisabled)
            {
                __result = EmptyCoroutine();
                return false;
            }
            return true;
        }

        private static IEnumerator EmptyCoroutine()
        {
            yield break; // Ends immediately
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.OpponentsLeftStandingNow))]
    class GameManagerOpponentsLeftStandingNowPatch
    {

        static bool Prefix(GameManager __instance, ref int __result)
        {
            if (PatchGameManager.OpponentsLeftStandingNow != null)
            {
                int oppLeft = PatchGameManager.OpponentsLeftStandingNow(__instance);
                __result = oppLeft;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.AddGoldenDiscPoint))]
    class GameManagerAddGoldenDiscPointPatch
    {
        // This function is hardcoded to use the playerID rather than the RowID that accounts for teams.
        static int winningPlayerId;

        static void Prefix(GameManager __instance, Player winningPlayer)
        {
            // Override playerID to GetRowID, which handles the team case
            winningPlayerId = winningPlayer.playerID;
            winningPlayer.playerID = __instance.GetRowID(winningPlayer);
        }

        static void Postfix(Player winningPlayer)
        {
            // Restore the original id
            winningPlayer.playerID = winningPlayerId;
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.UpdateGoldenDiscScore))]
    class GameManagerUpdateGoldenDiscScorePatch
    {

        static void Prefix(GameManager __instance, Player player, ref float playerScore)
        {
            if (PatchGameManager.GoldenDiscPlayerScore != null)
            {
                playerScore = PatchGameManager.GoldenDiscPlayerScore(__instance, player);
            }
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.AddPlayerKill))]
    class GameManagerAddPlayerKillPatch
    {
        public static Player killedPlayer;
        private static GameMode.Relationship relationship;
        private static int teamKills;

        static void Prefix(GameManager __instance, Player player, ref bool killerWasOpponent)
        {
            // killedPlayer needs to be set from pre_Player.Die
            if (killedPlayer == null) return;

            Player killer = player;
            PatchGameManager.InvokePreAddPlayerKill(__instance, killer, killedPlayer);

            if (PatchGameManager.PlayerRelationship != null)
            {
                relationship = PatchGameManager.PlayerRelationship(__instance, killer, killedPlayer);
                killerWasOpponent = relationship == GameMode.Relationship.Opponent;
            } else
            {
                relationship = killerWasOpponent ? GameMode.Relationship.Opponent : GameMode.Relationship.Teammate;
            }
            int rowID = __instance.GetRowID(killer);
            teamKills = __instance.matchScores[rowID].TeamKills;

            killedPlayer = null;
        }

        static void Postfix(GameManager __instance, Player player)
        {
            int rowID = __instance.GetRowID(player);
            if (relationship != GameMode.Relationship.Teammate)
            {
                // AddPlayerKill can increment own team kills. If not a teammate, reset this
                __instance.matchScores[rowID].TeamKills = teamKills;
            }
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.AreOpponents), [typeof(Actor), typeof(Actor)])]
    class GameManagerAreOpponentsPatch
    {

        static bool Prefix(GameManager __instance, Actor actor1, Actor actor2, ref bool __result)
        {
            Player player = actor1 as Player;
            Player player2 = actor2 as Player;

            // If no custom PlayerRelationship defined, use default behavior
            if (PatchGameManager.PlayerRelationship == null) return true;
            // I don't know when this can happen, but just do default behavior
            if (player == null || player2 == null) return true;

            GameMode.Relationship relationship = PatchGameManager.PlayerRelationship(__instance, player, player2);
            __result = relationship == GameMode.Relationship.Opponent;
            return false;
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Setup))]
    class GameManagerSetupPatch
    {
        static void Postfix(GameManager __instance)
        {
            int maxPlayers = ModSettings.Instance.MaxPlayers;
            Array.Resize(ref __instance.hintBars, maxPlayers);
            Array.Resize(ref __instance.alertBars, maxPlayers);
            Array.Resize(ref __instance.powerupBars, maxPlayers);
            for (int i = 6; i < maxPlayers; i++)
            {
                __instance.characterColors.Add(i, __instance.characterColors[i % 6]);
                HintBar origHintBar = __instance.hintBars[i % 6];
                GameObject hintbarObject = UnityEngine.Object.Instantiate(origHintBar.transform.gameObject);
                hintbarObject.transform.SetParent(origHintBar.transform.parent, false);
                __instance.hintBars[i] = hintbarObject.GetComponent<HintBar>();

                AlertBar origAlertBar = __instance.alertBars[i % 6];
                GameObject alertbarObject = UnityEngine.Object.Instantiate(origAlertBar.transform.gameObject);
                alertbarObject.transform.SetParent(origAlertBar.transform.parent, false);
                __instance.alertBars[i] = alertbarObject.GetComponent<AlertBar>();

                PowerupBar origPowerupBar = __instance.powerupBars[i % 6];
                GameObject powerupObject = UnityEngine.Object.Instantiate(origPowerupBar.transform.gameObject);
                powerupObject.transform.SetParent(origPowerupBar.transform.parent, false);
                __instance.powerupBars[i] = powerupObject.GetComponent<PowerupBar>();

            }
        }
    }
}
