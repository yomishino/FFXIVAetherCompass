using AetherCompass.Common;
using AetherCompass.Compasses.Configs;
using Dalamud.Configuration;
using Newtonsoft.Json;
using System.Numerics;
using System.IO;
using System.Text.RegularExpressions;

namespace AetherCompass
{
    [Serializable]
    public class PluginConfig : IPluginConfiguration
    {
        [JsonIgnore]
        public const int ActiveVersion = 2; // !!

        public int Version { get; set; } = ActiveVersion;

        public bool Enabled = true;
        public bool ShowScreenMark = true;
        public float ScreenMarkSizeScale = 1;
        public float ScreenMarkTextRelSizeScale = 1;
        [JsonIgnore]
        public static readonly (float Min, float Max) ScreenMarkSizeBound = (.1f, 10);
        [JsonIgnore]
        public static readonly (float Min, float Max) ScreenMarkTextRelSizeBound = (.5f, 2);
        // L,D,R,U; how much to squeeze into centre on each side, so generally should be positive
        public Vector4 ScreenMarkConstraint = new(80, 80, 80, 80);
        [JsonIgnore]
        public const float ScreenMarkConstraintMin = 2;
        public bool HideScreenMarkIfNameplateInsideDisplayArea = false;
        public int HideScreenMarkEnabledDistance = 30;
        [JsonIgnore]
        public static readonly (int Min, int Max) HideScreenMarkEnabledDistanceBound = (5, 50);
        public bool ShowDetailWindow = false;
        public bool HideDetailInContents = false;
        public bool HideInEvent = false;
        public bool HideWhenCraftGather = false;
        public bool NotifyChat = false;
        public bool NotifySe = false;
        public bool NotifyToast = false;

        public bool ShowSponsor = false;

        public AetherCurrentCompassConfig AetherCurrentConfig { get; private set; } = new();
        public MobHuntCompassConfig MobHuntConfig { get; private set; } = new();
        public GatheringPointCompassConfig GatheringConfig { get; private set; } = new();
        public IslandSanctuaryCompassConfig IslandConfig { get; private set; } = new();
        public QuestCompassConfig QuestConfig { get; private set; } = new();
        public EurekanCompassConfig EurekanConfig { get; private set; } = new();

#if DEBUG
        [JsonIgnore]
        public bool DebugTestAllGameObjects = false;
#endif
        [JsonIgnore]
        public DebugCompassConfig DebugConfig { get; private set; } = new();

        public void CheckValueValidity(Vector2 screenSize)
        {
            ScreenMarkSizeScale = MathUtil.Clamp(ScreenMarkSizeScale,
                ScreenMarkSizeBound.Min, ScreenMarkSizeBound.Max);
            ScreenMarkTextRelSizeScale = MathUtil.Clamp(ScreenMarkTextRelSizeScale,
                ScreenMarkTextRelSizeBound.Min, ScreenMarkTextRelSizeBound.Max);

            ScreenMarkConstraint.X = MathUtil.Clamp(ScreenMarkConstraint.X,
                ScreenMarkConstraintMin, screenSize.X / 2 - 10);
            ScreenMarkConstraint.Y = MathUtil.Clamp(ScreenMarkConstraint.Y,
                ScreenMarkConstraintMin, screenSize.Y / 2 - 10);
            ScreenMarkConstraint.Z = MathUtil.Clamp(ScreenMarkConstraint.Z,
                ScreenMarkConstraintMin, screenSize.X / 2 - 10);
            ScreenMarkConstraint.W = MathUtil.Clamp(ScreenMarkConstraint.W,
                ScreenMarkConstraintMin, screenSize.Y / 2 - 10);

            HideScreenMarkEnabledDistance
                = (int)MathUtil.Clamp(HideScreenMarkEnabledDistance,
                    HideScreenMarkEnabledDistanceBound.Min,
                    HideScreenMarkEnabledDistanceBound.Max);

            AetherCurrentConfig.CheckValueValidity();
            MobHuntConfig.CheckValueValidity();
            GatheringConfig.CheckValueValidity();
            IslandConfig.CheckValueValidity();
            QuestConfig.CheckValueValidity();
            EurekanConfig.CheckValueValidity();
#if DEBUG
            DebugConfig.CheckValueValidity();
#endif
        }

