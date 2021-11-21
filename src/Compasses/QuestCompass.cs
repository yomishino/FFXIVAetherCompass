using AetherCompass.Common;
using AetherCompass.Common.SeFunctions;
using AetherCompass.Configs;
using AetherCompass.UI.GUI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using Lumina.Excel;
using System.Collections.Generic;

using Sheets = Lumina.Excel.GeneratedSheets;


namespace AetherCompass.Compasses
{
    public class QuestCompass : Compass
    {
        public override string CompassName => "Quest Compass";
        public override string Description => "Detecting NPC/objects nearby relevant to your in-progress quests.\n" +
            "** Due to some limitations, Battle NPCs (that is, NPCs that can fight, whether by your side or against you)" +
            " will not be detected by the compass.";
        private protected override string ClosestObjectDescription => "Quest NPC/Object";

        private QuestCompassConfig QuestConfig => (QuestCompassConfig)compassConfig;

        private static readonly System.Reflection.PropertyInfo?[,] cachedQuestSheetToDoChildLocationMap = new System.Reflection.PropertyInfo[24, 7];
        private readonly Dictionary<uint, (Quest RelatedQuest, bool TodoRevealed)> objQuestMap = new();
        private static readonly System.Numerics.Vector4 infoTextColour = new(.98f, .77f, .35f, 1);
        private static readonly float infoTextShadowLightness = .1f;


        public QuestCompass(PluginConfig config, QuestCompassConfig compassConfig) : base(config, compassConfig) 
        {
            InitQuestSheetToDoChildLocationMap();
        }

        public override bool IsEnabledTerritory(uint terr)
        {
            var terrItem = CompassUtil.GetTerritoryType(terr);
            // 0 is noninstance, 1 is solo instance
            return terrItem?.ExclusiveType == 0
                || ((QuestConfig?.EnabledInSoloContents ?? false) && terrItem?.ExclusiveType == 1)
                ;
        }

        private protected override void DisposeCompassUsedIcons()
            => IconManager.DisposeQuestCompassIcons();

        public override void OnLoopStart()
        {
            objQuestMap.Clear();
            ProcessQuestData();

            base.OnLoopStart();

            // TODO: may mark those location range in quest ToDOs if somehow we can know which ToDos are already done; 
            // we can find those locations easily from Quest sheet but only if we can know which ToDo are done!
            // (If we can know that, we can also detect relevant BNpcs as well..)
        }


        public override void DrawConfigUiExtra()
        {
            ImGui.Checkbox("Also enable this compass in solo instanced contents (?)", ref QuestConfig.EnabledInSoloContents);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("By default, this compass will not work in any type of instanced contents.\n" +
                    "You can enable it in solo instanced contents if needed.");
            //ImGui.Checkbox("Also detect quest related enemies", ref QuestConfig.DetectEnemy);
            //if (ImGui.IsItemHovered())
            //    ImGui.SetTooltip("By default, this compass will only detect event NPCs or objects," +
            //        "that is, NPCs/Objects that don't fight.\n" +
            //        "You can enable this option to have the compass detect also quest related enemies.");
            ImGui.Checkbox("Don't detect hidden quests (?)", ref QuestConfig.HideHidden);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Hidden quests are those that you've marked as ignored in Journal.\n" +
                    "If this option is enabled, will not detect NPC/Objects related to these hidden quests.");
            ImGui.Checkbox("Detect all NPCs and objects relevant to in-progress quests (?)", ref QuestConfig.ShowAllRelated);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("By default, this compass only detects NPC/objects that are objectives of the quests " +
                    "as shown in the quest Todos and on the Minimap.\n" +
                    "If this option is enabled, NPC/objects that are spawned due to the quests will also " +
                    "be detected by this compass, even if they may not be the objectives of the quests.\n" +
                    "Additionally, for quests that require looking for NPC/objects in a certain area, " +
                    "enabling this option may reveal the objectives' locations.\n\n" +
                    "In either case, NPC/objects that are known to be quest objectives will have a \"★\" mark by their names");
            if (config.ShowScreenMark)
            {
                ImGui.Checkbox("Show quest name by screen marker", ref QuestConfig.ShowQuestName);
                ImGui.Checkbox("Show NPC/Object's name by screen mark", ref QuestConfig.ShowObjName);
            }
        }

