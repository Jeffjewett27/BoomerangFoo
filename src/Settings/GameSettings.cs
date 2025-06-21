using System;
using System.Collections.Generic;
using System.Text;

namespace BoomerangFoo.Settings
{
    public class GameSettings
    {
        public float BoomerangBouncinessMultiplier = _CustomSettings.BoomerangBouncinessMultiplier;
        public float BoomerangSize = _CustomSettings.BoomerangSize;
        public int MaxPowerups = _CustomSettings.MaxPowerUps;

        public bool RapidPowerUpSpawning = _CustomSettings.RapidPowerUpSpawning;
        public int MatchScoreLimit = _CustomSettings.MatchScoreLimit;
        public float SuddenDeathTimeLimit = _CustomSettings.SuddenDeathTimeLimit;

        public PowerupType StartupPowerUps = _CustomSettings.StartupPowerUps;
        public bool NoBoomerangs = true;

        public float KnockbackFactor = 1f;
    }
}
