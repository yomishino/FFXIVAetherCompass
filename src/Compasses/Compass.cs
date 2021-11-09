using AetherCompass.Common;
using AetherCompass.Configs;
using AetherCompass.UI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using System;
using System.Numerics;



namespace AetherCompass.Compasses
{
    public abstract class Compass
    {
        private protected readonly IconManager iconManager = null!;
        private protected readonly Notifier notifier = new();
        private protected readonly Configuration config = null!;
        private protected readonly ICompassConfig compassConfig = null!;

        private bool ready = false;

        // Record last and 2nd last closest to prevent frequent notification when player is at a pos close to two objs
        private (IntPtr Ptr, float Distance3D, IntPtr LastClosest, IntPtr SecondLast) closestObj 
            = (IntPtr.Zero, float.MaxValue, IntPtr.Zero, IntPtr.Zero);
        private DateTime closestObjLastChangedTime = DateTime.MinValue;
        private const int closestObjResetDelayInSec = 10;
        
        internal bool HasFlagToProcess = false; // For notifying CompassManager
        internal Vector2 FlaggedMapCoord;


        public abstract string Description { get; }
        public abstract bool CompassEnabled { get; internal set; }
        public abstract bool DrawDetailsEnabled { get; private protected set; }
        public abstract bool MarkScreenEnabled { get; private protected set; }

        private protected abstract string ClosestObjectDescription { get; }
        

        public Compass(Configuration config, ICompassConfig compassConfig, IconManager iconManager)
        {
            this.config = config;
            this.compassConfig = compassConfig;
            this.iconManager = iconManager;
            ready = true;
        }

        private protected unsafe abstract bool IsObjective(GameObject* o);
        public unsafe abstract Action? CreateDrawDetailsAction(GameObject* o);
        public unsafe abstract Action? CreateMarkScreenAction(GameObject* o);

        #region Maybe TODO
        //public abstract bool ProcessMinimapEnabled { get; private protected set; }
        //public abstract bool ProcessMapEnabled { get; private protected set; }

        //private protected unsafe abstract void ProcessObjectiveOnMinimap(ObjectInfo* info);
        //private protected unsafe abstract void ProcessObjectiveOnMap(ObjectInfo* o);

        #endregion

        public unsafe virtual bool CheckObject(GameObject* o)
        {
            if (o == null) return false;
            if (IsObjective(o))
            {
                var dist = CompassUtil.Get3DDistanceFromPlayer(o);
                if (o->ObjectID != Plugin.ClientState.LocalPlayer?.ObjectId && dist < closestObj.Distance3D)
                {
                    closestObj.Ptr = (IntPtr)o;
                    closestObj.Distance3D = dist;
                }
                return true;
            }
            return false;
        }

        public unsafe virtual void OnLoopEnd()
        {
            HasFlagToProcess = false;
            if (ready)
            {
                if ((DateTime.UtcNow - closestObjLastChangedTime).TotalSeconds > closestObjResetDelayInSec)
                {
                    closestObj.SecondLast = IntPtr.Zero;
                    closestObjLastChangedTime = DateTime.UtcNow;
                    //Plugin.LogDebug($"{GetType().Name}:reset2");
                }
                if (closestObj.Ptr != IntPtr.Zero && closestObj.Ptr != closestObj.LastClosest && closestObj.Ptr != closestObj.SecondLast)
                {
                    var obj = (GameObject*)closestObj.Ptr;
                    if (obj != null)
                    {
                        var dir = CompassUtil.GetDirectionFromPlayer(obj);
                        var coord = CompassUtil.GetMapCoordInCurrentMap(obj->Position);
                        if (compassConfig.NotifyChat)
                        {
                            var msg = Chat.CreateMapLink(Plugin.ClientState.TerritoryType, CompassUtil.GetCurrentMapId(), coord, false);   // TODO: showZ?
                            msg.PrependText($"Found {ClosestObjectDescription} at ");
                            msg.AppendText($", on {dir}, {closestObj.Distance3D:0.0} yalms from you");
                            notifier.TryNotifyByChat(GetType().Name, msg, compassConfig.NotifySe, compassConfig.NotifySeId);
                        }
                        if (compassConfig.NotifyToast)
                        {
                            var msg = $"Found {ClosestObjectDescription} on {dir}, {closestObj.Distance3D:0} yalms from you, at {CompassUtil.MapCoordToFormattedString(coord)}";
                            notifier.TryNotifyByToast(msg);
                        }
                    }
                    //Plugin.LogDebug($"{GetType().Name}:reset1:BEFORE: {closestObj.LastClosest}, {closestObj.SecondLast}");
                    // Set new SecondLast two old LastClosest; then reset LastClosest
                    closestObj.SecondLast = closestObj.LastClosest;
                    closestObj.LastClosest = closestObj.Ptr;
                    closestObjLastChangedTime = DateTime.UtcNow;
                    //Plugin.LogDebug($"{GetType().Name}:reset1:AFTER: {closestObj.LastClosest}, {closestObj.SecondLast}");
                }
            }
            closestObj.Ptr = IntPtr.Zero;
            closestObj.Distance3D = float.MaxValue;
        }

