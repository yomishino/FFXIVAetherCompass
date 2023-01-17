using System.Runtime.InteropServices;

namespace AetherCompass.Game.SeFunctions
{
    public static class ZoneMap
    {
        private static readonly IntPtr ZoneMapInfoPtr 
            = Plugin.SigScanner.GetStaticAddressFromSig(
                "8B 2D ?? ?? ?? ?? 41 BF");

        // Some territories' use maps that are not "00" named, such as "s1t1/01",
        // and many but not all of them are multi-layered maps such as housing maps.
        // For those, this returns the real map id.
        // But will return 0 for those "00" named maps.
        public static uint GetCurrentAltMapId() 
            => ZoneMapInfoPtr == IntPtr.Zero 
            ? 0 : (uint)Marshal.ReadInt32(ZoneMapInfoPtr, 0);
        
        // Same as above?
        public static uint GetCurrentAltMapId2() 
            => ZoneMapInfoPtr == IntPtr.Zero 
            ? 0 : (uint)Marshal.ReadInt32(ZoneMapInfoPtr, 4);

        // Returns 0 when not in any named area
        public static uint GetCurrentAreaPlaceNameId() 
            => ZoneMapInfoPtr == IntPtr.Zero 
            ? 0 : (uint)Marshal.ReadInt32(ZoneMapInfoPtr, 0x10);

        // Returns 0 when not in range of any named landmark
        public static uint GetCurrentLandmarkPlaceNameId() 
            => ZoneMapInfoPtr == IntPtr.Zero ? 0 : (uint)Marshal.ReadInt32(ZoneMapInfoPtr, 0x14);
    }
}
