using AetherCompass.Common;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Numerics;
using static FFXIVClientStructs.FFXIV.Client.UI.UI3DModule;

namespace AetherCompass.Compasses.Objectives
{
    public unsafe class CachedCompassObjective
    {
        public readonly IntPtr GameObject;
        public readonly GameObjectID GameObjectId;
        public readonly string Name;
        public readonly uint NpcId;
        public readonly uint DataId;
        public readonly Vector3 Position;
        public readonly float Distance3D; 
        public readonly float AltitudeDiff;
        public readonly CompassDirection CompassDirectionFromPlayer;
        public readonly float GameObjectHeight;
        public readonly Vector3 CurrentMapCoord;
        public readonly Vector3 NormalisedNameplatePos;
        

        public CachedCompassObjective(GameObject* obj)
        {
            GameObject = (IntPtr)obj;
            if (obj != null)
            {
                GameObjectId = obj->GetObjectID();
                Name = CompassUtil.GetName(obj);
                NpcId = obj->GetNpcID();
                DataId = obj->DataID;
                Position = obj->Position;
                Distance3D = CompassUtil.Get3DDistanceFromPlayer(Position);
                AltitudeDiff = CompassUtil.GetAltitudeDiffFromPlayer(Position);
                CompassDirectionFromPlayer = CompassUtil.GetDirectionFromPlayer(Position);
                GameObjectHeight = obj->GetHeight();
                CurrentMapCoord = CompassUtil.GetMapCoordInCurrentMap(Position);
            }
            else
                Name = string.Empty;
        }

        public CachedCompassObjective(ObjectInfo* info)
            : this(info != null ? info->GameObject : null)
        {
            if (info != null)
            {
                NormalisedNameplatePos = info->NamePlatePos;
            }
        }

        public bool IsCacheFor(GameObject* obj) => IsCacheFor((IntPtr)obj);

        public bool IsCacheFor(IntPtr gameObjPtr)
            => GameObject == gameObjPtr;

        public bool IsEmpty() => GameObject == IntPtr.Zero;
    }
}
