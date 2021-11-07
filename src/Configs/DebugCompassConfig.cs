using System;

namespace AetherCompass.Configs
{
    [Serializable]
    public class DebugCompassConfig : ICompassConfig
    {
        public bool Enabled { get; set; } = true;
        public bool MarkScreen { get; set; } = true;
        public bool DetailWindow { get; set; } = false;
        public bool Notify { get; set; } = true;
        public bool NotifySe { get; set; } = true;
        public int NotifySeId { get; set; } = 1;
    }
}
