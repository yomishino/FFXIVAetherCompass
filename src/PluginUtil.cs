using Dalamud.Logging;

namespace AetherCompass
{
    internal class PluginUtil
    {
        public static void LogDebug(string msg) => PluginLog.Debug(msg);

        public static void LogWarning(string msg) => PluginLog.Warning(msg);

        public static void LogError(string msg) => PluginLog.Error(msg);

        public static void LogWarningExcelSheetNotLoaded(string sheetName) 
            => PluginLog.Warning($"Failed to load Excel Sheet: {sheetName}");

    }
}
