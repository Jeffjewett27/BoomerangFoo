﻿using System;
using System.Collections.Generic;
using BoomerangFoo.GameModes;
using I2.Loc;
using TMPro;
using UnityEngine;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using System.Text;
using System.IO;

namespace BoomerangFoo.UI
{
    public class Modifiers
    {
        public static Dictionary<string, ModifierSetting> settings = new Dictionary<string, ModifierSetting>();
        private static readonly FieldInfo platformData = typeof(DataManager).GetField("platformData", BindingFlags.NonPublic | BindingFlags.Instance);
        public static GameObject GetModifiersSettingsContainer(UIMenuMatchSettings matchSettingsUI)
        {
            UIMatchSettingModule modifiers = matchSettingsUI.GetMatchSettingModule(UIMatchSettingModule.MatchSetting.Modifiers);
            GameObject settings = modifiers.gameObject.transform.GetChild(1).GetChild(0).GetChild(0).gameObject;
            
            return settings;
        }

        public static Dictionary<string, ModifierSetting> GetModifierSettings(UIMenuMatchSettings matchSettingsUI)
        {
            Dictionary<string, ModifierSetting> settings = new Dictionary<string, ModifierSetting>();

            GameObject container = GetModifiersSettingsContainer(matchSettingsUI);
            for (int i = 0; i < container.transform.childCount; i++)
            {
                var setting = container.transform.GetChild(i);
                ModifierSetting modifierSetting;
                switch (setting.gameObject.name)
                {
                    case "ROW_HEAD":
                        var headTextMesh = setting.GetComponentInChildren<TextMeshProUGUI>();
                        Component.Destroy(setting.GetComponentInChildren<Localize>());
                        modifierSetting = new ModifierSetting(ModifierSetting.Type.Head, headTextMesh.text, headTextMesh.text, setting.gameObject, null);
                        break;
                    case "ROW":
                        var label = setting.GetChild(0);
                        var labelTextMesh = label.GetComponentInChildren<TextMeshProUGUI>();
                        var slider = setting.GetChild(1).GetComponent<UISliderButton>();
                        Component.Destroy(label.GetComponentInChildren<Localize>());
                        modifierSetting = new ModifierSetting(ModifierSetting.Type.Slider, labelTextMesh.text, labelTextMesh.text, setting.gameObject, slider);
                        break;
                    case "ROW_TOGGLES":
                        // the powerup cluster
                        var labelTemplate = settings["Match length"].gameObject.transform.GetChild(0);
                        var newLabel = UnityEngine.Object.Instantiate(labelTemplate.gameObject);
                        newLabel.transform.SetParent(setting.gameObject.transform, false);
                        newLabel.transform.SetSiblingIndex(0);
                        newLabel.SetActive(false);
                        modifierSetting = new ModifierSetting(ModifierSetting.Type.Powerup, "powerupSelections", "", setting.gameObject);
                        break;
                    default:
                        continue;
                }
                if (!settings.ContainsKey(modifierSetting.id))
                {
                    settings.Add(modifierSetting.id, modifierSetting);
                }
            }
            return settings;
        }

        public static ModifierSetting CloneModifierSetting(string id, string newLabel, string cloneSetting, string attachSetting)
        {
            if (settings.ContainsKey(id))
            {
                // Don't duplicate
                return settings[id];
            }
            if (!settings.ContainsKey(cloneSetting))
            {
                BoomerangFoo.Logger.LogError($"Could not clone modifier setting {cloneSetting}");
            }
            if (!settings.ContainsKey(attachSetting))
            {
                BoomerangFoo.Logger.LogError($"Could not attach to modifier setting {attachSetting}");
            }
            var cloneModifier = settings[cloneSetting];
            var settingContainer = cloneModifier.gameObject.transform.parent;
            var newSettingGO = UnityEngine.Object.Instantiate(cloneModifier.gameObject);
            newSettingGO.transform.SetParent(settingContainer, false);
            int attachIndex = settings[attachSetting].gameObject.transform.GetSiblingIndex();
            newSettingGO.transform.SetSiblingIndex(attachIndex + 1);
            var slider = newSettingGO.transform.GetChild(1).GetComponent<UISliderButton>();
            var labelTextMesh = newSettingGO.transform.GetChild(0).GetComponentInChildren<TextMeshProUGUI>();
            labelTextMesh.text = newLabel;
            Component.Destroy(newSettingGO.transform.GetChild(0).GetComponentInChildren<Localize>());

            var newSetting = new ModifierSetting(cloneModifier.type, id, newLabel, newSettingGO, slider);
            settings[newSetting.id] = newSetting;
            return newSetting;
        }

        public static void ShowSelectedGameMode(string gameModeId)
        {
            foreach (var setting in settings.Values)
            {
                if (setting.id.StartsWith("gameMode"))
                {
                    if (setting.id.StartsWith($"gameMode.{gameModeId}"))
                    {
                        setting?.gameObject.SetActive(true);
                    }
                    else
                    {
                        setting?.gameObject.SetActive(false);
                    }
                }
            }
        }

        public static void SaveSettings()
        {
            if (Modifiers.settings != null)
            {
                PlatformData pdata = (PlatformData)platformData.GetValue(Singleton<DataManager>.Instance);
                Dictionary<string, int> settingIndices = Modifiers.settings
                    .Where(kvp => kvp.Value.GetSelectedIndex() >= 0 && kvp.Value.defaultIndex >= 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.GetSelectedIndex()
                    );
                string path = pdata.GetPath("customModifiers.json");
                string json = JsonConvert.SerializeObject(settingIndices, Formatting.Indented);
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }
                byte[] bytes = Encoding.ASCII.GetBytes(json);
                if (bytes == null || bytes.Length == 0)
                {
                    return;
                }
                else
                {
                    File.WriteAllBytes(path, bytes);
                }
            }
        }

