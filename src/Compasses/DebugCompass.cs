using AetherCompass.Common;
using AetherCompass.UI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using System;
using System.Numerics;

using ObjectInfo = FFXIVClientStructs.FFXIV.Client.UI.UI3DModule.ObjectInfo;


namespace AetherCompass.Compasses
{
    public class DebugCompass : Compass
    {
        public override string Description => "For Debug";
        public override bool CompassEnabled
        {
            get => config.DebugEnabled;
            internal set => config.DebugEnabled = value;
        }
        public override bool MarkScreenEnabled
        {
            get => config.DebugScreen;
            private protected set => config.DebugScreen = value;
        }
        public override bool DrawDetailsEnabled 
        { 
            get => config.DebugDetails; 
            private protected set => config.DebugDetails = value; 
        } 
                

        public DebugCompass(Configuration config, IconManager iconManager) 
            : base(config, iconManager) { }

        public override unsafe bool IsObjective(GameObject* o)
            => o != null && (o->ObjectID == Plugin.ClientState.LocalPlayer?.ObjectId
            || o->ObjectKind == (byte)ObjectKind.EventObj 
            //|| o->ObjectKind == (byte)ObjectKind.EventNpc
            || o->ObjectKind == (byte)ObjectKind.GatheringPoint
            || o->ObjectKind == (byte)ObjectKind.Aetheryte
            || o->ObjectKind == (byte)ObjectKind.AreaObject);


        public override unsafe Action? CreateDrawDetailsAction(ObjectInfo* info)
        {
            if (info == null || info->GameObject == null) return null;
            var obj = info->GameObject;
            return new Action(() =>
            {
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

                //ImGui.BulletText($"MapInfo: MapId, IconId, Unk");
                //ImGui.Indent();
                //ImGui.Indent();
                //ImGui.Columns(3);
                //ImGui.Text($"{detail.ObjectInfo.MapInfo.MapId}");
                //ImGui.NextColumn();
                //ImGui.Text($"{detail.ObjectInfo.MapInfo.IconId}");
                //ImGui.NextColumn();
                //ImGui.Text($"{detail.ObjectInfo.MapInfo.Unk_12}");
                //ImGui.Columns(1);
                //ImGui.Unindent();
                //ImGui.Unindent();

                DrawFlagButton(((long)obj).ToString(), CompassUtil.GetMapCoordInCurrentMap(obj->Position));

                ImGui.NewLine();
            });
        }



        public override unsafe Action? CreateMarkScreenAction(ObjectInfo* info)
        {
            if (info == null || info->GameObject == null) return null;
            var obj = info->GameObject;
            var marker = iconManager.DebugMarkerIcon;
            if (marker == null) return null;

            // These are already handled by the Draw...Default method,
            // here is just for debug record
            var markerSize = IconManager.DebugMarkerIconSize;
            //bool inFrontOfCamera = CompassUtil.WorldToScreenPos(obj->Position, out var hitboxScrPos);
            //bool insideViewport = CompassUtil.IsScreenPosInsideMainViewport(hitboxScrPos, markerSize);
            //Vector2 screenPos;
            //screenPos = hitboxScrPos;
            Projection.WorldToScreen(obj->Position, out var screenPos, out var pCoordsRaw);
            screenPos.Y -= ImGui.GetMainViewport().Size.Y / 50; // slightly raise it up from hitbox screen pos
            //screenPos = FixDrawPos(screenPos, inFrontOfCamera, insideViewport);

            return new Action(() =>
            {
                string info = $"name={CompassUtil.GetName(obj)}\n" +
                            $"worldPos={(Vector3)obj->Position}, dist={CompassUtil.Get3DDistanceFromPlayer(obj):0.0}\n" +
                            //$"sPosFixed=<{screenPos.X:0.0}, {screenPos.Y:0.0}>, inFrontOfCam={inFrontOfCamera}\n";
                            $"sPos=<{screenPos.X:0.0}, {screenPos.Y:0.0}>, raw=<{pCoordsRaw.X:0.0}, {pCoordsRaw.Y:0.0}, {pCoordsRaw.Z:0.0}>";
                DrawScreenMarkerDefault(obj, marker, markerSize, .9f, info, new(1, 1, 1, 1), out _);

            });
        }

    }
}
