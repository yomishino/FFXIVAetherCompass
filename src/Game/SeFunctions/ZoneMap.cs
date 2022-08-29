using System.Runtime.InteropServices;

namespace AetherCompass.Game.SeFunctions
{
    public static class ZoneMap
    {
        public static readonly IntPtr ZoneMapInfoPtr;

        // Some territories' use maps that are not "00" named, such as "s1t1/01",
        // and many but not all of them are multi-layered maps such as housing maps.
        // For those, this returns the real map id.
        // But will return 0 for those "00" named maps.
        public static uint GetCurrentTerritoryAltMapId() => ZoneMapInfoPtr == IntPtr.Zero ? 0 : (uint)Marshal.ReadInt32(ZoneMapInfoPtr, 0);

        // Returns 0 when not in any named area
        public static uint GetCurrentAreaPlaceNameId() => ZoneMapInfoPtr == IntPtr.Zero ? 0 : (uint)Marshal.ReadInt32(ZoneMapInfoPtr, 0xC);

        // Returns 0 when not in range of any named landmark
        public static uint GetCurrentLandmarkPlaceNameId() => ZoneMapInfoPtr == IntPtr.Zero ? 0 : (uint)Marshal.ReadInt32(ZoneMapInfoPtr, 0x10);

        static ZoneMap()
        {
            // thanks to https://github.com/FFXIVAPP/sharlayan-resources/blob/master/signatures/latest/x64.json#L42
            var basePtr = Plugin.SigScanner.GetStaticAddressFromSig("48 89 5C 24 08 57 48 83 EC 20 48 63 FA 41 0F B6 D8 45 84 C9 75 10 48 8D 0D");
            // Because we need the stuff at this place. Should have a better sig tho
            if (basePtr != IntPtr.Zero) ZoneMapInfoPtr = basePtr - 12;
        }
    }
}
