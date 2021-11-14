using System;

namespace AetherCompass.Configs
{
    public class DebugCompassConfig : CompassConfig
    {
        public DebugCompassConfig()
        {
            Enabled = true;
            ShowDetail = true;
            NotifySeId = 4;
        }
    }
}
