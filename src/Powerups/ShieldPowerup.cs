using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoomerangFoo.Patches;
using BoomerangFoo.UI;
using I2.Loc;
using UnityEngine;

namespace BoomerangFoo.Powerups
{
    class ShieldPowerup : CustomPowerup
    {
        public static int DefaultShieldHits = 1;
        public static readonly float[] Intensities = { 0, 0.1f, 0.5f, 0.8f };

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
        public bool EachRound {  get; set; }

        protected ShieldPowerup()
        {
            Name = "Shield";
            LocalizationTerm = "powerup_shield";
            Bitmask = PowerupType.Shield;
            ShieldHits = DefaultShieldHits;
        }

        public override void Activate()
        {
            PatchPlayer.OnPreInit += PlayerInit;
            PatchPlayer.OnPostUpdate += ShieldColorUpdate;
            PatchPlayer.OnPostSpawnIn += PostSpawnIn;
        }

        public override void Deactivate()
        {
            PatchPlayer.OnPreInit -= PlayerInit;
            PatchPlayer.OnPostUpdate -= ShieldColorUpdate;
        }

        private void PlayerInit(Player player)
        {
            PlayerState playerState = CommonFunctions.GetPlayerState(player);
            Renderer renderer = player.shield.GetComponent<Renderer>();
            playerState.shieldRenderer = renderer;
        }

        private void PostSpawnIn(Player player)
        {
            if (EachRound) {
                player.StartShield();
            }
        }

        private void ShieldColorUpdate(Player player)
        {
            PlayerState playerState = CommonFunctions.GetPlayerState(player);
            Renderer renderer = playerState.shieldRenderer;
            Color origColor = player.character.Color;
            if (renderer != null)
            {
                if (renderer.material == null) {
                    return;
                }
                HSBColor hsbColor = HSBColor.FromColor(origColor);
                hsbColor.b -= 0.4f;
                hsbColor.s += 0.2f;
                if (hsbColor.s >= 10000)
                {
                    hsbColor.s = 1f;
                }
                renderer.material.SetColor("_RimColor", HSBColor.ToColor(hsbColor));
                if (player.shieldHitsLeft < 10000) //not infinite
                {
                    hsbColor.b = Intensities[Math.Min(player.shieldHitsLeft, Intensities.Length-1)];
                } else
                {
                    hsbColor.b = 0.1f;
                }
                renderer.material.SetColor("_InnerColor", HSBColor.ToColor(hsbColor));
            }
        }

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

            var revive = Modifiers.CloneModifierSetting($"customPowerup.{Name}.eachRound", "ModifierShieldRound", "ui_label_warmuplevel", $"customPowerup.{Name}.shieldHits");
            SettingIds.Add(revive.id);
            revive.SetSliderOptions(["ui_off", "ui_on"], 0, ["ModifierShieldEachRoundOffHint", "ModifierShieldEachRoundOnHint"]);
            revive.SetGameStartCallback((gameMode, sliderIndex) =>
            {
                ShieldPowerup.Instance.EachRound = (sliderIndex == 1);
            });
        }
    }
}
