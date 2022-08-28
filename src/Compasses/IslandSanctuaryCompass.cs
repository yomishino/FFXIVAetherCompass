using AetherCompass.Common;
using AetherCompass.Common.Attributes;
using AetherCompass.Compasses.Objectives;
using AetherCompass.Configs;
using AetherCompass.Game;
using AetherCompass.UI.GUI;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using ImGuiScene;
using Lumina.Excel;
using System.Collections.Generic;
using Sheets = Lumina.Excel.GeneratedSheets;


namespace AetherCompass.Compasses
{
    [CompassType(CompassType.Experimental)]
    public class IslandSanctuaryCompass : Compass
    {
        public override string CompassName => "Island Sanctuary Compass";
        public override string Description => "Detecting nearby gathering objects and animals in Island Sanctuary\n" +
            "** Warning: Be prepared to be overwhelmed by too many markers!";

        private protected override CompassConfig CompassConfig => Plugin.Config.IslandConfig;
        private IslandSanctuaryCompassConfig IslandConfig => (IslandSanctuaryCompassConfig)CompassConfig;


        private readonly Dictionary<uint, IslandGatheringObjectData> 
            islandGatherDict = new();   // npcId => data
        private readonly List<IslandGatheringObjectData>
            islandGatherList = new();   // ordered by row id
        private readonly Dictionary<uint, IslandAnimalData>
            islandAnimalDict = new();   // dataId => data

        private static readonly System.Numerics.Vector4 infoTextColourGather 
            = new(.75f, .98f, .9f, 1);
        private static readonly System.Numerics.Vector4 infoTextColourAnimal
            = new(.98f, .8f, .85f, 1);
        private static readonly float infoTextShadowLightness = .1f;


        public IslandSanctuaryCompass() : base()
        {
            BuildIslandGatherDict();
            BuildIslandAnimalDict();
        }


        // TerritoryType RowId = 1055; TerritoryIntendedUse = 49
        public override bool IsEnabledInCurrentTerritory()
            => ZoneWatcher.CurrentTerritoryType?.TerritoryIntendedUse == 49;

        public override unsafe bool IsObjective(GameObject* o)
        {
            if (o == null) return false;
            if (IslandConfig.DetectGathering && o->ObjectKind == (byte)ObjectKind.CardStand)
                return islandGatherDict.TryGetValue(o->GetNpcID(), out var data)
                    && (IslandConfig.GatheringObjectsToShow & (1u << (int)data.SheetRowId)) != 0;
            if (IslandConfig.DetectAnimals && o->ObjectKind == (byte)ObjectKind.BattleNpc)
                return islandAnimalDict.ContainsKey(o->DataID);
            return false;
        }

        protected override unsafe CachedCompassObjective CreateCompassObjective(GameObject* obj)
        {
            if (obj == null)
                return new IslandCachedCompassObjective(obj, 0);
            return obj->ObjectKind switch
            {
                (byte)ObjectKind.CardStand => 
                    new IslandCachedCompassObjective(obj, IslandObjectType.Gathering),
                (byte)ObjectKind.BattleNpc => 
                    new IslandCachedCompassObjective(obj, IslandObjectType.Animal),
                _ => new IslandCachedCompassObjective(obj, 0),
            };
        }

        protected override unsafe CachedCompassObjective CreateCompassObjective(UI3DModule.ObjectInfo* info)
        {
            var obj = info != null ? info->GameObject : null;
            if (obj == null) return new IslandCachedCompassObjective(obj, 0);
            return obj->ObjectKind switch
            {
                (byte)ObjectKind.CardStand =>
                    new IslandCachedCompassObjective(info, IslandObjectType.Gathering),
                (byte)ObjectKind.BattleNpc =>
                    new IslandCachedCompassObjective(info, IslandObjectType.Animal),
                _ => new IslandCachedCompassObjective(info, 0),
            };
        }


        private protected override void DisposeCompassUsedIcons() => Plugin.IconManager.DisposeIslandCompassIcons();

        private protected override unsafe string GetClosestObjectiveDescription(CachedCompassObjective objective)
            => objective.Name;

