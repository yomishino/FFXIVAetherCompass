using AetherCompass.Common;
using AetherCompass.Configs;
using AetherCompass.UI;
using AetherCompass.UI.GUI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using System;
using System.Numerics;



namespace AetherCompass.Compasses
{
    public abstract class Compass
    {
        private protected readonly PluginConfig config = null!;
        private protected readonly CompassConfig compassConfig = null!;

        private protected readonly CompassDetailsWindow detailsWindow = null!;
        private protected readonly CompassOverlay overlay = null!;

        private bool ready = false;

        // Record last and 2nd last closest to prevent frequent notification when player is at a pos close to two objs
        private (IntPtr Ptr, float Distance3D, IntPtr LastClosest, IntPtr SecondLast) closestObj 
            = (IntPtr.Zero, float.MaxValue, IntPtr.Zero, IntPtr.Zero);
        private DateTime closestObjLastChangedTime = DateTime.MinValue;
        private const int closestObjResetDelayInSec = 60;
        
        internal bool HasFlagToProcess = false; // For notifying CompassManager
        internal Vector2 FlaggedMapCoord;


        public abstract string CompassName { get; }
        public abstract string Description { get; }
        

        private bool _compassEnabled = false;
        public bool CompassEnabled
        {
            get => _compassEnabled;
            set 
            {
                if (!value) DisposeCompassUsedIcons();
                _compassEnabled = value;
            }
        }

        public virtual bool MarkScreen => config.ShowScreenMark && compassConfig.MarkScreen;
        public virtual bool ShowDetail => config.ShowDetailWindow && compassConfig.ShowDetail;

        public virtual bool NotifyChat => config.NotifyChat && compassConfig.NotifyChat;
        public virtual bool NotifySe => config.NotifySe && compassConfig.NotifySe;
        public virtual bool NotifyToast => config.NotifyToast && compassConfig.NotifyToast;


        public Compass(PluginConfig config, CompassConfig compassConfig, CompassDetailsWindow detailsWindow, CompassOverlay overlay)
        {
            this.config = config;
            this.compassConfig = compassConfig;
            this.detailsWindow = detailsWindow;
            this.overlay = overlay;
            _compassEnabled = compassConfig.Enabled;   // assign to field to void trigger Icon manager when init
            ready = true;
        }


        public abstract bool IsEnabledTerritory(uint terr);
        private protected unsafe abstract bool IsObjective(GameObject* o);
        private protected unsafe abstract string GetClosestObjectiveDescription(GameObject* o);
        public unsafe abstract DrawAction? CreateDrawDetailsAction(GameObject* o);
        public unsafe abstract DrawAction? CreateMarkScreenAction(GameObject* o);

        private protected abstract void DisposeCompassUsedIcons();


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

        public unsafe virtual void OnLoopStart()
        {  }

        public unsafe virtual void OnLoopEnd()
        {
            HasFlagToProcess = false;
            if (ready)
            {
                if ((DateTime.UtcNow - closestObjLastChangedTime).TotalSeconds > closestObjResetDelayInSec)
                {
                    closestObj.SecondLast = IntPtr.Zero;
                    closestObjLastChangedTime = DateTime.UtcNow;
                }
                else if (closestObj.Ptr != IntPtr.Zero && closestObj.Ptr != closestObj.LastClosest && closestObj.Ptr != closestObj.SecondLast)
                {
                    var obj = (GameObject*)closestObj.Ptr;
                    if (obj != null)
                    {
                        var dir = CompassUtil.GetDirectionFromPlayer(obj);
                        var coord = CompassUtil.GetMapCoordInCurrentMap(obj->Position);
                        if (NotifyChat)
                        {
                            var msg = Chat.CreateMapLink(
                                Plugin.ClientState.TerritoryType, CompassUtil.GetCurrentMapId(), coord, CompassUtil.CurrentHasZCoord());
                            msg.PrependText($"Found {GetClosestObjectiveDescription(obj)} at ");
                            msg.AppendText($", on {dir}, {CompassUtil.DistanceToDescriptiveString(closestObj.Distance3D, false)} from you");
                            Notifier.TryNotifyByChat(msg, NotifySe, compassConfig.NotifySeId);
                        }
                        if (NotifyToast)
                        {
                            var msg = $"Found {GetClosestObjectiveDescription(obj)} on {dir}, " +
                                $"{CompassUtil.DistanceToDescriptiveString(closestObj.Distance3D, true)} from you, " +
                                $"at {CompassUtil.MapCoordToFormattedString(coord)}";
                            Notifier.TryNotifyByToast(msg);
                        }
                    }
                    closestObj.SecondLast = closestObj.LastClosest;
                    closestObj.LastClosest = closestObj.Ptr;
                    closestObjLastChangedTime = DateTime.UtcNow;
                }
            }
            closestObj.Ptr = IntPtr.Zero;
            closestObj.Distance3D = float.MaxValue;
        }


