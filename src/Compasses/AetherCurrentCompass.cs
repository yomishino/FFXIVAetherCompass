using AetherCompass.Common;
using AetherCompass.Configs;
using AetherCompass.UI.GUI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;

namespace AetherCompass.Compasses
{
    public class AetherCurrentCompass : Compass
    {
        public override string CompassName => "Aether Current Compass";
        public override string Description => "Compass that detects Aether Currents nearby.";
        
        private protected override string ClosestObjectDescription => "Aether Current";

        private static System.Numerics.Vector4 infoTextColour = new(.8f, .95f, .75f, 1);
        private const float infoTextShadowLightness = .1f;


        public AetherCurrentCompass(PluginConfig config, AetherCurrentCompassConfig compassConfig, IconManager iconManager) : 
            base(config, compassConfig, iconManager) { }

        public override bool IsEnabledTerritory(uint terr)
            => CompassUtil.GetTerritoryType(terr)?.TerritoryIntendedUse == 1;

        public override unsafe DrawAction? CreateDrawDetailsAction(GameObject* obj)
        {
            if (obj == null) return null;
            var dist = CompassUtil.Get3DDistanceFromPlayer(obj);
            return new(() =>
            {
                if (obj == null) return;
                ImGui.Text($"{CompassUtil.GetName(obj)}");
                ImGui.BulletText($"{CompassUtil.GetMapCoordInCurrentMapFormattedString(obj->Position)} (approx.)");
                ImGui.BulletText(($"{CompassUtil.GetDirectionFromPlayer(obj)} " +
                    $"{CompassUtil.DistanceToFormattedString(dist, false)}; " +
                    $"Altitude diff: {(int)CompassUtil.GetAltitudeDiffFromPlayer(obj)}"));
                DrawFlagButton($"##{(long)obj}", CompassUtil.GetMapCoordInCurrentMap(obj->Position));
                ImGui.Separator();
            }, dist < 80);
        }

        public override unsafe DrawAction? CreateMarkScreenAction(GameObject* obj)
        {
            if (obj == null) return null;
            var icon = iconManager.AetherCurrentMarkerIcon;
            if (icon == null) return null;
            var name = CompassUtil.GetName(obj);
            var dist = CompassUtil.DistanceToFormattedString(CompassUtil.Get3DDistanceFromPlayer(obj), true);
            return new(() => DrawScreenMarkerDefault(obj, icon, IconManager.MarkerIconSize,
                .9f, $"{name}\n{dist}", infoTextColour, infoTextShadowLightness, out _));
        }

        private protected override unsafe bool IsObjective(GameObject* o)
        {
            if (o == null) return false;
            if (o->ObjectKind != (byte)ObjectKind.EventObj) return false;
            //var eObjNames = Plugin.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.EObjName>();
            //if (eObjNames == null) return false;
            //return IsNameOfAetherCurrent(eObjNames.GetRow(o->DataID)?.Singular.RawString)
            //    || IsNameOfAetherCurrent(eObjNames.GetRow(o->DataID)?.Plural.RawString);
            return IsNameOfAetherCurrent(CompassUtil.GetName(o));
        }

        private static bool IsNameOfAetherCurrent(string? name)
        {
            if (name == null) return false;
            name = name.ToLower();
            return name == "aether current" || name == "aether currents"
                || name == "風脈の泉"
                || name == "windätherquelle" || name == "windätherquellen"
                || name == "vent éthéré" || name == "vents éthérés"
                ;
        }
    }
}
