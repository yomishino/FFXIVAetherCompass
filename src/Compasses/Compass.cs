using AetherCompass.Common;
using AetherCompass.UI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using System;
using System.Numerics;

using ObjectInfo = FFXIVClientStructs.FFXIV.Client.UI.UI3DModule.ObjectInfo;


namespace AetherCompass.Compasses
{
    public abstract class Compass
    {
        private protected readonly IconManager iconManager = null!;
        private protected readonly Configuration config = null!;

        // For notifying CompassManager
        internal bool HasFlagToProcess = false;
        internal Vector2 FlaggedMapCoord;

        public abstract string Description { get; }
        public abstract bool CompassEnabled { get; internal set; }
        public abstract bool DrawDetailsEnabled { get; private protected set; }
        public abstract bool MarkScreenEnabled { get; private protected set; }

        
        public Compass(Configuration config, IconManager iconManager)
        {
            this.config = config;
            this.iconManager = iconManager;
        }

        public unsafe abstract bool IsObjective(GameObject* o);

        public unsafe abstract Action? CreateDrawDetailsAction(ObjectInfo* info);
        public unsafe abstract Action? CreateMarkScreenAction(ObjectInfo* info);


        #region Maybe TODO
        //public abstract bool ProcessMinimapEnabled { get; private protected set; }
        //public abstract bool ProcessMapEnabled { get; private protected set; }

        //private protected unsafe abstract void ProcessObjectiveOnMinimap(ObjectInfo* info);
        //private protected unsafe abstract void ProcessObjectiveOnMap(ObjectInfo* o);

        #endregion


        #region Helpers

        private protected void DrawFlagButton(string id, Vector3 mapCoordToFlag)
        {
            if (ImGui.Button($"Set flag on map##{GetType().Name}_{id}"))
            {
                HasFlagToProcess = true;
                FlaggedMapCoord = new Vector2(mapCoordToFlag.X, mapCoordToFlag.Y);
            }
        }

        private protected virtual unsafe bool DrawScreenMarkerDefault(GameObject* obj,
            ImGuiScene.TextureWrap icon, Vector2 iconSize, float iconAlpha, string info,
            Vector4 infoTextColour, out Vector2 lastDrawEndPos)
        {
            lastDrawEndPos = new(0, 0);
            if (obj == null) return false;

            bool inFrontOfCamera = Projection.WorldToScreen(obj->Position, out var hitboxScrPos);

            lastDrawEndPos = hitboxScrPos;
            lastDrawEndPos.Y -= ImGui.GetMainViewport().Size.Y / 50; // slightly raise it up from hitbox screen pos

            lastDrawEndPos = PushToSideOnXIfNeeded(lastDrawEndPos, inFrontOfCamera);

            var altidueDiff = CompassUtil.GetAltitudeDiffFromPlayer(obj);

            // Draw direction indicator
            DrawDirectionIcon(lastDrawEndPos, IconManager.DebugMarkerIconColour, out float rotationFromUpward, out lastDrawEndPos);
            // Marker
            bool markerDrawn = DrawScreenMarkerIcon(icon.ImGuiHandle, lastDrawEndPos, iconSize, true, iconAlpha, out lastDrawEndPos);
            if (markerDrawn)
            {
                // Altitude
                DrawAltitudeDiffIcon(altidueDiff, lastDrawEndPos, true, iconAlpha, out _);
                // Info
                DrawExtraInfoByMarker(info, infoTextColour, config.ScreenMarkFontSize, lastDrawEndPos,
                    iconSize, rotationFromUpward, out _);
            }
            return markerDrawn;
        }

