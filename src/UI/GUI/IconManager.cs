using AetherCompass.Configs;
using System;
using System.Numerics;

using TextureWrap = ImGuiScene.TextureWrap;

namespace AetherCompass.UI.GUI
{
    public sealed class IconManager : IDisposable
    {
        private readonly PluginConfig config = null!;

        public const uint AltitudeHigherIconId = 60954;
        internal static TextureWrap? AltitudeHigherIcon { get; private set; }
        public const uint AltitudeLowerIconId = 60955;
        internal static TextureWrap? AltitudeLowerIcon { get; private set; }
        internal static readonly Vector2 AltitudeIconSize = new(45, 45);

        // NaviMap thing with those quests/fate etc. direction markers are in 10001400
        // but we'll use something else for easier work.
        // 60541 up, 60545 down; there are also two sets that are smaller
        public const uint DirectionScreenIndicatorIconId = 60541;
        internal static TextureWrap? DirectionScreenIndicatorIcon { get; private set; }
        internal static readonly Vector2 DirectionScreenIndicatorIconSize = new(45, 45);
        internal static readonly uint DirectionScreenIndicatorIconColour = ImGuiNET.ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 1));

        internal static readonly Vector2 MarkerIconSize = new(30, 30);

        public const uint ConfigDummyMarkerIconId = 25948;
        internal static TextureWrap? ConfigDummyMarkerIcon { get; private set; }

        public const uint AetherCurrentMarkerIconId = 60033;
        internal TextureWrap? AetherCurrentMarkerIcon { get; private set; }
        
        // Armorer job icon, just randomly picked a asymmetrical one for debug
        public const uint DebugMarkerIconId = 62110;
        internal TextureWrap? DebugMarkerIcon { get; private set; }
        

        public IconManager(PluginConfig config)
        {
            this.config = config;
        }

        public void ReloadIcons()
        {
            DisposeAllIcons();

            if (!config.Enabled) return;

            LoadCommonIcons();
            if (config.AetherCurrentConfig.Enabled)
                LoadAetherCurrentCompassIcons();
#if DEBUG
            if (config.DebugConfig.Enabled)
                LoadDebugCompassIcons();
#endif
        }

        private void LoadCommonIcons()
        {
            AltitudeHigherIcon = GetIconAsImGuiTexture(AltitudeHigherIconId);
            AltitudeLowerIcon = GetIconAsImGuiTexture(AltitudeLowerIconId);
            DirectionScreenIndicatorIcon = GetIconAsImGuiTexture(DirectionScreenIndicatorIconId);
            ConfigDummyMarkerIcon = GetIconAsImGuiTexture(ConfigDummyMarkerIconId);

            if (AltitudeHigherIcon == null) ShowLoadIconError(AltitudeHigherIconId);
            if (AltitudeLowerIcon == null) ShowLoadIconError(AltitudeLowerIconId);
            if (DirectionScreenIndicatorIcon == null) ShowLoadIconError(DirectionScreenIndicatorIconId);
            if (ConfigDummyMarkerIcon == null) ShowLoadIconError(ConfigDummyMarkerIconId);
        }

        private void LoadAetherCurrentCompassIcons()
        {
            AetherCurrentMarkerIcon = GetIconAsImGuiTexture(AetherCurrentMarkerIconId);
            
            if (AetherCurrentMarkerIcon == null) ShowLoadIconError(AetherCurrentMarkerIconId);
        }

        private void LoadDebugCompassIcons()
        {
            DebugMarkerIcon = GetIconAsImGuiTexture(DebugMarkerIconId);
            if (DebugMarkerIcon == null) ShowLoadIconError(DebugMarkerIconId);
        }


        private static void DisposeCommonIcons()
        {
            if (AltitudeHigherIcon != null)
            {
                AltitudeHigherIcon.Dispose();
                AltitudeHigherIcon = null;
            }
            if (AltitudeLowerIcon != null)
            {
                AltitudeLowerIcon.Dispose();
                AltitudeLowerIcon = null;
            }
            if (DirectionScreenIndicatorIcon != null)
            {
                DirectionScreenIndicatorIcon.Dispose();
                DirectionScreenIndicatorIcon = null;
            }
            if (ConfigDummyMarkerIcon != null)
            {
                ConfigDummyMarkerIcon.Dispose();
                ConfigDummyMarkerIcon = null;
            }
        }

        private void DisposeAetherCurrentCompassIcons()
        {
            if (AetherCurrentMarkerIcon != null) 
            {
                AetherCurrentMarkerIcon.Dispose();
                AetherCurrentMarkerIcon = null;
            }
        }

        private void DisposeDebugIcons()
        {
            if (DebugMarkerIcon != null) 
            {
                DebugMarkerIcon.Dispose();
                DebugMarkerIcon = null;
            }
        }

        private void DisposeAllIcons()
        {
            DisposeCommonIcons();
            DisposeAetherCurrentCompassIcons();
            DisposeDebugIcons();
        }

        public void Dispose()
        {
            DisposeAllIcons();
        }

        private TextureWrap? GetIconAsImGuiTexture(uint iconId)
            => config.HqIcon ? Plugin.DataManager.GetImGuiTextureHqIcon(iconId) : Plugin.DataManager.GetImGuiTextureIcon(iconId);

        private static void ShowLoadIconError(uint iconId)
        {
            string name = iconId switch
            {
                AltitudeHigherIconId => nameof(AltitudeHigherIcon),
                AltitudeLowerIconId => nameof(AltitudeLowerIcon),
                DirectionScreenIndicatorIconId => nameof(DirectionScreenIndicatorIcon),
                ConfigDummyMarkerIconId => nameof(ConfigDummyMarkerIcon),
                AetherCurrentMarkerIconId => nameof(AetherCurrentMarkerIcon),
                DebugMarkerIconId => nameof(DebugMarkerIcon),
                _ => "(UnknownIcon)"
            };
            Plugin.ShowError($"Plugin encountered an error: Failed to load icon",
                $"Failed to load icon: {name}, IconId = {iconId}");
        }
    }
}
