using AetherCompass.Compasses;
using AetherCompass.Common;
using ImGuiNET;
using System;
using System.Collections.Generic;


namespace AetherCompass.UI.GUI
{
    public class CompassDetailsWindow
    {
        private readonly Dictionary<Compass, ActionQueue> drawActions = new();


        public bool RegisterCompass(Compass c)
            => drawActions.TryAdd(c, new(50));

        public bool UnregisterCompass(Compass c)
            => drawActions.Remove(c);

        public bool RegisterDrawAction(Compass c, Action? a, bool dequeueOldIfFull = false)
        {
            if (a == null) return false;
            if (!drawActions.TryGetValue(c, out var queue))
                return false;
            queue.QueueAction(a, dequeueOldIfFull);
            return true;
        }

        public void Draw()
        {
            var map = CompassUtil.GetCurrentMap();
            if (map == null)
            {
                foreach (var queue in drawActions.Values)
                    queue.Clear();
                return;
            }

            if (ImGui.Begin("AetherCompass: Detected Objects' Details"))
            {
                var regionName = CompassUtil.GetPlaceNameToString(map.PlaceNameRegion.Row);
                var placeName = CompassUtil.GetPlaceNameToString(map.PlaceName.Row);
                var subName = CompassUtil.GetPlaceNameToString(map.PlaceNameSub.Row);
                var mapName = regionName;
                if (!string.IsNullOrEmpty(mapName) && !string.IsNullOrEmpty(placeName))
                    mapName += " > " + placeName;
                else if (!string.IsNullOrEmpty(placeName))
                    mapName = placeName;
                if (!string.IsNullOrEmpty(mapName) && !string.IsNullOrEmpty(subName))
                    mapName += " > " + subName;
                ImGui.TextWrapped($"Current Map:  {mapName}");
#if DEBUG
                //ImGui.Text($"Territory: {Plugin.ClientState.TerritoryType}; LocalContentId: {Plugin.ClientState.LocalContentId}");
                ImGui.BulletText($"Map data: SizeFactor={map.SizeFactor}, OffsetX={map.OffsetX}, OffsetY={map.OffsetY}");
                ImGui.BulletText($"Main Viewport: pos={Dalamud.Interface.ImGuiHelpers.MainViewport.Pos}, " +
                    $"size={Dalamud.Interface.ImGuiHelpers.MainViewport.Size}, dpi={Dalamud.Interface.ImGuiHelpers.MainViewport.DpiScale}");
#endif
                if (ImGui.BeginTabBar("CompassesTabBar", ImGuiTabBarFlags.Reorderable))
                {
                    foreach (Compass c in drawActions.Keys)
                    {
                        if (c.CompassEnabled && c.ShowDetail)
                        {
                            var name = c.GetType().Name;
                            name = name.Substring(0, name.Length - "Compass".Length);
                            if (ImGui.BeginTabItem(name))
                            {
                                drawActions[c].DoAll();
                                ImGui.EndTabItem();
                            }
                        }
                    }
                    ImGui.EndTabBar();
                }
                ImGui.End();
            }
        }

        public void Clear()
        {
            foreach (var q in drawActions.Values)
                q.Clear();
        }

    }
}