        public override unsafe DrawAction? CreateDrawDetailsAction(GameObject* o)
        {
            if (o == null) return null;
            if (!objQuestMap.TryGetValue(o->DataID, out var mappedInfo)) return null;
            var questId = mappedInfo.RelatedQuest.QuestID;
            return new(() =>
            {
                if (o == null) return;
                ImGui.Text($"{CompassUtil.GetName(o)} {(mappedInfo.TodoRevealed ? "★" : "")}");
                ImGui.BulletText($"Quest: {GetQuestName(questId)}");
#if DEBUG
                var qItem = GetQuestRow(questId);
                if (qItem != null)
                {
                    ImGui.BulletText(
                        $"JournalGenre: {qItem.JournalGenre.Row}," +
                        $"Type: {qItem.Type}");
                }
#endif
                ImGui.BulletText($"{CompassUtil.GetMapCoordInCurrentMapFormattedString(o->Position)} (approx.)");
                ImGui.BulletText($"{CompassUtil.GetDirectionFromPlayer(o)},  " +
                    $"{CompassUtil.Get3DDistanceFromPlayerDescriptive(o, false)}");
                ImGui.BulletText(CompassUtil.GetAltitudeDiffFromPlayerDescriptive(o));
                DrawFlagButton($"##{(long)o}", CompassUtil.GetMapCoordInCurrentMap(o->Position));
                ImGui.Separator();
            }, mappedInfo.RelatedQuest.IsPriority);
        }

        public override unsafe DrawAction? CreateMarkScreenAction(GameObject* o)
        {
            if (o == null) return null;
            if (!objQuestMap.TryGetValue(o->DataID, out var mappedInfo)) return null;
            var qRow = GetQuestRow(mappedInfo.RelatedQuest.QuestID);
            var icon = qRow == null || qRow.EventIconType.Value == null
                ? IconManager.DefaultQuestMarkerIcon
                : IconManager.GetQuestMarkerIcon(qRow.EventIconType.Value.NpcIconAvailable, qRow.EventIconType.Value.IconRange, mappedInfo.RelatedQuest.QuestSeq == questFinalSeqIdx);
            var dist = CompassUtil.Get3DDistanceFromPlayer(o);
            var descr = 
                (mappedInfo.TodoRevealed ? "★ " : "")
                + (QuestConfig.ShowObjName ? $"{CompassUtil.GetName(o)}, " : "")
                + CompassUtil.DistanceToDescriptiveString(dist, true);
            if (QuestConfig.ShowQuestName)
            {
                var questName = GetQuestName(mappedInfo.RelatedQuest.QuestID);
                if (questName != null) descr += $"\n(Quest: {questName})";
            }
            return new(
                () => DrawScreenMarkerDefault(o, icon, IconManager.MarkerIconSize,
                    .9f, descr,
                    infoTextColour, infoTextShadowLightness, out _),
                dist < 55 || mappedInfo.RelatedQuest.IsPriority);
        }


        private protected override unsafe bool IsObjective(GameObject* o)
        {
            if (o == null) return false;
            var kind = (ObjectKind)o->ObjectKind;
            uint dataId = o->DataID;
            if (dataId == 0) return false;
            // NOTE: already considered hidden quests or those not revealed by Todos when filling up objQuestMap
            // TODO: AreaObject???
            if (kind == ObjectKind.EventNpc || kind == ObjectKind.EventObj || kind == ObjectKind.AreaObject)
                return objQuestMap.ContainsKey(o->DataID);
            return false;
        }


