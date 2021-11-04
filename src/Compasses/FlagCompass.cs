using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;

namespace AetherCompass.Compasses
{
    // TODO: flag compass
    public sealed class FlagCompass : Compass
    {
        public override string Description => "Showing the map flag on screen";
        public override bool CompassEnabled { get => config.FlagEnabled; internal set => config.FlagEnabled = value; }
        public override bool DrawDetailsEnabled { get; private protected set; } = true;
        public override bool MarkScreenEnabled { get => config.FlagScreen; private protected set => config.FlagScreen = value; }


        public FlagCompass(Configuration config, IconManager iconManager) : base(config, iconManager) { }

        public override unsafe Action? CreateDrawDetailsAction(UI3DModule.ObjectInfo* info)
        {
            throw new NotImplementedException();
        }

        public override unsafe Action? CreateMarkScreenAction(UI3DModule.ObjectInfo* info)
        {
            throw new NotImplementedException();
        }

        public override unsafe bool IsObjective(GameObject* o)
            => false;

    }
}
