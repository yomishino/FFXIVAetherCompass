using AetherCompass.Compasses;
using AetherCompass.Configs;
using AetherCompass.UI;
using AetherCompass.UI.GUI;
using Dalamud.Game;
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

        private bool _enabled = false;
        public bool Enabled 
        {
            get => _enabled;
            private set 
            {
                _enabled = false;
                overlay.Clear();
                detailsWindow.Clear();
                iconManager?.ReloadIcons();
                _enabled = value;
            }
        }
        private bool useHqIcon = false;
        private bool inConfig = false;
        

        public Plugin()
        {
            config = PluginInterface.GetPluginConfig() as PluginConfig ?? new();
            overlay = new();
            detailsWindow = new();

            iconManager = new(config);
            compassMgr = new(overlay, detailsWindow, config);

            PluginCommands.AddCommands(this);

            compassMgr.AddCompass(new AetherCurrentCompass(config, config.AetherCurrentConfig, iconManager));
#if DEBUG
            compassMgr.AddCompass(new DebugCompass(config, config.DebugConfig, iconManager));
#endif
            
            Framework.Update += OnFrameworkUpdate;
            PluginInterface.UiBuilder.Draw += OnDrawUi;
            PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
            ClientState.TerritoryChanged += OnZoneChange;

            Reload();

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
            if (ClientState.LocalContentId == 0 || NotInCompassWorkZone()) return;

            if (Enabled)
            {
                if (ClientState.LocalContentId != 0 && ClientState.LocalPlayer != null)
                {
                    try
                    {
                        if (config.ShowScreenMark) overlay.Draw();
                        if (config.ShowDetailWindow)
                        {
                            if (!(config.HideDetailInContents && IsDetailWindowHideZone()))
                                detailsWindow.Draw();
                        }
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
                    if (config.ShowScreenMark) overlay.Clear();
                    if (config.ShowDetailWindow) detailsWindow.Clear();
                }
            }

            if (inConfig)
            {
                ImGui.Begin("AetherCompass: Configuration");
                ImGui.Checkbox("Enable plugin", ref config.Enabled);
                if (config.Enabled != _enabled) Enabled = config.Enabled;   // Clear&Reload iff Enabled changed
                if (config.Enabled)
                {
                    ImGui.Separator();
                    ImGui.NewLine();
                    ImGui.Text("Plugin Settings:");
                    ImGui.NewLine();
                    ImGui.Checkbox("Enable marking detected objects on screen (?)", ref config.ShowScreenMark);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("If enabled, will allow Compasses to mark objects detected by them on screen," +
                            "showing the direction and distance.\n\n" +
                            "You can configure this for each compass separately below.");
                    if (config.ShowScreenMark)
                    {
                        ImGui.TreePush();
                        ImGui.Checkbox("Use HQ icons for marker (?)", ref config.HqIcon);
                        if (useHqIcon != config.HqIcon) // reload icon iff changed
                        {
                            useHqIcon = config.HqIcon;
                            iconManager.ReloadIcons();
                        }
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("If enabled, will use HQ icons when marking detected objects.");
                        ImGui.Text("Marker size scale: ");
                        ImGui.SameLine();
                        ImGui.DragFloat("(?) ##screenmarkersizescale", ref config.ScreenMarkSizeScale, 
                            .01f, PluginConfig.ScreenMarkSizeScaleMin, PluginConfig.ScreenMarkSizeScaleMax);
                        overlay.AddDrawAction(
                            () => Compass.DrawConfigDummyMarker($"Marker size scale: {config.ScreenMarkSizeScale:0.0}", config.ScreenMarkSizeScale));
                        ImGui.Text("Marker display area (Left/Bottom/Right/Top): ");
                        ImGui.Indent();
                        var viewport = ImGui.GetMainViewport().Pos;
                        var vsize = ImGui.GetMainViewport().Size;
                        System.Numerics.Vector4 displayArea = new(
                            viewport.X + config.ScreenMarkConstraint.X, // L
                            viewport.Y + vsize.Y - config.ScreenMarkConstraint.Y, // D
                            viewport.X + vsize.X - config.ScreenMarkConstraint.Z, // R
                            viewport.Y + config.ScreenMarkConstraint.W); // U
                        ImGui.DragFloat4("(?)##markerdisplayarea", ref displayArea, 1, PluginConfig.ScreenMarkConstraintMin);
                        config.ScreenMarkConstraint = new(
                            displayArea.X - viewport.X, // L
                            viewport.Y + vsize.Y - displayArea.Y, // D
                            viewport.X + vsize.X - displayArea.Z, // R
                            displayArea.W - viewport.Y); // U
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("Set the display area for the markers.\n" +
                                "The display area is shown as the red rectangle on the screen." +
                                "Detected objects will be marked on screen within this area.");
                        overlay.AddDrawAction(() => ImGui.GetWindowDrawList().AddRect(
                            new(displayArea.X, displayArea.W), new(displayArea.Z, displayArea.Y),
                            ImGui.ColorConvertFloat4ToU32(new(1, 0, 0, 1)), 0,
                            ImDrawFlags.Closed, 4));
                        ImGui.Text($"(Screen display area is: " +
                            $"{viewport.X:0.0}, {viewport.Y + vsize.Y:0.0}, {viewport.X + vsize.X:0.0}, {viewport.Y:0.0} )");
                        ImGui.Unindent();
                        ImGui.TreePop();
                    }
                    ImGui.Checkbox("Show detected objects' details (?)", ref config.ShowDetailWindow);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("If enabled, will show a window listing details of detected objects.\n\n" +
                            "You can configure this for each compass separately below.");
                    if (config.ShowDetailWindow)
                    {
                        ImGui.TreePush();
                        ImGui.Checkbox("Don't show in instanced contents (?)", ref config.HideDetailInContents);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("If enabled, will auto hide the detail window in instanced contents" +
                                "such as dungeons, trials and raids.");
                        ImGui.TreePop();
                    }
                    ImGui.Checkbox("Enable chat notification (?)", ref config.NotifyChat);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("If enabled, will allow compasses to send notifications " +
                            "in game chat when detected an object.\n\n" +
                            "You can configure this for each compass separately below. ");
                    if (config.NotifyChat)
                    {
                        ImGui.TreePush();
                        ImGui.Checkbox("Also enable sound notification (?)", ref config.NotifySe);
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("If enabled, will allow compasses to make sound notification " +
                                "alongside chat notification.\n\n" +
                                "You can configure this for each compass separately below.");
                        ImGui.TreePop();
                    }
                    ImGui.Checkbox("Enable Toast notification (?)", ref config.NotifyToast);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("If enabled, will allow compasses to make Toast notifications " +
                            "on screen when detected an object.\n\n" +
                            "You can configure this for each compass separately below.");
#if DEBUG
                    ImGui.Checkbox("[DEBUG] Use full ObjectInfoArray", ref config.DebugUseFullArray);
#endif
                    ImGui.Separator();
                    ImGui.NewLine();
                    ImGui.Text("Compass Settings:");
                    ImGui.NewLine();
                    compassMgr.DrawCompassConfigUi();
                }
                ImGui.Separator();
                ImGui.NewLine();
                if (ImGui.Button("Save"))
                    config.Save();
                if (ImGui.Button("Save & Close"))
                {
                    config.Save();
                    inConfig = false;
                    Reload();
                }
                ImGui.NewLine();
                if (ImGui.Button("Close & Discard All Changes"))
                {
                    inConfig = false;
                    config.Load(PluginConfig.GetSavedPluginConfig());
                    Reload();
                }
                ImGui.End();

                config.CheckValueValidity(ImGui.GetMainViewport().Size);

                // for drawing the marker display area
                if (config.ShowScreenMark) overlay.Draw();
            }
        }

        private void Reload()
        {
            useHqIcon = config.HqIcon;
            // Will clear prev drawings & reload icons
            Enabled = config.Enabled;
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
            // Local player is almost always null when this event fired
            if (Enabled && ClientState.LocalContentId != 0)
                compassMgr.OnZoneChange();
        }

        // Not in work zone if its invalid zone or pvp zone
        private static bool NotInCompassWorkZone()
        {
            var terr = DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.TerritoryType>()?
                .GetRow(ClientState.TerritoryType);
            return terr == null || terr.IsPvpZone
                || terr.BattalionMode > 1   // pvp contents or LoVM
                || terr.TerritoryIntendedUse == 20  // chocobo race terr?
                ;
        }

        private static bool IsDetailWindowHideZone()
        {
            var terr = (DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.TerritoryType>()?
                .GetRow(ClientState.TerritoryType));
            // Exclusive type: 0 not instanced, 1 is solo instance, 2 is nonsolo instance.
            // Not sure about 3, seems quite mixed up with solo battles, diadem and misc stuff like LoVM
            return terr == null || terr.ExclusiveType == 1 || terr.ExclusiveType == 2;
        }


        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            //config.Save();

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