        public static void LoadSettings()
        {
            BoomerangFoo.Logger.LogInfo("Loading match settings");
            PlatformData pdata = (PlatformData)platformData.GetValue(Singleton<DataManager>.Instance);
            string path = pdata.GetPath("customModifiers.json");

            if (!pdata.FileExists(path))
            {
                BoomerangFoo.Logger.LogInfo("customModifiers.json does not exist");
                return;
            }
            string json;
            try
            {
                json = File.ReadAllText(path, Encoding.UTF8);

            }
            catch
            {
                // oh well we tried
                BoomerangFoo.Logger.LogInfo("failed to read all text");
                return;
            }

            Dictionary<string, int> settings = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
            if (settings != null)
            {
                Modifiers.settings ??= [];
                foreach (var kvp in settings)
                {
                    if (Modifiers.settings.ContainsKey(kvp.Key))
                    {
                        BoomerangFoo.Logger.LogInfo($"Setting {kvp.Key}, {kvp.Value}");
                        Modifiers.settings[kvp.Key].SelectIndex(kvp.Value);
                    }
                }
            }
            else
            {
                BoomerangFoo.Logger.LogInfo("failed to deserialize");
            }
        }
    }

    public class ModifierSetting
    {
        private readonly static FieldInfo helperText = typeof(UISliderButton).GetField("helperText", BindingFlags.NonPublic | BindingFlags.Instance);

        public enum Type
        {
            Head,
            Slider,
            Powerup
        }

        public Type type;
        public string id;
        public string text;
        public GameObject gameObject;
        public UISliderButton slider;
        public Action<GameMode, int> gameStartAction;
        public int defaultIndex = -1;

        private PowerupType powerupsActive = PowerupType.None;

        public ModifierSetting(Type type, string id, string text, GameObject gameObject, UISliderButton slider)
        {
            this.type = type;
            this.id = id;
            this.text = text;
            this.gameObject = gameObject;
            this.slider = slider;
        }

        public ModifierSetting(Type type, string id, string text, GameObject gameObject)
        {
            this.type = type;
            this.id = id;
            this.text = text;
            this.gameObject = gameObject;
        }

        public void SetSliderOptions(string[] values, int defaultIndex, string[] hints)
        {
            if (type != Type.Slider) return;

            slider.SetMaxValue(values.Length - 1);
            slider.stringMap = values.Select(x => (LocalizedString)x).ToArray();
            slider.SetValue(defaultIndex);
            this.defaultIndex = defaultIndex;

            if (hints != null)
            {
                //UIHelperText hint = (UIHelperText)helperText.GetValue(slider);
                UIHelperText hint = slider.gameObject.GetComponent<UIHelperText>();
                if (hint != null)
                {
                    hint.strings = hints.Select(x => (LocalizedString)x).ToArray();
                }
            }
        }

        //public void SetSliderCallback(UnityEngine.Events.UnityAction<int> onChange)
        //{
        //    slider.OnValueChanged.RemoveAllListeners();
        //    slider.OnValueChanged.AddListener(onChange);
        //}

        public void SetGameStartCallback(Action<GameMode, int> gameStartAction, bool removeDefault = true)
        {
            if (slider != null && removeDefault)
            {
                slider.OnValueChanged.RemoveAllListeners();
            }
            
            this.gameStartAction = gameStartAction;
        }

        public void TriggerGameStartCallback(GameMode gameMode)
        {
            if (gameStartAction == null)
            {
                return;
            }

            if (type == Type.Slider) {
                if (slider == null) return;

                gameStartAction.Invoke(gameMode, slider.value);
                return;
            }
            if (type == Type.Powerup)
            {
                gameStartAction.Invoke(gameMode, (int)powerupsActive);
                return;
            }
        }

        public void PreparePowerupToggles(PowerupType defaultPowerups, bool useLabel = true)
        {
            if (useLabel)
            {
                // assumes label is index 0, padding is index 1
                gameObject.transform.GetChild(0).gameObject.SetActive(true);
                gameObject.transform.GetChild(1).gameObject.SetActive(false);
            }
            this.defaultIndex = (int)defaultPowerups;

            var toggles = gameObject.transform.GetChild(2).transform.GetComponentsInChildren<UIPowerupToggle>();
            powerupsActive = defaultPowerups;
            foreach (UIPowerupToggle toggle in toggles)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.isOn = (powerupsActive & toggle.powerupType) != 0;
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        powerupsActive |= toggle.powerupType;
                    } else
                    {
                        powerupsActive &= ~toggle.powerupType;
                    }
                });
                var nav = toggle.navigation;
                nav.mode = UnityEngine.UI.Navigation.Mode.Automatic;
                toggle.navigation = nav;
            }
        }

        public int GetSelectedIndex()
        {
            if (type == Type.Slider)
            {
                if (slider == null) return -1;

                return slider.value;
            }
            if (type == Type.Powerup)
            {
                return (int)powerupsActive;
            }
            return -1;
        }

        public void ResetToDefault()
        {
            SelectIndex(defaultIndex);
        }

        public void SelectIndex(int index)
        {
            if (type == Type.Slider)
            {
                if (slider == null || defaultIndex < 0) return;

                slider.SetValue(index);
                return;
            }
            if (type == Type.Powerup)
            {
                var toggles = gameObject.transform.GetChild(2).transform.GetComponentsInChildren<UIPowerupToggle>();
                powerupsActive = (PowerupType)index;
                foreach (UIPowerupToggle toggle in toggles)
                {
                    toggle.isOn = (powerupsActive & toggle.powerupType) != 0;   
                }
                return;
            }
        }
    }
}
