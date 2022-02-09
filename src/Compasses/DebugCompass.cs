using AetherCompass.Common;
using AetherCompass.Compasses.Objectives;
using AetherCompass.Configs;
using AetherCompass.Game;
using AetherCompass.UI.GUI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;


namespace AetherCompass.Compasses
{
    public class DebugCompass : Compass
    {
        public override string CompassName => "Debug Compass"; 
        public override string Description => "For Debug";
        

        public DebugCompass(DebugCompassConfig compassConfig) 
            : base(compassConfig) { }


        public override bool IsEnabledInCurrentTerritory()
            => ZoneWatcher.CurrentTerritoryType?.RowId != 0;

        private protected override void DisposeCompassUsedIcons() { }

        public override unsafe bool IsObjective(GameObject* o)
            => o != null && (o->ObjectID == Plugin.ClientState.LocalPlayer?.ObjectId
            || o->ObjectKind == (byte)ObjectKind.EventObj 
            //|| o->ObjectKind == (byte)ObjectKind.EventNpc
            || o->ObjectKind == (byte)ObjectKind.GatheringPoint
            || o->ObjectKind == (byte)ObjectKind.Aetheryte
            || o->ObjectKind == (byte)ObjectKind.AreaObject);

        private protected override unsafe string GetClosestObjectiveDescription(CachedCompassObjective _)
            => "Debug Obj";

        protected override unsafe CachedCompassObjective CreateCompassObjective(GameObject* obj)
            => new DebugCachedCompassObjective(obj);
        
        public override unsafe DrawAction? CreateDrawDetailsAction(CachedCompassObjective objective)
        {
            if (objective.IsEmpty() || objective is not DebugCachedCompassObjective debugObjective) return null;
            return new(() =>
            {
                ImGui.Text($"Object: {debugObjective.Name}");
                ImGui.BulletText($"ObjectId: {debugObjective.GameObjectId.ObjectID}, type {debugObjective.GameObjectId.Type}");
                ImGui.BulletText($"ObjectKind: {debugObjective.ObjectKind}");
                ImGui.BulletText($"NpcId: {debugObjective.NpcId} DataId: {debugObjective.DataId}");
                ImGui.BulletText($"2D-Distance: {debugObjective.Distance2D:0.0}");
                ImGui.BulletText($"Height diff: {debugObjective.AltitudeDiff:0.0}");
                ImGui.BulletText($"3D-Distance: {debugObjective.Distance3D:0.0}");
                ImGui.BulletText($"Direction: {debugObjective.CompassDirectionFromPlayer}, {debugObjective.RotationFromPlayer:0.00}");
                ImGui.BulletText($"Position: {debugObjective.Position}");
                ImGui.BulletText($"MapCoord: {CompassUtil.MapCoordToFormattedString(debugObjective.CurrentMapCoord)}");

                DrawFlagButton(((long)debugObjective.GameObject).ToString(), debugObjective.CurrentMapCoord);

                ImGui.NewLine();
            });
        }

        public override unsafe DrawAction? CreateMarkScreenAction(CachedCompassObjective objective)
        {
            if (objective.IsEmpty()) return null;
            // These are already handled by the Draw...Default method,
            // here is just for debug record
            UiHelper.WorldToScreenPos(objective.Position, out var screenPos, out var pCoordsRaw);
            screenPos.Y -= ImGui.GetMainViewport().Size.Y / 50; // slightly raise it up from hitbox screen pos

            string info = $"name={objective.Name}\n" +
                            $"worldPos={objective.Position}, dist={objective.Distance3D:0.0}\n" +
                            $"sPosUnfixed=<{screenPos.X:0.0}, {screenPos.Y:0.0}>, raw=<{pCoordsRaw.X:0.0}, {pCoordsRaw.Y:0.0}, {pCoordsRaw.Z:0.0}>";
            return GenerateDefaultScreenMarkerDrawAction(objective, 
                IconManager.DebugMarkerIcon, IconManager.MarkerIconSize, .9f, info, new(1, 1, 1, 1), 0, out _);
        }
    }
}
