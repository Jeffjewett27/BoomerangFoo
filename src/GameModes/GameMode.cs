using BoomerangFoo.Powerups;
using BoomerangFoo.Settings;
using BoomerangFoo.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace BoomerangFoo.GameModes
{
    public class GameMode
    {
        public enum Slot
        {
            None = -1,
            Deathmatch = 0,
            TeamUp = 1,
            HideAndSeek = 2,
            GoldenBoomerang = 3,
            Extra1 = 4,
            Extra2 = 5,
            Extra3 = 6,
            Extra4 = 7,
            Extra5 = 8
        }

        public enum Relationship
        {
            Opponent, // KILL THEM and get rewarded
            Teammate, // you can't kill them and lose points if you cause their death
            Neutral, // you gain no benefit from killing them, but you can
            Temporary // the player is temporarily your ally and you can't kill them
        }

        public static GameMode selected;
        public static List<GameMode> slots = new List<GameMode>(4);
        public static Dictionary<string, GameMode> registered = new Dictionary<string, GameMode>();

        public static void Register(GameMode gameMode, Slot slotId)
        {
            string name = gameMode.id.ToLower();
            if (!registered.ContainsKey(name))
            {
                registered.Add(name, gameMode);
            }

            int idx = (int)slotId;
            if (idx < 0) return;

            if (slots.Count <= idx)
            {
                // Add default values (e.g., 0 for int) until the list has enough elements
                slots.AddRange(Enumerable.Repeat<GameMode>(null, idx - slots.Count + 1));
            }
            slots[idx] = gameMode;
        }

        public static void SetSlot(string name, Slot slot)
        {
            name = name.ToLower();
            if (!registered.ContainsKey(name))
            {
                BoomerangFoo.Logger.LogWarning($"Tried to set slot for {name}, which is not a registered gamemode.");
                return;
            }
            slots[(int)slot] = registered[name];
        }

        public static void MatchSelected(Slot gamemodeId)
        {
            if (gamemodeId < 0 || (int)gamemodeId >= slots.Count)
            {
                BoomerangFoo.Logger.LogWarning($"GameMode {gamemodeId} was selected, but no gamemode registered");
                return;
            }

            if (selected != null && selected.isHooked)
            {
                selected.Unhook();
                selected.isHooked = false;
            }
            selected = slots[(int)gamemodeId];
            Singleton<SettingsManager>.Instance.matchType = selected.matchType;
            Singleton<SettingsManager>.Instance.teamMatch = selected.teamMatch;
            Modifiers.ShowSelectedGameMode(selected.id);
            selected.Hook();
            selected.isHooked = true;
            BoomerangFoo.Logger.LogInfo($"[Slot {gamemodeId}] Selected gamemode {selected.id}!");

            Modifiers.LoadSettings();
        }

        public string id;
        public string name;
        public string hint;
        public SettingsManager.MatchType matchType;
		public bool teamMatch;
        public GameSettings gameSettings;

        // indicates which gamemode gameobject icon to clone
        // 0 - Deathmatch
        // 1 - Team Deathmatch
        // 2 - Hide and Seek
        // 3 - Golden Boomerang
        public int slotTemplate; 

        private bool isHooked = false;

        public GameMode(string id, string name, string hint, SettingsManager.MatchType matchType, bool teamMatch, int slotTemplate)
        {
            if (id== null) throw new ArgumentNullException("id");
            if (name == null) throw new ArgumentNullException("name");
            if (hint == null) hint = "";
            if (slotTemplate < 0 || slotTemplate >= 4) throw new ArgumentOutOfRangeException("slotTemplate should be 0, 1, 2, or 3");

            this.id = id;
            this.name = name;
            this.hint = hint;
            this.matchType = matchType;
            this.teamMatch = teamMatch;
            this.slotTemplate = slotTemplate;
            this.gameSettings = new GameSettings();
        }

        public virtual void Hook() { }

        public virtual void Unhook() { }

        public void GameStart()
        {
            foreach (var settingPair in Modifiers.settings)
            {
                settingPair.Value.TriggerGameStartCallback(this);
            }
        }

        public virtual void RegisterSettings()
        {
            if (!Modifiers.settings.ContainsKey("bounciness"))
            {
                var bounciness = Modifiers.CloneModifierSetting("bounciness", "Bounciness", "ui_label_edgeprotection", "ui_label_homing");
                bounciness.SetSliderOptions(["Low", "Regular", "High", "Extreme"], 1, ["20%", "100%", "200%", "400%"]);
                bounciness.SetGameStartCallback((gameMode, sliderIndex) => {
                    float[] options = [0.2f, 1f, 2f, 4f];
                    gameMode.gameSettings.BoomerangBouncinessMultiplier = options[sliderIndex];
                });
            }
            if (!Modifiers.settings.ContainsKey("boomerangSize"))
            {
                var bounciness = Modifiers.CloneModifierSetting("boomerangSize", "Size", "ui_label_edgeprotection", "bounciness");
                bounciness.SetSliderOptions(["Mini", "Regular", "Large", "Comical"], 1, ["20%", "100%", "200%", "400%"]);
                bounciness.SetGameStartCallback((gameMode, sliderIndex) => {
                    float[] options = [0.2f, 1f, 2f, 4f];
                    gameMode.gameSettings.BoomerangSize = options[sliderIndex];
                });
            }
            if (!Modifiers.settings.ContainsKey("maxPowerups"))
            {
                var bounciness = Modifiers.CloneModifierSetting("maxPowerups", "Max Number", "ui_label_edgeprotection", "ui_powerup_spawn_rate");
                bounciness.SetSliderOptions(["1", "2", "3", "4", "5", "6", "7", "8"], 2, ["1 powerup", "2 powerups", "3 powerups", "4 powerups", "5 powerups", "6 powerups", "7 powerups", "8 powerups"]);
                bounciness.SetGameStartCallback((gameMode, sliderIndex) => {
                    int[] options = [1, 2, 3, 4, 5, 6, 7, 8];
                    gameMode.gameSettings.MaxPowerups = options[sliderIndex];
                });
            }
            if (Modifiers.settings.ContainsKey("ui_powerup_spawn_rate"))
            {
                var spawnRate = Modifiers.settings["ui_powerup_spawn_rate"];
                spawnRate.SetSliderOptions(["0.5X", "1X", "2X", "Rapid"], 0, null);
                spawnRate.SetGameStartCallback((gameMode, sliderIndex) =>
                {
                    if (sliderIndex <= (int)PowerupSpawnFrequency.Fast)
                    {
                        Singleton<SettingsManager>.Instance.MatchSettings.powerupSpawnFrequency = (PowerupSpawnFrequency)sliderIndex;
                        gameMode.gameSettings.RapidPowerUpSpawning = false;
                    }
                    else
                    {
                        Singleton<SettingsManager>.Instance.MatchSettings.powerupSpawnFrequency = PowerupSpawnFrequency.Fast;
                        gameMode.gameSettings.RapidPowerUpSpawning = true;
                    }
                });
            }
            if (Modifiers.settings.ContainsKey("ui_label_suddendeath"))
            {
                var spawnRate = Modifiers.settings["ui_label_suddendeath"];
                spawnRate.SetSliderOptions(["ui_instant", "15 seconds", "ui_30secs", "1 min", "1.5 mins", "ui_2mins", "3 mins", "4 mins", "6 mins", "8 mins", "10 mins", "Infinite"], 5, null);
                int[] suddenDeathValues = { 0, 15, 30, 60, 90, 120, 180, 240, 360, 480, 600, int.MaxValue / 2 };
                spawnRate.SetGameStartCallback((gameMode, sliderIndex) =>
                {
                    Singleton<SettingsManager>.Instance.MatchSettings.suddenDeathTime = suddenDeathValues[sliderIndex];
                    gameMode.gameSettings.SuddenDeathTimeLimit = suddenDeathValues[sliderIndex];
                });
            }
            if (Modifiers.settings.ContainsKey("ui_label_matchlength"))
            {
                var matchLength = Modifiers.settings["ui_label_matchlength"];
                string[] options = new string[33];
                options[0] = "Quick";
                options[1] = "Standard";
                options[2] = "Long";
                for (int i = 3; i < 33; i++)
                {
                    options[i] = (i-2).ToString();
                }
                matchLength.SetSliderOptions(options, 1, null);
                matchLength.SetGameStartCallback((gameMode, sliderIndex) =>
                {
                    if (sliderIndex <= (int)MatchLength.Long)
                    {
                        Singleton<SettingsManager>.Instance.MatchSettings.matchLength = (MatchLength)sliderIndex;
                        gameMode.gameSettings.MatchScoreLimit = 0;
                    }
                    else
                    {
                        gameMode.gameSettings.MatchScoreLimit = sliderIndex - 2;
                    }
                });
            }
            if (!Modifiers.settings.ContainsKey("noBoomerangs"))
            {
                var noBoomerangs = Modifiers.CloneModifierSetting($"noBoomerangs", "NoBoomerangs", "ui_label_warmuplevel", $"boomerangSize");
                noBoomerangs.SetSliderOptions(["ui_off", "ui_on"], 0, ["NoBoomerangsOffHint", "NoBoomerangsOnHint"]);
                noBoomerangs.SetGameStartCallback((gameMode, sliderIndex) =>
                {
                    gameMode.gameSettings.NoBoomerangs = (sliderIndex == 1);
                });
            }
            if (!Modifiers.settings.ContainsKey("knockback"))
            {
                var knockback = Modifiers.CloneModifierSetting($"knockback", "KnockbackModifier", "ui_label_warmuplevel", $"noBoomerangs");
                float[] kbValues = { 0.1f, 0.3f, 0.5f, 0.8f, 1f, 1.2f, 1.5f, 2f, 2.5f, 3f, 4f, 5f, 6f, 8f, 10f, 15f };
                string[] kbOptions = new string[kbValues.Length];
                string[] kbHints = new string[kbOptions.Length];
                for (int i = 0; i < kbValues.Length; i++)
                {
                    kbOptions[i] = kbValues[i].ToString();
                    kbHints[i] = $"ModifierKnockbackHint__{kbOptions[i]}";
                }
                knockback.SetSliderOptions(kbOptions, 4, kbHints);
                knockback.SetGameStartCallback((gameMode, sliderIndex) =>
                {
                    gameMode.gameSettings.KnockbackFactor = kbValues[sliderIndex];
                });
            }
            if (!Modifiers.settings.ContainsKey("startPowers"))
            {
                var test = Modifiers.CloneModifierSetting("startPowers", "Starting Powers", "powerupSelections", "maxPowerups");
                test.PreparePowerupToggles(PowerupType.None);
                test.SetGameStartCallback((gameMode, powerups) =>
                {
                    gameMode.gameSettings.StartupPowerUps = (PowerupType)powerups;
                });
            }
            foreach (var powerup in CustomPowerup.Registered)
            {
                powerup.GenerateUI();
            }
        }
    }
}
