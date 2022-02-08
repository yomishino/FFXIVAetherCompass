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
using ImGuiNET;
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

        internal static CompassManager CompassManager { get; private set; } = null!;
        private readonly CompassOverlay overlay;
        private readonly CompassDetailsWindow detailsWindow;

        internal readonly PluginConfig Config;

        private bool _enabled = false;
        public bool Enabled 
        {
            get => _enabled;
            internal set 
            {
                _enabled = false;
                overlay.Clear();
                detailsWindow.Clear();
                if (!value) IconManager.DisposeAllIcons();
                _enabled = value;
                if (Config != null) Config.Enabled = value;
            }
        }
        internal bool InConfig { get; set; }


        public Plugin()
        {
            Config = PluginInterface.GetPluginConfig() as PluginConfig ?? new();
            overlay = new();
            detailsWindow = new();
            
            CompassManager = new(overlay, detailsWindow, Config);

            PluginCommands.AddCommands(this);

            CompassManager.AddCompass(new AetherCurrentCompass(Config, Config.AetherCurrentConfig, detailsWindow, overlay));
            CompassManager.AddCompass(new MobHuntCompass(Config, Config.MobHuntConfig, detailsWindow, overlay));
            CompassManager.AddCompass(new GatheringPointCompass(Config, Config.GatheringConfig, detailsWindow, overlay));
#if !RELEASE
            CompassManager.AddCompass(new QuestCompass(Config, Config.QuestConfig, detailsWindow, overlay));
#endif
#if DEBUG
            CompassManager.AddCompass(new DebugCompass(Config, Config.DebugConfig, detailsWindow, overlay));
#endif
            
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

            if (InConfig)
            {
                ImGui.Begin("AetherCompass: Configuration");
                ImGuiEx.Checkbox("Enable plugin", ref Config.Enabled,
                    "Enable/Disable this plugin. \n" +
                    "All compasses will auto pause in certain zones such as PvP zones regardless of this setting.");
                if (Config.Enabled != _enabled) Enabled = Config.Enabled;   // Clear&Reload iff Enabled changed
//#if DEBUG
//                ImGui.Text($"LocalContentId: {Plugin.ClientState.LocalContentId}");
//#endif
                if (Config.Enabled)
                {
                    ImGuiEx.Separator(true, true);
                    ImGui.Text("Plugin Settings:");
                    ImGui.NewLine();
                    ImGuiEx.Checkbox(
                        "Enable marking detected objects on screen", ref Config.ShowScreenMark,
                        "If enabled, will allow Compasses to mark objects detected by them on screen," +
                        "showing the direction and distance.\n\n" +
                        "You can configure this for each compass separately below.");
                    if (Config.ShowScreenMark)
                    {
                        ImGui.TreePush();
                        ImGuiEx.DragFloat("Marker size scale", ref Config.ScreenMarkSizeScale,
                            .01f, PluginConfig.ScreenMarkSizeScaleMin, PluginConfig.ScreenMarkSizeScaleMax);
                        overlay.AddDrawAction(Compass.GenerateConfigDummyMarkerDrawAction(
                            $"Marker size scale: {Config.ScreenMarkSizeScale:0.00}", Config.ScreenMarkSizeScale));
                        var viewport = ImGui.GetMainViewport().Pos;
                        var vsize = ImGui.GetMainViewport().Size;
                        System.Numerics.Vector4 displayArea = new(
                            viewport.X + Config.ScreenMarkConstraint.X, // L
                            viewport.Y + vsize.Y - Config.ScreenMarkConstraint.Y, // D
                            viewport.X + vsize.X - Config.ScreenMarkConstraint.Z, // R
                            viewport.Y + Config.ScreenMarkConstraint.W); // U
                        ImGuiEx.DragFloat4("Marker display area (Left/Bottom/Right/Top)", ref displayArea,
                            1, PluginConfig.ScreenMarkConstraintMin, 9999,
                            tooltip: "Set the display area for the markers.\n" +
                                "The display area is shown as the red rectangle on the screen. " +
                                "Detected objects will be marked on screen within this area.");
                        Config.ScreenMarkConstraint = new(
                            displayArea.X - viewport.X, // L
                            viewport.Y + vsize.Y - displayArea.Y, // D
                            viewport.X + vsize.X - displayArea.Z, // R
                            displayArea.W - viewport.Y); // U
                        overlay.AddDrawAction(() => ImGui.GetWindowDrawList().AddRect(
                            new(displayArea.X, displayArea.W), new(displayArea.Z, displayArea.Y),
                            ImGui.ColorConvertFloat4ToU32(new(1, 0, 0, 1)), 0, ImDrawFlags.Closed, 4));
                        ImGui.Indent();
                        ImGui.Text($"(Screen display area is: " +
                            $"<{viewport.X:0.0}, {viewport.Y + vsize.Y:0.0}, {viewport.X + vsize.X:0.0}, {viewport.Y:0.0}> )");
                        ImGui.Unindent();
                        ImGui.TreePop();
                    }
                    ImGuiEx.Checkbox("Show detected objects' details", ref Config.ShowDetailWindow,
                        "If enabled, will show a window listing details of detected objects.\n\n" +
                        "You can configure this for each compass separately below.");
                    if (Config.ShowDetailWindow)
                    {
                        ImGui.TreePush();
                        ImGuiEx.Checkbox("Don't show in instanced contents", ref Config.HideDetailInContents,
                            "If enabled, will auto hide the detail window in instance contents such as dungeons, trials and raids.");
                        ImGui.TreePop();
                    }
                    if (Config.ShowScreenMark || Config.ShowDetailWindow)
                    {
                        ImGuiEx.Checkbox("Hide compass UI when in event", ref Config.HideInEvent);
                        ImGuiEx.Checkbox("Hide compass UI when crafting/gathering/fishing", ref Config.HideWhenCraftGather);
                    }
                    ImGui.NewLine();
                    ImGuiEx.Checkbox("Enable chat notification", ref Config.NotifyChat,
                        "If enabled, will allow compasses to send notifications in game chat when detected an object.\n\n" +
                        "You can configure this for each compass separately below. ");
                    if (Config.NotifyChat)
                    {
                        ImGui.TreePush();
                        ImGuiEx.Checkbox("Also enable sound notification", ref Config.NotifySe,
                            "If enabled, will allow compasses to make sound notification alongside chat notification.\n\n" +
                            "You can configure this for each compass separately below.");
                        ImGui.TreePop();
                    }
                    ImGuiEx.Checkbox("Enable Toast notification", ref Config.NotifyToast, 
                        "If enabled, will allow compasses to make Toast notifications on screen when detected an object.\n\n" +
                        "You can configure this for each compass separately below.");
#if DEBUG
                    ImGuiEx.Checkbox("[DEBUG] Test all GameObjects", ref Config.DebugTestAllGameObjects);
#endif
                    ImGuiEx.Separator(true, true);
                    ImGui.Text("Compass Settings:");
                    ImGui.NewLine();
                    CompassManager.DrawCompassConfigUi();
                }
                ImGuiEx.Separator(false, true);
                if (ImGui.Button("Save"))
                    Config.Save();
                if (ImGui.Button("Save & Close"))
                {
                    Config.Save();
                    InConfig = false;
                    Reload();
                }
                ImGui.NewLine();
                if (ImGui.Button("Close & Discard All Changes"))
                {
                    InConfig = false;
                    Config.Load(PluginConfig.GetSavedPluginConfig());
                    Reload();
                }
                ImGui.End();

                Config.CheckValueValidity(ImGui.GetMainViewport().Size);
            }

            if (Enabled && ZoneWatcher.IsInCompassWorkZone && !InNotDrawingConditions())
            {
                if (ClientState.LocalPlayer != null)
                {
                    try
                    {
                        if (Config.ShowScreenMark) overlay.Draw();
                        if (Config.ShowDetailWindow)
                        {
                            if (!(Config.HideDetailInContents && ZoneWatcher.IsInDetailWindowHideZone))
                                detailsWindow.Draw();
                            else detailsWindow.Clear();
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
                    if (Config.ShowScreenMark) overlay.Clear();
                    if (Config.ShowDetailWindow) detailsWindow.Clear();
                }
            }
            else if (InConfig && Config.ShowScreenMark)
            {
                // for drawing the marker display area when in config
                overlay.Draw();
            }

        }

        private void Reload()
        {
            // Will clear prev drawings & dispose old icons
            Enabled = Config.Enabled;
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

        private bool InNotDrawingConditions()
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
            IconManager.DisposeAllIcons();

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