        private protected bool DrawDirectionIcon(Vector2 screenPosRaw, uint colour, out float rotationFromUpward, out Vector2 drawEndPos)
        {
            drawEndPos = screenPosRaw;
            rotationFromUpward = 0;
            var icon = iconManager.DirectionScreenIndicatorIcon;
            if (icon == null) return false;
            var iconSize = IconManager.DirectionScreenIndicatorIconSize;
            rotationFromUpward = CompassUi.GetAngleOnScreen(drawEndPos);
            // Flip the direction indicator along X when not inside viewport;
            if (!CompassUi.IsScreenPosInsideMainViewport(drawEndPos))
                rotationFromUpward = -rotationFromUpward;
            drawEndPos = CompassUi.GetConstrainedScreenPos(screenPosRaw, config.ScreenMarkConstraint, iconSize / 4);
            drawEndPos -= iconSize / 2;
            (var p1, var p2, var p3, var p4) = CompassUi.GetRotatedPointsOnScreen(drawEndPos, iconSize, rotationFromUpward);
            ImGui.GetWindowDrawList().AddImageQuad(icon.ImGuiHandle, p1, p2, p3, p4, new(0, 0), new(1, 0), new(1, 1), new(0, 1), colour);
            var iconCentre = (p1 + p3) / 2;
            drawEndPos = new Vector2(iconCentre.X + iconSize.Y / 2 * MathF.Sin(rotationFromUpward), 
                iconCentre.Y + iconSize.X / 2 * MathF.Cos(rotationFromUpward));
            return true;
        }

        private protected static bool DrawScreenMarkerIcon(IntPtr iconTexHandle, Vector2 drawScreenPos, 
            Vector2 iconSize, bool posIsRaw, float alpha, out Vector2 drawEndPos)
        {
            drawEndPos = drawScreenPos;
            if (iconTexHandle == IntPtr.Zero) return false;
            if (posIsRaw)
                drawEndPos -= iconSize / 2;
            ImGui.GetWindowDrawList().AddImage(iconTexHandle, drawEndPos, drawEndPos + iconSize, 
                new(0,0), new(1,1), ImGui.ColorConvertFloat4ToU32(new(1,1,1,alpha)));
            return true;
        }

        private protected bool DrawAltitudeDiffIcon(float altDiff, Vector2 screenPos, bool posIsRaw, float alpha, out Vector2 drawEndPos)
        {
            drawEndPos = screenPos;
            ImGuiScene.TextureWrap? icon = null;
            if (altDiff > 10) icon = iconManager.AltitudeHigherIcon;
            if (altDiff < -10) icon = iconManager.AltitudeLowerIcon;
            if (icon == null) return false;
            if (posIsRaw)
                drawEndPos -= IconManager.AltitudeIconSize / 2;
            ImGui.GetWindowDrawList().AddImage(icon.ImGuiHandle, drawEndPos, drawEndPos + IconManager.AltitudeIconSize,
                new(0,0), new(1,1), ImGui.ColorConvertFloat4ToU32(new(1,1,1,alpha)));
            drawEndPos += IconManager.AltitudeIconSize / 2;
            return true;
        }

        private protected static bool DrawExtraInfoByMarker(string info, Vector4 colour, float fontSize,
            Vector2 markerScreenPos, Vector2 markerSize, float directionRotationFromUpward, out Vector2 drawEndPos)
        {
            drawEndPos = markerScreenPos;
            if (string.IsNullOrEmpty(info)) return false;
            if (directionRotationFromUpward > -.95f)
            {
                // direction indicator would be on left side, so just draw text on right
                drawEndPos.X += markerSize.X + 2;
                ImGui.GetWindowDrawList().AddText(ImGui.GetFont(), fontSize, drawEndPos, ImGui.ColorConvertFloat4ToU32(colour), info);
            }
            else
            {
                // direction indicator would be on right side, so draw text on the left
                var size = CompassUi.GetTextSize(info, fontSize);
                drawEndPos.X -= size.X + 2;
                ImGui.GetWindowDrawList().AddText(ImGui.GetFont(), fontSize, drawEndPos, ImGui.ColorConvertFloat4ToU32(colour), info);
            }
            return true;
        }

        private protected static Vector2 PushToSideOnXIfNeeded(Vector2 drawPos, bool posInFrontOfCamera)
        {
            if (!posInFrontOfCamera && CompassUi.IsScreenPosInsideMainViewport(drawPos))
            {
                var viewport = ImGui.GetMainViewport();
                // Fix X-axis for some objs: push all those not in front of camera to side
                //  so that they don't dangle in the middle of the screen
                drawPos.X = drawPos.X - CompassUi.GetScreenCentre().X > 0
                    ? (viewport.Pos.X + viewport.Size.X) : viewport.Pos.X;
            }
            return drawPos;
        }

        #endregion
    }
}
