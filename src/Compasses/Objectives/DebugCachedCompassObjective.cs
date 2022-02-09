using AetherCompass.Common;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AetherCompass.Compasses.Objectives
{
    public unsafe class DebugCachedCompassObjective : CachedCompassObjective
    {
        public readonly ObjectKind ObjectKind;
        public readonly uint NpcId;
        public readonly float Distance2D;
        public readonly float RotationFromPlayer;

        public DebugCachedCompassObjective(GameObject* obj) : base(obj) 
        { 
            if (obj != null)
            {
                ObjectKind = (ObjectKind)obj->ObjectKind;
                NpcId = obj->GetNpcID();
                Distance2D = CompassUtil.Get2DDistanceFromPlayer(obj);
                RotationFromPlayer = CompassUtil.GetRotationFromPlayer(obj);
            }
        }
    }
}
