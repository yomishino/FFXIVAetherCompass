using AetherCompass.Common;
using AetherCompass.Common.Attributes;
using AetherCompass.Game;
using AetherCompass.UI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using ObjectInfo = FFXIVClientStructs.FFXIV.Client.UI.UI3DModule.ObjectInfo;


namespace AetherCompass.Compasses
{
    public unsafe sealed class CompassManager
    {
        private readonly HashSet<Compass> standardCompasses = new();
        private readonly HashSet<Compass> experimentalCompasses = new();
#if DEBUG
        private readonly HashSet<Compass> debugCompasses = new();
#endif

        private readonly SortedSet<Compass> allAddedCompasses 
            = new(new CompassSortComp());
        private readonly List<Compass> workingCompasses = new();
        
        private bool hasMapFlagToProcess;
        private System.Numerics.Vector2 mapFlagCoord;


        public void Init()
        {
            foreach (var t in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
            {
                if (t.IsSubclassOf(typeof(Compass)) && !t.IsAbstract)
                {
                    var ctor = t.GetConstructor(Type.EmptyTypes);
                    if (ctor != null) AddCompass((Compass)ctor.Invoke(null));
                }
            }
        }

        public bool AddCompass(Compass c)
        {
            var added = false;
            switch (c.CompassType)
            {
                case CompassType.Standard:
                    added = standardCompasses.Add(c);
                    break;
                case CompassType.Experimental:
                    added = experimentalCompasses.Add(c);
                    break;
                case CompassType.Debug:
# if DEBUG
                    added = debugCompasses.Add(c);
#endif
                    break;
                default:
                    LogError($"Failed to enable compass {c.GetType().Name}: no valid compass type");
                    return false;
            }
            if (added)
            {
                allAddedCompasses.Add(c);
                Plugin.DetailsWindow.RegisterCompass(c);
                if (c.IsEnabledInCurrentTerritory())
                    workingCompasses.Add(c);
            }
            return true;
        }

        public void RemoveCompass(Compass c)
        {
            c.Reset();
            Plugin.DetailsWindow.UnregisterCompass(c);
            workingCompasses.Remove(c);
            switch (c.CompassType)
            {
                case CompassType.Standard:
                    standardCompasses.Remove(c);
                    break;
                case CompassType.Experimental:
                    experimentalCompasses.Remove(c);
                    break;
#if DEBUG
                case CompassType.Debug:
                    debugCompasses.Remove(c);
                    break;
#endif
                default: break;
            };
            allAddedCompasses.Remove(c);
        }


        public void RegisterMapFlag(System.Numerics.Vector2 flagCoord)
        {
            hasMapFlagToProcess = true;
            mapFlagCoord = flagCoord;
        }

        public void OnTick()
        {
            Plugin.Overlay.Clear();
            Plugin.DetailsWindow.Clear();

            if (workingCompasses.Count > 0)
            {
                foreach (var compass in workingCompasses)
                    if (compass.CompassEnabled) compass.CancelLastUpdate();

#if DEBUG
                var debugTestAll = Plugin.Config.DebugTestAllGameObjects;
                void* array = debugTestAll 
                    ? GameObjects.ObjectListFiltered 
                    : GameObjects.SortedObjectInfoPointerArray;
                int count = debugTestAll 
                    ? GameObjects.ObjectListFilteredCount 
                    : GameObjects.SortedObjectInfoCount;
#else
                var array = GameObjects.SortedObjectInfoPointerArray;
                var count = GameObjects.SortedObjectInfoCount;
#endif

                if (array == null) return;

                foreach (var compass in workingCompasses)
                {
                    if (compass.CompassEnabled)
                    {
#if DEBUG
                        if (debugTestAll)
                            compass.ProcessLoopDebugAllObjects((GameObject**)array, count);
                        else
#endif
                            compass.ProcessLoop((ObjectInfo**)array, count);
                    }

                }
            }

            ProcessFlagOnTickEnd();
        }

        private void ProcessFlagOnTickEnd()
        {
            if (!hasMapFlagToProcess) return;

            // NOTE: Dirty fix
            // Currently Dalamud's MapLinkPayload internally does not take into account Map's X/Y-offset,
            // so in map with non-zero offsets (e.g., Mist subdivision) it's always incorrect.
            // Tweak it with a FixedMapLinkPayload that has the original raw X/Y
            // but our calcualted map coord to fix this issue.
            var terrId = Plugin.ClientState.TerritoryType;
            //var maplink = new MapLinkPayload(terrId, ZoneWatcher.CurrentMapId, 
            //    mapFlagCoord.X, mapFlagCoord.Y, fudgeFactor: 0.01f);
            var map = ZoneWatcher.CurrentMap;
            if (map != null)
            {
                var fixedMapLink = FixedMapLinkPayload.FromMapCoord(terrId, ZoneWatcher.CurrentMapId,
                    mapFlagCoord.X, mapFlagCoord.Y, map.SizeFactor, map.OffsetX, map.OffsetY);
#if DEBUG
                LogDebug($"Create MapLinkPayload from {mapFlagCoord}: {fixedMapLink}");
#endif
                //if (Plugin.GameGui.OpenMapWithMapLink(maplinkFix))
                //{
                //    var msg = Chat.CreateMapLink(terrId, ZoneWatcher.CurrentMapId, maplink.XCoord, maplink.YCoord).PrependText("Flag set: ");
                //    Chat.PrintChat(msg);
                //    hasMapFlagToProcess = false;
                //}
                if (Plugin.GameGui.OpenMapWithMapLink(fixedMapLink))
                {
                    var msg = Chat.CreateMapLink(fixedMapLink).PrependText("Flag set: ");
                    Chat.PrintChat(msg);
                }
            }

            hasMapFlagToProcess = false;
        }

        public void OnZoneChange()
        {
            try
            {
                workingCompasses.Clear();
                foreach (var compass in allAddedCompasses)
                {
                    if (compass.IsEnabledInCurrentTerritory())
                        workingCompasses.Add(compass);
                    compass.OnZoneChange();
                }
            } catch (ObjectDisposedException) { }
        }

        public void DrawCompassConfigUi()
        {
            foreach (var compass in allAddedCompasses)
            {
                compass.DrawConfigUi();
                ImGui.NewLine();
            }
        }


        private class CompassSortComp : IComparer<Compass>
        {
            public int Compare(Compass? x, Compass? y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return int.MinValue;
                if (y == null) return int.MaxValue;
                var ret = x.CompassType.CompareTo(y.CompassType);
                return ret != 0 ? ret
                    : x.GetType().Name.CompareTo(y.GetType().Name);
            }
        }
    }
}
