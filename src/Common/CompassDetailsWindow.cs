using AetherCompass.Compasses;
using System;
using System.Collections.Generic;
using ImGuiNET;


namespace AetherCompass.Common
{
    public class CompassDetailsWindow
    {
        private readonly Dictionary<Compass, Queue<Action>> drawActions = new();

        public bool Visible { get; internal set; }

        public bool RegisterCompass(Compass c)
            => drawActions.TryAdd(c, new());

        public bool UnregisterCompass(Compass c)
            => drawActions.Remove(c);

        public bool RegisterDrawAction(Compass c, Action a)
        {
            if (!Visible) return false;
            if (!drawActions.TryGetValue(c, out var queue))
                return false;
            queue.Enqueue(a);
            return true;
        }

        public void Draw()
        {
            if (!Visible) return;
            var map = CompassUtil.GetCurrentMap();
            if (map == null)
            {
                foreach (var queue in drawActions.Values)
                    queue.Clear();
                return;
            }
            ImGui.Begin("AetherCompass: Compasses");
            ImGui.Text($"Current Map: {CompassUtil.GetPlaceNameToString(map.PlaceNameRegion.Row, "-")}" +
                $" > {CompassUtil.GetPlaceNameToString(map.PlaceName.Row, "-")}" +
                $" > {CompassUtil.GetPlaceNameToString(map.PlaceNameSub.Row, "-")} ");
#if DEBUG
            ImGui.Text($"Map data: SizeFactor={map.SizeFactor}, OffsetX={map.OffsetX}, OffsetY={map.OffsetY}");
            ImGui.Text($"Main Viewport: pos={Dalamud.Interface.ImGuiHelpers.MainViewport.Pos}, " +
                $"size={Dalamud.Interface.ImGuiHelpers.MainViewport.Size}, dpi={Dalamud.Interface.ImGuiHelpers.MainViewport.DpiScale}");
#endif
            if (ImGui.BeginTabBar("CompassesTabBar", ImGuiTabBarFlags.Reorderable))
            {
                foreach (Compass c in drawActions.Keys)
                {
                    if (c.CompassEnabled && c.DrawDetailsEnabled)
                    {
                        var name = c.GetType().Name;
                        name = name.Substring(0, name.Length - "Compass".Length);
                        if (ImGui.BeginTabItem(name))
                        {
                            foreach (Action a in drawActions[c])
                                a.Invoke();
                            ImGui.EndTabItem();
                        }
                        drawActions[c].Clear();
                    }
                }
                ImGui.EndTabBar();
            }
            ImGui.End();
        }

    }
}
