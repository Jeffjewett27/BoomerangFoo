using BoomerangFoo.GameModes;
using BoomerangFoo.Patches;
using BoomerangFoo.UI;
using RewiredConsts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BoomerangFoo.Powerups
{
    public class FlyingPowerup : CustomPowerup
    {
        public static float DefaultDuration = 0.75f;
        public static bool DefaultResetOnGround = true;

        static readonly FieldInfo reviveTimeUIClock = typeof(Player).GetField("reviveTimeUIClock", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo reviveTimeUI = typeof(Player).GetField("reviveTimeUI", BindingFlags.NonPublic | BindingFlags.Instance);
        //static readonly FieldInfo isFalling = typeof(Player).GetField("isFalling", BindingFlags.NonPublic | BindingFlags.Instance);

        private static FlyingPowerup instance;
        public static FlyingPowerup Instance
        {
            get
            {
                instance ??= new FlyingPowerup();
                return instance;
            }
        }

        public float Duration { get; set; }
        public bool ResetOnGround { get; set; }

        protected FlyingPowerup()
        {
            Name = "Hovering";
            Bitmask = PowerupType.DashThroughWalls;
            Duration = DefaultDuration;
            ResetOnGround = DefaultResetOnGround;
        }
        public override void Activate()
        {
            base.Activate();
            PatchPlayer.OnPreInit += PlayerInit;
            PatchPlayer.OnPostSpawnIn += PostSpawnIn;
            PatchPlayer.OnPreUpdate += PreUpdate;
            PatchPlayer.PreStartFall = PreStartFall;
        }

        public override void Deactivate()
        {
            base.Deactivate();
            PatchPlayer.OnPreInit -= PlayerInit;
            PatchPlayer.OnPostSpawnIn -= PostSpawnIn;
            PatchPlayer.OnPreUpdate -= PreUpdate;
            PatchPlayer.PreStartFall = null;
        }

        public override void GenerateUI()
        {
            if (hasGeneratedUI) return;
            base.GenerateUI();

            // hover duration
            var flyDuration = Modifiers.CloneModifierSetting($"customPowerup.{Name}.duration", "Hover Duration", "Fall protection", $"customPowerup.{Name}.header");
            SettingIds.Add(flyDuration.id);

            float[] hoverValues = [0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f, 4f, 6f, 8f, 10f, 15f, 20f, float.MaxValue / 2];
            string[] options = new string[hoverValues.Length];
            string[] hints = new string[options.Length];
            options[options.Length - 1] = "Infinite";
            hints[options.Length-1] = "Hover over hazards forever!";
            for (int i = 0; i < hoverValues.Length-1; i++)
            {
                options[i] = hoverValues[i].ToString();
                hints[i] = $"Hover for {options[i]} seconds";
            }
            flyDuration.SetSliderOptions(options, 3, hints);
            flyDuration.SetGameStartCallback((gameMode, sliderIndex) => {
                FlyingPowerup.Instance.Duration = hoverValues[sliderIndex];
            });

            // refresh
            var timeRefresh = Modifiers.CloneModifierSetting($"customPowerup.{Name}.timeRefresh", "Timer Refresh", "Warm up round", $"customPowerup.{Name}.duration");
            SettingIds.Add(timeRefresh.id);
            timeRefresh.SetSliderOptions(["Ground", "Round"], 0, ["Refresh timer when touching ground", "Refreshes timer each round"]);
            timeRefresh.SetGameStartCallback((gameMode, sliderIndex) => {
                FlyingPowerup.Instance.ResetOnGround = (sliderIndex == 0);
            });

            // powerup
            var powerup = Modifiers.CloneModifierSetting($"customPowerup.{Name}.powerup", "Hover Powerup", "powerupSelections", $"customPowerup.{Name}.timeRefresh");
            powerup.PreparePowerupToggles(PowerupType.None);
            powerup.SetGameStartCallback((gameMode, powerups) =>
            {
                FlyingPowerup.Instance.Bitmask = (PowerupType)powerups;
            });
        }

        private void PlayerInit(Player player)
        {
            PlayerState playerState = CommonFunctions.GetPlayerState(player);
            playerState.flyingForceFall = false;
            playerState.flyingTimer = 0f;
            playerState.isFlyingTimerOn = false;
            playerState.isFlying = false;
        }

        private void PostSpawnIn(Player player)
        {
            //refreshFlyingMethod.Invoke(player, null);
            RefreshFlying(player);
        }

        private void PreUpdate(Player player)
        {
            RunFlyingTimer(player);
        }

        private bool PreStartFall(Player player)
        {
            PlayerState playerState = CommonFunctions.GetPlayerState(player);

            if (Bitmask != 0 && player.activePowerup.HasPowerup(Bitmask) && !playerState.flyingForceFall)
            {
                StartFlyingTimer(player, playerState.flyingDuration);
                return false;
            }
            return true;
        }

        private void StartFlyingTimer(Player player, float duration)
        {
            PlayerState playerState = CommonFunctions.GetPlayerState(player);

            if (!playerState.isFlyingTimerOn)
            {
                ((Image)(reviveTimeUIClock.GetValue(player))).enabled = true;

                if (playerState.resetOnGround)
                {
                    playerState.flyingTimeLimit = duration;
                    playerState.flyingTimer = duration;
                    playerState.flyingForceFall = false;
                }
                playerState.isFlyingTimerOn = true;
                playerState.isFlying = true;
                ((Canvas)(reviveTimeUI.GetValue(player))).GetComponent<Animator>().SetBool("IsVisible", value: true);
            }
        }

        private void RunFlyingTimer(Player player)
        {
            PlayerState playerState = CommonFunctions.GetPlayerState(player);
            var reviveClock = ((Image)(reviveTimeUIClock.GetValue(player)));
            var reviveUI = ((Canvas)(reviveTimeUI.GetValue(player)));

            if ((playerState.flyingForceFall || playerState.isFlying) && !player.IsPositionOverHazard(player.transform.position))
            {
                playerState.isFlyingTimerOn = false;
                playerState.isFlying = false;
                playerState.flyingForceFall = false;
                reviveUI.GetComponent<Animator>().SetBool("IsVisible", value: false);
            }

            if (playerState.isFlyingTimerOn)
            {
                if (playerState.flyingTimer > 0f)
                {
                    reviveClock.fillAmount = playerState.flyingTimer / playerState.flyingTimeLimit;
                    playerState.flyingTimer -= Time.deltaTime * player.timeScaler;
                    return;
                }
                reviveUI.GetComponent<Animator>().SetBool("IsVisible", value: false);
                reviveClock.fillAmount = 0f;
                playerState.flyingTimer = playerState.flyingDuration;
                playerState.isFlyingTimerOn = false;
                playerState.flyingForceFall = true;
                playerState.isFlying = false;
            }
            else if (playerState.isFlying)
            {
                reviveUI.GetComponent<Animator>().SetBool("IsVisible", value: false);
                reviveClock.fillAmount = 0f;
                playerState.flyingTimer = playerState.flyingDuration;
                playerState.isFlyingTimerOn = false;
                playerState.flyingForceFall = false;
            }
        }

        private void RefreshFlying(Player player)
        {
            PlayerState playerState = CommonFunctions.GetPlayerState(player);

            playerState.flyingTimeLimit = playerState.flyingDuration;
            playerState.flyingTimer = playerState.flyingDuration;
            playerState.isFlyingTimerOn = false;
            playerState.flyingForceFall = false;
            playerState.isFlying = false;

            var reviveClock = ((Image)(reviveTimeUIClock.GetValue(player)));
            var reviveUI = ((Canvas)(reviveTimeUI.GetValue(player)));
            reviveUI.GetComponent<Animator>().SetBool("IsVisible", value: false);
            reviveClock.fillAmount = 0f;
        }
    }
}
