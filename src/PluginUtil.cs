using Dalamud.Logging;
using System.Reflection;

namespace AetherCompass
{
    internal class PluginUtil
    {
        public static void LogDebug(string msg) => PluginLog.Debug(msg);

        public static void LogInfo(string msg) => PluginLog.Information(msg);

        public static void LogWarning(string msg) => PluginLog.Warning(msg);

        public static void LogError(string msg) => PluginLog.Error(msg);

        public static void LogWarningExcelSheetNotLoaded(string sheetName) 
            => PluginLog.Warning($"Failed to load Excel Sheet: {sheetName}");

        public static Version? GetPluginVersion()
            => Assembly.GetExecutingAssembly().GetName().Version;

        public static string GetPluginVersionAsString()
            => GetPluginVersion()?.ToString() ?? "0.0.0.0";

        public static int ComparePluginVersion(string v1, string v2)
        {
            var v1split = v1.Split('.', StringSplitOptions.TrimEntries);
            var v2split = v2.Split('.', StringSplitOptions.TrimEntries);
            var len = v1split.Length <= v2split.Length 
                ? v1split.Length : v2split.Length;
            for (int i = 0; i < len; i++)
            {
                var comp1 = i < v1split.Length && int.TryParse(v1split[i], out var c1)
                    ? c1 : 0;
                var comp2 = i < v2split.Length && int.TryParse(v2split[i], out var c2)
                    ? c2 : 0;
                if (comp1 == comp2) continue;
                return comp1.CompareTo(comp2);
            }
            return 0;
        }
    }
}
