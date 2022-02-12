using AetherCompass.Compasses;
using AetherCompass.Configs;
using AetherCompass.Game;
using AetherCompass.UI;
using AetherCompass.UI.GUI;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
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
        internal static Condition ClientCondition { get; private set; } = null!;
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

        internal static readonly IconManager IconManager = new();
        internal static readonly CompassManager CompassManager = new();
        internal static readonly CompassOverlay Overlay = new();
        internal static readonly CompassDetailsWindow DetailsWindow = new();

        internal static PluginConfig Config { get; private set; } = null!;

        private static bool _enabled = false;
        public static bool Enabled 
        {
            get => _enabled;
            internal set 
            {
                _enabled = false;
                Overlay.Clear();
                DetailsWindow.Clear();
                if (!value) IconManager.DisposeAllIcons();
                _enabled = value;
                if (Config != null) Config.Enabled = value;
            }
        }
        internal static bool InConfig;

        
        public Plugin()
        {
            Config = PluginInterface.GetPluginConfig() as PluginConfig ?? new();
            CompassManager.Init();

            PluginCommands.AddCommands();
            
            Framework.Update += OnFrameworkUpdate;
            PluginInterface.UiBuilder.Draw += OnDrawUi;
            PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
            ClientState.TerritoryChanged += OnZoneChange;

            Reload();
            OnZoneChange(null, ClientState.TerritoryType);  // update zone related stuff on init
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
            if (ClientState.LocalContentId == 0) return;

            if (InConfig) ConfigUi.Draw();

            if (Enabled && ZoneWatcher.IsInCompassWorkZone && !InNotDrawingConditions())
            {
                if (ClientState.LocalPlayer != null)
                {
                    try
                    {
                        if (Config.ShowScreenMark) Overlay.Draw();
                        if (Config.ShowDetailWindow)
                        {
                            if (!(Config.HideDetailInContents && ZoneWatcher.IsInDetailWindowHideZone))
                                DetailsWindow.Draw();
                            else DetailsWindow.Clear();
                        }
                    }
                    catch (Exception e)
                    {
                        //ShowError("Plugin encountered an error.", e.ToString());
                        LogError(e.ToString());
                    }
                }
                else
                {
                    // Clear when should not draw to avoid any action remaining in queue be drawn later
                    // which would cause game crash due to access violation etc.
                    if (Config.ShowScreenMark) Overlay.Clear();
                    if (Config.ShowDetailWindow) DetailsWindow.Clear();
                }
            }
            else if (InConfig && Config.ShowScreenMark)
            {
                // for drawing the marker display area when in config
                Overlay.Draw();
            }

        }

        public static void Reload()
        {
            // Will clear prev drawings & dispose old icons
            Enabled = Config.Enabled;
        }

        internal static void SetEnabledIfConfigChanged()
        {
            if (Config.Enabled != _enabled) 
                Enabled = Config.Enabled;   // Clear&Reload iff Enabled changed
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            if (Enabled && ClientState.LocalContentId != 0 && ZoneWatcher.IsInCompassWorkZone)
            {
                try
                {
                    CompassManager.OnTick();
                }
                catch (Exception e)
                {
                    //ShowError("Plugin encountered an error.", e.ToString());
                    LogError(e.ToString());
                }
            }
        }

        private void OnOpenConfigUi()
        {
            InConfig = true;
        }

        private void OnZoneChange(object? _, ushort terr)
        {
            ZoneWatcher.OnZoneChange();
            if (terr == 0) return;
            // Local player is almost always null when this event fired
            if (Enabled && ClientState.LocalContentId != 0)
                CompassManager.OnZoneChange();
        }

        private static bool InNotDrawingConditions()
            => Config.HideInEvent &&
            (  ClientCondition[ConditionFlag.ChocoboRacing]
            || ClientCondition[ConditionFlag.CreatingCharacter]
            || ClientCondition[ConditionFlag.DutyRecorderPlayback]
            || ClientCondition[ConditionFlag.OccupiedInCutSceneEvent]
            || ClientCondition[ConditionFlag.OccupiedInEvent]
            || ClientCondition[ConditionFlag.OccupiedInQuestEvent]
            || ClientCondition[ConditionFlag.OccupiedSummoningBell]
            || ClientCondition[ConditionFlag.Performing]
            || ClientCondition[ConditionFlag.PlayingLordOfVerminion]
            || ClientCondition[ConditionFlag.PlayingMiniGame]
            || ClientCondition[ConditionFlag.WatchingCutscene]
            || ClientCondition[ConditionFlag.WatchingCutscene78]
            ) || Config.HideWhenCraftGather &&
            (  ClientCondition[ConditionFlag.Crafting]
            || ClientCondition[ConditionFlag.Crafting40]
            || ClientCondition[ConditionFlag.Fishing]
            || ClientCondition[ConditionFlag.Gathering]
            || ClientCondition[ConditionFlag.Gathering42]
            || ClientCondition[ConditionFlag.PreparingToCraft]
            );



#region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            //config.Save();

            PluginCommands.RemoveCommands();
            IconManager.Dispose();

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
