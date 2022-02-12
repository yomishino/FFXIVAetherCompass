using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

using TextureWrap = ImGuiScene.TextureWrap;

namespace AetherCompass.UI.GUI
{
    public sealed class IconManager : IDisposable
    {
        private readonly ConcurrentIconMap iconMap = new();


        public const uint AltitudeHigherIconId = 60954;
        internal TextureWrap? AltitudeHigherIcon => iconMap[AltitudeHigherIconId];
        public const uint AltitudeLowerIconId = 60955;
        internal TextureWrap? AltitudeLowerIcon => iconMap[AltitudeLowerIconId];
        public static readonly Vector2 AltitudeIconSize = new(45, 45);

        // NaviMap thing with those quests/fate etc. direction markers are in 10001400
        // but use something else for easier work:
        // 60541 up, 60545 down; there are also two sets that are smaller
        public const uint DirectionScreenIndicatorIconId = 60541;
        internal TextureWrap? DirectionScreenIndicatorIcon => iconMap[DirectionScreenIndicatorIconId];
        public static readonly Vector2 DirectionScreenIndicatorIconSize = new(45, 45);
        public static readonly uint DirectionScreenIndicatorIconColour = ImGuiNET.ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 0, 1));


        public static readonly Vector2 MarkerIconSize = new(30, 30);

        public const uint ConfigDummyMarkerIconId = 25948;
        internal TextureWrap? ConfigDummyMarkerIcon => iconMap[ConfigDummyMarkerIconId];
        internal TextureWrap? DebugMarkerIcon => ConfigDummyMarkerIcon;

        public const uint AetherCurrentMarkerIconId = 60033;
        internal TextureWrap? AetherCurrentMarkerIcon => iconMap[AetherCurrentMarkerIconId];

        public const uint MobHuntMarkerIconId = 61710;
        internal TextureWrap? MobHuntMarkerIcon => iconMap[MobHuntMarkerIconId];

        private static readonly HashSet<uint> gatheringMarkerIconIds = new();
        internal TextureWrap? GetGatheringMarkerIcon(uint iconId)
        {
            gatheringMarkerIconIds.Add(iconId);
            return iconMap[iconId];
        }

        // NPC AnnounceIcon starts from 71200; see lumina sheet EventIconType
        // For types whose IconRange is 6, the 3rd is in-progress and 5th is last seq (checkmark icon),
        // because +0 is the dummy, so 1st icon in the range would start from +1.
        // Each type has availabled and locked ver, but rn idk how to accurately tell if a quest is avail or locked
        public const uint DefaultQuestMarkerIconId = 71223;
        internal TextureWrap? DefaultQuestMarkerIcon => iconMap[DefaultQuestMarkerIconId];

        private readonly HashSet<uint> questMarkerIconIds = new();

        internal TextureWrap? GetQuestMarkerIcon(uint iconId)
        {
            questMarkerIconIds.Add(iconId);
            return iconMap[iconId];
        }

        internal TextureWrap? GetQuestMarkerIcon(uint baseIconId, byte iconRange, bool questLastSeq = false)
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

        internal void DisposeAetherCurrentCompassIcons()
        {
            iconMap.Remove(AetherCurrentMarkerIconId);
        }

        internal void DisposeMobHuntCompassIcons()
        {
            iconMap.Remove(MobHuntMarkerIconId);
        }

        internal void DisposeGatheringPointCompassIcons()
        {
            foreach (uint id in gatheringMarkerIconIds)
                iconMap.Remove(id);
        }

        internal void DisposeQuestCompassIcons()
        {
            iconMap.Remove(DefaultQuestMarkerIconId);
            foreach (uint id in questMarkerIconIds)
                iconMap.Remove(id);
        }

        public void DisposeAllIcons() => iconMap.Clear();

        public void Dispose() => DisposeAllIcons();

        #endregion
    }



    class ConcurrentIconMap : IDisposable
    {
        private readonly ConcurrentDictionary<uint, Lazy<TextureWrap?>> map = new();

        public TextureWrap? this[uint iconId]
        {
            get
            {
                if (map.TryGetValue(iconId, out var tex)) 
                    return tex.Value;
                LoadIconAsync(iconId);
                return null;
            }
        }

        public bool Remove(uint id)
        {
            var removed = map.TryRemove(id, out var tex);
            tex?.Value?.Dispose();
            return removed;
        }

        public void Clear()
        {
            foreach (var tex in map.Values) tex?.Value?.Dispose();
            map.Clear();
        }

        private async void LoadIconAsync(uint iconId)
        {
            var icon = await Task.Run(() => Plugin.DataManager.GetImGuiTextureIcon(iconId));
            if (icon == null) throw new IconLoadFailException(iconId);
            map.TryAdd(iconId, new(() => icon, 
                System.Threading.LazyThreadSafetyMode.ExecutionAndPublication));
        }

        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Clear();
                }
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }


    public class IconLoadFailException : Exception
    {
        public readonly uint IconId;

        public IconLoadFailException(uint iconId) : base($"Failed to load icon: {iconId}")
            => IconId = iconId;
    }
}
