using AetherCompass.Common;
using AetherCompass.Common.Attributes;
using AetherCompass.Compasses.Objectives;
using AetherCompass.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;


namespace AetherCompass.Compasses
{
    [CompassType(CompassType.Experimental)]
    public class EurekanCompass : Compass
    {
        public override string CompassName => "Eureka Elemental Compass";
        public override string Description => "Detecting nearby Eureka Elementals. (By apetih.)";

        private protected override CompassConfig CompassConfig => Plugin.Config.EurekanConfig;

        private static System.Numerics.Vector4 infoTextColour = new(.8f, .95f, .75f, 1);
        private const float infoTextShadowLightness = .1f;

        private const uint elementalMarkerIconId = 15835;
        private static readonly System.Numerics.Vector2
            elementalMarkerIconSize = new(25, 25);

        public override bool IsEnabledInCurrentTerritory()
            => ZoneWatcher.CurrentTerritoryType?.TerritoryIntendedUse == 41;


        private protected override unsafe string GetClosestObjectiveDescription(
            CachedCompassObjective objective) => objective.Name;

        public override unsafe bool IsObjective(GameObject* o) 
            => o != null && (o->ObjectKind == (byte)ObjectKind.BattleNpc) 
            && IsEurekanElementalName(CompassUtil.GetName(o));

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
                elementalMarkerIconId, new System.Numerics.Vector2(24,32), .9f,
                $"{objective.Name}, {CompassUtil.DistanceToDescriptiveString(objective.Distance3D, true)}",
                infoTextColour, infoTextShadowLightness, out _, important: objective.Distance3D < 60);
        }

        private static bool IsEurekanElementalName(string? name)
        {
            if (name == null) return false;
            name = name.ToLower();
            return name == "hydatos elemental" || name == "pyros elemental" || name == "pagos elemental" || name == "anemos elemental"
                || name == "ヒュダトス・エレメンタル" ||  name == "パゴス・エレメンタル" || name == "ピューロス・エレメンタル" || name == "アネモス・エレメンタル"
                || name == "élémentaire hydatos" || name == "élémentaire pyros" || name == "élémentaire pagos" || name == "élémentaire anemos"
                || name == "hydatos-elementar" || name == "pyros-elementar" || name == "pagos-elementar" || name == "anemos-elementar"
                ;
        }
    }
}
