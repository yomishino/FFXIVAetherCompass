using static Dalamud.ClientLanguage;

namespace AetherCompass.Game
{
    public static class Language
    {
        public static Dalamud.ClientLanguage ClientLanguage
            => Plugin.ClientState.ClientLanguage;

        public static int GetAdjustedTextMaxLength(int maxLengthEN)
            => ClientLanguage == Japanese ? maxLengthEN / 2 : maxLengthEN;

        public static class Unit
        {
            public static string Yalm
                => ClientLanguage == Japanese ? "m" : "y";
        }
    }
}