        public async virtual void OnZoneChange(ushort terr)
        {
            ready = false;
            await System.Threading.Tasks.Task.Delay(2500);
            ready = true;
            closestObj = (IntPtr.Zero, float.MaxValue, IntPtr.Zero, IntPtr.Zero);
        }


        #region Config UI
        public void DrawConfigUi()
        {
            ImGuiEx.Checkbox($"Enable Compass: {CompassName}", ref compassConfig.Enabled);
            // Also dispose icons if disabled
            if (compassConfig.Enabled != _compassEnabled) CompassEnabled = compassConfig.Enabled;
            ImGui.Indent();
            ImGui.Indent();
            ImGuiEx.IconTextCompass(nextSameLine: true);
            ImGui.TextWrapped(Description);
            ImGui.Unindent();
            if (compassConfig.Enabled)
            {
                ImGui.PushID($"{CompassName}");
                if (ImGui.TreeNode($"Compass settings"))
                {
                    if (config.ShowScreenMark)
                    {
                        ImGuiEx.Checkbox("Mark detected objects on screen", ref compassConfig.MarkScreen,
                            "Mark objects detected by this compass on screen, showing the direction and distance.");
                    }
                    if (config.ShowDetailWindow)
                    {
                        ImGuiEx.Checkbox("Show objects details", ref compassConfig.ShowDetail,
                            "List details of objects detected by this compass in the Details Window.");
                    }
                    if (config.NotifyChat)
                    {
                        ImGuiEx.Checkbox("Chat Notification", ref compassConfig.NotifyChat,
                            "Allow this compass to send a chat message about an object detected.");
                        if (config.NotifySe)
                        {
                            ImGuiEx.Checkbox("Sound Notification", ref compassConfig.NotifySe,
                                "Also allow this compass to make sound when sending chat message notification.");
                            if (compassConfig.NotifySe)
                            {
                                ImGuiEx.InputInt("Sound Effect ID", ref compassConfig.NotifySeId,
                                    "Input the Sound Effect ID for sound notification, from 1 to 16.\n\n" +
                                    "Sound Effect ID is the same as the game's macro sound effects <se.1>~<se.16>. " +
                                    "For example, if <se.1> is to be used, then enter \"1\" here.");
                                if (compassConfig.NotifySeId < 1) compassConfig.NotifySeId = 1;
                                if (compassConfig.NotifySeId > 16) compassConfig.NotifySeId = 16;
                            }
                        }
                    }
                    if (config.NotifyToast)
                    {
                        ImGuiEx.Checkbox("Toast Notification", ref compassConfig.NotifyToast,
                            "Allow this compass to make a Toast notification about an object detected.");
                    }
                    DrawConfigUiExtra();
                    ImGui.TreePop();
                }
                ImGui.PopID();
            }
            ImGui.Unindent();
        }

        public virtual void DrawConfigUiExtra() { }
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


