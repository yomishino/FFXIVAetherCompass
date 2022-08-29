namespace AetherCompass.Compasses.Configs
{
    [Serializable]
    public class MobHuntCompassConfig : CompassConfig
    {
        public bool DetectS = true;
        public bool DetectA = true;
        public bool DetectB = true;
        public bool DetectSSMinion = true;

        public override void Load(CompassConfig config)
        {
            base.Load(config);
            if (config is not MobHuntCompassConfig mhc) return;
            DetectS = mhc.DetectS;
            DetectA = mhc.DetectA;
            DetectB = mhc.DetectB;
            DetectSSMinion = mhc.DetectSSMinion;
        }
    }
}
