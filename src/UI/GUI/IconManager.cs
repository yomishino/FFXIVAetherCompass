using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

using TextureWrap = ImGuiScene.TextureWrap;

namespace AetherCompass.UI.GUI
{
    public sealed class IconManager : IDisposable
    {
        private static readonly IconMap iconMap = new();


        public const uint AltitudeHigherIconId = 60954;
        internal static TextureWrap? AltitudeHigherIcon => iconMap[AltitudeHigherIconId];
        public const uint AltitudeLowerIconId = 60955;
        internal static TextureWrap? AltitudeLowerIcon => iconMap[AltitudeLowerIconId];
        public static readonly Vector2 AltitudeIconSize = new(45, 45);

        // NaviMap thing with those quests/fate etc. direction markers are in 10001400
        // but use something else for easier work:
        // 60541 up, 60545 down; there are also two sets that are smaller
        public const uint DirectionScreenIndicatorIconId = 60541;
        internal static TextureWrap? DirectionScreenIndicatorIcon => iconMap[DirectionScreenIndicatorIconId];
        public static readonly Vector2 DirectionScreenIndicatorIconSize = new(45, 45);
        public static readonly uint DirectionScreenIndicatorIconColour = ImGuiNET.ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 1));


        public static readonly Vector2 MarkerIconSize = new(30, 30);

        public const uint ConfigDummyMarkerIconId = 25948;
        internal static TextureWrap? ConfigDummyMarkerIcon => iconMap[ConfigDummyMarkerIconId];
        internal static TextureWrap? DebugMarkerIcon => ConfigDummyMarkerIcon;

        public const uint AetherCurrentMarkerIconId = 60033;
        internal static TextureWrap? AetherCurrentMarkerIcon => iconMap[AetherCurrentMarkerIconId];

        public const uint MobHuntMarkerIconId = 61710;
        internal static TextureWrap? MobHuntMarkerIcon => iconMap[MobHuntMarkerIconId];

        private static readonly HashSet<uint> gatheringMarkerIconIds = new();
        internal static TextureWrap? GetGatheringMarkerIcon(uint iconId)
        {
            gatheringMarkerIconIds.Add(iconId);
            return iconMap[iconId];
        }

        // NPC AnnounceIcon starts from 71200; see lumina sheet EventIconType
        // For types whose IconRange is 6, the 3rd is in-progress and 5th is last seq (checkmark icon),
        // because +0 is the dummy, so 1st icon in the range would start from +1.
        // Each type has availabled and locked ver, but rn idk how to accurately tell if a quest is avail or locked
        public const uint DefaultQuestMarkerIconId = 71223;
        internal static TextureWrap? DefaultQuestMarkerIcon => iconMap[DefaultQuestMarkerIconId];

        private static readonly HashSet<uint> questMarkerIconIds = new();

        internal static TextureWrap? GetQuestMarkerIcon(uint iconId)
        {
            questMarkerIconIds.Add(iconId);
            return iconMap[iconId];
        }

        internal static TextureWrap? GetQuestMarkerIcon(uint baseIconId, byte iconRange, bool questLastSeq = false)
            => GetQuestMarkerIcon(GetQuestMarkerIconId(baseIconId, iconRange, questLastSeq));

        private static uint GetQuestMarkerIconId(uint baseIconId, byte iconRange, bool questLastSeq = false)
            => baseIconId + iconRange switch
            {
                6 => questLastSeq ? 5u : 3u,
                1 => 0,
                _ => 1,
            };



        #region Dispose

        //private static void DisposeCommonIcons()
        //{
        //    iconMap.Remove(AltitudeHigherIconId);
        //    iconMap.Remove(AltitudeLowerIconId);
        //    iconMap.Remove(DirectionScreenIndicatorIconId);
        //    iconMap.Remove(ConfigDummyMarkerIconId);
        //}

        internal static void DisposeAetherCurrentCompassIcons()
        {
            iconMap.Remove(AetherCurrentMarkerIconId);
        }

        internal static void DisposeMobHuntCompassIcons()
        {
            iconMap.Remove(MobHuntMarkerIconId);
        }

        internal static void DisposeGatheringPointCompassIcons()
        {
            foreach (uint id in gatheringMarkerIconIds)
                iconMap.Remove(id);
        }

        internal static void DisposeQuestCompassIcons()
        {
            iconMap.Remove(DefaultQuestMarkerIconId);
            foreach (uint id in questMarkerIconIds)
                iconMap.Remove(id);
        }

        public static void DisposeAllIcons() => iconMap.Clear();

        public void Dispose() => DisposeAllIcons();

        #endregion
    }


    class IconMap
    {
        private readonly Dictionary<uint, TextureWrap?> map = new();

        public TextureWrap? this[uint iconId]
        {
            get
            {
                if (map.TryGetValue(iconId, out var tex)) return tex;
                map.Add(iconId, null);
                LoadIconAsync(iconId);
                return null;
            }
            set
            {
                if (map.TryGetValue(iconId, out var tex) && tex != null) tex.Dispose();
                map.Add(iconId, value);
            }
        }

        public void Add(uint id, TextureWrap? tex) => map.Add(id, tex);

        public bool TryGet(uint id, out TextureWrap? tex) => map.TryGetValue(id, out tex);

        public bool Remove(uint id)
        {
            if (map.TryGetValue(id, out var tex) && tex != null) tex.Dispose();
            return map.Remove(id);
        }

        public void Clear()
        {
            foreach (var tex in map.Values) tex?.Dispose();
            map.Clear();
        }

        private async void LoadIconAsync(uint iconId)
        {
            var icon = await Task.Run(() => Plugin.DataManager.GetImGuiTextureIcon(iconId));
            if (icon == null) throw new IconLoadFailException(iconId);
            map[iconId] = icon;
        }
    }


    public class IconLoadFailException : Exception
    {
        public readonly uint IconId;

        public IconLoadFailException(uint iconId) : base($"Failed to load icon: {iconId}")
            => IconId = iconId;
    }
}
