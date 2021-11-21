using System;
using System.Collections.Generic;
using System.Numerics;

using TextureWrap = ImGuiScene.TextureWrap;

namespace AetherCompass.UI.GUI
{
    public sealed class IconManager : IDisposable
    {
        public const uint AltitudeHigherIconId = 60954;
        private static TextureWrap? _altitudeHigherIcon;
        internal static TextureWrap? AltitudeHigherIcon
        {
            get
            {
                if (_altitudeHigherIcon == null)
                    _altitudeHigherIcon = GetIconAsImGuiTexture(AltitudeHigherIconId);
                return _altitudeHigherIcon;
            }
            private set
            {
                _altitudeHigherIcon?.Dispose();
                _altitudeHigherIcon = value;
            }
        }
        public const uint AltitudeLowerIconId = 60955;
        private static TextureWrap? _altitudeLowerIcon;
        internal static TextureWrap? AltitudeLowerIcon
        {
            get
            {
                if (_altitudeLowerIcon == null)
                    _altitudeLowerIcon = GetIconAsImGuiTexture(AltitudeLowerIconId);
                return _altitudeLowerIcon;
            }
            private set
            {
                _altitudeLowerIcon?.Dispose();
                _altitudeLowerIcon = value;
            }
        }
        public static readonly Vector2 AltitudeIconSize = new(45, 45);

