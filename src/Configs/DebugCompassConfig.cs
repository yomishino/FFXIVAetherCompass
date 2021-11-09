using System;

namespace AetherCompass.Configs
{
    [Serializable]
    public class DebugCompassConfig : ICompassConfig
    {
        public bool Enabled { get; set; } = true;
        public bool MarkScreen { get; set; } = true;
        public bool DetailWindow { get; set; } = false;
        public bool NotifyChat { get; set; } = true;
        public bool NotifySe { get; set; } = true;
        public int NotifySeId { get; set; } = 4;
        public bool NotifyToast { get; set; } = true;
    }
}