        internal static bool DrawConfigDummyMarker(string info, float scale)
        {
            var icon = IconManager.ConfigDummyMarkerIcon;
            if (icon == null) return false;
            var drawPos = UiHelper.GetScreenCentre();
            if (DrawScreenMarkerIcon(icon.ImGuiHandle, drawPos, IconManager.MarkerIconSize, true, scale, 1, out drawPos))
            {
                DrawExtraInfoByMarker(info, scale, new(1, 1, 1, 1), 0, drawPos, IconManager.MarkerIconSize, 0, out _);
                return true;
            }
            return false;
        }
        
        private protected virtual unsafe bool DrawScreenMarkerDefault(GameObject* obj, 
            ImGuiScene.TextureWrap? icon, Vector2 iconSizeRaw, float iconAlpha, string info,
            Vector4 infoTextColour, float textShadowLightness, out Vector2 lastDrawEndPos)
        {
            lastDrawEndPos = new(0, 0);
            if (obj == null) return false;
            return DrawScreenMarkerDefault(obj->Position, obj->GetHeight(), icon, iconSizeRaw, iconAlpha,
                info, infoTextColour, textShadowLightness, out lastDrawEndPos);
        }

        private protected virtual bool DrawScreenMarkerDefault(Vector3 objWorldPos, float objHeight,
            ImGuiScene.TextureWrap? icon, Vector2 iconSizeRaw, float iconAlpha, string info,
            Vector4 infoTextColour, float textShadowLightness, out Vector2 lastDrawEndPos)
        {
            // Make the marker drawn slightly higher than object's hitbox position
            Vector3 hitboxPosAdjusted = new(objWorldPos.X, objWorldPos.Y + objHeight + .5f, objWorldPos.Z);
            bool inFrontOfCamera = UiHelper.WorldToScreenPos(hitboxPosAdjusted, out lastDrawEndPos);
            lastDrawEndPos = PushToSideOnXIfNeeded(lastDrawEndPos, inFrontOfCamera);

            var altidueDiff = CompassUtil.GetAltitudeDiffFromPlayer(objWorldPos);

            // Draw direction indicator
            DrawDirectionIcon(lastDrawEndPos, config.ScreenMarkSizeScale,
                IconManager.DirectionScreenIndicatorIconColour,
                out float rotationFromUpward, out lastDrawEndPos);
            // Marker
            bool markerDrawn = icon != null
                && DrawScreenMarkerIcon(icon.ImGuiHandle, lastDrawEndPos, iconSizeRaw, true,
                config.ScreenMarkSizeScale, iconAlpha, out lastDrawEndPos);
            if (markerDrawn)
            {
                // Altitude
                DrawAltitudeDiffIcon(altidueDiff, lastDrawEndPos, true,
                    config.ScreenMarkSizeScale, iconAlpha, out _);
                // Info
                DrawExtraInfoByMarker(info, config.ScreenMarkSizeScale, infoTextColour,
                    textShadowLightness, lastDrawEndPos, iconSizeRaw, rotationFromUpward, out _);
            }
            return markerDrawn;
        }

        private protected bool DrawDirectionIcon(Vector2 screenPosRaw, float scale,
            uint colour, out float rotationFromUpward, out Vector2 drawEndPos)
        {
            drawEndPos = screenPosRaw;
            rotationFromUpward = 0;
            var icon = IconManager.DirectionScreenIndicatorIcon;
            if (icon == null) return false;
            var iconSize = IconManager.DirectionScreenIndicatorIconSize * scale;
            rotationFromUpward = UiHelper.GetAngleOnScreen(drawEndPos);
            // Flip the direction indicator along X when not inside viewport;
            if (!UiHelper.IsScreenPosInsideMainViewport(drawEndPos))
                rotationFromUpward = -rotationFromUpward;
            drawEndPos = UiHelper.GetConstrainedScreenPos(screenPosRaw, config.ScreenMarkConstraint, iconSize / 4);
            drawEndPos -= iconSize / 2;
            (var p1, var p2, var p3, var p4) = UiHelper.GetRotatedPointsOnScreen(drawEndPos, iconSize, rotationFromUpward);
            ImGui.GetWindowDrawList().AddImageQuad(icon.ImGuiHandle, p1, p2, p3, p4, new(0, 0), new(1, 0), new(1, 1), new(0, 1), colour);
            var iconCentre = (p1 + p3) / 2;
            drawEndPos = new Vector2(iconCentre.X + iconSize.Y / 2 * MathF.Sin(rotationFromUpward), 
                iconCentre.Y + iconSize.X / 2 * MathF.Cos(rotationFromUpward));
            return true;
        }

