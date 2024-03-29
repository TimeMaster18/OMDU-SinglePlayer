﻿using IniParser.Model;
using SingleplayerLauncher.Model;
using SingleplayerLauncher.Utils;
using System;

namespace SingleplayerLauncher.GameSettings
{
    public static class DefaultGame
    {
        private static readonly GameInfo GameInfo = GameInfo.Instance;

        private const string DefaultGameIniFileName = "DefaultGame.ini";
        private const string DefaultGameIniPath = "..//SpitfireGame//Config//DefaultGame.ini";

        private const string RDisplayColorInfoSection = "SpitfireGame.RDisplayColorInfo";
        private const string RGameReplicationInfoSection = "SpitfireGame.RGameReplicationInfo";
        private const string RHUDBaseSection = "SpitfireGame.RHUDBase";

        private const string GameReplicationInfoKeyGameMode = "DefaultOfflineDifficulty";
        private const string GameReplicationInfoKeyMapLevel = "DefaultOfflineSuggestedLevel";
        private const string GameReplicationInfoKeyPlayerLevel = "DefaultOfflinePlayerLevel";
        private const string GameReplicationInfoKeyPlayerCount = "PlayerCountOverride";
        private const string GameReplicationInfoKeyPauseTimerInSeconds = "DefaultOfflinePauseTimerDurationInSeconds";
        private const string RHUDBaseKeyShowFlyoffsForTrapDamage = "ShowFlyoffsForTrapDamage";

        private const string DefaultPauseTimerInSeconds = "999999";

        public static void Apply()
        {
            _ = GameInfo.Battleground ?? throw new ArgumentNullException(nameof(Battleground), "Mandatory parameter");
            _ = GameInfo.Battleground.Difficulty ?? throw new ArgumentNullException(nameof(Battleground.Difficulty), "Mandatory parameter");
            _ = Mods.Mods.ShowTrapDamageFlyoffs ?? throw new ArgumentNullException(nameof(Mods.Mods.ShowTrapDamageFlyoffs), "Mandatory parameter");

            ConfigFile defaultGame = new ConfigFile(DefaultGameIniPath);
            IniData data = defaultGame.data;

            data[RHUDBaseSection][RHUDBaseKeyShowFlyoffsForTrapDamage] = Mods.Mods.ShowTrapDamageFlyoffs.IsEnabled.ToString();
            data[RGameReplicationInfoSection][GameReplicationInfoKeyGameMode] = GameInfo.Battleground.GameMode.Id.ToString();
            data[RGameReplicationInfoSection][GameReplicationInfoKeyMapLevel] = GameInfo.Battleground.Difficulty.EnemyLevel.ToString();
            data[RGameReplicationInfoSection][GameReplicationInfoKeyPlayerLevel] = GameInfo.Battleground.Difficulty.AccountLevel.ToString();
            data[RGameReplicationInfoSection][GameReplicationInfoKeyPlayerCount] = GameInfo.Battleground.Difficulty.PlayerCount.ToString();

            defaultGame.Write(data);
        }

        public static void ApplyMods(bool areEnabled)
        {
            _ = Mods.Mods.ShowTrapDamageFlyoffs ?? throw new ArgumentNullException(nameof(Mods.Mods.ShowTrapDamageFlyoffs), "Mandatory parameter");

            ConfigFile defaultGame = new ConfigFile(DefaultGameIniPath);
            IniData data = defaultGame.data;

            data[RHUDBaseSection][RHUDBaseKeyShowFlyoffsForTrapDamage] = areEnabled ? false.ToString() : Mods.Mods.ShowTrapDamageFlyoffs.IsEnabled.ToString();

            defaultGame.Write(data);
        }

        public static void Initialize()
        {
            FileUtils.CreateBackup(DefaultGameIniFileName, DefaultGameIniPath);

            ConfigFile defaultGame = new ConfigFile(DefaultGameIniPath);
            IniData data = defaultGame.data;

            data[RGameReplicationInfoSection][GameReplicationInfoKeyPauseTimerInSeconds] = DefaultPauseTimerInSeconds;

            data.Sections.RemoveSection(RDisplayColorInfoSection);

            defaultGame.Write(data);
        }
    }


}
