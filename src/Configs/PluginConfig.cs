using AetherCompass.Configs;
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

        public bool Enabled { get; set; } = true;
        public bool ShowDetailWindow { get; set; } = true;
        public bool HqIcon { get; set; } = true;
        public float ScreenMarkFontSize { get; set; } = 17;
        // L,D,R,U; how much to squeeze into centre on each side, so generally should be positive
        public Vector4 ScreenMarkConstraint { get; set; } = new(80, 80, 80, 80);

        public bool FlagEnabled { get; set; } = true;
        public bool FlagScreen { get; set; } = true;

        //public bool AetherEnabled { get; set; } = true;
        //public bool AetherScreen { get; set; } = true;
        //public bool AetherDetails { get; set; } = true;
        //public bool AetherShowAetherite { get; set; } = true;
        public AetherCurrentCompassConfig AetherCurrentConfig { get; set; } = new();

#if DEBUG
        //[JsonIgnore]
        //public bool DebugEnabled { get; set; } = true;
        //[JsonIgnore]
        //public bool DebugScreen { get; set; } = false;
        //[JsonIgnore]
        //public bool DebugDetails { get; set; } = true;
        [JsonIgnore]
        public bool DebugUseFullArray { get; set; } = false;    // TEMP: for debug
        [JsonIgnore]
        public DebugCompassConfig DebugConfig { get; set; } = new();
#endif

        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }
    }
}
