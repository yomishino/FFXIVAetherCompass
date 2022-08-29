using System.Collections.Concurrent;
using System.Numerics;
using System.Threading.Tasks;

using TextureWrap = ImGuiScene.TextureWrap;

namespace AetherCompass.UI.GUI
{
    public sealed class IconManager : IDisposable
    {
        private readonly ConcurrentIconMap iconMap = new();

        public TextureWrap? GetIcon(uint iconId) => iconMap[iconId];

        public void DisposeIcons(HashSet<uint> iconIds)
        {
            foreach (var id in iconIds)
                iconMap.Remove(id);
        }
        
        public void DisposeAllIcons() => iconMap.Clear();
        public void Dispose() => DisposeAllIcons();


        #region Common Icons

        public const uint DefaultMarkerIconId = 25948;
        internal TextureWrap? DefaultMarkerIcon => iconMap[DefaultMarkerIconId];

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

        public const uint ConfigDummyMarkerIconId = DefaultMarkerIconId;
        internal TextureWrap? ConfigDummyMarkerIcon => iconMap[ConfigDummyMarkerIconId];

        //private static void DisposeCommonIcons()
        //{
        //    iconMap.Remove(AltitudeHigherIconId);
        //    iconMap.Remove(AltitudeLowerIconId);
        //    iconMap.Remove(DirectionScreenIndicatorIconId);
        //    iconMap.Remove(ConfigDummyMarkerIconId);
        //}

        #endregion
    }



    class ConcurrentIconMap : IDisposable
    {
        private readonly ConcurrentDictionary<uint, Lazy<TextureWrap?>> map = new();

        public TextureWrap? this[uint iconId]
        {
            get
            {
                if (iconId == 0) return null;
                if (map.TryGetValue(iconId, out var tex)) 
                    return tex.Value;
                LoadIconAsync(iconId);
                return null;
            }
        }

        public bool Remove(uint id)
        {
            if (id == 0) return false;
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
