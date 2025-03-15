using BoomerangFoo.Patches;
using BoomerangFoo.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BoomerangFoo.GameModes
{
    public class GoldenBoomerang : GameMode
    {
        public float HoldTime = -1;
        public float DeathPenalty = 0;

        public GoldenBoomerang() : base("GoldenBoomerang", "ui_goldenboomerang", "ui_helper_goldenboomerang", SettingsManager.MatchType.GoldenDisc, false, 3)
        {
        }

        protected GoldenBoomerang(string id, string name, string hint, SettingsManager.MatchType matchType, bool teamMatch, int slotTemplate) 
            : base(id, name, hint, matchType, teamMatch, slotTemplate) { }

        public override void Hook()
        {
            PatchSettingsManager.GoldenDiscHoldTime = (settingsManager) => HoldTime;
            PatchPlayer.GoldenDiscPenalty = GetPenalty;
        }

        public override void Unhook()
        {
            PatchSettingsManager.GoldenDiscHoldTime = null;
            PatchPlayer.GoldenDiscPenalty = null;
        }

        public override void RegisterSettings()
        {
            string headerId = $"gameMode.GoldenBoomerang.header";
            var header = Modifiers.CloneModifierSetting(headerId, name, "ui_boomerangs", "ui_label_friendlyfire");

            string timeId = $"gameMode.GoldenBoomerang.time";
            var time = Modifiers.CloneModifierSetting(timeId, "Hold Time To Win", "ui_label_warmuplevel", headerId);
            int[] timeValues = [-1, 4, 6, 8, 10, 12, 14, 16, 18, 20, 25, 30];
            string[] timeLabels = timeValues.Select(x => x.ToString()).ToArray();
            timeLabels[0] = "Default";
            string[] timeHints = timeValues.Select(x => $"Hold for {x} seconds").ToArray();
            timeHints[0] = "Use default values, between 9-14 seconds, based on player count";
            time.SetSliderOptions(timeLabels, 0, timeHints);
            time.SetGameStartCallback((gameMode, sliderIndex) =>
            {
                if (gameMode is GoldenBoomerang golden)
                {
                    golden.HoldTime = timeValues[sliderIndex];
                }
            });

            string penaltyId = $"gameMode.GoldenBoomerang.penalty";
            var penalty = Modifiers.CloneModifierSetting(penaltyId, "Death Penalty", "ui_label_warmuplevel", timeId);
            int[] penaltyValues = [0, 100, 5, 10, 15, 20, 25, 30, 40, 50, 75];
            string[] penaltyLabels = penaltyValues.Select(x => $"{x}%").ToArray();
            penaltyLabels[0] = "None";
            penaltyLabels[1] = "Reset";
            string[] penaltyHints = penaltyValues.Select((x) => $"Subtract {x}% of meter on death").ToArray();
            penaltyHints[0] = "No penalty on death";
            penaltyHints[1] = "Time is fully reset on death";
            penalty.SetSliderOptions(penaltyLabels, 0, penaltyHints);
            penalty.SetGameStartCallback((gameMode, sliderIndex) =>
            {
                if (gameMode is GoldenBoomerang golden)
                {
                    golden.DeathPenalty = penaltyValues[sliderIndex] / 100f;
                }
            });
        }
        public float GetPenalty(Player player, Player.HoldingGoldenDisc oldHolding, Player.HoldingGoldenDisc newHolding)
        {
            if (oldHolding == Player.HoldingGoldenDisc.IsHolding)
            {
                if (newHolding == Player.HoldingGoldenDisc.No || newHolding == Player.HoldingGoldenDisc.IsDropped)
                {
                    // Dropped boomerang, so penalize
                    return DeathPenalty * Singleton<GameManager>.Instance.goldenGoalScore;
                }
            }
            return 0;
        }
    }
}
