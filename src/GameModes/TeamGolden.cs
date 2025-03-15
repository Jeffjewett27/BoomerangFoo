using System;
using System.Linq;
using System.Reflection;
using BoomerangFoo.Patches;
using BoomerangFoo.UI;
using UnityEngine.UI;

namespace BoomerangFoo.GameModes
{
    public class TeamGolden : GoldenBoomerang
    {

        private static readonly FieldInfo goldenTimeUIClock = typeof(Player).GetField("goldenTimeUIClock", BindingFlags.Instance | BindingFlags.NonPublic);

        public TeamGolden() : base("TeamGolden", "GameModeTeamGoldenName", "GameModeTeamGoldenHint", SettingsManager.MatchType.GoldenDisc, true, 3)
        {
        }

        public override void Hook()
        {
            PatchGameManager.GoldenDiscPlayerScore = GetTeamGoldenDiscTime;
            PatchPlayer.OnPostRunGoldenDiscTimer += UpdateGoldenTimer;
            PatchSettingsManager.GoldenDiscHoldTime = (settingsManager) => HoldTime;
            PatchPlayer.GoldenDiscPenalty = GetPenalty;
            Modifiers.ShowSelectedGameMode("GoldenBoomerang", false);
        }

        public override void Unhook()
        {
            PatchGameManager.GoldenDiscPlayerScore = null;
            PatchPlayer.OnPostRunGoldenDiscTimer -= UpdateGoldenTimer;
            PatchSettingsManager.GoldenDiscHoldTime = null;
        }

        public float GetTeamGoldenDiscTime(GameManager gameManager, Player player)
        {
            return (from i in gameManager.players
                    where i.team == player.team
                    select Math.Max(i.goldenHoldTime,0)).Sum();
        }

        public new float GetPenalty(Player player, Player.HoldingGoldenDisc oldHolding, Player.HoldingGoldenDisc newHolding)
        {
            if (oldHolding == Player.HoldingGoldenDisc.IsHolding)
            {
                if (newHolding == Player.HoldingGoldenDisc.No || newHolding == Player.HoldingGoldenDisc.IsDropped)
                {
                    if (Singleton<GameManager>.Instance.players
                        .Where(i => i.team == player.team && i != player)
                        .Any(i => i.HasGoldenDisc == Player.HoldingGoldenDisc.IsHolding ||
                                  i.HasGoldenDisc == Player.HoldingGoldenDisc.IsThrown))
                    {
                        // Teammate caught it
                        return 0;
                    }
                    // Dropped boomerang, so penalize
                    return DeathPenalty * Singleton<GameManager>.Instance.goldenGoalScore;
                }
            }
            return 0;
        }

        public void UpdateGoldenTimer(Player player) {
            Image uiclock = (Image)goldenTimeUIClock.GetValue(player);
            float score = GetTeamGoldenDiscTime(Singleton<GameManager>.Instance, player);
            uiclock.fillAmount = score / Singleton<GameManager>.Instance.goldenGoalScore;
        }
    }

}