using AetherCompass.Configs;
using AetherCompass.Common;
using AetherCompass.UI;
using AetherCompass.UI.GUI;
using Dalamud.Game.Text.SeStringHandling.Payloads;
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
            if (c.IsEnabledTerritory(Plugin.ClientState.TerritoryType))
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
            foreach (var compass in workingCompasses)
                compass.OnLoopStart();

            var map = CompassUtil.GetCurrentMap();
            if (map == null) return;

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

                foreach (var compass in workingCompasses)
                {
                    if (!compass.CompassEnabled) continue;
                    if (!compass.CheckObject(obj)) continue;
                    if (compass.ShowDetail)
                    {
                        var action = compass.CreateDrawDetailsAction(obj);
                        if (action != null)
                            detailsWindow.AddDrawAction(compass, action);
                    }
                    if (compass.MarkScreen)
                    {
                        var action = compass.CreateMarkScreenAction(obj);
                        if (action != null)
                            overlay.AddDrawAction(action);
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
                            var msg = Chat.CreateMapLink(terrId, map.RowId, maplink.XCoord, maplink.YCoord).PrependText("Flag set: ");
                            Chat.PrintChat(msg);
                            compass.HasFlagToProcess = false;
                        }
                    }
                }
            }
            foreach (var compass in workingCompasses)
                compass.OnLoopEnd();
        }

        public void OnZoneChange()
        {
            workingCompasses.Clear();
            var terr = Plugin.ClientState.TerritoryType;
            foreach (var compass in compasses)
            {
                compass.OnZoneChange();
                if (compass.IsEnabledTerritory(terr))
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
