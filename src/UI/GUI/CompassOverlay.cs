using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Collections.Generic;


namespace AetherCompass.UI.GUI
{
    public class CompassOverlay
    {
        private readonly Queue<Action> drawActions = new();

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
            while (drawActions.TryDequeue(out Action? a))
                a?.Invoke();
            ImGui.End();
        }

        public bool RegisterDrawAction(Action? a)
        {
            if (a == null) return false;
            // TEMP:
            while (drawActions.Count > 100) drawActions.Dequeue();
            drawActions.Enqueue(a);
            return true;
        }

        public void Clear()
        {
            drawActions.Clear();
        }

    }
}
