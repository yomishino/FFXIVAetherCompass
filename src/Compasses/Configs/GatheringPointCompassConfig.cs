namespace AetherCompass.Compasses.Configs
{
    [Serializable]
    public class GatheringPointCompassConfig : CompassConfig
    {
        public bool ShowExported = true;

        public override void Load(CompassConfig config)
        {
            base.Load(config);
            if (config is GatheringPointCompassConfig gpc)
                ShowExported = gpc.ShowExported;
        }
    }
}
