global using System;
global using System.Collections.Generic;
global using static AetherCompass.PluginUtil;
global using Sheets = Lumina.Excel.GeneratedSheets;
using AetherCompass.Compasses;
using AetherCompass.Game;
using AetherCompass.UI;
using AetherCompass.UI.GUI;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace AetherCompass
{
    public class Plugin : IDalamudPlugin
    {
        // Plugin Services
        [PluginService]
        internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService]
        internal static ISigScanner SigScanner { get; private set; } = null!;
        [PluginService]
        internal static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService]
        internal static IDataManager DataManager { get; private set; } = null!;
        [PluginService]
        internal static ITextureProvider TextureProvider { get; private set; } = null!;
        [PluginService]
        internal static IFramework Framework { get; private set; } = null!;
        [PluginService]
        internal static IClientState ClientState { get; private set; } = null!;
        [PluginService]
        internal static ICondition ClientCondition { get; private set; } = null!;
        [PluginService]
        internal static IGameGui GameGui { get; private set; } = null!;
        [PluginService]
        internal static IChatGui ChatGui { get; private set; } = null!;
        [PluginService]
        internal static IToastGui ToastGui { get; private set; } = null!;


        public string Name => "Aether Compass"
#if PRE
            + " [PREVIEW]"
#endif
#if DEBUG
            + " [DEV]"
#elif TEST
            + " [TEST]"
#endif
            ;

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
            Config = new();
            Config.Load();
            CompassManager.Init();

            PluginCommands.AddCommands();
            
            Framework.Update += OnFrameworkUpdate;
            PluginInterface.UiBuilder.Draw += OnDrawUi;
            PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
            ClientState.TerritoryChanged += OnZoneChange;

            Reload();
            OnZoneChange(ClientState.TerritoryType);  // update zone related stuff on init
        }

        public static void ShowError(string chatMsg, string logMsg)
        {
            Chat.PrintErrorChat(chatMsg);
            LogError(logMsg);
        }

        public static void OpenConfig(bool setFocus = false)
        {
            InConfig = true;
            if (setFocus) ConfigUi.IsFocus = true;
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

        private void OnFrameworkUpdate(IFramework framework)
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

        private void OnOpenConfigUi() => OpenConfig(true);

        private void OnZoneChange( ushort terr)
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