        private protected static bool DrawScreenMarkerIcon(IntPtr iconTexHandle, 
            Vector2 drawScreenPos, Vector2 iconSizeRaw, bool posIsRaw,
            float scale, float alpha, out Vector2 drawEndPos)
        {
            var iconSize = iconSizeRaw * scale;
            drawEndPos = drawScreenPos;
            if (iconTexHandle == IntPtr.Zero) return false;
            if (posIsRaw)
                drawEndPos -= iconSize / 2;
            ImGui.GetWindowDrawList().AddImage(iconTexHandle, drawEndPos, drawEndPos + iconSize,
                new(0, 0), new(1, 1), ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, alpha)));
            return true;
        }

        private protected static bool DrawAltitudeDiffIcon(float altDiff, Vector2 screenPos, 
            bool posIsRaw, float scale, float alpha, out Vector2 drawEndPos)
        {
            drawEndPos = screenPos;
            
            ImGuiScene.TextureWrap? icon = null;
            if (altDiff > 5) icon = IconManager.AltitudeHigherIcon;
            if (altDiff < -5) icon = IconManager.AltitudeLowerIcon;
            if (icon == null) return false;
            var iconSize = IconManager.AltitudeIconSize * scale;
            if (posIsRaw)
                drawEndPos -= iconSize / 2;
            ImGui.GetWindowDrawList().AddImage(icon.ImGuiHandle, drawEndPos, drawEndPos + iconSize,
                new(0, 0), new(1, 1), ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, alpha)));
            drawEndPos += iconSize / 2;
            return true;
        }

        private protected static bool DrawExtraInfoByMarker(string info, float scale, 
            Vector4 colour, float shadowLightness, Vector2 markerScreenPos, 
            Vector2 markerSizeRaw, float directionRotationFromUpward, out Vector2 drawEndPos)
        {
            drawEndPos = markerScreenPos;
            if (string.IsNullOrEmpty(info)) return false;
            var fontsize = ImGui.GetFontSize() * scale;
            drawEndPos.Y += 2;  // make it slighly lower
            if (directionRotationFromUpward > -.95f)
            {
                // direction indicator would be on left side, so just draw text on right
                drawEndPos.X += markerSizeRaw.X * scale + 2;
            }
            else
            {
                // direction indicator would be on right side, so draw text on the left
                var size = UiHelper.GetTextSize(info, fontsize);
                drawEndPos.X -= size.X + 2;
            }
            UiHelper.DrawTextWithShadow(ImGui.GetWindowDrawList(), info, drawEndPos,
                ImGui.GetFont(), ImGui.GetFontSize(), scale, colour, shadowLightness);
            return true;
        }

        private protected static Vector2 PushToSideOnXIfNeeded(Vector2 drawPos, bool posInFrontOfCamera)
        {
            if (!posInFrontOfCamera && UiHelper.IsScreenPosInsideMainViewport(drawPos))
            {
                var viewport = ImGui.GetMainViewport();
                // Fix X-axis for some objs: push all those not in front of camera to side
                //  so that they don't dangle in the middle of the screen
                drawPos.X = drawPos.X - UiHelper.GetScreenCentre().X > 0
                    ? (viewport.Pos.X + viewport.Size.X) : viewport.Pos.X;
            }
            return drawPos;
        }

        #endregion
    }
}
