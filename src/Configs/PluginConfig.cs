using AetherCompass.Common;
using Dalamud.Configuration;
using Newtonsoft.Json;
using System;
using System.Numerics;

namespace AetherCompass.Configs
{
    [Serializable]
    public class PluginConfig : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool Enabled = false;
        public bool ShowScreenMark = false;
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
#if DEBUG
            DebugConfig.CheckValueValidity();
#endif
        }

        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }

        public void Load(PluginConfig config)
        {
            if (Version == config.Version)
            {
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

                AetherCurrentConfig.Load(config.AetherCurrentConfig);
                MobHuntConfig.Load(config.MobHuntConfig);
                GatheringConfig.Load(config.GatheringConfig);
                IslandConfig.Load(config.IslandConfig);
                QuestConfig.Load(config.QuestConfig);
            }
            // NOTE: config ver conversion if needed; and remind users
        }

        public static PluginConfig GetSavedPluginConfig()
            => Plugin.PluginInterface.GetPluginConfig() as PluginConfig ?? new();
    }
}
