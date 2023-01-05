namespace AetherCompass.Compasses.Configs
{
    [Serializable]
    public class IslandSanctuaryCompassConfig : CompassConfig
    {
        public bool DetectGathering = true;
        public bool DetectAnimals = true;
        public bool ShowNameOnMarkerGathering = true;
        public bool ShowNameOnMarkerAnimals = true;
        public bool HideMarkerWhenNotInScreenGathering = false;
        public bool HideMarkerWhenNotInScreenAnimals = false;
        public bool UseAnimalSpecificIcons = false;
        public uint GatheringObjectsToShow = uint.MaxValue;
        public uint AnimalsToShow = uint.MaxValue;

        public override void Load(CompassConfig config)
        {
            base.Load(config);
            if (config is not IslandSanctuaryCompassConfig isc) return;
            DetectGathering = isc.DetectGathering;
            DetectAnimals = isc.DetectAnimals;
            ShowNameOnMarkerGathering = isc.ShowNameOnMarkerGathering;
            ShowNameOnMarkerAnimals = isc.ShowNameOnMarkerAnimals;
            HideMarkerWhenNotInScreenGathering = isc.HideMarkerWhenNotInScreenGathering;
            HideMarkerWhenNotInScreenAnimals = isc.HideMarkerWhenNotInScreenAnimals;
            UseAnimalSpecificIcons = isc.UseAnimalSpecificIcons;
            GatheringObjectsToShow = isc.GatheringObjectsToShow;
            AnimalsToShow = isc.AnimalsToShow;
        }
    }
}
