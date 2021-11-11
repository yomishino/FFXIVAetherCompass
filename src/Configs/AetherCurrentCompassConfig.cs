using System;

namespace AetherCompass.Configs
{
    [Serializable]
    public class AetherCurrentCompassConfig : CompassConfig
    {
        public bool ShowAetherite = true;

        public override void Load(CompassConfig config)
        {
            base.Load(config);
            if (config is AetherCurrentCompassConfig aetherCurrentConfig)
                ShowAetherite = aetherCurrentConfig.ShowAetherite;
        }
    }
}
