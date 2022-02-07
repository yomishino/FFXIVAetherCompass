using System.Runtime.InteropServices;

namespace AetherCompass.Game.SeFunctions
{
    internal static class Sound
    {
        private delegate long PlaySoundDelegate(int id, long unk1, long unk2);
        private static readonly PlaySoundDelegate? playSound;

        static Sound()
        {
            var addr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 4D 39 BE");
            playSound ??= Marshal.GetDelegateForFunctionPointer<PlaySoundDelegate>(addr);
        }

        public static void PlaySoundEffect(int macroSeId)
         => playSound?.Invoke(FromMacroSeId(macroSeId), 0, 0);

        private static int FromMacroSeId(int id)
            => id switch
            {
                1 => 0x25,
                2 => 0x26,
                3 => 0x27,
                4 => 0x28,
                5 => 0x29,
                6 => 0x2A,
                7 => 0x2B,
                8 => 0x2C,
                9 => 0x2D,
                10 => 0x2E,
                11 => 0x2F,
                12 => 0x30,
                13 => 0x31,
                14 => 0x32,
                15 => 0x33,
                16 => 0x34,
                _ => 0x0
            };

    }
}
