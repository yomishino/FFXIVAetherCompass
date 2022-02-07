using AetherCompass.Common;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Numerics;

namespace AetherCompass.Compasses
{
    public unsafe class CachedCompassObjective
    {
        public readonly GameObject* GameObject;
        public readonly GameObjectID GameObjectId;
        public readonly string Name;
        public readonly uint DataId;
        public readonly Vector3 Position;
        public readonly float Distance3D; 
        public readonly float AltitudeDiff;
        public readonly CompassDirection CompassDirectionFromPlayer;
        public readonly float GameObjectHeight;
        public readonly Vector3 CurrentMapCoord;


        public CachedCompassObjective(GameObject* obj)
        {
            GameObject = obj;
            if (GameObject != null)
            {
                GameObjectId = GameObject->GetObjectID();
                Name = CompassUtil.GetName(GameObject);
                DataId = GameObject->DataID;
                Position = GameObject->Position;
                Distance3D = CompassUtil.Get3DDistanceFromPlayer(GameObject);
                AltitudeDiff = CompassUtil.GetAltitudeDiffFromPlayer(GameObject);
                CompassDirectionFromPlayer = CompassUtil.GetDirectionFromPlayer(Position);
                GameObjectHeight = GameObject->GetHeight();
                CurrentMapCoord = CompassUtil.GetMapCoordInCurrentMap(Position);
            }
            else
                Name = string.Empty;
        }
    }
}
