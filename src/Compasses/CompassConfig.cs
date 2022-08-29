namespace AetherCompass.Compasses
{
    [Serializable]
    public abstract class CompassConfig
    {
        public bool Enabled = false;
        public bool MarkScreen = true;
        public bool ShowDetail = true;
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

        public virtual void CheckValueValidity()
        {
            if (NotifySeId < 1) NotifySeId = 1;
            if (NotifySeId > 16) NotifySeId = 16;
        }
    }
}
