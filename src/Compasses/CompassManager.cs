using AetherCompass.Common;
using AetherCompass.UI;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using ObjectInfo = FFXIVClientStructs.FFXIV.Client.UI.UI3DModule.ObjectInfo;


namespace AetherCompass.Compasses
{
    public unsafe sealed class CompassManager
    {
        private readonly HashSet<Compass> compasses = new();
        private readonly CompassOverlay overlay = null!;
        private readonly CompassDetailsWindow detailsWindow = null!;
        private readonly Configuration config = null!;

        private unsafe static readonly UI3DModule* UI3DModule = ((UIModule*)Plugin.GameGui.GetUIModule())->GetUI3DModule();

#if DEBUG
        public unsafe static ObjectInfo* ObjectInfoArray => UI3DModule != null ? (ObjectInfo*)UI3DModule->ObjectInfoArray : null;
        public const int ObjectInfoArraySize = 424;
#endif

        // Gives only the ones that would be on screen, so ruling out non-interactable ones
        private unsafe static ObjectInfo** SortedObjectInfoPointerArray 
            => UI3DModule != null ? (ObjectInfo**)UI3DModule->SortedObjectInfoPointerArray : null;
        private unsafe static int SortedObjectInfoCount => UI3DModule != null ? UI3DModule->SortedObjectInfoCount : 0;


        public CompassManager(CompassOverlay overlay, CompassDetailsWindow window, Configuration config)
        {
            this.overlay = overlay;
            this.detailsWindow = window;
            this.config = config;
        }

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
            var map = CompassUtil.GetCurrentMap();
            if (map == null) return;

            try
            {
                void* array;
                int count;
#if DEBUG
                array = config.DebugUseFullArray ? ObjectInfoArray : SortedObjectInfoPointerArray;
                count = config.DebugUseFullArray ? ObjectInfoArraySize : SortedObjectInfoCount;
#else
                array = SortedObjectInfoPointerArray;
                count = SortedObjectInfoCount;
#endif
                if (array == null) return;
                for (int i = 0; i < count; i++)
                {
                    var info =
#if DEBUG
                        config.DebugUseFullArray ? &((ObjectInfo*)array)[i] :
#endif
                        ((ObjectInfo**)array)[i];
                    if (info == null) continue;
                    foreach (var compass in compasses)
                    {
                        if (!compass.CompassEnabled) continue;
                        if (!compass.CheckObject(info->GameObject)) continue;
                        if (config.ShowDetailWindow && compass.DrawDetailsEnabled)
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
                            var terrId = CompassUtil.GetCurrentTerritoryType()!.RowId;
                            var maplink = new MapLinkPayload(terrId, map.RowId,
                                compass.FlaggedMapCoord.X, compass.FlaggedMapCoord.Y, fudgeFactor: 0.01f);
#if DEBUG
                            Plugin.LogDebug($"Create MapLinkPayload from {compass.FlaggedMapCoord}: {maplink}");
#endif
                            if (Plugin.GameGui.OpenMapWithMapLink(maplink))
                            {
                                var msg = Chat.CreateMapLink(terrId, map.RowId, maplink.XCoord, maplink.YCoord).PrependText("Flag set @");
                                Chat.PrintChat(msg);
                                compass.HasFlagToProcess = false;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.ShowError("Plugin encountered an error", e.ToString());
            }
            foreach (var compass in compasses)
                compass.OnLoopEnd();
        }

        public void OnZoneChange()
        {
            foreach (var compass in compasses)
                compass.OnZoneChange();
        }
    }
}
