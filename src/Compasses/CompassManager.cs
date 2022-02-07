using AetherCompass.Configs;
using AetherCompass.Game;
using AetherCompass.UI;
using AetherCompass.UI.GUI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using System.Collections.Generic;
using ObjectInfo = FFXIVClientStructs.FFXIV.Client.UI.UI3DModule.ObjectInfo;


namespace AetherCompass.Compasses
{
    public unsafe sealed class CompassManager
    {
        private readonly HashSet<Compass> compasses = new();
        private readonly HashSet<Compass> workingCompasses = new();
        private readonly CompassOverlay overlay = null!;
        private readonly CompassDetailsWindow detailsWindow = null!;
        private readonly PluginConfig config = null!;


        private unsafe static UI3DModule* UI3DModule => ((UIModule*)Plugin.GameGui.GetUIModule())->GetUI3DModule();

        // Those that would be rendered on screen
        private unsafe static ObjectInfo** SortedObjectInfoPointerArray
            => UI3DModule != null ? (ObjectInfo**)UI3DModule->SortedObjectInfoPointerArray : null;
        private unsafe static int SortedObjectInfoCount => UI3DModule != null ? UI3DModule->SortedObjectInfoCount : 0;

#if DEBUG
        private unsafe static readonly GameObjectManager* gameObjMgr = GameObjectManager.Instance();
#endif

        public CompassManager(CompassOverlay overlay, CompassDetailsWindow window, PluginConfig config)
        {
            this.overlay = overlay;
            this.detailsWindow = window;
            this.config = config;
        }

        public bool AddCompass(Compass c)
        {
            if (!compasses.Add(c)) return false;
            if (!detailsWindow.RegisterCompass(c)) return false;
            if (c.IsEnabledInCurrentTerritory())
                workingCompasses.Add(c);
            return true;
        }

        public bool RemoveCompass(Compass c)
        {
            if (!compasses.Contains(c)) return false;
            detailsWindow.UnregisterCompass(c);
            workingCompasses.Remove(c);
            return compasses.Remove(c);
        }

        public void OnTick()
        {
            overlay.Clear();
            detailsWindow.Clear();

            foreach (var compass in workingCompasses)
                if (compass.CompassEnabled) compass.OnLoopStart();
            
            void* array;
            int count;
#if DEBUG
            if (gameObjMgr == null) return;
            array = config.DebugTestAllGameObjects ? gameObjMgr->ObjectListFiltered : SortedObjectInfoPointerArray;
            count = config.DebugTestAllGameObjects ? gameObjMgr->ObjectListFilteredCount : SortedObjectInfoCount;
#else
                array = SortedObjectInfoPointerArray;
                count = SortedObjectInfoCount;
#endif
            if (array == null) return;
            for (int i = 0; i < count; i++)
            {
                GameObject* obj;
                var info = ((ObjectInfo**)array)[i];
#if DEBUG
                if (config.DebugTestAllGameObjects)
                    obj = ((GameObject**)array)[i];
                else
#endif
                    obj = info != null ? info->GameObject : null;
                if (obj == null) continue;
                if (obj->ObjectKind == (byte)ObjectKind.Pc
#if DEBUG
                        && !config.DebugTestAllGameObjects
#endif
                        ) continue;

                CachedCompassObjective objective = new(null);
                foreach (var compass in workingCompasses)
                {
                    if (!compass.CompassEnabled) continue;
                    if (!compass.IsObjective(obj)) continue;
                    if (objective.GameObject != obj) objective = new(obj);
                    
                    compass.UpdateClosestObjective(objective);
                    if (compass.ShowDetail)
                    {
                        var action = compass.CreateDrawDetailsAction(objective);
                        if (action != null)
                            detailsWindow.AddDrawAction(compass, action);
                    }
                    if (compass.MarkScreen)
                    {
                        var action = compass.CreateMarkScreenAction(objective);
                        if (action != null)
                            overlay.AddDrawAction(action);
                    }
                    if (compass.HasFlagToProcess)
                    {
                        // NOTE: Dirty fix
                        // Currently Dalamud's MapLinkPayload internally does not take into account Map's X/Y-offset,
                        // so in map with non-zero offsets (e.g., Mist subdivision) it's always incorrect.
                        // Tweak it with a FixedMapLinkPayload that has the original raw X/Y
                        // but our calcualted map coord to fix this issue.
                        var terrId = Plugin.ClientState.TerritoryType;
                        //var maplink = new MapLinkPayload(terrId, ZoneWatcher.CurrentMapId, 
                        //    compass.FlaggedMapCoord.X, compass.FlaggedMapCoord.Y, fudgeFactor: 0.01f);
                        var map = ZoneWatcher.CurrentMap;
                        if (map != null)
                        {
                            var fixedMapLink = Common.FixedMapLinkPayload.FromMapCoord(terrId, ZoneWatcher.CurrentMapId,
                                compass.FlaggedMapCoord.X, compass.FlaggedMapCoord.Y, map.SizeFactor, map.OffsetX, map.OffsetY);
#if DEBUG
                            Plugin.LogDebug($"Create MapLinkPayload from {compass.FlaggedMapCoord}: {fixedMapLink}");
#endif
                            //if (Plugin.GameGui.OpenMapWithMapLink(maplinkFix))
                            //{
                            //    var msg = Chat.CreateMapLink(terrId, ZoneWatcher.CurrentMapId, maplink.XCoord, maplink.YCoord).PrependText("Flag set: ");
                            //    Chat.PrintChat(msg);
                            //    compass.HasFlagToProcess = false;
                            //}
                            if (Plugin.GameGui.OpenMapWithMapLink(fixedMapLink))
                            {
                                var msg = Chat.CreateMapLink(fixedMapLink).PrependText("Flag set: ");
                                Chat.PrintChat(msg);
                                compass.HasFlagToProcess = false;
                            }
                        }
                    }
                }
            }
            foreach (var compass in workingCompasses)
                if (compass.CompassEnabled) compass.OnLoopEnd();
        }

        public void OnZoneChange()
        {
            workingCompasses.Clear();
            foreach (var compass in compasses)
            {
                compass.OnZoneChange();
                if (compass.IsEnabledInCurrentTerritory())
                    workingCompasses.Add(compass);
            }
        }

        public void DrawCompassConfigUi()
        {
            foreach (var compass in compasses)
            {
                compass.DrawConfigUi();
                ImGuiNET.ImGui.NewLine();
            }
        }
    }
}
