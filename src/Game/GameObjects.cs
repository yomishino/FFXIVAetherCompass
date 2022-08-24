using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using static FFXIVClientStructs.FFXIV.Client.UI.UI3DModule;

namespace AetherCompass.Game
{
    internal unsafe static class GameObjects
    {
        private unsafe static readonly UI3DModule* UI3DModule 
            = ((UIModule*)Plugin.GameGui.GetUIModule())->GetUI3DModule();

        // [!] Fix offsets; Object array now is of size 596
        // Array offset = 0xDFA0, count offset = 0xF240;

        // Those that would be rendered on screen
        internal unsafe static ObjectInfo** SortedObjectInfoPointerArray
            => UI3DModule != null
            //? (ObjectInfo**)UI3DModule->SortedObjectInfoPointerArray : null;
            ? (ObjectInfo**)((IntPtr)UI3DModule + 0xDFA0) : null;
        internal unsafe static int SortedObjectInfoCount 
            //=> UI3DModule != null ? UI3DModule->SortedObjectInfoCount : 0;
            => UI3DModule != null ? *(int*)((IntPtr)UI3DModule + 0xF240) : 0;

#if DEBUG
        private unsafe static readonly GameObjectManager* gameObjMgr 
            = GameObjectManager.Instance();
        internal unsafe static GameObject* ObjectListFiltered
            => (GameObject*)gameObjMgr->ObjectListFiltered;
        internal unsafe static int ObjectListFilteredCount
            => gameObjMgr->ObjectListFilteredCount;

        static GameObjects()
        {
            Plugin.LogDebug($"UI3DModule @{(IntPtr)UI3DModule:X}");
            Plugin.LogDebug($"SortedObjectInfoPointerArray @{(IntPtr)UI3DModule->SortedObjectInfoPointerArray:X}");
            Plugin.LogDebug($"SortedObjectInfoCount = {SortedObjectInfoCount}");
        }
#endif

    }
}
