using AetherCompass.Common;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Numerics;
using ObjectInfo = FFXIVClientStructs.FFXIV.Client.UI.UI3DModule.ObjectInfo;

namespace AetherCompass.Compasses
{
    public unsafe sealed class CompassManager
    {
        private readonly HashSet<Compass> compasses = new();
        private readonly CompassOverlay overlay = new();
        private readonly CompassDetailsWindow detailsWindow = new();


        private unsafe static UI3DModule* UI3DModule = ((UIModule*)Plugin.GameGui.GetUIModule())->GetUI3DModule();
        //public unsafe static ObjectInfo* ObjectInfoArray => UI3DModule != null ? (ObjectInfo*)UI3DModule->ObjectInfoArray : null;
        //public const int ObjectInfoArraySize = 424;

        // Gives only the ones that would be on screen, so ruling out non-interactable ones
        private unsafe static ObjectInfo** SortedObjectInfoPointerArray 
            => UI3DModule != null ? (ObjectInfo**)UI3DModule->SortedObjectInfoPointerArray : null;
        private unsafe static int SortedObjectInfoCount => UI3DModule != null ? UI3DModule->SortedObjectInfoCount : 0;


        public bool AddCompass(Compass c)
        {
            if (!compasses.Add(c)) return false;
            if (c.DrawDetailsEnabled && !detailsWindow.RegisterCompass(c)) return false;
            return true;
        }

        public bool RemoveCompass(Compass c)
        {
            if (!compasses.Contains(c)) return false;
            detailsWindow.UnregisterCompass(c);
            return compasses.Remove(c);
        }

        public void OnTick()
        {
            var array = SortedObjectInfoPointerArray;
            if (array == null) return;

            var map = CompassUtil.GetCurrentMap();
            if (map == null) return;

            for (int i = 0; i < SortedObjectInfoCount; i++)
            {
                var info = array[i];
                if (info == null) continue;
                foreach (var compass in compasses)
                {
                    if (!compass.CompassEnabled) continue;
                    if (!compass.IsObjective(info->GameObject)) continue;
                    if (detailsWindow.Visible && compass.DrawDetailsEnabled)
                    {
                        var action = compass.CreateDrawDetailsAction(info);
                        if (action != null)
                            detailsWindow.RegisterDrawAction(compass, action);
                    }
                    if (compass.MarkScreenEnabled)
                    {
                        var action = compass.CreateMarkScreenAction(info);
                        if (action != null)
                            overlay.RegisterDrawAction(action);
                    }
                    if (compass.HasFlagToProcess)
                    {

                        var payload = new MapLinkPayload(CompassUtil.GetCurrentTerritoryType()!.RowId,
                            map.RowId, compass.FlaggedMapCoord.X, compass.FlaggedMapCoord.Y, fudgeFactor: 0.01f);
#if DEBUG
                        Plugin.LogDebug($"Create MapLinkPayload from {compass.FlaggedMapCoord}: {payload}");
#endif
                        Plugin.GameGui.OpenMapWithMapLink(payload);
                        compass.HasFlagToProcess = false;
                    }
                }
            }

            overlay.Draw();
            detailsWindow.Draw();
        }
    }
}
