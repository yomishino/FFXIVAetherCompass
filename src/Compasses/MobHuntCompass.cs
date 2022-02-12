using AetherCompass.Common;
using AetherCompass.Common.Attributes;
using AetherCompass.Compasses.Objectives;
using AetherCompass.Configs;
using AetherCompass.Game;
using AetherCompass.UI.GUI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel;
using ImGuiNET;
using System.Collections.Generic;

using Sheets = Lumina.Excel.GeneratedSheets;

namespace AetherCompass.Compasses
{
    [CompassType(CompassType.Standard)]
    public class MobHuntCompass : Compass
    {
        public override string CompassName => "Mob Hunt Compass";
        public override string Description => "Detecting Elite Marks (Notorious Monsters) nearby.";
        
        private readonly Dictionary<uint, NMData> nmDataMap = new(); // BnpcDataId => NMData
        private static readonly System.Numerics.Vector4 infoTextColour = new(1, .6f, .6f, 1);
        private static readonly float infoTextShadowLightness = .1f;

        private protected override CompassConfig CompassConfig => Plugin.Config.MobHuntConfig;
        private MobHuntCompassConfig MobHuntConfig => (MobHuntCompassConfig)CompassConfig;


        public MobHuntCompass() : base() 
        {
            InitNMDataMap();
        }

        public override bool IsEnabledInCurrentTerritory()
            => ZoneWatcher.CurrentTerritoryType?.TerritoryIntendedUse == 1;

        public override unsafe bool IsObjective(GameObject* o)
            => o != null && nmDataMap.TryGetValue(o->DataID, out var data) && data.IsValid
            && ((data.Rank == NMRank.S && MobHuntConfig.DetectS)
                || (data.Rank == NMRank.A && MobHuntConfig.DetectA)
                || (data.Rank == NMRank.B && MobHuntConfig.DetectB))
            && CompassUtil.IsCharacterAlive(o);

        private protected override unsafe string GetClosestObjectiveDescription(CachedCompassObjective objective)
            => !nmDataMap.TryGetValue(objective.DataId, out var data) ? string.Empty : data.GetNMName();

        private protected override void DisposeCompassUsedIcons()
            => Plugin.IconManager.DisposeMobHuntCompassIcons();

        public override unsafe DrawAction? CreateDrawDetailsAction(CachedCompassObjective objective)
            => objective.IsEmpty() || !nmDataMap.TryGetValue(objective.DataId, out var nmData) ? null : new(() =>
            {
                ImGui.Text(nmData.GetNMName());
                ImGui.BulletText($"{CompassUtil.MapCoordToFormattedString(objective.CurrentMapCoord)} (approx.)");
                ImGui.BulletText($"{objective.CurrentMapCoord},  " +
                    $"{CompassUtil.DistanceToDescriptiveString(objective.Distance3D, false)}");
                ImGui.BulletText(CompassUtil.AltitudeDiffToDescriptiveString(objective.AltitudeDiff));
                DrawFlagButton($"##{(long)objective.GameObject}", objective.CurrentMapCoord);
                ImGui.Separator();
            });

        public override unsafe DrawAction? CreateMarkScreenAction(CachedCompassObjective objective)
        {
            if (objective.IsEmpty() || !nmDataMap.TryGetValue(objective.DataId, out var nmData)) return null;
            string descr = $"{nmData.Name}\nRank: {nmData.Rank}, {CompassUtil.DistanceToDescriptiveString(objective.Distance3D, true)}";
            return GenerateDefaultScreenMarkerDrawAction(objective, Plugin.IconManager.MobHuntMarkerIcon, IconManager.MarkerIconSize,
                .9f, descr, infoTextColour, infoTextShadowLightness, out _, 
                important: nmData.Rank == NMRank.S || nmData.Rank == NMRank.A);
        }

        public override void DrawConfigUiExtra()
        {
            ImGui.BulletText("More options:");
            ImGui.Indent();
            ImGui.Checkbox("Detect Rank S", ref MobHuntConfig.DetectS);
            ImGui.Checkbox("Detect Rank A", ref MobHuntConfig.DetectA);
            ImGui.Checkbox("Detect Rank B", ref MobHuntConfig.DetectB);
            ImGui.Unindent();
        }


        private static ExcelSheet<Sheets.NotoriousMonster>? NMSheet 
            => Plugin.DataManager.GetExcelSheet<Sheets.NotoriousMonster>();

        private void InitNMDataMap()
        {
            if (NMSheet != null)
            {
                foreach (var row in NMSheet)
                {
                    if (row.BNpcBase.Row != 0)
                        nmDataMap.TryAdd(row.BNpcBase.Row, new(row.RowId));
                }
            }
        }


        class NMData
        {
            public readonly uint BNpcDataId;
            public readonly uint NMSheetId;
            public readonly NMRank Rank;
            public readonly string Name = null!;
            public readonly bool IsValid;

            public NMData(uint nmSheetId)
            {
                NMSheetId = nmSheetId;
                if (NMSheet == null) return;
                var row = NMSheet.GetRow(nmSheetId);
                if (row == null) return;
                BNpcDataId = row.BNpcBase.Row;
                Rank = (NMRank)row.Rank;
                Name = row.BNpcName.Value?.Singular ?? string.Empty;
                IsValid = true;
            }

            public string GetNMName(bool showRank = true)
                => Name + (showRank ? $"(Rank: {Rank})" : string.Empty);
        }

        enum NMRank : byte
        {
            B = 1,
            A = 2,
            S = 3,
        }
    }
}
