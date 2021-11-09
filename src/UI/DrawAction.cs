using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using System;

namespace AetherCompass.UI
{
    public unsafe class DrawAction
    {
        private readonly GameObject* obj;
        private readonly Action<ImDrawListPtr> action;

        public unsafe DrawAction(GameObject* obj, Action<ImDrawListPtr> action)
        {
            this.obj = obj;
            this.action = action; 
        }

        public unsafe void Draw(ImDrawListPtr drawList)
        {
            // Check if null ptr
            if (obj == null) return;
            action.Invoke(drawList);
        }
    }
}
