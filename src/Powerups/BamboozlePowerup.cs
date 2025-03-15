using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BoomerangFoo.Patches;
using BoomerangFoo.UI;

namespace BoomerangFoo.Powerups
{
    public class BamboozlePowerup : CustomPowerup
    {
        public static bool DefaultImmunity = false;
        public static float DefaultDuration = 6f;

        private static BamboozlePowerup instance;
        public static BamboozlePowerup Instance { get
            {
                instance ??= new BamboozlePowerup();
                return instance;
            } 
        }

        private static readonly FieldInfo isPowerupTimerOn = typeof(Player).GetField("isPowerupTimerOn", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo powerupTimer = typeof(Player).GetField("powerupTimer", BindingFlags.NonPublic | BindingFlags.Instance);

        public bool Immunity;
        public float Duration;

        protected BamboozlePowerup()
        {
            Name = "Bamboozled";
            LocalizationTerm = "powerup_reverseinputs";
            Immunity = DefaultImmunity;
            Duration = DefaultDuration;
        }
        public override void Activate()
        {
            base.Activate();
            PatchPlayer.OnPreInit += PlayerInit;
        }

        public override void Deactivate()
        {
            base.Deactivate();
            PatchPlayer.OnPreInit -= PlayerInit;
        }

        public override void GenerateUI()
        {
            if (hasGeneratedUI) return;
            base.GenerateUI();

            // duration
            var duration = Modifiers.CloneModifierSetting($"customPowerup.{Name}.duration", "ModifierBamboozleDuration", "ui_label_edgeprotection", $"customPowerup.{Name}.header");
            SettingIds.Add(duration.id);

            float[] durationValues = [1f, 2f, 3f, 4f, 6f, 9f, 12f, 15f, float.MaxValue / 2];
            string[] options = new string[durationValues.Length];
            string[] hints = new string[options.Length];
            options[options.Length - 1] = "ModifierInfinite";
            hints[options.Length - 1] = "ModifierBamboozleDurationHintInfinite";
            for (int i = 0; i < options.Length-1; i++)
            {
                options[i] = durationValues[i].ToString();
                hints[i] = $"ModifierBamboozleDurationHint__{options[i]}";
            }
            duration.SetSliderOptions(options, 4, hints);
            duration.SetGameStartCallback((gameMode, sliderIndex) => {
                BamboozlePowerup.Instance.Duration = durationValues[sliderIndex];
            });

            // invulnerable
            //var invulnerable = Modifiers.CloneModifierSetting($"customPowerup.{Name}.invulnerable", "Invulnerability", "Warm up round", $"customPowerup.{Name}.duration");
            //SettingIds.Add(invulnerable.id);
            //invulnerable.SetSliderOptions(["Off", "On"], 0, ["", "Cannot die while bamboozled"]);
            //invulnerable.SetSliderCallback((sliderIndex) => {
            //    BamboozlePowerup.Instance.Immunity = (sliderIndex == 1);
            //});
        }

        private void PlayerInit(Player player)
        {
            PlayerState playerState = CommonFunctions.GetPlayerState(player);
            playerState.reverseInputsDuration = Duration;
            playerState.reverseInputsImmunity = Immunity;
        }

        public static float StartReverseInputs(Player player)
        {
            PlayerState playerState = CommonFunctions.GetPlayerState(player);

            if (playerState.reverseInputsImmunity)
            {
                player.IsInvulnerable = true;
            }

            float duration = playerState.reverseInputsDuration;
            return duration;
        }

        public static void StopReverseInputs(Player player)
        {
            if ((bool)isPowerupTimerOn.GetValue(player))
            {
                if ((float)powerupTimer.GetValue(player) <= 0)
                {
                    player.IsInvulnerable = false;
                }
            }
        }
    }
}
