using AetherCompass.Compasses;
using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;

namespace AetherCompass
{
    public class Plugin : IDalamudPlugin
    {
        // Plugin Services
        [PluginService]
        [RequiredVersion("1.0")]
        internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService]
        [RequiredVersion("1.0")]
        internal static Dalamud.Game.Command.CommandManager CommandManager { get; private set; } = null!;
        [PluginService]
        [RequiredVersion("1.0")]
        internal static Dalamud.Data.DataManager DataManager { get; private set; } = null!;
        [PluginService]
        [RequiredVersion("1.0")]
        internal static Framework Framework { get; private set; } = null!;
        [PluginService]
        [RequiredVersion("1.0")]
        internal static Dalamud.Game.ClientState.ClientState ClientState { get; private set; } = null!;
        [PluginService]
        [RequiredVersion("1.0")]
        internal static Dalamud.Game.Gui.GameGui GameGui { get; private set; } = null!;
        [PluginService]
        [RequiredVersion("1.0")]
        internal static Dalamud.Game.Gui.ChatGui ChatGui { get; private set; } = null!;
        [PluginService]
        [RequiredVersion("1.0")]
        internal static Dalamud.Game.Gui.Toast.ToastGui ToastGui { get; private set; } = null!;


        public string Name =>
#if DEBUG
            "Aether Compass [DEV]";
#elif TEST
            "Aether Compass [TEST]";
#else
            "Aether Compass";
#endif

        private readonly Configuration config;
        private readonly IconManager iconManager;
        private readonly CompassManager compassMgr;

        public bool Enabled 
        {
            get => config.Enabled;
            private set => config.Enabled = value;
        }
        private bool inConfig = false;

        public Plugin()
        {
            config = PluginInterface.GetPluginConfig() as Configuration ?? new();
            iconManager = new(config);
            compassMgr = new(config);

            PluginCommands.AddCommands(this);

            Framework.Update += OnFrameworkUpdate;

            PluginInterface.UiBuilder.Draw += OnDrawUi;
            PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;

            compassMgr.AddCompass(new AetherCurrentCompass(config, iconManager));
#if DEBUG
            compassMgr.AddCompass(new DebugCompass(config, iconManager));
#endif
        }

        public static void LogDebug(string msg) => PluginLog.Debug(msg);

        public static void LogError(string msg) => PluginLog.Error(msg);

        public static void ShowError(string chatMsg, string logMsg)
        {
            ChatGui.PrintError(chatMsg);
            LogError(logMsg);
        
        }

        public static void PrintChat(string msg)
        {
            ChatGui.Print(msg);
        }

        public static void PrintChat(Dalamud.Game.Text.SeStringHandling.SeString msg)
        {
            ChatGui.PrintChat(new Dalamud.Game.Text.XivChatEntry()
            {
                Message = msg,
                Type = Dalamud.Game.Text.XivChatType.Echo
            });
        }

        private void OnDrawUi()
        {
            if (Enabled && ClientState.LocalContentId != 0 && ClientState.LocalPlayer != null)
            {
                compassMgr.OnTick();
                
            }

            if (inConfig)
            {
                // TODO: draw config ui
            }
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            
        }

        private void OnOpenConfigUi()
        {
            inConfig = true;
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            // TEMP:
            //PluginInterface.SavePluginConfig(this.config);

            PluginCommands.RemoveCommands();
            iconManager.Dispose();
            
            PluginInterface.UiBuilder.Draw -= OnDrawUi;
            PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;

            Framework.Update -= OnFrameworkUpdate;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
