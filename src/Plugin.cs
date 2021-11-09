using AetherCompass.Compasses;
using AetherCompass.Configs;
using AetherCompass.UI;
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
        internal static SigScanner SigScanner { get; private set; } = null!;
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

        private readonly PluginConfig config;
        private readonly IconManager iconManager;
        private readonly CompassManager compassMgr;
        private readonly CompassOverlay overlay;
        private readonly CompassDetailsWindow detailsWindow;

        public bool Enabled 
        {
            get => config.Enabled;
            private set 
            { 
                config.Enabled = value;
                if (!value)
                {
                    overlay.Clear();
                    detailsWindow.Clear();
                }
            }
        }
        private bool inConfig = false;

        public Plugin()
        {
            config = PluginInterface.GetPluginConfig() as PluginConfig ?? new();
            overlay = new();
            detailsWindow = new();

            iconManager = new(config);
            compassMgr = new(overlay, detailsWindow, config);

            PluginCommands.AddCommands(this);

            Framework.Update += OnFrameworkUpdate;

            PluginInterface.UiBuilder.Draw += OnDrawUi;
            PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;

            compassMgr.AddCompass(new AetherCurrentCompass(config, config.AetherCurrentConfig, iconManager));
#if DEBUG
            compassMgr.AddCompass(new DebugCompass(config, config.DebugConfig, iconManager));
#endif
            
            ClientState.TerritoryChanged += OnZoneChange;
        }

        public static void LogDebug(string msg) => PluginLog.Debug(msg);

        public static void LogError(string msg) => PluginLog.Error(msg);

        public static void ShowError(string chatMsg, string logMsg)
        {
            Chat.PrintErrorChat(chatMsg);
            LogError(logMsg);
        
        }

        private void OnDrawUi()
        {
            if (Enabled)
            {
                if (ClientState.LocalContentId != 0 && ClientState.LocalPlayer != null)
                {
                    try
                    {
                        overlay.Draw();
                        if (config.ShowDetailWindow) detailsWindow.Draw();
                    }
                    catch(Exception e)
                    {
                        ShowError("Plugin encountered an error.", e.ToString());
                    }
                }
                else
                {
                    // Clear when should not draw to avoid any action remaining in queue be drawn later
                    // which would cause game crash due to accessing invalid address and so on
                    // (Mostly I think is due to invalid access when calling WorldToScreen related things)
                    overlay.Clear();
                    if (config.ShowDetailWindow) detailsWindow.Clear();
                }
            }

            if (inConfig)
            {
                // TODO: draw config ui
            }
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            if (Enabled && ClientState.LocalContentId != 0 && ClientState.LocalPlayer != null)
            {
                try
                {
                    compassMgr.OnTick();
                }
                catch(Exception e)
                {
                    ShowError("Plugin encountered an error.", e.ToString());
                }
            }
        }

        private void OnOpenConfigUi()
        {
            inConfig = true;
        }

        private void OnZoneChange(object? _, ushort terr)
        {
            if (terr == 0) return;
            // Do not do null check of LocalPlayer here, local player is almost always null when this event fired
            if (Enabled && ClientState.LocalContentId != 0)
                compassMgr.OnZoneChange();
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            // TEMP:
            //PluginInterface.SavePluginConfig(this.config);

            PluginCommands.RemoveCommands();
            iconManager.Dispose();

            ClientState.TerritoryChanged -= OnZoneChange;

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