        private static uint QuestIdToQuestRowId(ushort questId) => questId + (uint)65536;
        private static ushort QuestRowIdToQuestId(uint questRowId)
        {
            if (questRowId <= 65536) return 0;
            var id = questRowId - 65536;
            if (id <= ushort.MaxValue) return (ushort)id;
            return 0;
        }


        private static ExcelSheet<Sheets.Quest>? QuestSheet 
            => Plugin.DataManager.GetExcelSheet<Sheets.Quest>();
        private static ExcelSheet<Sheets.EObj>? EObjSheet 
            => Plugin.DataManager.GetExcelSheet<Sheets.EObj>();
        private static ExcelSheet<Sheets.ENpcBase>? ENpcSheet
            => Plugin.DataManager.GetExcelSheet<Sheets.ENpcBase>();
        private static ExcelSheet<Sheets.Level>? LevelSheet
            => Plugin.DataManager.GetExcelSheet<Sheets.Level>();

        private static Sheets.Quest? GetQuestRow(ushort questId)
            => QuestSheet?.GetRow(QuestIdToQuestRowId(questId));
        private static string? GetQuestName(ushort questId)
            => GetQuestRow(questId)?.Name?.ToString();


        // ActorSpawn, ActorDespawn, Listener, etc.
        private const int questSheetActorArrayLength = 64;
        private const int questSheetToDoArrayLength = 24;
        private const int questSheetToDoChildMaxCount = 7;
        private const byte questFinalSeqIdx = byte.MaxValue;

        private unsafe void ProcessQuestData()
        {
            var questlist = Quests.GetQuestListArray();
            if (questlist == null)
            {
                Plugin.LogError("Failed to get QuestListArray");
                return;
            }
            
            static bool ShouldExitActorArrayLoop(Sheets.Quest q, int idx)
                => (idx >= 0 && idx < questSheetActorArrayLength / 2 && q.QuestUInt8A[idx] == 0)
                || (idx >= questSheetActorArrayLength / 2 && idx < questSheetActorArrayLength
                    && q.QuestUInt8B[idx - questSheetActorArrayLength / 2] == 0);

            for (int i = 0; i < Quests.QuestListArrayLength; i++)
            {
                var quest = questlist[i];
                if (quest.QuestID == 0) continue;
                if (quest.IsHidden && QuestConfig.HideHidden) continue;
                var questRow = GetQuestRow(quest.QuestID);
                if (questRow == null) continue;

                // ToDos: find out objective gameobjs revealed by ToDos, if any
                HashSet<uint> todoRevealedObjs = new();
                for (int j = 0; j < questSheetToDoArrayLength; j++)
                {
                    // NOTE: ignore Level location for now,
                    // because we cant tell if the ToDos are completed or not when there are multiple Todos
                    if (questRow.ToDoCompleteSeq[j] == quest.QuestSeq)
                    {
                        var mainLoc = questRow.ToDoMainLocation[j].Value;
                        if (mainLoc != null && mainLoc.Object != 0)
                            todoRevealedObjs.Add(mainLoc.Object);
                        for (int k = 0; k < questSheetToDoChildMaxCount; k++)
                        {
                            var childLocRowId = GetQuestToDoChildLocationRowId(questRow, j, k);
                            if (childLocRowId != 0)
                            {
                                var childLoc = LevelSheet?.GetRow(childLocRowId);
                                if (childLoc != null && childLoc.Object != 0)
                                    todoRevealedObjs.Add(childLoc.Object);
                            }
                        }
                    }
                    if (questRow.ToDoCompleteSeq[j] == questFinalSeqIdx) break;
                }

                // Actor related arrays: find out related gameobjs
                List<uint> objsThisSeq = new(); // objectives (usually listeners) that will be deactivated when this Seq completes
                for (int j = 0; j < questSheetActorArrayLength; j++)
                {
                    if (ShouldExitActorArrayLoop(questRow, j)) break;
                    var listener = questRow.Listener[j];
                    // Track ConditionValue if ConditionType non-zero instead of listener itself;
                    // this usually happens with BNpc etc. which we don't consider yet, but anyway
                    var objToTrack = questRow.ConditionType[j] > 0 ? questRow.ConditionValue[j] : listener;
                    bool todoRevealed = todoRevealedObjs.Contains(objToTrack);
                    // Skip those not revealed by ToDos if option not enabled
                    if (!QuestConfig.ShowAllRelated && !todoRevealed) continue;
                    if (questRow.ActorSpawnSeq[j] == 0) continue; // Invalid? usually won't have this
                    if (questRow.ActorSpawnSeq[j] > quest.QuestSeq) continue; // Not spawn/active yet
                    if (questRow.ActorDespawnSeq[j] < questSheetToDoArrayLength)
                    {
                        // I think ActorDespawnSeq corresponds to ToDo idx, not Seq
                        var despawnSeq = questRow.ToDoCompleteSeq[questRow.ActorDespawnSeq[j]];
                        if (despawnSeq < quest.QuestSeq)
                            continue; // Despawned/deactivated when previous Seq ends
                        // TODO: should we also check if it's spawned at start of this Seq?
                        if (despawnSeq == quest.QuestSeq)
                            objsThisSeq.Add(objToTrack);
                    }
                    if (!objQuestMap.ContainsKey(objToTrack) || quest.IsPriority)
                        objQuestMap[objToTrack] = (quest, todoRevealed);
                }
                // Filter out already interacted ones
                if (quest.ObjectiveObjectsInteractedFlags != 0)
                {
                    for (int k = 0; k < objsThisSeq.Count; k++)
                        if (quest.IsObjectiveInteracted(k))
                            objQuestMap.Remove(objsThisSeq[k]);
                }
            }
        }

