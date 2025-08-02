using BoomerangFoo.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace BoomerangFoo.Powerups
{
    public class CustomPowerup
    {
        public static List<CustomPowerup> Registered { get; } = [];

        public static void ActivateAll()
        {
            foreach (var power in Registered)
            {
                power.Activate();
            }
        }

        public static void DeactivateAll()
        {
            foreach (var power in Registered)
            {
                power.Deactivate();
            }
        }

        protected bool hasGeneratedUI = false;

        protected CustomPowerup() { }

        public string Name { get; protected set; }

        public string LocalizationTerm { get; protected set; }

        public PowerupType Bitmask { get; protected set; }
        public List<string> SettingIds { get; } = [];

        public bool IsActivated { get; set; } = false;

        public virtual void Activate() 
        {
            IsActivated = true;
        }

        public virtual void Deactivate() 
        {
            IsActivated = false;
        }

        public virtual void GenerateUI()
        {
            if (hasGeneratedUI) return;
            hasGeneratedUI = true;
            string nameText = LocalizationTerm ?? Name;
            var header = Modifiers.CloneModifierSetting($"customPowerup.{Name}.header", nameText, "ui_boomerangs", "gameSpeed");
            SettingIds.Add(header.id);
        }
    }
}
