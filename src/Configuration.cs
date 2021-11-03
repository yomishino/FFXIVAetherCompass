using Dalamud.Configuration;
using Newtonsoft.Json;
using System;

namespace AetherCompass
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool Enabled { get; set; } = true;
        public bool ShowDetailWindow { get; set; } = true;
        public bool HqIcon { get; set; } = true;

        public bool FlagEnabled { get; set; } = true;
        public bool FlagScreen { get; set; } = true;

        public bool AetherEnabled { get; set; } = true;
        public bool AetherScreen { get; set; } = true;
        public bool AetherDetails { get; set; } = true;

#if DEBUG
        [JsonIgnore]
        public bool DebugEnabled { get; set; } = true;
        [JsonIgnore]
        public bool DebugScreen { get; set; } = true;
        [JsonIgnore]
        public bool DebugDetails { get; set; } = true;
#endif

        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }
    }
}
