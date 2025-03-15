using BoomerangFoo.UI;

namespace BoomerangFoo.Powerups
{
    public class ExplosivePowerup : CustomPowerup
    {
        public static PowerupType PowerupBitmask = PowerupType.ExplosiveDisc;

        public static float DefaultRadiusMultiplier = 1f;
        public static float DefaultFreezingRadiusMultiplier = 1f;
        public static float DefaultMiniRadiusMultiplier = 1f;

        private static ExplosivePowerup instance;
        public static ExplosivePowerup Instance { get
            {
                instance ??= new ExplosivePowerup();
                return instance;
            } 
        }

        public float RadiusMultiplier { get; set; }
        public float FreezingRadiusMultiplier { get; set; }
        public float MiniRadiusMultiplier { get; set; }

        protected ExplosivePowerup()
        {
            Name = "Explosive";
            LocalizationTerm = "powerup_explosivedisc";
            Bitmask = PowerupType.ExplosiveDisc;
            RadiusMultiplier = DefaultRadiusMultiplier;
            FreezingRadiusMultiplier = DefaultFreezingRadiusMultiplier;
            MiniRadiusMultiplier = DefaultMiniRadiusMultiplier;
        }

        public override void GenerateUI()
        {
            if (hasGeneratedUI) return;
            base.GenerateUI();

            // defaultRadius
            var defaultRadius = Modifiers.CloneModifierSetting($"customPowerup.{Name}.defaultRadius", "Radius Factor", "ui_label_edgeprotection", $"customPowerup.{Name}.header");
            SettingIds.Add(defaultRadius.id);

            float[] radiusValues = [0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f, 2.5f, 3f, 20f];
            string[] radiusOptions = new string[radiusValues.Length];
            string[] radiusHints = new string[radiusOptions.Length];
            for (int i = 0; i < radiusValues.Length; i++)
            {
                radiusOptions[i] = radiusValues[i].ToString();
                radiusHints[i] = $"ModifierExplosionRadiusHint__{radiusValues[i]}";
            }
            radiusHints[3] = "ModifierExplosionRadiusNormalHint";
            radiusOptions[radiusOptions.Length - 1] = "ModifierExplosionRadiusNuke";
            radiusHints[radiusHints.Length - 1] = "ModifierExplosionRadiusNukeHint";
            defaultRadius.SetSliderOptions(radiusOptions, 3, radiusHints);
            defaultRadius.SetGameStartCallback((gameMode, sliderIndex) => {
                ExplosivePowerup.Instance.RadiusMultiplier = radiusValues[sliderIndex];
            });

            // freezeRadius
            var freezeRadius = Modifiers.CloneModifierSetting($"customPowerup.{Name}.freezeRadius", "Ice Factor", "ui_label_edgeprotection", $"customPowerup.{Name}.defaultRadius");
            SettingIds.Add(defaultRadius.id);

            float[] freezeValues = [0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f, 2.5f, 3f, 20f];
            string[] freezeOptions = new string[freezeValues.Length];
            string[] freezeHints = new string[freezeOptions.Length];
            for (int i = 0; i < freezeValues.Length; i++)
            {
                freezeOptions[i] = freezeValues[i].ToString();
                freezeHints[i] = $"ModifierExplosionIceRadiusHint__{freezeOptions[i]}";
            }
            freezeHints[3] = "ModifierExplosionIceRadiusDefaultHint";
            freezeOptions[freezeOptions.Length - 1] = "ModifierExplosionIceAgeRadius";
            freezeHints[freezeHints.Length - 1] = "ModifierExplosionIceAgeRadiusHint";
            freezeRadius.SetSliderOptions(freezeOptions, 3, freezeHints);
            freezeRadius.SetGameStartCallback((gameMode, sliderIndex) => {
                ExplosivePowerup.Instance.FreezingRadiusMultiplier = freezeValues[sliderIndex];
            });

            // miniRadius
            var miniRadius = Modifiers.CloneModifierSetting($"customPowerup.{Name}.miniRadius", "ModifierExplosionMultiRadius", "ui_label_edgeprotection", $"customPowerup.{Name}.freezeRadius");
            SettingIds.Add(miniRadius.id);

            float[] miniValues = [0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f, 2.5f, 3f];
            string[] miniOptions = new string[miniValues.Length];
            string[] miniHints = new string[miniOptions.Length];
            for (int i = 0; i < miniValues.Length; i++)
            {
                miniOptions[i] = miniValues[i].ToString();
                miniHints[i] = $"ModifierExplosionMultiRadiusHint__{miniOptions[i]}";
            }
            miniHints[3] = "ModifierExplosionMultiRadiusHintDefault";
            miniRadius.SetSliderOptions(miniOptions, 3, miniHints);
            miniRadius.SetGameStartCallback((gameMode, sliderIndex) => {
                ExplosivePowerup.Instance.MiniRadiusMultiplier = miniValues[sliderIndex];
            });
        }

        public static float GetRadiusMultipler(PowerupType discPowerup, bool isMiniDisc)
        {
            if (!Instance.IsActivated) return 1;

            float multiplier = 1f;

            if (discPowerup.HasPowerup(PowerupType.SlimeDisc))
            {
                multiplier = Instance.FreezingRadiusMultiplier;
            }
            else if (discPowerup.HasPowerup(PowerupType.FireDisc))
            {
                // consider adding a different setting for fire
                multiplier = Instance.RadiusMultiplier;
            }
            else
            {
                multiplier = Instance.RadiusMultiplier;
            }
            if (isMiniDisc)
            {
                multiplier *= Instance.MiniRadiusMultiplier;
            }
            if (multiplier > 20f)
            {
                multiplier = 20f;
            }
            return multiplier;
        }
    }
}