        public override unsafe DrawAction? CreateDrawDetailsAction(CachedCompassObjective objective)
            => objective.IsEmpty() || objective is not IslandCachedCompassObjective islObjective ? null : new(() =>
            {
                if (islObjective.Type == IslandObjectType.Gathering)
                    ImGui.Text($"{islObjective.Name}, Type: {islObjective.Type}" +
                        $" - {GetIslandGatherType(islObjective)}");
                else ImGui.Text($"{islObjective.Name}, Type: {islObjective.Type}");
                ImGui.BulletText($"{CompassUtil.MapCoordToFormattedString(islObjective.CurrentMapCoord)} (approx.)");
                ImGui.BulletText($"{islObjective.CompassDirectionFromPlayer},  " +
                    $"{CompassUtil.DistanceToDescriptiveString(islObjective.Distance3D, false)}");
                ImGui.BulletText(CompassUtil.AltitudeDiffToDescriptiveString(islObjective.AltitudeDiff));
                DrawFlagButton($"##{(long)islObjective.GameObject}", islObjective.CurrentMapCoord);
                ImGui.Separator();
            });

        public override unsafe DrawAction? CreateMarkScreenAction(CachedCompassObjective objective)
        {
            if (objective.IsEmpty() || objective is not IslandCachedCompassObjective islObjective) return null;
            var icon = GetIslandMarkerIcon(islObjective);
            var iconSize = islObjective.Type == IslandObjectType.Animal
                && IslandConfig.UseAnimalSpecificIcons
                ? IconManager.AnimalSpecificMarkerIconSize : IconManager.MarkerIconSize;
            var textColour = islObjective.Type == IslandObjectType.Gathering
                ? infoTextColourGather : infoTextColourAnimal;
            string descr = islObjective.Type switch
            {
                IslandObjectType.Animal => IslandConfig.ShowNameOnMarkerAnimals 
                    ? $"{objective.Name}, " : "",
                IslandObjectType.Gathering => IslandConfig.ShowNameOnMarkerGathering
                    ? $"{objective.Name}, ": "",
                _ => "",
            } + $"{CompassUtil.DistanceToDescriptiveString(objective.Distance3D, true)}";
            var showIfOutOfScreen = islObjective.Type switch
            {
                IslandObjectType.Animal => !IslandConfig.HideMarkerWhenNotInScreenAnimals,
                IslandObjectType.Gathering => !IslandConfig.HideMarkerWhenNotInScreenGathering,
                _ => false
            };
            return GenerateDefaultScreenMarkerDrawAction(objective, icon, iconSize,
                    .9f, descr, textColour, infoTextShadowLightness, out _,
                    important: false, showIfOutOfScreen: showIfOutOfScreen);
        }

        public override void DrawConfigUiExtra()
        {
            ImGui.BulletText("More options:");
            ImGui.Indent();
            ImGui.Checkbox("Detect Gathering Objects", ref IslandConfig.DetectGathering);
            if (IslandConfig.DetectGathering)
            {
                ImGui.TreePush();
                ImGui.Checkbox("Show gathering object names on the markers", 
                    ref IslandConfig.ShowNameOnMarkerGathering);
                ImGui.Checkbox("Hide markers for gathering objects that are out of screen",
                    ref IslandConfig.HideMarkerWhenNotInScreenGathering);
                if(ImGui.CollapsingHeader("Detect only the following objects ..."))
                {
                    ImGui.TreePush();
                    if (ImGui.Button("Select all"))
                        IslandConfig.GatheringObjectsToShow = uint.MaxValue;
                    ImGui.SameLine();
                    if (ImGui.Button("Unselect all"))
                        IslandConfig.GatheringObjectsToShow = uint.MinValue;
                    if (ImGui.BeginListBox("##DetectGatheringObjectFilter"))
                    {
                        for (int i = 0; i < islandGatherList.Count; i++)
                        {
                            var data = islandGatherList[i];
                            if (data.NpcId == 0) continue;
                            var flagval = 1u << i;
                            ImGui.CheckboxFlags(data.Name,
                                ref IslandConfig.GatheringObjectsToShow, flagval);
                        }
                        ImGui.EndListBox();
                    }
                    ImGui.TreePop();
                }
                ImGui.TreePop();
            }
            ImGui.Checkbox("Detect Animals", ref IslandConfig.DetectAnimals);
            if (IslandConfig.DetectAnimals)
            {
                ImGui.TreePush();
                ImGui.Checkbox("Show animal names on the markers", 
                    ref IslandConfig.ShowNameOnMarkerAnimals);
                ImGui.Checkbox("Hide markers for animals that are out of screen",
                    ref IslandConfig.HideMarkerWhenNotInScreenAnimals);
                ImGui.Checkbox("Use different icons for different animals",
                    ref IslandConfig.UseAnimalSpecificIcons);
                ImGui.TreePop();
            }
            ImGui.Unindent();
        }


        private static ExcelSheet<Sheets.EObjName>? EObjNameSheet
            => Plugin.DataManager.GetExcelSheet<Sheets.EObjName>();

