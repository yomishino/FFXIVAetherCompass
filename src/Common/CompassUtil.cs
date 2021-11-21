using AetherCompass.Common.SeFunctions;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Numerics;
using System.Runtime.InteropServices;


namespace AetherCompass.Common
{
    public static class CompassUtil
    {
        public unsafe static string GetName(GameObject* o)
            => o == null ? string.Empty
            : MemoryHelper.ReadSeStringNullTerminated((IntPtr)o->Name).TextValue;

        public unsafe static byte GetCharacterLevel(GameObject* o)
            => o != null && o->IsCharacter() ? ((Character*)o)->Level : byte.MinValue;

        // Character struct offset +0x197C byte flag: 0x2 is Dead
        // Better than checking hp; hp>0 seems still true when bnpc dead but not removed 
        public unsafe static bool IsCharacterAlive(GameObject* o)
            => o != null && o->IsCharacter() && (Marshal.ReadByte((IntPtr)o + 0x197C) & 2) == 0;

        public unsafe static float Get3DDistance(GameObject* o1, GameObject* o2)
        {
            if (o1 == null || o2 == null) return float.NaN;
            return MathF.Sqrt(MathF.Pow(o1->Position.X - o2->Position.X, 2)
                + MathF.Pow(o1->Position.Y - o2->Position.Y, 2)
                + MathF.Pow(o1->Position.Z - o2->Position.Z, 2));
        }

        public unsafe static float Get3DDistanceFromPlayer(GameObject* o)
        {
            if (o == null) return float.NaN;
            var player = Plugin.ClientState.LocalPlayer;
            if (player == null) return float.NaN;
            return MathF.Sqrt(MathF.Pow(o->Position.X - player.Position.X, 2)
                + MathF.Pow(o->Position.Y - player.Position.Y, 2)
                + MathF.Pow(o->Position.Z - player.Position.Z, 2));
        }

        public unsafe static float Get2DDistanceFromPlayer(GameObject* o)
        {
            if (o == null) return float.NaN;
            var player = Plugin.ClientState.LocalPlayer;
            if (player == null) return float.NaN;
            return MathF.Sqrt(MathF.Pow(o->Position.X - player.Position.X, 2)
                + MathF.Pow(o->Position.Z - player.Position.Z, 2));
        }

        public unsafe static float GetYDistanceFromPlayer(GameObject* o)
        {
            if (o == null) return float.NaN;
            var player = Plugin.ClientState.LocalPlayer;
            if (player == null) return float.NaN;
            return MathF.Abs(o->Position.Y - player.Position.Y);
        }

        public static string DistanceToDescriptiveString(float dist, bool integer)
            => (integer ? $"{dist:0}" : $"{dist:0.0}")
            + (Plugin.ClientState.ClientLanguage == Dalamud.ClientLanguage.Japanese
                ? "m" : "y");

        public unsafe static string Get3DDistanceFromPlayerDescriptive(GameObject* o, bool integer)
            => DistanceToDescriptiveString(Get3DDistanceFromPlayer(o), integer);

        public unsafe static float GetAltitudeDiffFromPlayer(GameObject* o)
        {
            if (o == null) return float.NaN;
            var player = Plugin.ClientState.LocalPlayer;
            if (player == null) return float.NaN;
            return o->Position.Y - player.Position.Y;
        }

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

        public unsafe static float GetRotationFromPlayer(GameObject* o)
        {
            if (o == null) return float.NaN;
            var player = Plugin.ClientState.LocalPlayer;
            if (player == null) return float.NaN;
            return MathF.Atan2(o->Position.X - player.Position.X, o->Position.Z - player.Position.Z);
        }

        public unsafe static CompassDirection GetDirectionFromPlayer(GameObject* o)
        {
            var r = GetRotationFromPlayer(o);
            if (float.IsNaN(r)) return CompassDirection.NaN;
            CompassDirection d = 0;
            if (MathF.Abs(r) <= 3 * MathF.PI / 8) d |= CompassDirection.South;
            if (MathF.Abs(r) > 5 * MathF.PI / 8) d |= CompassDirection.North;
            if (MathF.PI / 8 < r && r <= 7 * MathF.PI / 8) d |= CompassDirection.East;
            if (-7 * MathF.PI / 8 < r && r <= -MathF.PI / 8) d |= CompassDirection.West;
            return d;
        }


        public static TerritoryType? GetTerritoryType(uint terrId)
            => Plugin.DataManager.GetExcelSheet<TerritoryType>()?.GetRow(terrId);

        public static TerritoryType? GetCurrentTerritoryType()
            => GetTerritoryType(Plugin.ClientState.TerritoryType);

        public static short GetTerritoryZOffset(uint terrId)
            => Plugin.DataManager.GetExcelSheet<TerritoryTypeTransient>()?.GetRow(terrId)?.OffsetZ ?? 0;

        public static short GetCurrentTerritoryZOffset()
            => GetTerritoryZOffset(Plugin.ClientState.TerritoryType);

        public static uint GetCurrentMapId()
        {
            var altMapId = ZoneMap.GetCurrentTerritoryAltMapId();
            if (altMapId > 0) return altMapId;
            return GetCurrentTerritoryType()?.Map.Row ?? 0;
        }

        public static Map? GetCurrentMap()
            => Plugin.DataManager.GetExcelSheet<Map>()?.GetRow(GetCurrentMapId());

        public static string GetPlaceNameToString(uint placeNameRowId, string emptyPlaceName = "")
        {
            var name = Plugin.DataManager.GetExcelSheet<PlaceName>()?.GetRow(placeNameRowId)?.Name.ToString();
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
            mx = TruncateToOneDecimalPlace(mx);
            my = TruncateToOneDecimalPlace(my);
            mz = TruncateToOneDecimalPlace(mz);
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

        private static float TruncateToOneDecimalPlace(float v)
            => MathF.Truncate(v * 10) / 10f;

        public static Vector3 GetMapCoordInCurrentMap(Vector3 worldPos)
        {
            var map = GetCurrentMap();
            if (map == null) return new Vector3(float.NaN, float.NaN, float.NaN);
            return GetMapCoord(worldPos, map.SizeFactor, map.OffsetX, map.OffsetY, GetCurrentTerritoryZOffset());
        }

        // Among valid maps, all that officially has no Z coord has Z-offset of -10000
        public static bool HasZCoord(uint terrId)
            => GetTerritoryZOffset(terrId) > -10000;

        public static bool CurrentHasZCoord()
            => HasZCoord(Plugin.ClientState.TerritoryType);

        public static string MapCoordToFormattedString(Vector3 coord, bool showZ = true)
            => $"X:{coord.X:0.0}, Y:{coord.Y:0.0}{(showZ && CurrentHasZCoord() ? $", Z:{coord.Z:0.0}" : string.Empty)}";
            //=> $"X:{coord.X}, Y:{coord.Y}{(showZ && CurrentHasZCoord() ? $", Z:{coord.Z}" : string.Empty)}";

        public static string GetMapCoordInCurrentMapFormattedString(Vector3 worldPos, bool showZ = true)
            => MapCoordToFormattedString(GetMapCoordInCurrentMap(worldPos), showZ);
    }
}
