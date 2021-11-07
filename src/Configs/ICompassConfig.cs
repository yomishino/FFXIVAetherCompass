namespace AetherCompass.Configs
{
    public interface ICompassConfig
    {
        public bool Enabled { get; set; }
        public bool MarkScreen { get; set; }
        public bool DetailWindow { get; set; }
        public bool Notify { get; set; }
        public bool NotifySe { get; set; }
        public int NotifySeId { get; set; }
    }
}
