using Dalamud.Game.ClientState.Objects.Types;


namespace AetherCompass.Compasses
{
    public abstract class Compass
    {
        public abstract bool IsObjective(GameObject? o);
        public abstract void ShowObjectiveDetails(GameObject? o);
        public abstract void DrawObjectiveOnMiniMap(GameObject? o);
        public abstract void DrawObjectiveOnMap(GameObject? o);
        public abstract void DrawObjectiveOnScreen(GameObject? o);
    }
}
