namespace AetherCompass.Compasses.Configs
{
    [Serializable]
    public class QuestCompassConfig : CompassConfig
    {
        public bool EnabledInSoloContents = true;
        //public bool DetectEnemy = true;
        public bool HideHidden = true;
        public bool ShowQuestName = true;
        public bool ShowAllRelated = true;
        public bool MarkerTextInOneLine = false;

        public override void Load(CompassConfig config)
        {
            base.Load(config);
            if (config is not QuestCompassConfig qc) return;
            EnabledInSoloContents = qc.EnabledInSoloContents;
            //DetectEnemy = qc.DetectEnemy;
            HideHidden = qc.HideHidden;
            ShowQuestName = qc.ShowQuestName;
            ShowAllRelated = qc.ShowAllRelated;
            MarkerTextInOneLine = qc.MarkerTextInOneLine;
        }
    }
}