        // ExcelSheet "MJIGatheringObject"
        // Col#10 iconId
        // Col#11 NpcId == EObj's Row Id
        private static ExcelSheet<Sheets.MJIGatheringObject>? GatheringObjectSheet
            => Plugin.DataManager.GetExcelSheet<Sheets.MJIGatheringObject>();

        // ExcelSheet "MJIAnimal"
        // Col#0 DataId
        // Col#4, Col#5 Item RowId (飼育動物の〇〇 / Sanctuary xxx)
        // Col#6 IconId for some UI icons
        private static ExcelSheet<Sheets.MJIAnimals>? AnimalSheet
            => Plugin.DataManager.GetExcelSheet<Sheets.MJIAnimals>();

        private void BuildIslandGatherDict()
        {
            islandGatherDict.Clear();
            var gatheringSheet = GatheringObjectSheet;
            var eObjNameSheet = EObjNameSheet;
            if (gatheringSheet == null)
            {
                Plugin.LogError("Failed to load ExcelSheet: MJIGatheringObject");
                return;
            }
            if (EObjNameSheet == null)
            {
                Plugin.LogError("Failed to load ExcelSheet: EobjName");
            }
            foreach (var row in gatheringSheet)
            {
                var name = eObjNameSheet?.GetRow(row.Unknown11)?.Singular.RawString
                    ?? string.Empty;
                var data = new IslandGatheringObjectData(
                        row.RowId, row.Unknown11, row.Unknown10, name);
                islandGatherDict.Add(row.Unknown11, data);
                islandGatherList.Add(data);
            }
        }

        private IslandGatherType GetIslandGatherType(IslandCachedCompassObjective islObjective)
            => islandGatherDict.TryGetValue(islObjective.NpcId, out var data)
            ? data.GatherType : 0;

        private uint GetIslandGatherIconId(IslandCachedCompassObjective islObjective)
            => islandGatherDict.TryGetValue(islObjective.NpcId, out var data)
            ? data.IconId : 0;

        private void BuildIslandAnimalDict()
        {
            islandAnimalDict.Clear();
            var sheet = AnimalSheet;
            if (sheet == null)
            {
                Plugin.LogError("Failed to load ExcelSheet: MJIAnimal");
                return;
            }
            foreach (var row in sheet)
            {
                islandAnimalDict.Add(row.Unknown0,
                    new(row.RowId, row.Unknown0, (uint)row.Unknown6));
            }
        }

        private uint GetIslandAnimalIconId(IslandCachedCompassObjective islObjective)
            => islandAnimalDict.TryGetValue(islObjective.DataId, out var data)
            ? data.IconId : 0;

        private TextureWrap? GetIslandMarkerIcon(IslandCachedCompassObjective islObjective)
            => Plugin.IconManager.GetIslandMarkerIcon(islObjective.Type switch
            {
                IslandObjectType.Gathering => GetIslandGatherIconId(islObjective),
                IslandObjectType.Animal 
                    => IslandConfig.UseAnimalSpecificIcons
                    ? GetIslandAnimalIconId(islObjective)
                    : IconManager.IslandAnimalDefaultMarkerIconId,
                _ => 0
            });
    }


    public class IslandAnimalData
    {
        public readonly uint SheetRowId;
        public readonly uint DataId;
        public readonly uint IconId;

        public IslandAnimalData(uint rowId, uint dataId, uint iconId)
        {
            SheetRowId = rowId;
            DataId = dataId;
            IconId = iconId;
        }
    }

    public class IslandGatheringObjectData
    {
        public readonly uint SheetRowId;
        public readonly uint NpcId;
        public readonly uint IconId;
        public readonly IslandGatherType GatherType;
        public readonly string Name;

        public IslandGatheringObjectData(uint rowId, uint npcId, uint iconId, string name)
        {
            SheetRowId = rowId;
            NpcId = npcId;
            IconId = iconId;
            GatherType = GetIslandGatherType(iconId);
            Name = name;
        }

        private static IslandGatherType GetIslandGatherType(uint iconId)
            => iconId switch
            {
                63963 => IslandGatherType.Crops,
                63964 => IslandGatherType.Trees,
                63965 => IslandGatherType.Rocks,
                63966 => IslandGatherType.Sands,
                63967 => IslandGatherType.Sea,
                _ => 0
            };
    }

    public enum IslandObjectType : byte
    {
        Animal = 1,
        Gathering = 2
    }

    // Classified according to icons
    public enum IslandGatherType : byte
    {
        Crops = 1,  // 63963
        Trees = 2,  // 63964
        Rocks = 3,  // 63965
        Sands = 4,  // 63966
        Sea = 5,    // 63967
    }
}
