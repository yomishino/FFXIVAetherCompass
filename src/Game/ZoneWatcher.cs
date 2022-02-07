using AetherCompass.Game.SeFunctions;
using Excel = Lumina.Excel.GeneratedSheets;

namespace AetherCompass.Game
{
    public static class ZoneWatcher
    {
        public static Excel.TerritoryType? TerritoryType { get; private set; }
        public static Excel.TerritoryTypeTransient? TerritoryTypeTransient { get; private set; }
       
        public static uint MapId
        {
            get
            {
                var altMapId = ZoneMap.GetCurrentTerritoryAltMapId();
                if (altMapId > 0) return altMapId;
                return TerritoryType?.Map.Row ?? 0;
            }
        }
        private static Excel.Map? cachedMap;
        public static Excel.Map? Map
        {
            get
            {
                if (cachedMap == null || MapId != cachedMap.RowId) cachedMap = GetMap();
                return cachedMap;
            }
        }

        public static bool IsInCompassWorkZone { get; private set; }
        public static bool IsInDetailWindowHideZone { get; private set; }

        public static void OnZoneChange()
        {
            var terrId = Plugin.ClientState.TerritoryType;
            TerritoryType = terrId == 0 ? null
                : Plugin.DataManager.GetExcelSheet<Excel.TerritoryType>()?.GetRow(terrId);
            TerritoryTypeTransient = terrId == 0 ? null
                : Plugin.DataManager.GetExcelSheet<Excel.TerritoryTypeTransient>()?.GetRow(terrId);

            cachedMap = GetMap();

            CheckCompassWorkZone();
            CheckDetailWindowHideZone();
        }

        private static Excel.Map? GetMap()
            => Plugin.DataManager.GetExcelSheet<Excel.Map>()?.GetRow(MapId);


        // Work only in PvE zone, also excl LoVM / chocobo race etc.
        private static void CheckCompassWorkZone()
        {
            IsInCompassWorkZone = TerritoryType != null
                && !TerritoryType.IsPvpZone
                && TerritoryType.BattalionMode <= 1   // > 1 are pvp contents or LoVM
                && TerritoryType.TerritoryIntendedUse != 20  // chocobo race terr?
                ;
        }

        private static void CheckDetailWindowHideZone()
        {
            // Exclusive type: 0 not instanced, 1 is solo instance, 2 is nonsolo instance.
            // Not sure about 3, seems quite mixed up with solo battles, diadem and misc stuff like LoVM
            IsInDetailWindowHideZone = TerritoryType == null || TerritoryType.ExclusiveType > 0;
        }

    }
}
