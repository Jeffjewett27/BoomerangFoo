using BoomerangFoo.Patches;
using BoomerangFoo.UI;
using RewiredConsts;
using System.Linq;
using System.Reflection;
using UnityEngine.Profiling.Memory.Experimental;

namespace BoomerangFoo.GameModes
{
    public class PowerDrain : GameMode
    {
        private readonly MethodInfo stopShield = typeof(Player).GetMethod("StopShield", BindingFlags.NonPublic | BindingFlags.Instance);

        public int PowerupsLostOnDeath = 1;
        public bool GivePityShield = false;

        public PowerDrain() : base("PowerDrain", "GameModePowerDrainName", "GameModePowerDrainHint", SettingsManager.MatchType.DeathMatch, false, 0)
        {
            
        }

        public override void Hook()
        {
            PatchPlayer.OnPostDie += OnDeath;
            PatchGameManager.OnPreAddPlayerKill += OnAddPlayerKill;
            PatchPlayer.OnPostGetReady += OnGetReady;
            //PatchLevelManager.BlockPowerupSpawn = true;
        }

        public override void Unhook()
        {
            PatchPlayer.OnPostDie -= OnDeath;
            PatchGameManager.OnPreAddPlayerKill -= OnAddPlayerKill;
            PatchPlayer.OnPostGetReady -= OnGetReady;
            //PatchLevelManager.BlockPowerupSpawn = false;
        }

        public override void RegisterSettings()
        {
            string headerId = $"gameMode.{id}.header";
            var header = Modifiers.CloneModifierSetting(headerId, name, "ui_boomerangs", "ui_label_friendlyfire");

            string lossId = $"gameMode.{id}.powerupLoss";
            var loss = Modifiers.CloneModifierSetting(lossId, "GameModePowerDrainLost", "ui_label_warmuplevel", headerId);
            string[] options = new string[5];
            string[] hints = new string[5];
            options[0] = "GameModePowerDrainLostAll";
            hints[0] = "GameModePowerDrainLostHintAll";
            for (int i = 1; i < options.Length; i++)
            {
                options[i] = i.ToString();
                hints[i] = $"GameModePowerDrainLostHint__{i}";
            }
            loss.SetSliderOptions(options, 1, hints);
            loss.SetGameStartCallback((gameMode, sliderIndex) =>
            {
                if (gameMode is PowerDrain drain)
                {
                    drain.PowerupsLostOnDeath = sliderIndex > 0 ? sliderIndex : 100;
                }
            });

            string shieldId = $"gameMode.{id}.pityShield";
            var swap = Modifiers.CloneModifierSetting(shieldId, "Pity Shield", "ui_label_warmuplevel", lossId);
            swap.SetSliderOptions(["ui_off", "ui_on"], 1, ["GameModePowerDrainPityHintOff", "GameModePowerDrainPityHintOn"]);
            swap.SetGameStartCallback((gameMode, sliderIndex) =>
            {
                if (gameMode is PowerDrain drain)
                {
                    drain.GivePityShield = (sliderIndex == 1);
                }
            });
        }

        public void OnDeath(Player player)
        {
            if (PowerupsLostOnDeath > 16)
            {
                player.ClearPowerups();
                return;
            }
            for (int i = 0; i < PowerupsLostOnDeath; i++) {
                if (player.activePowerup != 0 && player.powerupHistory.Any())
                {
                    player.activePowerup &= ~player.powerupHistory[0];
                    player.powerupHistory.RemoveAt(0);
                }
            }
        }

        public void OnAddPlayerKill(GameManager gameManager, Player killer, Player killed)
        {
            if (killer.IsShielded)
            {
                stopShield.Invoke(killer, null);
            }
            CollectPowerDrainPowerup(killer);
        }

        public void OnGetReady(Player player)
        {
            if (GivePityShield && player.activePowerup == PowerupType.None)
            {
                player.StartShield();
            }
        }

        private void CollectPowerDrainPowerup(Player player)
        {
            if (player.alertBar != null && player.alertBar.currentAlert != 0)
            {
                player.alertBar.HideAlert();
            }
            PowerupType powerupType = Singleton<LevelManager>.Instance.SelectRandomPower(player);
            player.alertBar.ShowPowerup(powerupType);
            Singleton<ParticleManager>.Instance.PlayPFX(PFXType.SparkleBurst, player.transform.position, player.alertBar.GetPowerupColor(powerupType));
            Singleton<AudioManager>.Instance.PlayOneShotAttached("event:/powerups/collect_power", player.gameObject);
            player.StartPowerup(powerupType);
            player.playerInput.StartVibration(0.5f, 0.25f);
            player.metadata.PowerupsCollected++;
        }
    }
}