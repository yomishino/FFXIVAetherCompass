using AetherCompass.Common;
using AetherCompass.Configs;
using AetherCompass.UI.GUI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using Lumina.Excel;
using System.Numerics;

using Sheets = Lumina.Excel.GeneratedSheets;

namespace AetherCompass.Compasses
{
    public class GatheringPointCompass : Compass
    {
        public override string CompassName => "Gathering Point Compass";
        public override string Description => "Detecting nearby gathering points";

        private GatheringPointCompassConfig GatheringConfig => (GatheringPointCompassConfig)compassConfig;

        private static readonly Vector4 infoTextColour = new(.55f, .98f, 1, 1);
        private static readonly float infoTextShadowLightness = .1f;


        public GatheringPointCompass(PluginConfig config, GatheringPointCompassConfig compassConfig, CompassDetailsWindow detailsWindow, CompassOverlay overlay)
            : base(config, compassConfig, detailsWindow, overlay) { }

        public override bool IsEnabledTerritory(uint terr)
            => CompassUtil.GetTerritoryType(terr)?.TerritoryIntendedUse == 1
            // TODO: diadem? 
            || CompassUtil.GetTerritoryType(terr)?.TerritoryIntendedUse == 47;

        private protected override unsafe bool IsObjective(GameObject* o)
            => o != null && o->ObjectKind == (byte)ObjectKind.GatheringPoint;

        public override unsafe DrawAction? CreateDrawDetailsAction(GameObject* o)
            => new(() =>
            {
                if (o == null) return;
                ImGui.Text($"Lv{GetGatheringLevel(o->DataID)} {CompassUtil.GetName(o)}");
                ImGui.BulletText($"{CompassUtil.GetMapCoordInCurrentMapFormattedString(o->Position)} (approx.)");
                ImGui.BulletText($"{CompassUtil.GetDirectionFromPlayer(o)},  " +
                    $"{CompassUtil.Get3DDistanceFromPlayerDescriptive(o, false)}");
                ImGui.BulletText(CompassUtil.GetAltitudeDiffFromPlayerDescriptive(o));
                DrawFlagButton($"##{(long)o}", CompassUtil.GetMapCoordInCurrentMap(o->Position));
                ImGui.Separator();
            });

        public override unsafe DrawAction? CreateMarkScreenAction(GameObject* o)
            => new(() =>
            {
                if (o == null) return;
                var icon = IconManager.GetGatheringMarkerIcon(GetGatheringPointIconId(o->DataID));
                string descr = $"Lv{GetGatheringLevel(o->DataID)} {CompassUtil.GetName(o)}, {CompassUtil.Get3DDistanceFromPlayerDescriptive(o, true)}";
                DrawScreenMarkerDefault(o, icon, IconManager.MarkerIconSize,
                    .9f, descr, infoTextColour, infoTextShadowLightness, out _);
            });

        private protected override void DisposeCompassUsedIcons() => IconManager.DisposeGatheringPointCompassIcons();

        private protected override unsafe string GetClosestObjectiveDescription(GameObject* o)
            => CompassUtil.GetName(o);


        private static ExcelSheet<Sheets.GatheringPoint>? GatheringPointSheet
            => Plugin.DataManager.GetExcelSheet<Sheets.GatheringPoint>();

        // True for those that use special icon;
        // 1 normal, 2 unspoiled, 3 leve (normal), 4 aetherial reduction (normal), 5 folklore
        // 6 spearfishing special, 7 diadem normal, 8 diadem special
        private static bool IsSpecialGatheringPoint(uint dataId)
        {
            var gatherPointData = GatheringPointSheet?.GetRow(dataId);
            if (gatherPointData == null) return false;
            return gatherPointData.Type == 2
                || gatherPointData.Type == 5
                || gatherPointData.Type == 6
                || gatherPointData.Type == 8
                ;
        }

        private static uint GetGatheringPointIconId(uint dataId)
        {
            var gatherType = GatheringPointSheet?.GetRow(dataId)?.GatheringPointBase.Value?.GatheringType.Value;
            if (gatherType == null) return 0;
            return (uint)(IsSpecialGatheringPoint(dataId) ? gatherType.IconOff : gatherType.IconMain);
        }

        private static byte GetGatheringLevel(uint dataId)
        {
            var gatherPointBase = GatheringPointSheet?.GetRow(dataId)?.GatheringPointBase.Value;
            return gatherPointBase == null ? byte.MinValue : gatherPointBase.GatheringLevel;
        }

    }
}
