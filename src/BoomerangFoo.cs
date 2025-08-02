using BepInEx;
using BepInEx.Logging;
using BoomerangFoo.GameModes;
using BoomerangFoo.Patches;
using BoomerangFoo.Powerups;
using BoomerangFoo.Settings;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace BoomerangFoo;

[BepInPlugin("Jeffjewett27.plugins.BoomerangFoo", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class BoomerangFoo : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
        
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} is loaded!");

        var harmony = new Harmony("Jeffjewett27.patch");
        harmony.PatchAll();

        // Register powerup hooks
        CustomPowerup.Registered.Add(BamboozlePowerup.Instance);
        CustomPowerup.Registered.Add(DecoyPowerup.Instance);
        CustomPowerup.Registered.Add(ExplosivePowerup.Instance);
        CustomPowerup.Registered.Add(FirePowerup.Instance);
        CustomPowerup.Registered.Add(FlyingPowerup.Instance);
        CustomPowerup.Registered.Add(MoveFasterPowerup.Instance);
        CustomPowerup.Registered.Add(MultiBoomerangPowerup.Instance);
        CustomPowerup.Registered.Add(ShieldPowerup.Instance);
        CustomPowerup.ActivateAll();

        GameMode.Register(new GameMode("Deathmatch", "ui_deathmatch", "ui_helper_deathmatch", SettingsManager.MatchType.DeathMatch, false, 0), GameMode.Slot.Deathmatch);
        GameMode.Register(new GameMode("TeamDeathmatch", "ui_teams", "ui_helper_teamup", SettingsManager.MatchType.DeathMatch, true, 1), GameMode.Slot.TeamUp);
        GameMode.Register(new GameMode("HideAndSeek", "ui_hideandseek", "ui_helper_hideandseek", SettingsManager.MatchType.HideAndSeek, false, 2), GameMode.Slot.HideAndSeek);
        GameMode.Register(new GoldenBoomerang(), GameMode.Slot.GoldenBoomerang);
        PatchUIMenuMatchSettings.OnMatchTypeSelected += GameMode.MatchSelected;

        GameMode.Register(new PowerDrain(), GameMode.Slot.Extra1);
        GameMode.Register(new RamboHulk(), GameMode.Slot.Extra2);
        GameMode.Register(new SurviveTillDawn(), GameMode.Slot.Extra3);
        GameMode.Register(new TeamGolden(), GameMode.Slot.Extra4);

        PatchGameManager.OnPreStartMatch += (gameManager) =>
        {
            GameMode.selected.GameStart();
            Singleton<SettingsManager>.Instance.PrepareMatchLength();
        };

        PatchGameManager.OnPostUpdate += (gameManager) =>
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                BoomerangFoo.Logger.LogInfo("Debug mode toggled");
                Singleton<SettingsManager>.Instance.debugMode = !Singleton<SettingsManager>.Instance.debugMode;
            }

            if (GameMode.selected?.gameSettings != null)
            {
                Singleton<TimeKeeper>.Instance.targetScale = GameMode.selected.gameSettings.GameSpeed;
            }
        };
    }
}
