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
        public bool HqIcon = true;
        public int ScreenMarkFontSize = 17;
        // L,D,R,U; how much to squeeze into centre on each side, so generally should be positive
        public Vector4 ScreenMarkConstraint = new(80, 80, 80, 80);
        public bool ShowDetailWindow = false;
        public bool HideDetailInContents = false;
        public bool NotifyChat = false;
        public bool NotifySe = false;
        public bool NotifyToast = false;

        public AetherCurrentCompassConfig AetherCurrentConfig { get; private set; } = new();

#if DEBUG
        [JsonIgnore]
        public bool DebugUseFullArray = false;    // TEMP: for debug
        [JsonIgnore]
        public DebugCompassConfig DebugConfig { get; private set; } = new();
#endif

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
                HqIcon = config.HqIcon;
                ScreenMarkFontSize = config.ScreenMarkFontSize;
                ScreenMarkConstraint = config.ScreenMarkConstraint;
                ShowDetailWindow = config.ShowDetailWindow;
                HideDetailInContents = config.HideDetailInContents;
                NotifyChat = config.NotifyChat;
                NotifySe = config.NotifySe;
                NotifyToast = config.NotifyToast;

                AetherCurrentConfig.Load(config.AetherCurrentConfig);
            }
            // TODO: config version
        }

        public static PluginConfig GetSavedPluginConfig()
            => Plugin.PluginInterface.GetPluginConfig() as PluginConfig ?? new();
    }
}
