using Dalamud.Interface;
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

        public static Vector2 GetScreenCentre()
            => ImGuiHelpers.MainViewport.GetCenter();

        // NOTE: for some objs, ObjectProjectedScreenSpace is not stable, can fly around when kept at certain distance? (NamePlatePos is fine)
        public static Vector2 GetProjectedScreenPos(Vector3 projection)
        {
            var centre = ImGuiHelpers.MainViewport.GetCenter();
            var size = ImGuiHelpers.MainViewport.Size;
            return new Vector2(
                centre.X + projection.X * size.X / 2,
                centre.Y - projection.Y * size.Y / 2); // projection Y goes upwards, i.e. reversed of screen Y
        }

        public static bool IsScreenPosInsideMainViewport(Vector2 screenPos, Vector2 offset)
        {
            var pos = ImGuiHelpers.MainViewport.Pos;
            var size = ImGuiHelpers.MainViewport.Size;
            return screenPos.X >= pos.X && screenPos.X + offset.X < pos.X + size.X
                && screenPos.Y >= pos.Y && screenPos.Y + offset.Y < pos.Y + size.Y;
        }

        public static bool WorldToScreenPos(Vector3 worldPos, out Vector2 screenPos)
            => Plugin.GameGui.WorldToScreen(worldPos, out screenPos);

        public static Vector2 GetConstrainedScreenPos(Vector2 screenPosUL, Vector4 screenConstraint, Vector2 extraConstraint)
        {
            var constraintUL = ImGuiHelpers.MainViewport.Pos + extraConstraint;
            var constraintBR = ImGuiHelpers.MainViewport.Pos + ImGuiHelpers.MainViewport.Size - extraConstraint;
            var x = MathF.Max(constraintUL.X + screenConstraint.X, MathF.Min(constraintBR.X - screenConstraint.Z, screenPosUL.X));
            var y = MathF.Max(constraintUL.Y + screenConstraint.Z, MathF.Min(constraintBR.Y - screenConstraint.Y, screenPosUL.Y));
            return new Vector2(x, y);
        }

        public static float GetAngleOnScreen(Vector2 origin, Vector2 point)
            => MathF.Atan2(point.X - origin.X, point.Y - origin.Y);

        public static float GetAngleOnScreen(Vector2 point)
            => GetAngleOnScreen(GetScreenCentre(), point);

        public static (Vector2 P1, Vector2 P2, Vector2 P3, Vector2 P4) 
            GetRotatedPointsOnScreen(Vector2 screenPosUL, Vector2 size, float rotation)
        {
            // Seems p1~p4 is UL, DL, DR, DU of the image
            // but because our coord system has y going downwards
            // while image itself is upwards,
            // we need to swap the points here to make p1~p4 be DR, UR, UL, DL
            Vector2 p3 = screenPosUL;
            Vector2 p4 = new(screenPosUL.X + size.X, screenPosUL.Y);
            Vector2 p1 = screenPosUL + size;
            Vector2 p2 = new(screenPosUL.X, screenPosUL.Y + size.Y);

            Vector2 p0 = screenPosUL + size / 2;

            // Rotate
            p1 = RotatePointOnPlnae(p1 - p0, rotation) + p0;
            p2 = RotatePointOnPlnae(p2 - p0, rotation) + p0;
            p3 = RotatePointOnPlnae(p3 - p0, rotation) + p0;
            p4 = RotatePointOnPlnae(p4 - p0, rotation) + p0;

            return (p1, p2, p3, p4);
        }

        private static Vector2 RotatePointOnPlnae(Vector2 p, float rotation)
        {
            var a = MathF.Atan2(p.X, p.Y);
            var d = MathF.Sqrt(p.X * p.X + p.Y * p.Y);
            return new Vector2(d * MathF.Sin(a + rotation), d * MathF.Cos(a + rotation));
        }

        public static Direction GetDirectionOnScreen(Vector2 origin, Vector2 point)
        {
            var theta = GetAngleOnScreen(origin, point);
            if (float.IsNaN(theta)) return Direction.O;
            Direction d = 0;
            if (MathF.Abs(theta) <= 3 * MathF.PI / 8) d |= Direction.Down;
            if (MathF.Abs(theta) > 5 * MathF.PI / 8) d |= Direction.Up;
            if (MathF.PI / 8 < theta && theta <= 7 * MathF.PI / 8) d |= Direction.Right;
            if (-7 * MathF.PI / 8 < theta && theta <= -MathF.PI / 8) d |= Direction.Left;
            return d;
        }

        public static Direction GetDirectionOnScreen(Vector2 point)
            => GetDirectionOnScreen(GetScreenCentre(), point);
    }
}
