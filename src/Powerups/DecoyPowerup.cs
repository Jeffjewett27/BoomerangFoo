using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoomerangFoo.Patches;
using BoomerangFoo.UI;
using RewiredConsts;
using static Actor;
using UnityEngine.TextCore;
using UnityEngine;
using System.Reflection;

namespace BoomerangFoo.Powerups
{
    class DecoyPowerup : CustomPowerup
    {
        public static int DefaultMaxDecoyCount = 1;
        public static bool DefaultReviveAsDecoy = false;

        private static DecoyPowerup instance;
        public static DecoyPowerup Instance { get
            {
                instance ??= new DecoyPowerup();
                return instance;
            } 
        }

        public int MaxDecoyCount;
        public bool ReviveAsDecoy;

        protected DecoyPowerup()
        {
            Name = "Decoy";
            LocalizationTerm = "powerup_decoy";
            Bitmask = PowerupType.Decoy;
            MaxDecoyCount = DefaultMaxDecoyCount;
            ReviveAsDecoy = DefaultReviveAsDecoy;
        }

        public override void Activate()
        {
            base.Activate();
            PatchPlayer.OnPreInit += PlayerInit;
            PatchPlayer.OnPostCreateDecoy += CreateDecoy;
            PatchPlayer.OnPreDie += PreDie;
        }

        public override void Deactivate()
        {
            base.Deactivate();
            PatchPlayer.OnPreInit -= PlayerInit;
            PatchPlayer.OnPostCreateDecoy -= CreateDecoy;
            PatchPlayer.OnPreDie -= PreDie;
        }

        public override void GenerateUI()
        {
            if (hasGeneratedUI) return;
            base.GenerateUI();
            var maxDecoys = Modifiers.CloneModifierSetting($"customPowerup.{Name}.maxDecoys", "ModifierDecoyMax", "ui_label_edgeprotection", $"customPowerup.{Name}.header");
            SettingIds.Add(maxDecoys.id);

            string[] options = new string[31];
            string[] hints = new string[31];
            options[0] = "ModifierInfinite";
            hints[0] = "ModifierDecoyMaxWarning";
            for (int i = 1; i < 31; i++)
            {
                options[i] = i.ToString();
                hints[i] = $"ModifierDecoyMaxHint__{i}";
            }
            maxDecoys.SetSliderOptions(options, 1, hints);
            maxDecoys.SetGameStartCallback((gameMode, sliderIndex) => {
                // maxValue / 2 is big and no chance of overflow
                int maxValue = sliderIndex == 0 ? int.MaxValue / 2 : sliderIndex;
                DecoyPowerup.Instance.MaxDecoyCount = maxValue;
            });

            //revive(TODO)
            var revive = Modifiers.CloneModifierSetting($"customPowerup.{Name}.respawn", "ModifierDecoyRevive", "ui_label_warmuplevel", $"customPowerup.{Name}.maxDecoys");
            SettingIds.Add(revive.id);
            revive.SetSliderOptions(["ui_off", "ui_on"], 0, ["ModifierDecoyReviveOffHint", "ModifierDecoyReviveOnHint"]);
            revive.SetGameStartCallback((gameMode, sliderIndex) =>
            {
                DecoyPowerup.Instance.ReviveAsDecoy = (sliderIndex == 1);
            });
        }

        private void PlayerInit(Player player) 
        {
            PlayerState playerState = CommonFunctions.GetPlayerState(player);
            playerState.maxDecoyCount = MaxDecoyCount;
            // TODO reviveAsDecoy
            playerState.reviveAsDecoy = ReviveAsDecoy;
            playerState.decoyCounter = 0;
        }

        private void PreDie(Player player)
        {
            if (ReviveAsDecoy && RespawnAsDecoy(player))
            {
                PlayerDiePatch.stopDie = true;
            }
        }

        private void CreateDecoy(Player player)
        {
            PlayerState playerState = CommonFunctions.GetPlayerState(player);
            playerState.decoyCounter++;
            if (playerState.decoyCounter > playerState.maxDecoyCount) playerState.decoyCounter = playerState.maxDecoyCount;
            player.hasCreatedDecoy = playerState.decoyCounter >= playerState.maxDecoyCount;
        }

        public void DecoyDie(Player player)
        {
            PlayerState playerState = CommonFunctions.GetPlayerState(player);
            playerState.decoyCounter--;
            if (playerState.decoyCounter < 0) playerState.decoyCounter = 0;
            player.hasCreatedDecoy = playerState.decoyCounter >= playerState.maxDecoyCount;
        }

        private static readonly FieldInfo playerDustTrail = typeof(Player).GetField("dustTrail", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo playerBreakDisguise = typeof(Player).GetMethod("BreakDisguise", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo playerStopRetrieve = typeof(Player).GetMethod("StopRetrieve", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo playerStopDash = typeof(Player).GetMethod("StopDash", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo playerNotifyPlantsOfDeath = typeof(Player).GetMethod("NotifyPlantsOfDeath", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo playerGetMoveSpeed = typeof(Player).GetMethod("GetMoveSpeed", BindingFlags.NonPublic | BindingFlags.Instance);

        protected bool RespawnAsDecoy(Player player)
        {
            PlayerState playerState = CommonFunctions.GetPlayerState(player);
            Decoy decoy = Singleton<GameManager>.Instance.activeDecoys.FirstOrDefault((Decoy i) => i.playerID == player.playerID);
            if (decoy == null || playerState.decoyCounter == 0)
            {
                return false;
            }
            player.activePowerup &= ~PowerupType.Decoy;
            player.powerupHistory.Remove(PowerupType.Decoy);
            decoy.Die(DeathType.telefrag);
            var dustTrail = (playerDustTrail.GetValue(player) as ParticleSystem);
            dustTrail.Stop();
            if (player.moveFasterPFX.gameObject.activeSelf)
            {
                player.moveFasterPFX.Stop();
            }
            Singleton<ParticleManager>.Instance.PlayPFX(PFXType.Teleport, player.transform.position, player.character.Color);
            player.transform.SetParent(null, worldPositionStays: true);
            player.interpolatedTransform.ForgetPreviousTransforms();
            player.transform.position = new Vector3(decoy.transform.position.x, decoy.transform.position.y, player.transform.position.z);
            player.interpolatedTransform.ForgetPreviousTransforms();
            Singleton<ParticleManager>.Instance.PlayPFX(PFXType.TeleportExit, player.transform.position, player.character.Color);
            dustTrail.Play();
            if (player.moveFasterPFX.gameObject.activeSelf)
            {
                player.moveFasterPFX.Play();
            }
            playerBreakDisguise.Invoke(player, null);
            playerStopRetrieve.Invoke(player, null);
            player.character.anim.SetTrigger("Teleport");
            if (player.IsDashing)
            {
                player.dashTrail.Stop();
                playerStopDash.Invoke(player, [true]);
            }
            player.illuminatePFX.Play();
            player.attackCooldownTimer = 0f;
            Singleton<AudioManager>.Instance.PlayOneShotAt("event:/powerups/teleport", player.transform.position);
            if (player.isWading)
            {
                playerNotifyPlantsOfDeath.Invoke(player, null);
            }
            player.character.Reset();
            player.character.anim.SetFloat("MoveSpeed", (float)playerGetMoveSpeed.Invoke(player, null));
            player.physicsCollider.enabled = true;
            player.damageCollider.enabled = true;
            player.actorState = ActorState.Alive;
            return true;
        }
    }
}
