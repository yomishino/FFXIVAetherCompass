using AetherCompass.Common;
using Dalamud.Interface;
using ImGuiNET;
using System;


namespace AetherCompass.UI.GUI
{
    public class CompassOverlay
    {
        private readonly ActionQueue drawActions = new(200);

        private static readonly ImGuiWindowFlags winFlags =
            ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoDocking 
            | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoInputs
            | ImGuiWindowFlags.NoBackground
            | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing;

        public void Draw()
        {
            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGui.SetNextWindowPos(ImGuiHelpers.MainViewport.Pos);
            ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);
            ImGui.Begin("AetherCompassOverlay", winFlags);
            drawActions.DoAll();
            ImGui.End();
        }

        public bool RegisterDrawAction(Action? a, bool dequeueOldIfFull = false)
            => a != null && drawActions.QueueAction(a, dequeueOldIfFull);

        public void Clear() => drawActions.Clear();

    }
}
