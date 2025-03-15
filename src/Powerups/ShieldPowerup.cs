using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoomerangFoo.Patches;
using BoomerangFoo.UI;
using I2.Loc;

namespace BoomerangFoo.Powerups
{
    class ShieldPowerup : CustomPowerup
    {
        public static int DefaultShieldHits = 1;

        private static ShieldPowerup instance;
        public static ShieldPowerup Instance
        {
            get
            {
                instance ??= new ShieldPowerup();
                return instance;
            }
        }

        public int ShieldHits { get; set; }

        protected ShieldPowerup()
        {
            Name = "Shield";
            LocalizationTerm = "powerup_shield";
            Bitmask = PowerupType.Shield;
            ShieldHits = DefaultShieldHits;
        }

        //public override void Activate()
        //{
        //    PatchPlayer.OnPreInit += PlayerInit;
        //}

        //public override void Deactivate()
        //{
        //    PatchPlayer.OnPreInit -= PlayerInit;
        //}

        //private void PlayerInit(Player player)
        //{
        //    PlayerState playerState = CommonFunctions.GetPlayerState(player);
        //    playerState.shieldHits = ShieldHits;
        //}

        public override void GenerateUI()
        {
            if (hasGeneratedUI) return;
            base.GenerateUI();
            var shieldHits = Modifiers.CloneModifierSetting($"customPowerup.{Name}.shieldHits", "ModifierShieldDurability", "ui_label_edgeprotection", $"customPowerup.{Name}.header");
            SettingIds.Add(shieldHits.id);

            string[] options = new string[31];
            string[] hints = new string[31];
            options[0] = "ModifierInfinite";
            hints[0] = "ModifierShieldInfiniteHint";
            for (int i = 1; i < 31; i++)
            {
                options[i] = i.ToString();
                hints[i] = $"ModifierShieldHint__{i}";
            }
            shieldHits.SetSliderOptions(options, 1, hints);
            shieldHits.SetGameStartCallback((gameMode, sliderIndex) => {
                // maxValue / 2 is big and no chance of overflow
                int hits = sliderIndex == 0 ? int.MaxValue / 2 : sliderIndex; 
                ShieldPowerup.Instance.ShieldHits = hits;
            });
        }
    }
}
