using AetherCompass.Common;
using AetherCompass.Common.Attributes;
using AetherCompass.Compasses.Objectives;
using AetherCompass.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;


namespace AetherCompass.Compasses
{
    [CompassType(CompassType.Standard)]
    public class AetherCurrentCompass : Compass
    {
        public override string CompassName => "Aether Current Compass";
        public override string Description => "Detecting Aether Currents nearby.";

        private protected override CompassConfig CompassConfig => Plugin.Config.AetherCurrentConfig;

        private static System.Numerics.Vector4 infoTextColour = new(.8f, .95f, .75f, 1);
        private const float infoTextShadowLightness = .1f;

        private const uint aetherCurrentMarkerIconId = 60033;

        
        public override bool IsEnabledInCurrentTerritory()
            => ZoneWatcher.CurrentTerritoryType?.TerritoryIntendedUse == 1; // mostly normal wild field


        private protected override unsafe string GetClosestObjectiveDescription(CachedCompassObjective _)
            => "Aether Current";

        public override unsafe DrawAction? CreateDrawDetailsAction(CachedCompassObjective objective)
        {
            if (objective.IsEmpty()) return null;
            return new(() =>
            {
                ImGui.Text($"{objective.Name}");
                ImGui.BulletText($"{CompassUtil.MapCoordToFormattedString(objective.CurrentMapCoord)} (approx.)");
                ImGui.BulletText($"{objective.CompassDirectionFromPlayer},  " +
                    $"{CompassUtil.DistanceToDescriptiveString(objective.Distance3D, false)}");
                ImGui.BulletText(CompassUtil.AltitudeDiffToDescriptiveString(objective.AltitudeDiff));
                DrawFlagButton($"{(long)objective.GameObject}", objective.CurrentMapCoord);
                ImGui.Separator();
            });
        }

        public override unsafe DrawAction? CreateMarkScreenAction(CachedCompassObjective objective)
        {
            if (objective.IsEmpty()) return null;
            return GenerateDefaultScreenMarkerDrawAction(objective,
                aetherCurrentMarkerIconId, DefaultMarkerIconSize, .9f,
                $"{objective.Name}, {CompassUtil.DistanceToDescriptiveString(objective.Distance3D, true)}",
                infoTextColour, infoTextShadowLightness, out _, important: objective.Distance3D < 60);
        }

        public override unsafe bool IsObjective(GameObject* o)
        {
            if (o == null) return false;
            if (o->ObjectKind != (byte)ObjectKind.EventObj) return false;
            return IsNameOfAetherCurrent(CompassUtil.GetName(o));
        }

        private static bool IsNameOfAetherCurrent(string? name)
        {
            if (name == null) return false;
            name = name.ToLower();
            return name == "aether current"
                || name == "風脈の泉"
                || name == "windätherquelle"
                || name == "vent éthéré"
                ;
        }
    }
}
