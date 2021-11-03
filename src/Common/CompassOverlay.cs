using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace AetherCompass.Common
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
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0), ImGuiCond.Always);
            ImGui.SetNextWindowSize(ImGuiHelpers.MainViewport.Size);
            ImGui.Begin("AetherCompassOverlay", winFlags);
            foreach (Action a in drawActions)
                a.Invoke();
            ImGui.End();
            drawActions.Clear();
        }

        public bool RegisterDrawAction(Action a)
        {
            if (a == null) return false;
            drawActions.Enqueue(a);
            return true;
        }

        
    }
}
