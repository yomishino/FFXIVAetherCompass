using AetherCompass.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Numerics;
using System.Runtime.InteropServices;


namespace AetherCompass.Common
{
    public static class CompassUtil
    {
        // Too slow to read the name as SeString
        // Just read it as plain UTF8 string for now, unless necessary
        public unsafe static string GetName(GameObject* o)
            => o == null ? string.Empty
            //: MemoryHelper.ReadSeString((IntPtr)o->Name, 64).TextValue;  
            : Marshal.PtrToStringUTF8((IntPtr)o->Name) ?? string.Empty;

        public static string ToTitleCase(string s)
            => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s);

        public unsafe static byte GetCharacterLevel(GameObject* o)
            => o != null && o->IsCharacter() ? ((Character*)o)->Level : byte.MinValue;

        // Character struct offset +0x197C byte flag: 0x2 is Dead
        // Better than checking hp; hp>0 seems still true when bnpc dead but not removed 
        public unsafe static bool IsCharacterAlive(GameObject* o)
            => o != null && o->IsCharacter() && (Marshal.ReadByte((IntPtr)o + 0x197C) & 2) == 0;

        public unsafe static bool IsHostileCharacter(GameObject* o)
            => o != null && o->IsCharacter() && (((Character*)o)->StatusFlags & 1) != 0;


        public unsafe static float Get3DDistanceFromPlayer(GameObject* o)
            => o == null ? float.NaN : Get3DDistanceFromPlayer(o->Position);

        public unsafe static float Get3DDistanceFromPlayer(Vector3 gameObjPos)
            => Plugin.ClientState.LocalPlayer == null 
            ? float.NaN : Vector3.Distance(gameObjPos, Plugin.ClientState.LocalPlayer.Position);

        public unsafe static float Get2DDistanceFromPlayer(GameObject* o)
        {
            if (o == null) return float.NaN;
            var player = Plugin.ClientState.LocalPlayer;
            if (player == null) return float.NaN;
            return MathF.Sqrt(MathF.Pow(o->Position.X - player.Position.X, 2)
                + MathF.Pow(o->Position.Z - player.Position.Z, 2));
        }

        public static string DistanceToDescriptiveString(float dist, bool integer)
            => (integer ? $"{dist:0}" : $"{dist:0.0}") + Language.Unit.Yalm;

        public static string Get3DDistanceFromPlayerDescriptive(Vector3 gameObjPos, bool integer)
            => DistanceToDescriptiveString(Get3DDistanceFromPlayer(gameObjPos), integer);

        public unsafe static string Get3DDistanceFromPlayerDescriptive(GameObject* o, bool integer)
            => DistanceToDescriptiveString(Get3DDistanceFromPlayer(o), integer);

        public unsafe static float GetAltitudeDiffFromPlayer(GameObject* o)
            => o == null ? float.NaN : GetAltitudeDiffFromPlayer(o->Position);

        public static float GetAltitudeDiffFromPlayer(Vector3 gameObjPos)
            => Plugin.ClientState.LocalPlayer == null ? float.NaN : (gameObjPos.Y - Plugin.ClientState.LocalPlayer.Position.Y);

        public static string AltitudeDiffToDescriptiveString(float diff)
        {
            var diffAbs = MathF.Abs(diff);
            if (diffAbs < 1) return "At same altitude";
            string s = DistanceToDescriptiveString(diffAbs, true);
            if (diff > 0) return s + " higher than you";
            else return s + " lower than you";
        }

        public unsafe static string GetAltitudeDiffFromPlayerDescriptive(GameObject* o)
            => AltitudeDiffToDescriptiveString(GetAltitudeDiffFromPlayer(o));

        public static string GetAltitudeDiffFromPlayerDescriptive(Vector3 gameObjPos)
            => AltitudeDiffToDescriptiveString(GetAltitudeDiffFromPlayer(gameObjPos));

        public unsafe static float GetRotationFromPlayer(GameObject* o)
            => o == null ? float.NaN : GetRotationFromPlayer(o->Position);

        public static float GetRotationFromPlayer(Vector3 gameObjPos)
        {
            var player = Plugin.ClientState.LocalPlayer;
            if (player == null) return float.NaN;
            return MathF.Atan2(gameObjPos.X - player.Position.X, gameObjPos.Z - player.Position.Z);
        }

        private static readonly float directionSpan = MathF.Sin(3 * MathF.PI / 8);

        public unsafe static CompassDirection GetDirectionFromPlayer(GameObject* o)
            => o == null ? CompassDirection.NaN : GetDirectionFromPlayer(o->Position);

