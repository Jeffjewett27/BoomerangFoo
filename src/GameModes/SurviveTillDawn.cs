using BoomerangFoo.Patches;
using BoomerangFoo.UI;
using RewiredConsts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UI;

namespace BoomerangFoo.GameModes
{
    public class SurviveTillDawn : GameMode
    {
        public bool Respawning = false;
        public float TimeAsHulk = 20f;
        public PowerupType RamboHulkPowers = PowerupType.MoveFaster | PowerupType.DashThroughWalls | PowerupType.ExtraDisc;
        public PowerupType PeasantPowers = PowerupType.None;

        private Player RamboHulkPlayer = null;
        private bool roundInitialized = false;

        static readonly FieldInfo goldenTimeUIClock= typeof(Player).GetField("goldenTimeUIClock", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo goldenTimeUI = typeof(Player).GetField("goldenTimeUI", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly MethodInfo stopShield = typeof(Player).GetMethod("StopShield", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly MethodInfo setAppearanceColors = typeof(Player).GetMethod("SetAppearanceColors", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo controlledPFX = typeof(Player).GetField("controlledPFX", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo retrievePFX = typeof(Player).GetField("retrievePFX", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly MethodInfo endMatch = typeof(GameManager).GetMethod("EndMatch", BindingFlags.NonPublic| BindingFlags.Instance);


        private static PowerupType originalAvailablePowerups = Powerup.AllPowerups();

        public SurviveTillDawn() : base("SurviveTillDawn", "GameModeSurviveDawnName", "GameModeSurviveDawnHint", SettingsManager.MatchType.DeathMatch, false, 0) { }

        public override void Hook()
        {
            originalAvailablePowerups = Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups;
            Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups = PowerupType.None;
            PatchPlayer.OnPostUpdate += PlayerUpdate;
            PatchGameManager.OnPostPrepareRound += StartRound;
            PatchGameManager.OnPreStartRoundSequence += StartRound;
            PatchGameManager.OnPreStartMatch += StartMatch;
            PatchPlayer.OnPostDie += OnDie;
            PatchGameManager.OnPreAddPlayerKill += OnAddPlayerKill;
            PatchGameManager.OnPostUpdate += GameUpdate;
            PatchGameManager.PlayerRelationship = PlayerRelationship;
            PatchGameManager.OpponentsLeftStandingNow = OpponentsLeftStandingNow;
        }

        public override void Unhook()
        {
            Singleton<SettingsManager>.Instance.MatchSettings.availablePowerups = originalAvailablePowerups;
            PatchPlayer.OnPostUpdate -= PlayerUpdate;
            PatchGameManager.OnPostPrepareRound -= StartRound;
            PatchGameManager.OnPreStartRoundSequence -= StartRound;
            PatchGameManager.OnPreStartMatch -= StartMatch;
            PatchPlayer.OnPostDie -= OnDie;
            PatchGameManager.OnPreAddPlayerKill -= OnAddPlayerKill;
            PatchGameManager.OnPostUpdate -= GameUpdate;
            PatchGameManager.PlayerRelationship = null;
            PatchGameManager.OpponentsLeftStandingNow = null;
        }

        public override void RegisterSettings()
        {
            string headerId = $"gameMode.{id}.header";
            var header = Modifiers.CloneModifierSetting(headerId, name, "ui_boomerangs", "ui_label_friendlyfire");

            string respawnId = $"gameMode.{id}.respawn";
            var respawn = Modifiers.CloneModifierSetting(respawnId, "GameModeSurviveDawnRounds", "ui_label_warmuplevel", headerId);
            respawn.SetSliderOptions(["GameModeSurviveDawnRoundsSingle", "GameModeSurviveDawnRoundsMultiple"], 1, ["GameModeSurviveDawnRoundsHintSingle", "GameModeSurviveDawnRoundsHintMultiple"]);
            respawn.SetGameStartCallback((gameMode, sliderIndex) =>
            {
                if (gameMode is SurviveTillDawn tilDawn)
                {
                    tilDawn.Respawning = (sliderIndex == 0);
                }
            });

            // powerup
            string hulkPowerId = $"gameMode.{id}.hulkPowerup";
            var powerup = Modifiers.CloneModifierSetting(hulkPowerId, "GameModeSurviveDawnPowerupMutant", "powerupSelections", respawnId);
            powerup.PreparePowerupToggles(PowerupType.MoveFaster | PowerupType.DashThroughWalls | PowerupType.ExtraDisc);
            powerup.SetGameStartCallback((gameMode, powerups) =>
            {
                if (gameMode is SurviveTillDawn tillDawn)
                {
                    tillDawn.RamboHulkPowers = (PowerupType)powerups;
                }
            });

            string timerId = $"gameMode.{id}.timer";
            var timer = Modifiers.CloneModifierSetting(timerId, "GameModeSurviveDawnTimer", "ui_label_warmuplevel", hulkPowerId);
            int[] timerOptions = [1, 2, 3, 4, 5, 6, 8, 10, 12, 15, 18, 20, 25, 30];
            string[] timerValues = new string[timerOptions.Length];
            string[] timerHints = new string[timerOptions.Length];
            for (int i = 0; i < timerOptions.Length; i++)
            {
                timerValues[i] = timerOptions[i].ToString();
                timerHints[i] = $"GameModeSurviveDawnTimerHint__{timerOptions[i]}";
            }
            timer.SetSliderOptions(timerValues, 5, timerHints);
            timer.SetGameStartCallback((gameMode, sliderIndex) =>
            {
                if (gameMode is SurviveTillDawn tillDawn)
                {
                    tillDawn.TimeAsHulk = timerOptions[sliderIndex];
                }
            });

            // other
            string otherPowerId = $"gameMode.{id}.otherPowerup";
            var otherPowerup = Modifiers.CloneModifierSetting(otherPowerId, "GameModeSurviveDawnPowerupSurvivor", "powerupSelections", timerId);
            otherPowerup.PreparePowerupToggles(PowerupType.None);
            otherPowerup.SetGameStartCallback((gameMode, powerups) =>
            {
                if (gameMode is SurviveTillDawn tillDawn)
                {
                    tillDawn.PeasantPowers = (PowerupType)powerups;
                }
            });
        }

        private void GameUpdate(GameManager gameManager)
        {
            if (gameManager.gameState != GameManager.GameState.Playing) return;

            // handle an edge case for respawning not ending the match
            if (Respawning && gameManager.matchScores.Any((KeyValuePair<int, MatchScore> i) => i.Value.Points >= Singleton<SettingsManager>.Instance.matchScoreGoal))
            {
                int key = gameManager.matchScores.OrderByDescending((KeyValuePair<int, MatchScore> i) => i.Value.Points).First().Key;
                gameManager.matchWinners.Clear();
                gameManager.matchWinners.Add(key);
                Singleton<AudioManager>.Instance.WinMatch();
                System.Collections.IEnumerator endMatchCoroutine = (System.Collections.IEnumerator)endMatch.Invoke(gameManager, null);
                gameManager.StartCoroutine(endMatchCoroutine);
            }
        }

        private void PlayerUpdate(Player player)
        {
            if (!roundInitialized) return;

            if (player != RamboHulkPlayer)
            {
                if (player.HasDiscs > 0 && RamboHulkPlayer != null && (RamboHulkPlayer.HasDiscs + RamboHulkPlayer.pendingDiscs) == 0)
                {
                    // has stole up the last disc from the hulk
                    PickDifferentPlayerInSurviveTillDawn(player);
                }
                else
                {
                    RemoveBoomerang(player);
                }
            } else
            {
                stopShield.Invoke(player, null);
                // I have no idea if this code was necessary
                //if ((HasDiscs == 0 && !activePowerup.HasPowerup(PowerupType.ExtraDisc)) || (HasDiscs == 1 && activePowerup.HasPowerup(PowerupType.ExtraDisc)))
                //{
                //    RespawnDisc(4f);
                //}
                //List<Disc> list = thrownDiscs.Where((Disc i) => i.movement == Projectile.Movement.Dropped).ToList();
                //if (list.Count > 2)
                //{
                //    list.Take(list.Count - 2).ToList().ForEach(delegate (Disc i)
                //    {
                //        i.GetPickedUp(vanish: true);
                //    });
                //}
            }
            RunSurviveTillDawnTimer(player);
        }

        private void OnDie(Player player)
        {
            if (Respawning)
            {
                Singleton<GameManager>.Instance.RespawnPlayer(player, 2f);
            }
            if (player == RamboHulkPlayer)
            {
                PickDifferentPlayerInSurviveTillDawn();
            }
        }

        private void StartMatch(GameManager gameManager) {
            RamboHulkPlayer = null;
            roundInitialized = false;
        }

        private void StartRound(GameManager gameManager)
        {
            PickDifferentPlayerInSurviveTillDawn(RamboHulkPlayer);
            roundInitialized = true;
        }

        private void RunSurviveTillDawnTimer(Player player)
        {
            if (Singleton<GameManager>.Instance.gameState != GameManager.GameState.Playing)
            {
                return;
            }
            Canvas timeui = (Canvas)goldenTimeUI.GetValue(player);
            Image uiclock = (Image)goldenTimeUIClock.GetValue(player);
            if (player == RamboHulkPlayer)
            {
                if (player.goldenHoldTime > 0f)
                {
                    timeui.GetComponent<Animator>().SetBool("IsVisible", value: true);
                    uiclock.fillAmount = player.goldenHoldTime / TimeAsHulk;
                    player.goldenHoldTime -= Time.deltaTime * player.timeScaler;
                }
                else
                {
                    timeui.GetComponent<Animator>().SetBool("IsVisible", value: false);
                    uiclock.fillAmount = 0f;
                    // not sure why flying timer was set. probably not needed
                    //flyingTimer = _CustomSettings.SurviveTillDawnTimer;
                    player.goldenHoldTime = TimeAsHulk;
                    PickDifferentPlayerInSurviveTillDawn();
                }
            }
            else
            {
                timeui.GetComponent<Animator>().SetBool("IsVisible", value: false);
                uiclock.fillAmount = 0f;
                player.goldenHoldTime = TimeAsHulk;
            }
        }

        public void PickDifferentPlayerInSurviveTillDawn(Player preselected = null)
        {
            GameManager gameManager = Singleton<GameManager>.Instance;
            if (RamboHulkPlayer != null)
            {
                CleanUpDecoysForPlayer(RamboHulkPlayer.playerID);
            }

            Player newHulk = preselected;
            if (preselected == null)
            {
                List<Player> aliveOtherPlayers = gameManager.players.Where((Player p) => p != RamboHulkPlayer && p.actorState != Actor.ActorState.Dead).ToList();
                if (aliveOtherPlayers.Count == 0) {
                    BoomerangFoo.Logger.LogWarning("SurviveTillDawn no players available to pick");
                }
                newHulk = aliveOtherPlayers[UnityEngine.Random.Range(0, aliveOtherPlayers.Count)];
            }

            CleanUpDecoysForPlayer(newHulk.playerID);
            newHulk.goldenHoldTime = TimeAsHulk;
            LoadRamboHulkPowers(newHulk);
            if (newHulk != RamboHulkPlayer)
            {
                int targetBoomerangs = newHulk.activePowerup.HasPowerup(PowerupType.ExtraDisc) ? 2 : 1;
                newHulk.RespawnDisc(0f, Math.Max(targetBoomerangs - newHulk.HasDiscs - newHulk.pendingDiscs, 0));
            }
            RamboHulkPlayer = newHulk;

            foreach (Player p in gameManager.players.Where((p) => p != RamboHulkPlayer))
            {
                LoadNonRamboHulkPowers(p);
            }
        }

        private void CleanUpDecoysForPlayer(int playerId)
        {
            GameManager gameManager = Singleton<GameManager>.Instance;
            foreach (Decoy item in gameManager.activeDecoys.Where((Decoy i) => i.playerID == playerId).ToList())
            {
                Singleton<Camera3D>.Instance.RemoveCameraTarget(item.transform);
                item.gameObject.Recycle();
            }
            gameManager.activeDecoys = gameManager.activeDecoys.Where((Decoy i) => i.playerID != playerId).ToList();
        }

        public void LoadRamboHulkPowers(Player player)
        {
            PowerupType powerups = RamboHulkPowers;
            player.ClearPowerups();
            CommonFunctions.GetEnumPowerUpValues(powerups).ForEach(delegate (PowerupType i)
            {
                player.StartPowerup(i);
            });
            player.team = 1;
            UpdateVisuals(player, true);
        }

        public void LoadNonRamboHulkPowers(Player player)
        {
            PowerupType powerups = PeasantPowers;
            player.ClearPowerups();
            CommonFunctions.GetEnumPowerUpValues(powerups).ForEach(delegate (PowerupType i)
            {
                player. StartPowerup(i);
            });
            player.team = 2;
            UpdateVisuals(player, false);
        }

        public void OnAddPlayerKill(GameManager gameManager, Player killer, Player killed)
        {
            if (killed == null) return;
            if (killer == RamboHulkPlayer)
            {
                killer.goldenHoldTime += _CustomSettings.SurviveTillDawnKillGainTime;
                if (killer.goldenHoldTime > TimeAsHulk)
                {
                    killer.goldenHoldTime = TimeAsHulk;
                }
            }
            if (killed == RamboHulkPlayer)
            {
                if (killer != null && killer.actorState != Actor.ActorState.Dead)
                {
                    PickDifferentPlayerInSurviveTillDawn(killer);
                } else
                {
                    PickDifferentPlayerInSurviveTillDawn();
                }
            }
        }

        public void RemoveBoomerang(Player player)
        {
            player.StopAiming();
            player.character.SetMeleeDiscs(0);
            player.attacksLeft = 0;

            player.IsDisarmed = true;
            player.pendingDiscs = 0;
            player.HasDiscs = 0;
            player.thrownDiscs.ToList().ForEach(delegate (Disc i)
            {
                i.GetPickedUp(vanish: true);
            });
            player.thrownDiscs.Clear();
        }

        public void UpdateVisuals(Player newHulk, bool isRamboHulk)
        {
            if (newHulk == null) return;
            Color color = (isRamboHulk ? Singleton<GameManager>.Instance.goldenDiscColor : newHulk.character.Color);
            ParticleSystem.MainModule main = newHulk.meleeTrailRight.main;
            main.startColor = color - new Color(0f, 0f, 0f, 0.2f);
            main = newHulk.meleeTrailLeft.main;
            main.startColor = color - new Color(0f, 0f, 0f, 0.2f);
            var cPFX = (GameObject)(controlledPFX.GetValue(newHulk));
            main = cPFX.GetComponent<ParticleSystem>().main;
            main.startColor = color;
            var rPFX = (GameObject)(retrievePFX.GetValue(newHulk));
            main = rPFX.GetComponent<ParticleSystem>().main;
            main.startColor = color;
            newHulk.goldenDiscPFX.gameObject.SetActive(isRamboHulk);
            newHulk.goldenPlayerPFX.gameObject.SetActive(isRamboHulk);
            //newHulk.SetAppearanceColors(color);
            setAppearanceColors.Invoke(newHulk, [color]);
        }

        public Relationship PlayerRelationship(GameManager gameManager, Player self, Player other)
        {
            if (self == RamboHulkPlayer)
            {
                return Relationship.Opponent;
            }
            if (other == RamboHulkPlayer)
            {
                return Relationship.Opponent;
            }
            return Relationship.Temporary;
        }

        public int OpponentsLeftStandingNow(GameManager gameManager)
        {
            if (Respawning)
            {
                gameManager.lastPlayerStanding = null;
                // all players are always consider 'left standing'
                // the round never ends
                return gameManager.players.Count; 
            } else
            {
                List<Player> alivePlayers = gameManager.players.Where((Player i) => i.actorState != Actor.ActorState.Dead).ToList();
                gameManager.lastPlayerStanding = null;
                if (alivePlayers.Count == 1)
                {
                    gameManager.lastPlayerStanding = alivePlayers[0];
                }
                return alivePlayers.Count;
            }
        }
    }
}
