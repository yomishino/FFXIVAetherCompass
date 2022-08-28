using FFXIVClientStructs.FFXIV.Client.Game.Object;
using static FFXIVClientStructs.FFXIV.Client.UI.UI3DModule;

namespace AetherCompass.Compasses.Objectives
{
    public unsafe class IslandCachedCompassObjective : CachedCompassObjective
    {
        public IslandObjectType Type { get; private set; }

        public IslandCachedCompassObjective(GameObject* obj, IslandObjectType type)
            : base(obj)
        {
            Init(type);
        }

        public IslandCachedCompassObjective(ObjectInfo* info, IslandObjectType type)
            : base(info)
        {
            Init(type);
        }

        private void Init(IslandObjectType type)
        {
            Type = type;
        }
    }
}
