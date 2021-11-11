using System;

namespace AetherCompass.Configs
{
    [Serializable]
    public abstract class CompassConfig
    {
        public bool Enabled = false;
        public bool MarkScreen = true;
        public bool ShowDetail = false;
        public bool NotifyChat = false;
        public bool NotifySe = false;
        public int NotifySeId = 1;
        public bool NotifyToast = false;

        public virtual void Load(CompassConfig config)
        {
            Enabled = config.Enabled;
            MarkScreen = config.MarkScreen;
            ShowDetail = config.ShowDetail;
            NotifyChat = config.NotifyChat;
            NotifySe = config.NotifySe;
            NotifySeId = config.NotifySeId;
            NotifyToast = config.NotifyToast;
        }
    }
}
