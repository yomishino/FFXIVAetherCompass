using AetherCompass.Common;
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

        private protected virtual unsafe bool DrawScreenMarkAllDefault(GameObject* obj,
            ImGuiScene.TextureWrap icon, Vector2 iconSize, float iconAlpha, out Vector2 lastDrawEndPos)
        {
            lastDrawEndPos = new(0, 0);
            if (obj == null) return false;
            // Use hitbox pos instead of the two pos from ObjectInfo because they are often inconsistent/out-of-date esp. when obj not on screen
            bool inFrontOfCamera = CompassUtil.WorldToScreenPos(obj->Position, out var hitboxScrPos);
            bool insideViewport = CompassUtil.IsScreenPosInsideMainViewport(hitboxScrPos, iconSize);

            lastDrawEndPos = hitboxScrPos;
            lastDrawEndPos.Y -= ImGui.GetMainViewport().Size.Y / 50; // slightly raise it up from hitbox screen pos

            var altidueDiff = CompassUtil.GetAltitudeDiffFromPlayer(obj);

            lastDrawEndPos = FixDrawPos(lastDrawEndPos, inFrontOfCamera, insideViewport);

            // Draw direction indicator
            bool dirDrawn = DrawDirectionIcon(lastDrawEndPos, IconManager.DebugMarkerIconColour, out lastDrawEndPos);
            // Marker
            bool markerDrawn = DrawScreenMarkerIcon(icon.ImGuiHandle, lastDrawEndPos, iconSize, !dirDrawn, iconAlpha, out lastDrawEndPos);
            // Altitude
            DrawAltitudeDiffIcon(altidueDiff, lastDrawEndPos, true, iconAlpha, out lastDrawEndPos);
            return markerDrawn;
        }

        private protected bool DrawDirectionIcon(Vector2 screenPosRaw, uint colour, out Vector2 nextDrawScreenPos)
        {
            nextDrawScreenPos = screenPosRaw;
            var icon = iconManager.DirectionScreenIndicatorIcon;
            if (icon == null) return false;
            var iconSize = IconManager.DirectionScreenIndicatorIconSize;
            nextDrawScreenPos = CompassUtil.GetConstrainedScreenPos(screenPosRaw, config.ScreenMarkConstraint, iconSize / 4);
            nextDrawScreenPos -= iconSize / 2;
            var rotation = CompassUtil.GetAngleOnScreen(nextDrawScreenPos);
            (var p1, var p2, var p3, var p4) = CompassUtil.GetRotatedPointsOnScreen(nextDrawScreenPos, iconSize, rotation);
            ImGui.GetWindowDrawList().AddImageQuad(icon.ImGuiHandle, p1, p2, p3, p4, new(0, 0), new(1, 0), new(1, 1), new(0, 1), colour);
            var scrCentre = CompassUtil.GetScreenCentre();
            nextDrawScreenPos -= new Vector2(MathF.CopySign(2 * iconSize.X / 3, nextDrawScreenPos.X - scrCentre.X),
                MathF.CopySign(2 * iconSize.Y / 3, nextDrawScreenPos.Y - scrCentre.Y));
            return true;
        }

        private protected bool DrawScreenMarkerIcon(IntPtr iconTexHandle, Vector2 drawScreenPos, 
            Vector2 iconSize, bool posIsRaw, float alpha, out Vector2 nextDrawPos)
        {
            nextDrawPos = drawScreenPos;
            if (iconTexHandle == IntPtr.Zero) return false;
            if (posIsRaw)
                nextDrawPos -= iconSize / 2;
            ImGui.GetWindowDrawList().AddImage(iconTexHandle, nextDrawPos, nextDrawPos + iconSize, 
                new(0,0), new(1,1), ImGui.ColorConvertFloat4ToU32(new(1,1,1,alpha)));
            return true;
        }

        private protected bool DrawAltitudeDiffIcon(float altDiff, Vector2 screenPos, bool posIsRaw, float alpha, out Vector2 nextDrawPos)
        {
            nextDrawPos = screenPos;
            ImGuiScene.TextureWrap? icon = null;
            if (altDiff > 10) icon = iconManager.AltitudeHigherIcon;
            if (altDiff < -10) icon = iconManager.AltitudeLowerIcon;
            if (icon == null) return false;
            if (posIsRaw)
                nextDrawPos -= IconManager.AltitudeIconSize / 2;
            ImGui.GetWindowDrawList().AddImage(icon.ImGuiHandle, nextDrawPos, nextDrawPos + IconManager.AltitudeIconSize,
                new(0,0), new(1,1), ImGui.ColorConvertFloat4ToU32(new(1,1,1,alpha)));
            nextDrawPos += IconManager.AltitudeIconSize / 2;
            return true;
        }

        private protected static Vector2 FixDrawPos(Vector2 drawPos, bool posInFrontOfCamera, bool posInsideViewport)
        {
            // Fix X-axis for some objs: push all those not in front of camera to side
            //  so that they don't dangle in the middle of the screen
            if (!posInFrontOfCamera && posInsideViewport)
            {
                var viewport = ImGui.GetMainViewport();
                drawPos.X = drawPos.X - CompassUtil.GetScreenCentre().X > 0
                    ? (viewport.Pos.X + viewport.Size.X) : viewport.Pos.X;
            }

            // TODO: there's also problems on Y-axis when altitude difference and camera rotation on Y all mix together,
            // because we use screen Y-axis to show both world-Z and altitude but they sometimes conflict with each other
            // I dont know how to fix them right now

            return drawPos;
        }

        #endregion
    }
}
