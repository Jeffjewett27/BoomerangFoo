﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BoomerangFoo.Patches;
using RewiredConsts;
using UnityEngine;
using UnityEngine.TextCore;

namespace BoomerangFoo.GameModes
{
    public class RamboHulk : GameMode
    {
        public int RamboHulkKilledBy = -1;
        public Player RamboHulkPlayer = null;

        private MethodInfo stopShield = typeof(Player).GetMethod("StopShield", BindingFlags.NonPublic | BindingFlags.Instance);
        private MethodInfo setAppearanceColors = typeof(Player).GetMethod("SetAppearanceColors", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo controlledPFX = typeof(Player).GetField("controlledPFX", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo retrievePFX = typeof(Player).GetField("retrievePFX", BindingFlags.NonPublic | BindingFlags.Instance);

        public RamboHulk() : base("RamboHulk", "Juggernaut", "Dynamic 1 vs all", SettingsManager.MatchType.DeathMatch, false, 0) { }

        public override void Hook()
        {
            // TODO CharacterBubbleTea
            // TODO CharacterMilk
            // TODO GameManager.OpponentsLeftStandingNow
            Singleton<SettingsManager>.Instance.MatchSettings.teamRevives = _CustomSettings.RamboHulkEnableRevive;
            PatchLevelManager.BlockPowerupSpawn = true;
            PatchPlayer.OnPostGetReady += OnGetReady;
            PatchPlayer.OnPreDie += OnDie;
            PatchPlayer.OnPostDie += AfterDie;
            PatchGameManager.OnPreAddPlayerKill += OnAddPlayerKill;
            PatchGameManager.OnPostPrepareRound += ResolveRamboHulkRound;
            PatchGameManager.OnPreStartRoundSequence += ResolveRamboHulkRound;
            PatchGameManager.OnPostUpdate += ResolveRamboHulkRound;
            PatchGameManager.PlayerRelationship = PlayerRelationship;
            // TODO PlayerAI (revives)
            // TODO Player.Update (skull particles)
        }

        public override void Unhook()
        {
            PatchLevelManager.BlockPowerupSpawn = false;
            PatchPlayer.OnPostGetReady -= OnGetReady;
            PatchPlayer.OnPreDie -= OnDie;
            PatchPlayer.OnPostDie -= AfterDie;
            PatchGameManager.OnPostPrepareRound -= ResolveRamboHulkRound;
            PatchGameManager.OnPreStartRoundSequence -= ResolveRamboHulkRound;
            PatchGameManager.PlayerRelationship = null;
        }

        public void OnGetReady(Player player)
        {
            player.ClearPowerups();
            if (player == RamboHulkPlayer)
            {
                LoadRamboHulkPowers(player);
            }
            else
            {
                LoadNonRamboHulkPowers(player);
            }
        }

        public void OnDie(Player player) 
        {
            if (RamboHulkPlayer == player)
            {
                RamboHulkKilledBy = -1;
                UpdateVisuals(player, false);
            }
        }

        public void AfterDie(Player player)
        {
            if (RamboHulkPlayer == player)
            {
                RamboHulkPlayer = null;
            }
        }

        public void OnAddPlayerKill(GameManager gameManager, Player killer, Player killed)
        {
            if (killed == RamboHulkPlayer)
            {
                if (_CustomSettings.RamboHulkKeepSwapping)
                {
                    LoadNonRamboHulkPowers(killed);
                }
                if (killer.actorState != Actor.ActorState.Dead)
                {
                    stopShield.Invoke(killer, null);
                    LoadRamboHulkPowers(killer);
                    RamboHulkKilledBy = killer.playerID;
                    RamboHulkPlayer = killer;
                }
            }
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

        public void UpdateVisuals(Player newHulk, bool isRamboHulk)
        {
            //newHulk.team = (isRamboHulk ? 1 : 2);
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

        public void LoadRamboHulkPowers(Player player)
        {
            PowerupType powerups = _CustomSettings.RamboHulkPowerups;
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
            PowerupType powerups = _CustomSettings.RamboHulkOthersPowerups;
            player.ClearPowerups();
            CommonFunctions.GetEnumPowerUpValues(powerups).ForEach(delegate (PowerupType i)
            {
                player.StartPowerup(i);
            });
            player.team = 2;
            UpdateVisuals(player, false);
        }

        public void ResolveRamboHulkRound(GameManager gameManager)
        {
            if (RamboHulkPlayer == null)
            {
                // Select new rambo hulk
                Player newHulk;
                if (RamboHulkKilledBy == -1)
                {
                    var alivePlayers = gameManager.players.Where((Player i) => i.actorState != Actor.ActorState.Dead).ToList();
                    if (alivePlayers.Count == 0) return;

                    newHulk = alivePlayers[UnityEngine.Random.Range(0, alivePlayers.Count - 1)];
                } else
                {
                    newHulk = gameManager.players[RamboHulkKilledBy];
                }
                RamboHulkPlayer = newHulk;
                LoadRamboHulkPowers(newHulk);
            }
            stopShield.Invoke(RamboHulkPlayer, null);
        }
    }
}
