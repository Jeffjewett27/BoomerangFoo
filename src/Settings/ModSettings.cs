using System;
using System.Collections.Generic;
using System.Text;

namespace BoomerangFoo.Settings
{
    public class ModSettings
    {

        private static ModSettings instance;

        public static ModSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ModSettings();
                }
                return instance;
            }
        }
        public ModSettings() { }

        public int MaxPlayers = 12;


    }
}