        // NaviMap thing with those quests/fate etc. direction markers are in 10001400
        // but we'll use something else for easier work.
        // 60541 up, 60545 down; there are also two sets that are smaller
        public const uint DirectionScreenIndicatorIconId = 60541;
        private static TextureWrap? _directionScreenIndicatorIcon;
        internal static TextureWrap? DirectionScreenIndicatorIcon
        {
            get
            {
                if (_directionScreenIndicatorIcon == null)
                    _directionScreenIndicatorIcon = GetIconAsImGuiTexture(DirectionScreenIndicatorIconId);
                return _directionScreenIndicatorIcon;
            }
            private set
            {
                _directionScreenIndicatorIcon?.Dispose();
                _directionScreenIndicatorIcon = value;
            }
        }
        internal static readonly Vector2 DirectionScreenIndicatorIconSize = new(45, 45);
        internal static readonly uint DirectionScreenIndicatorIconColour = ImGuiNET.ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 1));


        internal static readonly Vector2 MarkerIconSize = new(30, 30);

        public const uint ConfigDummyMarkerIconId = 25948;
        private static TextureWrap? _configDummyMarkerIcon;
        internal static TextureWrap? ConfigDummyMarkerIcon
        { 
            get 
            {
                if (_configDummyMarkerIcon == null)
                    _configDummyMarkerIcon = GetIconAsImGuiTexture(ConfigDummyMarkerIconId);
                return _configDummyMarkerIcon;
            } 
            private set
            {
                _configDummyMarkerIcon?.Dispose();
                _configDummyMarkerIcon = value;
            }
        }
        //// Armorer job icon, just randomly picked a asymmetrical one for debug
        //public const uint DebugMarkerIconId = 62110;
        internal static TextureWrap? DebugMarkerIcon => ConfigDummyMarkerIcon;

        public const uint AetherCurrentMarkerIconId = 60033;
        private static TextureWrap? _aetherCurrentMarkerIcon;
        internal static TextureWrap? AetherCurrentMarkerIcon
        {
            get
            {
                if (_aetherCurrentMarkerIcon == null)
                    _aetherCurrentMarkerIcon = GetIconAsImGuiTexture(AetherCurrentMarkerIconId);
                return _aetherCurrentMarkerIcon;
            }
            private set
            {
                _aetherCurrentMarkerIcon?.Dispose();
                _aetherCurrentMarkerIcon = value;
            }
        }

        public const uint MobHuntMarkerIconId = 61710;
        private static TextureWrap? _mobHuntMarkerIcon;
        internal static TextureWrap? MobHuntMarkerIcon
        {
            get
            {
                if (_mobHuntMarkerIcon == null)
                    _mobHuntMarkerIcon = GetIconAsImGuiTexture(MobHuntMarkerIconId);
                return _mobHuntMarkerIcon;
            }
            private set
            {
                _mobHuntMarkerIcon?.Dispose();
                _mobHuntMarkerIcon = null;
            }
        }

        // NPC AnnounceIcon starts from 71200; see lumina sheet EventIconType
        // For types whose IconRange is 6, the 3rd is in-progress and 5th is last seq (checkmark icon),
        // because +0 is the dummy, so 1st icon in the range would start from +1.
        // Each type has availabled and locked ver, but rn idk how to accurately tell if a quest is avail or locked
        public const uint DefaultQuestMarkerIconId = 71223;
        private static TextureWrap? _defaultQuestMarkerIcon;
        internal static TextureWrap? DefaultQuestMarkerIcon
        {
            get
            {
                if (_defaultQuestMarkerIcon == null)
                    _defaultQuestMarkerIcon = GetIconAsImGuiTexture(DefaultQuestMarkerIconId);
                return _defaultQuestMarkerIcon;
            }
            private set
            {
                _defaultQuestMarkerIcon?.Dispose();
                _defaultQuestMarkerIcon = value;
            }
        }

        private static readonly Dictionary<uint, TextureWrap?> _questMarkerIconMap = new();
        
        internal static TextureWrap? GetQuestMarkerIcon(uint iconId)
        {
            if (!_questMarkerIconMap.TryGetValue(iconId, out var tex) || tex == null)
                SetQuestMarkerIcon(iconId, GetIconAsImGuiTexture(iconId));
            return _questMarkerIconMap[iconId];
        }

        internal static TextureWrap? GetQuestMarkerIcon(uint baseIconId, byte iconRange, bool questLastSeq = false)
            => GetQuestMarkerIcon(GetQuestMarkerIconId(baseIconId, iconRange, questLastSeq));

        private static void SetQuestMarkerIcon(uint iconId, TextureWrap? icon)
        {
            if (!_questMarkerIconMap.TryGetValue(iconId, out var tex))
                _questMarkerIconMap[iconId] = icon;
            else
            {
                tex?.Dispose();
                _questMarkerIconMap[iconId] = icon;
            }
        }

        private static uint GetQuestMarkerIconId(uint baseIconId, byte iconRange, bool questLastSeq = false)
            => baseIconId + iconRange switch
            {
                6 => questLastSeq ? 5u : 3u,
                1 => 0,
                _ => 1,
            };


        #region Dispose

        // Disposing each icon is handled by setter

        private static void DisposeCommonIcons()
        {
            AltitudeHigherIcon = null;
            AltitudeLowerIcon = null;
            DirectionScreenIndicatorIcon = null;
            ConfigDummyMarkerIcon = null;
        }

        internal static void DisposeAetherCurrentCompassIcons()
        {
            AetherCurrentMarkerIcon = null;
        }

        internal static void DisposeMobHuntCompassIcons()
        {
            MobHuntMarkerIcon = null;
        }

        internal static void DisposeQuestCompassIcons()
        {
            DefaultQuestMarkerIcon = null;
            foreach (uint id in _questMarkerIconMap.Keys)
                SetQuestMarkerIcon(id, null);
        }

        //private void DisposeDebugIcons()
        //{
        //    DebugMarkerIcon = null;
        //}

        public static void DisposeAllIcons()
        {
            DisposeCommonIcons();
            DisposeAetherCurrentCompassIcons();
            DisposeMobHuntCompassIcons();
            DisposeQuestCompassIcons();
            //DisposeDebugIcons();
        }

        public void Dispose()
        {
            DisposeAllIcons();
        }

        #endregion


        private static TextureWrap? GetIconAsImGuiTexture(uint iconId)
        { 
            var icon = Plugin.DataManager.GetImGuiTextureIcon(iconId);
            if (icon == null) ShowLoadIconError(iconId);
            return icon;
        }

        private static void ShowLoadIconError(uint iconId)
        {
            string name = iconId switch
            {
                AltitudeHigherIconId => nameof(AltitudeHigherIcon),
                AltitudeLowerIconId => nameof(AltitudeLowerIcon),
                DirectionScreenIndicatorIconId => nameof(DirectionScreenIndicatorIcon),
                ConfigDummyMarkerIconId => nameof(ConfigDummyMarkerIcon),
                AetherCurrentMarkerIconId => nameof(AetherCurrentMarkerIcon),
                MobHuntMarkerIconId => nameof(MobHuntMarkerIcon),
                DefaultQuestMarkerIconId => nameof(DefaultQuestMarkerIcon),
                //QuestDefaultMarkerIconId => nameof(QuestDefaultMarkerIcon),
                //DebugMarkerIconId => nameof(DebugMarkerIcon),
                _ => _questMarkerIconMap.ContainsKey(iconId) ? $"QuestMarkerIcon_{iconId}" : "(UnknownIcon)",
            };
            //Plugin.ShowError($"Plugin encountered an error: Failed to load icon",
            //    $"Failed to load icon: {name}, IconId = {iconId}");
            Plugin.LogError($"Failed to load icon: {name}, IconId = {iconId}");
        }
    }
}