        // ToDoChildLocation is 24 * 7 array containing row id of corresponding Level
        // In sheet first col of every row get listed first, then 2nd col and so on.
        // (So together with ToDoMainLocation, we can have at most 24 ToDos for a quest and each ToDo can have at most 8 Levels.)
        public static uint GetQuestToDoChildLocationRowId(Sheets.Quest? questRow, int row, int col)
        {
            if (row < 0 || row >= questSheetToDoArrayLength 
                || col < 0 || col >= questSheetToDoChildMaxCount || questRow == null) return 0;
            var val = cachedQuestSheetToDoChildLocationMap[row, col]?.GetValue(questRow);
            if (val == null) return 0;
            if (row <= 6 && col == 0) // covered by uint array at col 1245
                return (val as uint[])?[row] ?? 0;
            else return (uint)val;
        }

        public static uint GetQuestToDoChildLocationRowId(uint questRowId, int row, int col)
            => GetQuestToDoChildLocationRowId(QuestSheet?.GetRow(questRowId), row, col);

        // (0, 0) is at col 1245. Row first.
        // Currently Lumina has at col 1245 a uint array span to col 1251 (incl.), which covers <row 0 ~ 6, col 0> of the ToDoChildLocation array;
        private static void InitQuestSheetToDoChildLocationMap()
        {
            var questSheetType = typeof(Sheets.Quest);
            int propIdx0 = 1245;
            for (int i = 0; i < questSheetToDoArrayLength; i++)
            {
                for (int j = 0; j < questSheetToDoChildMaxCount; j++)
                {
                    if (i <= 6 && j == 0)
                        // From uint array at col 1245
                        cachedQuestSheetToDoChildLocationMap[i, j] = questSheetType.GetProperty($"Unknown{propIdx0}");
                    else
                    {
                        int propIdx = propIdx0 + j * questSheetToDoArrayLength + i;
                        cachedQuestSheetToDoChildLocationMap[i, j] = questSheetType.GetProperty($"Unknown{propIdx}");
                    }
                }
            }
        }
    }
}
