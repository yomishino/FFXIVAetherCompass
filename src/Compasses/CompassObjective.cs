using AetherCompass.Common;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Numerics;

namespace AetherCompass.Compasses
{
    public unsafe class CompassObjective
    {
        public readonly GameObject* GameObject;
        public readonly GameObjectID GameObjectId;
        public readonly ObjectKind ObjectKind;
        public readonly string Name;
        public readonly uint DataId;
        public readonly Vector3 Position;
        public readonly float Distance3D;
        public readonly float AltitudeDiff;
        public readonly CompassDirection CompassDirectionFromPlayer;
        public readonly float GameObjectHeight;
        public readonly Vector3 CurrentMapCoord;


        public CompassObjective(GameObject* obj)
        {
            GameObject = obj;
            if (obj != null)
            {
                GameObjectId = obj->GetObjectID();
                ObjectKind = (ObjectKind)obj->ObjectKind;
                Name = CompassUtil.GetName(obj);
                DataId = obj->DataID;
                Position = obj->Position;
                Distance3D = CompassUtil.Get3DDistanceFromPlayer(obj);
                AltitudeDiff = CompassUtil.GetAltitudeDiffFromPlayer(obj);
                CompassDirectionFromPlayer = CompassUtil.GetDirectionFromPlayer(obj);
                GameObjectHeight = obj->GetHeight();
                CurrentMapCoord = CompassUtil.GetMapCoordInCurrentMap(obj->Position);
            }
            else
            {
                Name = string.Empty;
            }
        }

    }
}
