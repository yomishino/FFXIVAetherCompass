using AetherCompass.Common;
using AetherCompass.Configs;
using AetherCompass.UI.GUI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using System.Numerics;


namespace AetherCompass.Compasses
{
    public class DebugCompass : Compass
    {
        public override string CompassName => "Debug Compass"; 
        public override string Description => "For Debug";
        

        public DebugCompass(PluginConfig config, CompassConfig compassConfig) 
            : base(config, compassConfig) { }


        public override bool IsEnabledTerritory(uint terr)
            => true;

        private protected override void DisposeCompassUsedIcons() { }

        private protected override unsafe bool IsObjective(GameObject* o)
            => o != null && (o->ObjectID == Plugin.ClientState.LocalPlayer?.ObjectId
            || o->ObjectKind == (byte)ObjectKind.EventObj 
            //|| o->ObjectKind == (byte)ObjectKind.EventNpc
            || o->ObjectKind == (byte)ObjectKind.GatheringPoint
            || o->ObjectKind == (byte)ObjectKind.Aetheryte
            || o->ObjectKind == (byte)ObjectKind.AreaObject);

        private protected override unsafe string GetClosestObjectiveDescription(GameObject* o)
            => "Debug Obj";

        public override unsafe DrawAction? CreateDrawDetailsAction(GameObject* obj)
        {
            if (obj == null) return null;
            return new(() =>
            {
                if (obj == null) return;
                ImGui.Text($"Object: {CompassUtil.GetName(obj)}");
                ImGui.BulletText($"ObjectId: {obj->GetObjectID().ObjectID}, type {obj->GetObjectID().Type}");
                ImGui.BulletText($"ObjectKind: {(ObjectKind)obj->ObjectKind}");
                ImGui.BulletText($"NpcId: {obj->GetNpcID()} DataId: {obj->DataID}");
                ImGui.BulletText($"2D-Distance: {CompassUtil.Get2DDistanceFromPlayer(obj):0.0}");
                ImGui.BulletText($"Height diff: {CompassUtil.GetYDistanceFromPlayer(obj):0.0}");
                ImGui.BulletText($"3D-Distance: {CompassUtil.Get3DDistanceFromPlayer(obj):0.0}");
                ImGui.BulletText($"Direction: {CompassUtil.GetDirectionFromPlayer(obj)}, {CompassUtil.GetRotationFromPlayer(obj):0.00}");
                ImGui.BulletText($"Position: {(Vector3)obj->Position}");
                ImGui.BulletText($"MapCoord: {CompassUtil.GetMapCoordInCurrentMapFormattedString(obj->Position)}");

                DrawFlagButton(((long)obj).ToString(), CompassUtil.GetMapCoordInCurrentMap(obj->Position));

                ImGui.NewLine();
            });
        }

        public override unsafe DrawAction? CreateMarkScreenAction(GameObject* obj)
        {
            if (obj == null) return null;
            var marker = IconManager.DebugMarkerIcon;
            if (marker == null) return null;

            // These are already handled by the Draw...Default method,
            // here is just for debug record
            var markerSize = IconManager.MarkerIconSize;
            UiHelper.WorldToScreenPos(obj->Position, out var screenPos, out var pCoordsRaw);
            screenPos.Y -= ImGui.GetMainViewport().Size.Y / 50; // slightly raise it up from hitbox screen pos

            return new(() =>
            {
                if (obj == null) return;
                string info = $"name={CompassUtil.GetName(obj)}\n" +
                            $"worldPos={(Vector3)obj->Position}, dist={CompassUtil.Get3DDistanceFromPlayer(obj):0.0}\n" +
                            $"sPosUnfixed=<{screenPos.X:0.0}, {screenPos.Y:0.0}>, raw=<{pCoordsRaw.X:0.0}, {pCoordsRaw.Y:0.0}, {pCoordsRaw.Z:0.0}>";
                DrawScreenMarkerDefault(obj, marker, markerSize, .9f, info, new(1, 1, 1, 1), 0, out _);
            });
        }
    }
}
