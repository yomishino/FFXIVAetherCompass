using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Numerics;


namespace AetherCompass.Common
{
    public static class CompassUtil
    {
        public unsafe static string GetName(GameObject* o)
            => o == null ? string.Empty
            : MemoryHelper.ReadSeStringNullTerminated((IntPtr)o->Name).TextValue;

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

        public unsafe static float GetAltitudeDiffFromPlayer(GameObject* o)
        {
            if (o == null) return float.NaN;
            var player = Plugin.ClientState.LocalPlayer;
            if (player == null) return float.NaN;
            return o->Position.Y - player.Position.Y;
        }

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
            if (MathF.Abs(r) <= 3 * MathF.PI / 8) d |= CompassDirection.S;
            if (MathF.Abs(r) > 5 * MathF.PI / 8) d |= CompassDirection.N;
            if (MathF.PI / 8 < r && r <= 7 * MathF.PI / 8) d |= CompassDirection.E;
            if (-7 * MathF.PI / 8 < r && r <= -MathF.PI / 8) d |= CompassDirection.W;
            return d;
        }


        public static Vector3 GetMapCoord(Vector3 worldPos, float scale, float offsetX, float offsetY)
        {
            // Altitude is y in world position but z in map coord
            // Not entierly accurate tho
            float mx = WorldPositionToMapCoord(worldPos.X, scale, offsetX);
            float my = WorldPositionToMapCoord(worldPos.Z, scale, offsetY);
            // Altitude seems pos:coord=10:.1 for sizefactor=100 map, otherwise no idea;
            // scaling seems fine as known map with Z all have sizefactor=100;
            // TODO: but offset is different for different map and i can't find that in game rn
            float mz = worldPos.Y / (scale / 100f) / 100f;
            return new Vector3(mx, my, mz);
        }

        public static float WorldPositionToMapCoord(float v, float scale, float offset = 0)
            => 41f / (scale/100f) * ((MathF.Floor(v) + offset) * (scale / 100f) + 1024f) / 2048f + 1;

        public static Vector3 GetMapCoordInCurrentMap(Vector3 worldPos)
        {
            var map = GetCurrentMap();
            if (map == null) return new Vector3(float.NaN, float.NaN, float.NaN);
            return GetMapCoord(worldPos, map.SizeFactor, map.OffsetX, map.OffsetY);
        }

        // NOTE: TerritoryIntendedUse == 1 seems include iff maps with Z coord, but not sure 
        public static bool HasZCoord(uint terrId)
            //=> GetTerritoryType(terrId)?.TerritoryIntendedUse == 1;
            => false;   // TEMP: because we can't find offset of each map and so can't get a reasonably accurate Z-coord

        public static bool CurrentHasZCoord()
            => HasZCoord(Plugin.ClientState.TerritoryType);

        public static string GetMapCoordInCurrentMapFormattedString(Vector3 worldPos, bool showZ = true)
        {
            var coord = GetMapCoordInCurrentMap(worldPos);
            return $"X:{coord.X:0.0}, Y:{coord.Y:0.0}{(showZ && CurrentHasZCoord() ? $", Z:{coord.Z:0.0}" : string.Empty)}";
        }

        public static TerritoryType? GetTerritoryType(uint terrId)
            => Plugin.DataManager.GetExcelSheet<TerritoryType>()?.GetRow(terrId);

        public static TerritoryType? GetCurrentTerritoryType()
            => GetTerritoryType(Plugin.ClientState.TerritoryType);

        public static Map? GetCurrentMap()
            => Plugin.DataManager.GetExcelSheet<Map>()?.GetRow(GetCurrentTerritoryType()?.Map.Row ?? 0);

        public static string GetPlaceNameToString(uint placeNameRowId, string emptyPlaceName = "")
        {
            var name = Plugin.DataManager.GetExcelSheet<PlaceName>()?.GetRow(placeNameRowId)?.Name.ToString();
            if (string.IsNullOrEmpty(name)) return emptyPlaceName;
            return name;
        }


    }
}
