using System;
using System.Numerics;

using TextureWrap = ImGuiScene.TextureWrap;

namespace AetherCompass
{
    public sealed class IconManager : IDisposable
    {
        private readonly Configuration config = null!;

        public const uint AltitudeHigherIconId = 60954;
        internal TextureWrap? AltitudeHigherIcon { get; private set; }
        public const uint AltitudeLowerIconId = 60955;
        internal TextureWrap? AltitudeLowerIcon { get; private set; }
        internal static readonly Vector2 AltitudeIconSize = new(45, 45);

        // NaviMap thing with those quests/fate etc. direction markers are in 10001400 but may be we use something else for simplicty?
        // 60541 up, 60545 down; there are also two sets that are smaller
        public const uint DirectionScreenIndicatorIconId = 60541;
        internal TextureWrap? DirectionScreenIndicatorIcon { get; private set; }
        internal static readonly Vector2 DirectionScreenIndicatorIconSize = new(45, 45);

        public const uint AetherCurrentMarkerIconId = 60033;
        internal TextureWrap? AetherCurrentMarkerIcon { get; private set; }
        internal static readonly Vector2 AetherCurrentMarkerIconSize = new(35, 35);
        internal static readonly Vector2 AetherCurrentMarkerIconSizeSmall = new(25, 25);

        public const uint AetheryteMarkerIconId = 60453;
        internal TextureWrap? AetheryteMarkerIcon { get; private set; }
        internal static readonly Vector2 AetheryteMarkerIconSize = new(30, 30);

        // Armorer job icon, just randomly picked a asymmetrical one for debug
        public const uint DebugMarkerIconId = 62110;
        internal TextureWrap? DebugMarkerIcon { get; private set; }
        internal static readonly Vector2 DebugMarkerIconSize = new(30, 30);
        internal static readonly uint DebugMarkerIconColour = ImGuiNET.ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 1));


        public IconManager(Configuration config)
        {
            this.config = config;
            LoadIcon();
        }

        // TODO: load all or load according to enabled or not?
        public void LoadIcon()
        {
            DisposeIcons();

            AltitudeHigherIcon = GetIconAsImGuiTexture(AltitudeHigherIconId);
            AltitudeLowerIcon = GetIconAsImGuiTexture(AltitudeLowerIconId);
            DirectionScreenIndicatorIcon = GetIconAsImGuiTexture(DirectionScreenIndicatorIconId);

            if (config.AetherEnabled)
            {
                AetherCurrentMarkerIcon = GetIconAsImGuiTexture(AetherCurrentMarkerIconId);
                AetheryteMarkerIcon = GetIconAsImGuiTexture(AetheryteMarkerIconId);
            }

            if (DirectionScreenIndicatorIcon == null) ShowLoadIconError(DirectionScreenIndicatorIconId);
            if (AetherCurrentMarkerIcon == null) ShowLoadIconError(AetherCurrentMarkerIconId);
            if (AetheryteMarkerIcon == null) ShowLoadIconError(AetheryteMarkerIconId);

#if DEBUG
            if (config.DebugEnabled) DebugMarkerIcon = GetIconAsImGuiTexture(DebugMarkerIconId);
            if (AltitudeHigherIcon == null) ShowLoadIconError(AltitudeHigherIconId);
            if (AltitudeLowerIcon == null) ShowLoadIconError(AltitudeLowerIconId);
            if (DebugMarkerIcon == null) ShowLoadIconError(DebugMarkerIconId);
#endif
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
                AetherCurrentMarkerIconId => nameof(AetherCurrentMarkerIcon),
                AetheryteMarkerIconId => nameof(AetheryteMarkerIcon),
                DebugMarkerIconId => nameof(DebugMarkerIcon),
                _ => "(UnknownIcon)"
            };
            Plugin.ShowError($"AetherCompass encountered an error: Failed to load icon",
                $"Failed to load icon: {name}, IconId = {iconId}");
        }

        private void DisposeIcons()
        {
            if (AltitudeHigherIcon != null) AltitudeHigherIcon.Dispose();
            if (AltitudeLowerIcon != null) AltitudeLowerIcon.Dispose();
            if (DirectionScreenIndicatorIcon != null) DirectionScreenIndicatorIcon.Dispose();
            
            if (AetherCurrentMarkerIcon != null) AetherCurrentMarkerIcon.Dispose();
            if (AetheryteMarkerIcon != null) AetheryteMarkerIcon.Dispose();

            if (DebugMarkerIcon != null) DebugMarkerIcon.Dispose();
        }

        public void Dispose()
        {
            DisposeIcons();
        }
    
    }
}