        public static CompassDirection GetDirectionFromPlayer(Vector3 gameObjPos)
        {
            var player = Plugin.ClientState.LocalPlayer;
            if (player == null) return CompassDirection.NaN;
            var vec = Vector2.Normalize(new Vector2(gameObjPos.X - player.Position.X, gameObjPos.Z - player.Position.Z));
            CompassDirection d = 0;
            if (MathF.Abs(vec.X) < directionSpan)
                d |= vec.Y > 0 ? CompassDirection.South : CompassDirection.North;
            if (MathF.Abs(vec.Y) < directionSpan)
                d |= vec.X > 0 ? CompassDirection.East : CompassDirection.West;
            return d;
        }


        public static short GetCurrentTerritoryZOffset()
            => ZoneWatcher.CurrentTerritoryTypeTransient?.OffsetZ ?? 0;

        public static string GetPlaceNameToString(uint placeNameRowId, string emptyPlaceName = "")
        {
            var name = ZoneWatcher.PlaceName?.GetRow(placeNameRowId)?.Name.ToString();
            if (string.IsNullOrEmpty(name)) return emptyPlaceName;
            return name;
        }

        public static Vector3 GetMapCoord(Vector3 worldPos, ushort scale, 
            short offsetXCoord, short offsetYCoord, short offsetZCoord)
        {
            // Altitude is y in world position but z in map coord
            float mx = WorldPositionToMapCoord(worldPos.X, scale, offsetXCoord);
            float my = WorldPositionToMapCoord(worldPos.Z, scale, offsetYCoord);
            float mz = WorldPositionToMapCoordZ(worldPos.Y, offsetZCoord);
            // Also truncate coords to one decimal place seems give closer results
            mx = MathUtil.TruncateToOneDecimalPlace(mx);
            my = MathUtil.TruncateToOneDecimalPlace(my);
            mz = MathUtil.TruncateToOneDecimalPlace(mz);
            return new Vector3(mx, my, mz);
        }

        // "-1" before divided by 2048 seems a more accurate result?
        private static float WorldPositionToMapCoord(float v, ushort scale, short offset)
            => 41f * ((MathF.Truncate(v) + offset) * (scale / 100f) + 1024f - 1) / 2048f / (scale / 100f) + 1;

        // Altitude seems pos:coord=10:.1 and ignoring map's sizefactor.
        // Z-coord offset seems coming from TerritoryTypeTransient sheet,
        // and *subtract* it from worldPos.Y
        private static float WorldPositionToMapCoordZ(float worldY, short offset = 0)
        => (worldY - offset) / 100f;

        public static Vector3 GetMapCoordInCurrentMap(Vector3 worldPos)
        {
            var map = ZoneWatcher.CurrentMap;
            if (map == null) return new Vector3(float.NaN, float.NaN, float.NaN);
            return GetMapCoord(worldPos, map.SizeFactor, map.OffsetX, map.OffsetY, GetCurrentTerritoryZOffset());
        }

        // Among valid maps, all that officially has no Z coord has Z-offset of -10000
        public static bool CurrentHasZCoord()
            => GetCurrentTerritoryZOffset() > -10000;

        public static string MapCoordToFormattedString(Vector3 coord, bool showZ = true)
            => $"X:{coord.X:0.0}, Y:{coord.Y:0.0}{(showZ && CurrentHasZCoord() ? $", Z:{coord.Z:0.0}" : string.Empty)}";
            //=> $"X:{coord.X}, Y:{coord.Y}{(showZ && CurrentHasZCoord() ? $", Z:{coord.Z}" : string.Empty)}";

        public static string GetMapCoordInCurrentMapFormattedString(Vector3 worldPos, bool showZ = true)
            => MapCoordToFormattedString(GetMapCoordInCurrentMap(worldPos), showZ);

        public static Vector3 GetWorldPosition(Vector3 mapCoord, ushort scale,
            short offsetXCoord, short offsetYCoord, short offsetZCoord)
            => new(MapCoordToWorldPosition(mapCoord.X, scale, offsetXCoord),
                    MapCoordToWorldPositionY(mapCoord.Z, offsetZCoord),
                    MapCoordToWorldPosition(mapCoord.Y, scale, offsetYCoord));

        private static float MapCoordToWorldPosition(float v, ushort scale, short offset)
            => ((v - 1) * (scale / 100f) * 2048f / 41f + 1 - 1024f) / (scale / 100f) - offset;
        
        private static float MapCoordToWorldPositionY(float coordZ, short offset = 0)
        => coordZ * 100f + offset;

        public static Vector3 GetWorldPositionFromMapCoordInCurrentMap(Vector3 mapCoord)
        {
            var map = ZoneWatcher.CurrentMap;
            if (map == null) return new Vector3(float.NaN, float.NaN, float.NaN);
            return GetWorldPosition(mapCoord, map.SizeFactor, map.OffsetX, map.OffsetY, GetCurrentTerritoryZOffset());
        }
    }
}
