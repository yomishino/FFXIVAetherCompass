using AetherCompass.UI;
using AetherCompass.Configs;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;

namespace AetherCompass.Compasses
{
    // TODO: flag compass
    public sealed class FlagCompass : Compass
    {
        public override string Description => "Showing the map flag on screen";
        public override bool CompassEnabled { get => compassConfig.Enabled; internal set => compassConfig.Enabled = value; }
        public override bool DrawDetailsEnabled { get; private protected set; } = true;
        public override bool MarkScreenEnabled { get => compassConfig.MarkScreen; private protected set => compassConfig.MarkScreen = value; }

        private protected override string ClosestObjectDescription => "Flagged Position";


        public FlagCompass(PluginConfig config, ICompassConfig compassConfig, IconManager iconManager) : 
            base(config, compassConfig, iconManager) { }

        public override unsafe Action? CreateDrawDetailsAction(GameObject* obj)
        {
            throw new NotImplementedException();
        }

        public override unsafe Action? CreateMarkScreenAction(GameObject* obj)
        {
            throw new NotImplementedException();
        }

        private protected override unsafe bool IsObjective(GameObject* o)
            => false;

    }
}
