using AetherCompass.Common;
using AetherCompass.Common.Attributes;
using AetherCompass.Compasses.Objectives;
using AetherCompass.Game;
using AetherCompass.Compasses.Configs;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using Lumina.Excel;
using System.Numerics;

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

        private const uint rankSMarkerIconId = 61710;
        private const uint rankAMarkerIconId = 61709;
        private const uint rankBMarkerIconId = 61704;

        private protected override CompassConfig CompassConfig => Plugin.Config.MobHuntConfig;
        private MobHuntCompassConfig MobHuntConfig => (MobHuntCompassConfig)CompassConfig;


        public override bool IsEnabledInCurrentTerritory()
            => ZoneWatcher.CurrentTerritoryType?.TerritoryIntendedUse == 1;

        public override unsafe bool IsObjective(GameObject* o)
            => o != null && nmDataMap.TryGetValue(o->DataID, out var data) && data.IsValid
            && ((data.Rank == NMRank.S && MobHuntConfig.DetectS)
                || (data.Rank == NMRank.A && MobHuntConfig.DetectA)
                || (data.Rank == NMRank.B && !CompassUtil.IsHostileCharacter(o) && MobHuntConfig.DetectB)
                || (data.Rank == NMRank.B && CompassUtil.IsHostileCharacter(o) && MobHuntConfig.DetectSSMinion))
            && CompassUtil.IsCharacterAlive(o);

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

        private protected override unsafe string GetClosestObjectiveDescription(CachedCompassObjective objective)
            => objective.IsEmpty() || objective is not MobHunCachedCompassObjective mhObjective
            ? string.Empty : $"{mhObjective.Name} (Rank: {mhObjective.GetExtendedRank()})";

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
            var iconId = mhObjective.Rank switch
            {
                NMRank.S => rankSMarkerIconId,
                NMRank.A => rankAMarkerIconId,
                NMRank.B => mhObjective.IsSSMinion
                    ? rankSMarkerIconId : rankBMarkerIconId,
                _ => 0u
            };
            return GenerateDefaultScreenMarkerDrawAction(objective, iconId, 
                DefaultMarkerIconSize, .9f, descr, infoTextColour, infoTextShadowLightness, out _, 
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
            if (NMSheet == null)
            {
                LogWarningExcelSheetNotLoaded(typeof(Sheets.NotoriousMonster).Name);
                return;
            }
            foreach (var row in NMSheet)
            {
                if (row.BNpcBase.Row != 0)
                    nmDataMap.TryAdd(row.BNpcBase.Row, new(row.RowId));
            }
        }

        public MobHuntCompass() : base()
        {
            InitNMDataMap();
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
