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
using System.Numerics;

using Sheets = Lumina.Excel.GeneratedSheets;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace AetherCompass.Compasses
{
    [CompassType(CompassType.Standard)]
    public class MobHuntCompass : Compass
    {
        public override string CompassName => "Mob Hunt Compass";
        public override string Description => "Detecting Elite Marks (Notorious Monsters) nearby.";
        
        private readonly Dictionary<uint, NMData> nmDataMap = new(); // BnpcDataId => NMData
        private static readonly Vector4 infoTextColour = new(1, .6f, .6f, 1);
        private static readonly float infoTextShadowLightness = .1f;

        private protected override CompassConfig CompassConfig => Plugin.Config.MobHuntConfig;
        private MobHuntCompassConfig MobHuntConfig => (MobHuntCompassConfig)CompassConfig;


        public MobHuntCompass() : base() 
        {
            InitNMDataMap();
        }

        protected override unsafe CachedCompassObjective CreateCompassObjective(GameObject* obj)
            => obj != null && nmDataMap.TryGetValue(obj->DataID, out var data) && data.IsValid
            ? new MobHunCachedCompassObjective(obj, data.Rank, CompassUtil.IsHostileCharacter(obj))
            : new MobHunCachedCompassObjective(obj, 0, false);

        protected override unsafe CachedCompassObjective CreateCompassObjective(UI3DModule.ObjectInfo* info)
        {
            var obj = info != null ? info->GameObject : null;
            if (obj == null) return new MobHunCachedCompassObjective(obj, 0, false);
            return nmDataMap.TryGetValue(obj->DataID, out var data) && data.IsValid
                ? new MobHunCachedCompassObjective(info, data.Rank, CompassUtil.IsHostileCharacter(obj))
                : new MobHunCachedCompassObjective(info, 0, false);
        }

        public override bool IsEnabledInCurrentTerritory()
            => ZoneWatcher.CurrentTerritoryType?.TerritoryIntendedUse == 1;

        public override unsafe bool IsObjective(GameObject* o)
            => o != null && nmDataMap.TryGetValue(o->DataID, out var data) && data.IsValid
            && ((data.Rank == NMRank.S && MobHuntConfig.DetectS)
                || (data.Rank == NMRank.A && MobHuntConfig.DetectA)
                || (data.Rank == NMRank.B && !CompassUtil.IsHostileCharacter(o) && MobHuntConfig.DetectB)
                || (data.Rank == NMRank.B && CompassUtil.IsHostileCharacter(o) && MobHuntConfig.DetectSSMinion))
            && CompassUtil.IsCharacterAlive(o);

        private protected override unsafe string GetClosestObjectiveDescription(CachedCompassObjective objective)
            => objective.IsEmpty() || objective is not MobHunCachedCompassObjective mhObjective
            ? string.Empty : $"{mhObjective.Name} (Rank: {mhObjective.GetExtendedRank()})";

        private protected override void DisposeCompassUsedIcons()
            => Plugin.IconManager.DisposeMobHuntCompassIcons();

        public override unsafe DrawAction? CreateDrawDetailsAction(CachedCompassObjective objective)
            => objective.IsEmpty() || objective is not MobHunCachedCompassObjective mhObjective ? null : new(() =>
            {
                ImGui.Text($"{mhObjective.Name}, Rank: {mhObjective.GetExtendedRank()}");
                ImGui.BulletText($"{CompassUtil.MapCoordToFormattedString(mhObjective.CurrentMapCoord)} (approx.)");
                ImGui.BulletText($"{mhObjective.CurrentMapCoord},  " +
                    $"{CompassUtil.DistanceToDescriptiveString(mhObjective.Distance3D, false)}");
                ImGui.BulletText(CompassUtil.AltitudeDiffToDescriptiveString(mhObjective.AltitudeDiff));
                DrawFlagButton($"##{(long)mhObjective.GameObject}", mhObjective.CurrentMapCoord);
                ImGui.Separator();
            });

        public override unsafe DrawAction? CreateMarkScreenAction(CachedCompassObjective objective)
        {
            if (objective.IsEmpty() || objective is not MobHunCachedCompassObjective mhObjective) return null;
            string descr = $"{mhObjective.Name} (Rank: {mhObjective.GetExtendedRank()}), " +
                $"{CompassUtil.DistanceToDescriptiveString(mhObjective.Distance3D, true)}";
            var icon = mhObjective.Rank switch
            {
                NMRank.S => Plugin.IconManager.MobHuntRankSMarkerIcon,
                NMRank.A => Plugin.IconManager.MobHuntRankAMarkerIcon,
                NMRank.B => mhObjective.IsSSMinion
                    ? Plugin.IconManager.MobHuntRankSSMinionMarkerIcon
                    : Plugin.IconManager.MobHuntRankBMarkerIcon,
                _ => null
            };
            return GenerateDefaultScreenMarkerDrawAction(objective, icon, 
                IconManager.MarkerIconSize, .9f, descr, infoTextColour, infoTextShadowLightness, out _, 
                important: mhObjective.Rank == NMRank.S || mhObjective.Rank == NMRank.A || mhObjective.IsSSMinion);
        }

        public override void DrawConfigUiExtra()
        {
            ImGui.BulletText("More options:");
            ImGui.Indent();
            ImGui.Checkbox("Detect S Ranks / SS Ranks", ref MobHuntConfig.DetectS);
            ImGui.Checkbox("Detect SS Minions", ref MobHuntConfig.DetectSSMinion);
            ImGui.Checkbox("Detect A Ranks", ref MobHuntConfig.DetectA);
            ImGui.Checkbox("Detect B Ranks", ref MobHuntConfig.DetectB);
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
            public readonly uint NMSheetRowId;
            public readonly NMRank Rank;
            public readonly bool IsValid;

            public NMData(uint nmSheetRowId)
            {
                NMSheetRowId = nmSheetRowId;
                if (NMSheet == null) return;
                var row = NMSheet.GetRow(nmSheetRowId);
                if (row == null) return;
                BNpcDataId = row.BNpcBase.Row;
                Rank = (NMRank)row.Rank;
                IsValid = true;
            }
        }
    }

    public enum NMRank : byte
    {
        B = 1,
        A = 2,
        S = 3,
    }
}
