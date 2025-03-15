using BoomerangFoo.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Playables;

namespace BoomerangFoo.Powerups
{
    class MoveFasterPowerup : CustomPowerup
    {
        public static float DefaultAttackCooldown = originalAttackCooldownDuration;
        public static float DefaultDashForceMultiplier = 1f;
        public static float DefaultDashDurationMultiplier = 1f;
        public static float DefaultDashCooldownMultiplier = 1f;
        public static float DefaultMoveSpeedMultiplier = 1f;
        public static float DefaultTurnSpeed = originalTurningSpeed; 

        public static readonly FieldInfo playerAttackCooldownDuration = typeof(Player).GetField("attackCooldownDuration", BindingFlags.NonPublic | BindingFlags.Instance);
        public const float originalAttackCooldownDuration = 0.66f;
        public static readonly FieldInfo playerLungeCooldownDuration = typeof(Player).GetField("lungeCooldownDuration", BindingFlags.NonPublic | BindingFlags.Instance);
        public const float originalLungeCooldownDuration = 0.5f;
        public static readonly FieldInfo playerDashSpeed = typeof(Player).GetField("dashSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
        public const float originalDashSpeed = 660f;
        public static readonly FieldInfo playerDashCooldown = typeof(Player).GetField("dashCooldown", BindingFlags.NonPublic | BindingFlags.Instance);
        public const float originalDashCooldown = 0.55f;
        public static readonly FieldInfo playerDashDuration = typeof(Player).GetField("dashDuration", BindingFlags.NonPublic | BindingFlags.Instance);
        public const float originalDashDuration = 0.55f;
        public static readonly FieldInfo playerTurningSpeed = typeof(Player).GetField("turningSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
        public const float originalTurningSpeed = 12f;
        public static readonly FieldInfo playerWalkSpeed = typeof(Player).GetField("walkSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
        public const float originalWalkSpeed = 200f;
        public static readonly FieldInfo playerWalkSpeedUnarmed = typeof(Player).GetField("walkSpeedUnarmed", BindingFlags.NonPublic | BindingFlags.Instance);
        public const float originalWalkSpeedUnarmed = 230f;

        private static MoveFasterPowerup instance;
        public static MoveFasterPowerup Instance
        {
            get
            {
                instance ??= new MoveFasterPowerup();
                return instance;
            }
        }

        public float AttackCooldown { get; set; }
        public float DashForceMultiplier {  get; set; }
        public float DashDurationMultiplier { get; set; }
        public float DashCooldownMultiplier { get; set; }
        public float TurnSpeed {  get; set; }
        public float MoveSpeedMultiplier { get; set; }

        protected MoveFasterPowerup()
        {
            Name = "Caffeinated";
            LocalizationTerm = "powerup_movefaster";
            Bitmask = PowerupType.MoveFaster;
            AttackCooldown = DefaultAttackCooldown;
            DashForceMultiplier = DefaultDashForceMultiplier;
            DashDurationMultiplier = DefaultDashDurationMultiplier;
            DashCooldownMultiplier = DefaultDashCooldownMultiplier;
            MoveSpeedMultiplier = DefaultMoveSpeedMultiplier;
            TurnSpeed = DefaultTurnSpeed;
        }

        public override void Activate()
        {
            base.Activate();
            PowerupManager.OnAcquirePowerup += AcquirePowerup;
            PowerupManager.OnRemovePowerup += RemovePowerup;
        }

        public override void Deactivate()
        {
            base.Deactivate();
            PowerupManager.OnAcquirePowerup -= AcquirePowerup;
            PowerupManager.OnRemovePowerup -= RemovePowerup;
        }

        public override void GenerateUI()
        {
            if (hasGeneratedUI) return;
            base.GenerateUI();

            // speedFactor
            var speedFactor = Modifiers.CloneModifierSetting($"customPowerup.{Name}.speedFactor", "ModifierCaffeineSpeed", "ui_label_edgeprotection", $"customPowerup.{Name}.header");
            SettingIds.Add(speedFactor.id);

            float[] speedValues = [0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f, 3f, 4f, 5f, 6f];
            string[] speedOptions = new string[speedValues.Length];
            string[] speedHints = new string[speedOptions.Length];
            for (int i = 0; i < speedValues.Length; i++)
            {
                speedOptions[i] = speedValues[i].ToString();
                speedHints[i] = $"ModifierCaffeineSpeedHint__{speedValues[i]}";
            }
            speedHints[3] = "ModifierCaffeineSpeedHintDefault";
            speedFactor.SetSliderOptions(speedOptions, 3, speedHints);
            speedFactor.SetGameStartCallback((gameMode, sliderIndex) => {
                MoveFasterPowerup.Instance.MoveSpeedMultiplier = speedValues[sliderIndex];
                MoveFasterPowerup.Instance.TurnSpeed = originalTurningSpeed * speedValues[sliderIndex];
            });

            // attack speed
            var attackSpeed = Modifiers.CloneModifierSetting($"customPowerup.{Name}.attackFactor", "ModifierCaffeineAttackSpeed", "ui_label_edgeprotection", $"customPowerup.{Name}.speedFactor");
            SettingIds.Add(attackSpeed.id);

            float[] attackValues = [-100, -80, -50, -30, -20, -10, 0, 10, 20, 30, 50, 80, 100];
            string[] attackOptions = new string[attackValues.Length];
            string[] attackHints = new string[attackOptions.Length];
            for (int i = 0; i < attackValues.Length; i++)
            {
                attackOptions[i] = $"{attackValues[i]}%".ToString();
                if (attackValues[i] < 0)
                {
                    attackHints[i] = $"ModifierCaffeineAttackSpeedHintDecrease__{-attackValues[i]}";
                } else
                {
                    attackHints[i] = $"ModifierCaffeineAttackSpeedHintIncrease__{attackValues[i]}";
                }
            }
            attackHints[6] = "ModifierCaffeineAttackSpeedHintNormal";
            attackSpeed.SetSliderOptions(attackOptions, 6, attackHints);
            attackSpeed.SetGameStartCallback((gameMode, sliderIndex) => {
                float multiplier = (100 + attackValues[sliderIndex]) / 100f;
                MoveFasterPowerup.Instance.AttackCooldown = originalAttackCooldownDuration / multiplier;
            });
        }
        private void AcquirePowerup(Player player, List<PowerupType> powerupHistory, PowerupType newPowerup)
        {
            var previousActive = CommonFunctions.PreviousActive(powerupHistory);

            if (!newPowerup.HasPowerup(Bitmask) || previousActive.HasPowerup(Bitmask)) return;

            PlayerState playerState = CommonFunctions.GetPlayerState(player);

            playerState.originalAttackCooldownDuration = (float)playerAttackCooldownDuration.GetValue(player);
            playerAttackCooldownDuration.SetValue(player, playerState.moveFasterAttackCooldown);
            playerState.originalLungeCooldownDuration = (float)playerLungeCooldownDuration.GetValue(player);
            playerLungeCooldownDuration.SetValue(player, playerState.moveFasterAttackCooldown);
            playerState.originalDashSpeed = (float)playerDashSpeed.GetValue(player);
            playerDashSpeed.SetValue(player, (float)playerDashSpeed.GetValue(player) * playerState.moveFasterDashForceMultiplier);
            playerState.originalDashCooldown = (float)playerDashCooldown.GetValue(player);
            playerDashCooldown.SetValue(player, (float)playerDashCooldown.GetValue(player) * playerState.moveFasterDashCooldownMultiplier);
            playerState.originalDashDuration = (float)playerDashDuration.GetValue(player);
            playerDashDuration.SetValue(player, (float)playerDashDuration.GetValue(player) * playerState.moveFasterDashDurationMultiplier);
            playerState.originalTurningSpeed = (float)playerTurningSpeed.GetValue(player);
            playerTurningSpeed.SetValue(player, playerState.moveFasterTurnSpeed);
            playerState.originalWalkSpeed = (float)playerWalkSpeed.GetValue(player);
            playerWalkSpeed.SetValue(player, (float)playerWalkSpeed.GetValue(player) * playerState.moveFasterMoveSpeedMultiplier);
            playerState.originalWalkSpeedUnarmed = (float)playerWalkSpeedUnarmed.GetValue(player);
            playerWalkSpeedUnarmed.SetValue(player, (float)playerWalkSpeedUnarmed.GetValue(player) * playerState.moveFasterMoveSpeedMultiplier);
        }

        private void RemovePowerup(Player player, List<PowerupType> powerupHistory, PowerupType newPowerup)
        {
            if (!newPowerup.HasPowerup(Bitmask) || !player.activePowerup.HasPowerup(Bitmask)) return;

            PlayerState playerState = CommonFunctions.GetPlayerState(player);

            playerAttackCooldownDuration.SetValue(player, playerState.originalAttackCooldownDuration);
            playerLungeCooldownDuration.SetValue(player, playerState.originalAttackCooldownDuration);
            playerDashSpeed.SetValue(player, playerState.originalDashSpeed);
            playerDashCooldown.SetValue(player, playerState.originalDashCooldown);
            playerDashDuration.SetValue(player, playerState.originalDashDuration);
            playerTurningSpeed.SetValue(player, playerState.originalTurningSpeed);
            playerWalkSpeed.SetValue(player, playerState.originalWalkSpeed);
            playerWalkSpeedUnarmed.SetValue(player, playerState.originalWalkSpeedUnarmed);
        }
    }
}