        public async void OnZoneChange()
        {
            ready = false;
            await System.Threading.Tasks.Task.Delay(2500);
            ready = true;
            closestObj = (IntPtr.Zero, float.MaxValue, IntPtr.Zero, IntPtr.Zero);
        }


        #region Helpers

        private protected void DrawFlagButton(string id, Vector3 mapCoordToFlag)
        {
            if (ImGui.Button($"Set flag on map##{GetType().Name}_{id}"))
            {
                HasFlagToProcess = true;
                FlaggedMapCoord = new Vector2(mapCoordToFlag.X, mapCoordToFlag.Y);
            }
        }

        // TODO: found a bug that when logged out then log in, crash the game; may be fixed by clearing draw action queues

        private protected virtual unsafe bool DrawScreenMarkerDefault(GameObject* obj, 
            ImGuiScene.TextureWrap icon, Vector2 iconSize, float iconAlpha, string info,
            Vector4 infoTextColour, out Vector2 lastDrawEndPos)
        {
            lastDrawEndPos = new(0, 0);
            if (obj == null) return false;

            bool inFrontOfCamera = CompassUi.WorldToScreenPos(obj->Position, out var hitboxScrPos);

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

        private protected bool DrawDirectionIcon(Vector2 screenPosRaw, 
            uint colour, out float rotationFromUpward, out Vector2 drawEndPos)
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

        private protected static bool DrawScreenMarkerIcon(IntPtr iconTexHandle, 
            Vector2 drawScreenPos, Vector2 iconSize, bool posIsRaw, float alpha, out Vector2 drawEndPos)
        {
            drawEndPos = drawScreenPos;
            if (iconTexHandle == IntPtr.Zero) return false;
            if (posIsRaw)
                drawEndPos -= iconSize / 2;
            ImGui.GetWindowDrawList().AddImage(iconTexHandle, drawEndPos, drawEndPos + iconSize,
                new(0, 0), new(1, 1), ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, alpha)));
            return true;
        }

        private protected bool DrawAltitudeDiffIcon(float altDiff, Vector2 screenPos, 
            bool posIsRaw, float alpha, out Vector2 drawEndPos)
        {
            drawEndPos = screenPos;
            ImGuiScene.TextureWrap? icon = null;
            if (altDiff > 10) icon = iconManager.AltitudeHigherIcon;
            if (altDiff < -10) icon = iconManager.AltitudeLowerIcon;
            if (icon == null) return false;
            if (posIsRaw)
                drawEndPos -= IconManager.AltitudeIconSize / 2;
            ImGui.GetWindowDrawList().AddImage(icon.ImGuiHandle, drawEndPos, drawEndPos + IconManager.AltitudeIconSize,
                new(0, 0), new(1, 1), ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, alpha)));
            drawEndPos += IconManager.AltitudeIconSize / 2;
            return true;
        }

        private protected static bool DrawExtraInfoByMarker(string info, Vector4 colour, 
            float fontSize, Vector2 markerScreenPos, Vector2 markerSize, float directionRotationFromUpward, out Vector2 drawEndPos)
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
