using AetherCompass.UI;
using Dalamud.Game.Command;

namespace AetherCompass
{
    public static class PluginCommands
    {
        public const string MainCommand = "/aethercompass";

        public static void AddCommands(Plugin host)
        {
            // TODO: commands
            Plugin.CommandManager.AddHandler(
                MainCommand, new CommandInfo((cmd, args) => ProcessMainCommand(host, cmd, args))
                {
                    HelpMessage = "Toggles the plugin when no options provided\n" +
                    "\tOptions:\n" +
                    $"\t\ton: Enable the plugin\n" +
                    $"\t\toff: Disable the plugin\n" +
                    $"\t\tconfig: Open the Configuration window",
                    ShowInHelp = true
                });
        }

        public static void RemoveCommands()
        {
            Plugin.CommandManager.RemoveHandler(MainCommand);
        }

        public static void ProcessMainCommand(Plugin host, string command, string args)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                host.Enabled = !host.Enabled;
                return;
            }
            var argList = args.Split();
            if (argList.Length == 0)
            {
                host.Enabled = !host.Enabled;
                return;
            }
            switch (argList[0])
            {
                case "on":
                    host.Enabled = true;
                    return;
                case "off":
                    host.Enabled = false;
                    return;
                case "config":
                    host.InConfig = true;
                    return;
                default:
                    Chat.PrintErrorChat($"Unknown command args: {args}");
                    return;
            }

        }
    }
}