        private void LoadValues(PluginConfig config)
        {
            Version = ActiveVersion;

            Enabled = config.Enabled;
            ShowScreenMark = config.ShowScreenMark;
            ScreenMarkSizeScale = config.ScreenMarkSizeScale;
            ScreenMarkConstraint = config.ScreenMarkConstraint;
            ShowDetailWindow = config.ShowDetailWindow;
            HideDetailInContents = config.HideDetailInContents;
            HideInEvent = config.HideInEvent;
            HideWhenCraftGather = config.HideWhenCraftGather;
            NotifyChat = config.NotifyChat;
            NotifySe = config.NotifySe;
            NotifyToast = config.NotifyToast;
            HideScreenMarkEnabledDistance = config.HideScreenMarkEnabledDistance;
            HideScreenMarkIfNameplateInsideDisplayArea = config.HideScreenMarkIfNameplateInsideDisplayArea;

            AetherCurrentConfig.Load(config.AetherCurrentConfig);
            MobHuntConfig.Load(config.MobHuntConfig);
            GatheringConfig.Load(config.GatheringConfig);
            IslandConfig.Load(config.IslandConfig);
            QuestConfig.Load(config.QuestConfig);
            EurekanConfig.Load(config.EurekanConfig);
        }

        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }

        public void Load()
            => Load(PluginConfigHelper.GetSavedPluginConfig());

        public void Load(PluginConfig config)
        {
            var versionMatched = PreloadCheck(config, out var checkedConfig);
            if (!versionMatched)
                PluginConfigHelper.BackupSavedPluginConfig();
            LoadValues(checkedConfig);
            if (!versionMatched) Save();
        }

        private static bool PreloadCheck(PluginConfig config, 
            out PluginConfig checkedConfig)
        {
            if (ActiveVersion == config.Version)
            {
                LogDebug("Config version matched. Using saved config.");
                checkedConfig = config;
                return true;
            }
            else
            {
                var restored = PluginConfigHelper.RestoreBackupConfig(
                    PluginConfigHelper.FindMatchingConfigBackup(
                        ActiveVersion, GetPluginVersionAsString()));
                if (restored == null)
                {
                    LogWarning("Config version not matched and no backup found. "
                        + " Load saved config anyway.");
                    checkedConfig = config;
                }
                else
                {
                    LogWarning("Config version not matched. "
                        + "Trying to restore from backup.");
                    checkedConfig = restored;
                }
                return false;
            }
        }


        private static class PluginConfigHelper
        {
            const string ConfBkpFolderName = "confbkp";
            const string ConfBkpFilenamePart1 = "conf_";
            const string ConfBkpFileExt = ".json";
            const string confBkpPatternConfigVerKey = "cver";
            const string confBkpPatternPluginVerKey = "pver";
            const string ConfBkpFilenamePattern
                = @ConfBkpFilenamePart1
                + @$"c(?<{confBkpPatternConfigVerKey}>[0-9]+)_"
                + @$"v(?<{confBkpPatternPluginVerKey}>[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)"
                + @"\" + @ConfBkpFileExt;
            const string ConfBkpFileFullpathPattern
                = @".+[/\\]" + ConfBkpFilenamePattern;

            private static readonly Regex BackupConfigFullpathMatcher
                = new(ConfBkpFileFullpathPattern);

            private static string BackupConfigDirectoryPath
                => Path.Combine(
                    Plugin.PluginInterface.GetPluginConfigDirectory(),
                    ConfBkpFolderName);

