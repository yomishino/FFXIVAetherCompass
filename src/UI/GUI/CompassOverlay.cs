using AetherCompass.Common;
using Dalamud.Interface;
using ImGuiNET;


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

        public bool AddDrawAction(Action? a, bool important = false)
            => a != null && drawActions.QueueAction(a, important);

        public bool AddDrawAction(DrawAction? a)
            => a != null && drawActions.QueueAction(a);

        public void Clear() => drawActions.Clear();

    }
}
