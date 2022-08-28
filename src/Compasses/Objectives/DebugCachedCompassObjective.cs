using AetherCompass.Common;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ObjectInfo = FFXIVClientStructs.FFXIV.Client.UI.UI3DModule.ObjectInfo;

namespace AetherCompass.Compasses.Objectives
{
    public unsafe class DebugCachedCompassObjective : CachedCompassObjective
    {
        public ObjectKind ObjectKind { get; private set; }
        public float Distance2D { get; private set; }
        public float RotationFromPlayer { get; private set; }

        
        public DebugCachedCompassObjective(GameObject* obj) : base(obj) 
        {
            Init(obj);
        }

        public DebugCachedCompassObjective(ObjectInfo* info) : base(info) 
        {
            var obj = info != null ? info->GameObject : null;
            Init(obj);
        }

        private void Init(GameObject* obj)
        {
            if (obj != null)
            {
                ObjectKind = (ObjectKind)obj->ObjectKind;
                Distance2D = CompassUtil.Get2DDistanceFromPlayer(obj);
                RotationFromPlayer = CompassUtil.GetRotationFromPlayer(obj);
            }
        }
    }
}
