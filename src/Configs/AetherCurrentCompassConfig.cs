using System;

namespace AetherCompass.Configs
{
    [Serializable]
    public class AetherCurrentCompassConfig : ICompassConfig
    {
        public bool Enabled { get; set; } = false;
        public bool MarkScreen { get; set; } = false;
        public bool DetailWindow { get; set; } = false;
        public bool NotifyChat { get; set; } = false;
        public bool NotifySe { get; set; } = false;
        public int NotifySeId { get; set; } = 1;
        public bool NotifyToast { get; set; } = false;

        public bool ShowAetherite { get; set; } = true;
    }
}
