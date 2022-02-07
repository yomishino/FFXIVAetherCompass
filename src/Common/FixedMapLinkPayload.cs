using Dalamud.Game.Text.SeStringHandling.Payloads;
using System.Numerics;

namespace AetherCompass.Common
{
    // To fix the issue with Dalamud's MapLinkPayload not taking into account map's OffsetX and OffsetY
    // Only a quick fix so that the payload records our calculated coords instead of Dalamud's
    // which will be used in showing in Chat etc.
    public class FixedMapLinkPayload : MapLinkPayload
    {
        private readonly uint terrId;
        private readonly uint mapId;
        private readonly float coordX;
        private readonly float coordY;

        public FixedMapLinkPayload(uint terrId, uint mapId, int rawX, int rawY, float coordX, float coordY) : base(terrId, mapId, rawX, rawY)
        {
            this.terrId = terrId;
            this.mapId = mapId;
            this.coordX = coordX;
            this.coordY = coordY;
        }

        public static FixedMapLinkPayload FromMapCoord(uint terrId, uint mapId, float xCoord, float yCoord)
        {
            var map = Plugin.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Map>()?.GetRow(mapId);
            if (map == null) return new(terrId, mapId, 0, 0, 0, 0);
            return FromMapCoord(terrId, mapId, xCoord, yCoord, map.SizeFactor, map.OffsetX, map.OffsetY);
        }

        public static FixedMapLinkPayload FromMapCoord(uint terrId, uint mapId, float xCoord, float yCoord, ushort scale, short offsetX, short offsetY)
        {
            // because we don't care about Z-coord here
            var coord3 = new Vector3(xCoord, yCoord, 0);
            var pos = CompassUtil.GetWorldPosition(coord3, scale, offsetX, offsetY, 0);
            return new(terrId, mapId, (int)(pos.X * 1000), (int)(pos.Z * 1000), xCoord, yCoord);
        }

        public new string CoordinateString => $"( {coordX:0.0}  , {coordY:0.0} )";

        public override string ToString()
        {
            return $"{this.Type}(Fixed) - TerritoryTypeId: {terrId}, MapId: {mapId}, RawX: {RawX}, RawY: {RawY}, display: {PlaceName} {CoordinateString}";
        }
    }
}
