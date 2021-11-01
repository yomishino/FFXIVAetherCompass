using Dalamud.Game.Command;

namespace AetherCompass
{
    public static class PluginCommands
    {
        public const string MainCommand = "/aethercompass";

        public static void AddCommands(Plugin host)
        {
            Plugin.CommandManager.AddHandler(
                MainCommand, new CommandInfo((cmd, args) => ProcessMainCommand(host, cmd, args))
                {
                    HelpMessage = "",
                    ShowInHelp = true
                });
        }

        public static void RemoveCommands()
        {
            Plugin.CommandManager.RemoveHandler(MainCommand);
        }

        public static void ProcessMainCommand(Plugin host, string command, string args)
        {
            
        }
    }
}
