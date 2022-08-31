using AetherCompass.UI;
using Dalamud.Game.Command;

namespace AetherCompass
{
    public static class PluginCommands
    {
        public const string MainCommand = "/aethercompass";

        public static void AddCommands()
        {
            Plugin.CommandManager.AddHandler(MainCommand, new CommandInfo((cmd, args) => ProcessMainCommand(cmd, args))
            {
                HelpMessage = "Toggle the plugin between enabled/disabled when no options provided\n" +
                    $"{MainCommand} [on|off] → Enable/Disable the plugin\n" +
                    $"{MainCommand} mark → Toggle enabled/disabled for marking detected objects on screen\n" +
                    $"{MainCommand} detail → Toggle enabled/disabled for showing Object Detail Window\n" +
                    $"{MainCommand} config → Open the Configuration window",
                ShowInHelp = true
            });
        }

        public static void RemoveCommands()
        {
            Plugin.CommandManager.RemoveHandler(MainCommand);
        }

        private static void ProcessMainCommand(string command, string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                Plugin.Enabled = !Plugin.Enabled;
                return;
            }
            var argList = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (argList.Length == 0)
            {
                Plugin.Enabled = !Plugin.Enabled;
                return;
            }
            switch (argList[0])
            {
                case "on":
                    Plugin.Enabled = true;
                    return;
                case "off":
                    Plugin.Enabled = false;
                    return;
                case "mark":
                    Plugin.Config.ShowScreenMark = !Plugin.Config.ShowScreenMark;
                    return;
                case "detail":
                    Plugin.Config.ShowDetailWindow = !Plugin.Config.ShowDetailWindow;
                    return;
                case "config":
                    Plugin.OpenConfig(true);
                    return;
                default:
                    Chat.PrintErrorChat($"{command}: Unknown command args: {args}");
                    return;
            }

        }

    }
}
