using AetherCompass.Common;
using AetherCompass.Configs;
using AetherCompass.UI.GUI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel;
using ImGuiNET;
using System.Collections.Generic;

using Sheets = Lumina.Excel.GeneratedSheets;

namespace AetherCompass.Compasses
{
    public class MobHuntCompass : Compass
    {
        public override string CompassName => "Mob Hunt Compass";
        public override string Description => "Detecting Elite Marks (Notorious Monsters) nearby.";
        
        private readonly Dictionary<uint, NMData> nmDataMap = new(); // BnpcDataId => NMData
        private static readonly System.Numerics.Vector4 infoTextColour = new(1, .6f, .6f, 1);
        private static readonly float infoTextShadowLightness = .1f;

        private MobHuntCompassConfig MobHuntConfig => (MobHuntCompassConfig)compassConfig;



        public MobHuntCompass(PluginConfig config, MobHuntCompassConfig compassConfig, CompassDetailsWindow detailsWindow, CompassOverlay overlay)
            : base(config, compassConfig, detailsWindow, overlay) 
        {
            InitNMDataMap();
        }

        public override bool IsEnabledTerritory(uint terr)
            => CompassUtil.GetTerritoryType(terr)?.TerritoryIntendedUse == 1;

        private protected override unsafe bool IsObjective(GameObject* o)
            => o != null && nmDataMap.TryGetValue(o->DataID, out var data) && data.IsValid
            && ((data.Rank == NMRank.S && MobHuntConfig.DetectS)
                || (data.Rank == NMRank.A && MobHuntConfig.DetectA)
                || (data.Rank == NMRank.B && MobHuntConfig.DetectB))
            && CompassUtil.IsCharacterAlive(o);

        private protected override unsafe string GetClosestObjectiveDescription(GameObject* o)
            => o == null || !nmDataMap.TryGetValue(o->DataID, out var data)
            ? string.Empty : data.GetNMName();

        private protected override void DisposeCompassUsedIcons()
            => IconManager.DisposeMobHuntCompassIcons();

        public override unsafe DrawAction? CreateDrawDetailsAction(GameObject* o)
        {
            return new(() =>
            {
                if (o == null) return;
                if (!nmDataMap.TryGetValue(o->DataID, out var nmData)) return;
                ImGui.Text(nmData.GetNMName());
                ImGui.BulletText($"{CompassUtil.GetMapCoordInCurrentMapFormattedString(o->Position)} (approx.)");
                ImGui.BulletText($"{CompassUtil.GetDirectionFromPlayer(o)},  " +
                    $"{CompassUtil.Get3DDistanceFromPlayerDescriptive(o, false)}");
                ImGui.BulletText(CompassUtil.GetAltitudeDiffFromPlayerDescriptive(o));
                DrawFlagButton($"##{(long)o}", CompassUtil.GetMapCoordInCurrentMap(o->Position));
                ImGui.Separator();
            });
        }

        public override unsafe DrawAction? CreateMarkScreenAction(GameObject* o)
        {
            if (o == null || !nmDataMap.TryGetValue(o->DataID, out var nmData)) return null;
            return new(() =>
            {
                if (o == null) return;
                if (!nmDataMap.TryGetValue(o->DataID, out var nmData)) return;
                string descr = $"{nmData.Name}\nRank: {nmData.Rank}, {CompassUtil.Get3DDistanceFromPlayerDescriptive(o, true)}";
                DrawScreenMarkerDefault(o, IconManager.MobHuntMarkerIcon, IconManager.MarkerIconSize,
                    .9f, descr, infoTextColour, infoTextShadowLightness, out _);
            }, nmData.Rank == NMRank.S || nmData.Rank == NMRank.A);
        }

        public override void DrawConfigUiExtra()
        {
            ImGui.NewLine();
            ImGui.Checkbox("Detect Rank S", ref MobHuntConfig.DetectS);
            ImGui.Checkbox("Detect Rank A", ref MobHuntConfig.DetectA);
            ImGui.Checkbox("Detect Rank B", ref MobHuntConfig.DetectB);
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