            private static string GenerateConfBkpFileName(PluginConfig config)
                => ConfBkpFilenamePart1
                + $"c{config.Version}_v{GetPluginVersionAsString()}"
                + ConfBkpFileExt;

            internal static PluginConfig GetSavedPluginConfig()
                => GetSavedPluginConfigIfAny() ?? new();

            internal static PluginConfig? GetSavedPluginConfigIfAny()
                => Plugin.PluginInterface.GetPluginConfig() as PluginConfig;

            internal static string GetSavedPluginConfigPath()
                => Plugin.PluginInterface.ConfigFile.FullName;

            internal static void BackupSavedPluginConfig()
            {
                try
                {
                    var config = GetSavedPluginConfigIfAny();
                    if (config == null)
                    {
                        LogDebug("Ending config back up because no valid saved config is found");
                        return;
                    }

                    var dirpath = BackupConfigDirectoryPath;
                    var dir = Directory.CreateDirectory(dirpath);

                    var bkpFilename = GenerateConfBkpFileName(config);
                    var bkpFilePath = Path.Combine(dirpath, bkpFilename);
                    File.Copy(GetSavedPluginConfigPath(), bkpFilePath, true);
                    LogInfo($"Created config back up at: {bkpFilePath}");
                }
                catch (Exception e)
                {
                    LogError("Failed to back up plugin config.\n" + e);
                }
            }

            internal static PluginConfig? RestoreBackupConfig(string? fullpath)
            {
                if (string.IsNullOrEmpty(fullpath)) return null;

                if (!File.Exists(fullpath))
                {
                    LogError($"Failed to restore backup config. "
                        + $"File does not exist: {fullpath ?? "<null>"}");
                    return null;
                }

                try
                {
                    var contents = File.ReadAllText(fullpath);
                    var restoredConfig 
                        = JsonConvert.DeserializeObject<PluginConfig>(contents);
                    LogInfo($"Config restored from {fullpath}");
                    return restoredConfig;
                }
                catch (Exception e)
                {
                    LogError("Failed to restore plugin config.\n" + e);
                }
                return null;
            }

            internal static string? FindMatchingConfigBackup(
                int desiredConfigVersion, string desiredPluginVersion)
            {
                var dirpath = BackupConfigDirectoryPath;
                if (!Directory.Exists(dirpath)) return null;
                (string Path, int CVer, string PVer) match = ("", -1, "0.0.0.0");
                try
                {
                    var filepaths = Directory.GetFiles(dirpath);
                    foreach (var filepath in filepaths)
                    {
                        if (!BackupConfigFullpathMatcher.IsMatch(filepath))
                            continue;
                        var groups = BackupConfigFullpathMatcher.Match(filepath).Groups;
                        var bkpConfigVer
                            = groups.TryGetValue(confBkpPatternConfigVerKey, out var g1)
                            ? int.Parse(g1.Value) : -1;
                        var bkpPluginVer 
                            = groups.TryGetValue(confBkpPatternPluginVerKey, out var g2)
                            ? g2.Value : "0.0.0.0";
                        if (desiredConfigVersion != bkpConfigVer) continue;
                        if (desiredPluginVersion == bkpPluginVer)
                        {
                            match = (filepath, bkpConfigVer, bkpPluginVer);
                        }
                        else if (ComparePluginVersion(bkpPluginVer, match.PVer) > 0
                            && ComparePluginVersion(bkpPluginVer, desiredPluginVersion) <= 0)
                        {
                            match = (filepath, bkpConfigVer, bkpPluginVer);
                        }
                        if (match.CVer == desiredConfigVersion
                            && match.PVer == desiredPluginVersion)
                            return match.Path;  // exact match
                    }
                }
                catch (Exception e)
                {
                    LogError("Failed when finding matching config backup.\n" + e);
                }
                if (!string.IsNullOrEmpty(match.Path))
                    return match.Path;
                return null;
            }
        }

    }


}
