using AetherCompass.Common;
using AetherCompass.Compasses.Objectives;
using AetherCompass.Configs;
using AetherCompass.Game;
using AetherCompass.UI;
using AetherCompass.UI.GUI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ObjectInfo = FFXIVClientStructs.FFXIV.Client.UI.UI3DModule.ObjectInfo;
using ImGuiNET;
using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;



namespace AetherCompass.Compasses
{
    public abstract class Compass
    {
        private protected readonly CompassConfig compassConfig = null!;

        private bool ready = false;

        // Record last and 2nd last closest to prevent frequent notification when player is at a pos close to two objs
        private CachedCompassObjective? closestObj;
        private IntPtr closestObjPtrLast;
        private IntPtr closestObjPtrSecondLast;
        private DateTime closestObjLastChangedTime = DateTime.MinValue;
        private const int closestObjResetDelayInSec = 60;
        

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

        public virtual bool MarkScreen => Plugin.Config.ShowScreenMark && compassConfig.MarkScreen;
        public virtual bool ShowDetail => Plugin.Config.ShowDetailWindow && compassConfig.ShowDetail;

        public virtual bool NotifyChat => Plugin.Config.NotifyChat && compassConfig.NotifyChat;
        public virtual bool NotifySe => Plugin.Config.NotifySe && compassConfig.NotifySe;
        public virtual bool NotifyToast => Plugin.Config.NotifyToast && compassConfig.NotifyToast;


        public Compass(CompassConfig compassConfig)
        {
            this.compassConfig = compassConfig;
            _compassEnabled = compassConfig.Enabled;   // assign to field to avoid trigger Icon manager when init
            ready = true;
        }


        public abstract bool IsEnabledInCurrentTerritory();
        public unsafe abstract bool IsObjective(GameObject* o);
        private protected unsafe abstract string GetClosestObjectiveDescription(CachedCompassObjective objective);
        public unsafe abstract DrawAction? CreateDrawDetailsAction(CachedCompassObjective objective);
        public unsafe abstract DrawAction? CreateMarkScreenAction(CachedCompassObjective objective);

        private protected abstract void DisposeCompassUsedIcons();

        protected unsafe virtual CachedCompassObjective CreateCompassObjective(GameObject* obj)
            => new(obj);

        public unsafe virtual void UpdateClosestObjective(CachedCompassObjective objective)
        {
            if (closestObj == null) closestObj = objective;
            else if (objective.Distance3D < closestObj.Distance3D)
                closestObj = objective;
        }

        public virtual void ProcessOnLoopStart()
        { }

        public virtual void ProcessOnLoopEnd()
        {
            ProcessClosestObjOnLoopEnd();
        }

        public unsafe void ProcessLoop(ObjectInfo** infoArray, int count, CancellationToken token)
        {
            Task.Run(() =>
            {
                ProcessOnLoopStart();
                for (int i = 0; i < count; i++)
                {
                    if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();
                    var info = infoArray[i];
                    var obj = info != null ? info->GameObject : null;
                    if (obj == null || obj->ObjectKind == (byte)ObjectKind.Pc) continue;
                    if (!IsObjective(obj)) continue;
                    var objective = CreateCompassObjective(obj);
                    ProcessObjectiveInLoop(objective);
                }
                if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();
                ProcessOnLoopEnd();
            }, token).ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    foreach (var e in t.Exception.InnerExceptions)
                    {
                        if (e is OperationCanceledException or ObjectDisposedException) continue;
                        Plugin.LogError(e.ToString());
                    }
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

#if DEBUG
        public unsafe void ProcessLoopDebugAllObjects(GameObject** GameObjectList, int count, CancellationToken token)
        {
            Task.Run(() =>
            {
                ProcessOnLoopStart();
                for (int i = 0; i < count; i++)
                {
                    if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();
                    var obj = GameObjectList[i];
                    if (obj == null) continue;
                    if (!IsObjective(obj)) continue;
                    var objective = CreateCompassObjective(obj);
                    ProcessObjectiveInLoop(objective);
                }
                if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();
                ProcessOnLoopEnd();
            }, token).ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    foreach (var e in t.Exception.InnerExceptions)
                    {
                        if (e is OperationCanceledException or ObjectDisposedException) continue;
                        Plugin.LogError(e.ToString());
                    }
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
#endif

        private void ProcessObjectiveInLoop(CachedCompassObjective objective)
        {
            UpdateClosestObjective(objective);

            if (ShowDetail)
            {
                var action = CreateDrawDetailsAction(objective);
                Plugin.DetailsWindow.AddDrawAction(this, action);
            }
            if (MarkScreen)
            {
                var action = CreateMarkScreenAction(objective);
                Plugin.Overlay.AddDrawAction(action);
            }
        }

        private void ProcessObjectiveInLoop(CachedCompassObjective objective, CancellationToken token)
        {
            try
            {
                Task.Run(() =>
                {
                    ProcessObjectiveInLoop(objective);
                }, token).ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        foreach (var e in t.Exception.InnerExceptions)
                            Plugin.LogError(e.ToString());
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            catch (AggregateException e) 
            { 
                if (e.InnerException is not (TaskCanceledException or ObjectDisposedException))
                    throw;
            }
        }

        private unsafe void ProcessClosestObjOnLoopEnd()
        {
            if (ready)
            {
                if ((DateTime.UtcNow - closestObjLastChangedTime).TotalSeconds > closestObjResetDelayInSec)
                {
                    closestObjPtrSecondLast = IntPtr.Zero;
                    closestObjLastChangedTime = DateTime.UtcNow;
                }
                else if (closestObj != null && !closestObj.IsEmpty()
                    && !closestObj.IsCacheFor(closestObjPtrLast)
                    && !closestObj.IsCacheFor(closestObjPtrSecondLast))
                {
                    if (NotifyChat)
                    {
                        var msg = Chat.CreateMapLink(
                            Plugin.ClientState.TerritoryType, ZoneWatcher.CurrentMapId,
                            closestObj.CurrentMapCoord, CompassUtil.CurrentHasZCoord());
                        msg.PrependText($"Found {GetClosestObjectiveDescription(closestObj)} at ");
                        msg.AppendText($", on {closestObj.CompassDirectionFromPlayer}, " +
                            $"{CompassUtil.DistanceToDescriptiveString(closestObj.Distance3D, false)} from you");
                        Notifier.TryNotifyByChat(msg, NotifySe, compassConfig.NotifySeId);
                    }
                    if (NotifyToast)
                    {
                        var msg =
                            $"Found {GetClosestObjectiveDescription(closestObj)} " +
                            $"on {closestObj.CompassDirectionFromPlayer}, " +
                            $"{CompassUtil.DistanceToDescriptiveString(closestObj.Distance3D, true)} from you, " +
                            $"at {CompassUtil.MapCoordToFormattedString(closestObj.CurrentMapCoord)}";
                        Notifier.TryNotifyByToast(msg);
                    }
                    closestObjPtrSecondLast = closestObjPtrLast;
                    closestObjPtrLast = closestObj.GameObject;
                    closestObjLastChangedTime = DateTime.UtcNow;
                }
            }
        }

        public virtual void Reset()
        {
            closestObj = null;
        }


        public async virtual void OnZoneChange()
        {
            ready = false;
            Reset();
            closestObjPtrLast = IntPtr.Zero;
            closestObjPtrSecondLast = IntPtr.Zero;
            await Task.Delay(2500);
            ready = true;
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
                    if (Plugin.Config.ShowScreenMark)
                    {
                        ImGuiEx.Checkbox("Mark detected objects on screen", ref compassConfig.MarkScreen,
                            "Mark objects detected by this compass on screen, showing the direction and distance.");
                    }
                    if (Plugin.Config.ShowDetailWindow)
                    {
                        ImGuiEx.Checkbox("Show objects details", ref compassConfig.ShowDetail,
                            "List details of objects detected by this compass in the Details Window.");
                    }
                    if (Plugin.Config.NotifyChat)
                    {
                        ImGuiEx.Checkbox("Chat Notification", ref compassConfig.NotifyChat,
                            "Allow this compass to send a chat message about an object detected.");
                        if (Plugin.Config.NotifySe)
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
                    if (Plugin.Config.NotifyToast)
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

        protected void DrawFlagButton(string id, Vector3 mapCoordToFlag)
        {
            if (ImGui.Button($"Set flag on map##{CompassName}_{id}"))
                Plugin.CompassManager.RegisterMapFlag(new(mapCoordToFlag.X, mapCoordToFlag.Y));
        }

        internal static DrawAction? GenerateConfigDummyMarkerDrawAction(string info, float scale)
        {
            var icon = IconManager.ConfigDummyMarkerIcon;
            if (icon == null) info = "(Failed to load icon)\n" + info;
            var drawPos = UiHelper.GetScreenCentre();
            return DrawAction.Combine(important: true,
                GenerateScreenMarkerIconDrawAction(icon, drawPos, IconManager.MarkerIconSize, true, scale, 1, out drawPos),
                GenerateExtraInfoDrawAction(info, scale, new(1, 1, 1, 1), 0, drawPos, IconManager.MarkerIconSize, 0, out _));
        }
        
        protected static DrawAction? GenerateDefaultScreenMarkerDrawAction(CachedCompassObjective obj,
            ImGuiScene.TextureWrap? icon, Vector2 iconSizeRaw, float iconAlpha, string info,
            Vector4 infoTextColour, float textShadowLightness, out Vector2 lastDrawEndPos, bool important = false)
        {
            Vector3 hitboxPosAdjusted = new(obj.Position.X, obj.Position.Y + obj.GameObjectHeight + .5f, obj.Position.Z);
            bool inFrontOfCamera = UiHelper.WorldToScreenPos(hitboxPosAdjusted, out lastDrawEndPos);
            lastDrawEndPos = PushToSideOnXIfNeeded(lastDrawEndPos, inFrontOfCamera);

            // Direction indicator
            var directionIconDrawAction = GenerateDirectionIconDrawAction(lastDrawEndPos,
                Plugin.Config.ScreenMarkConstraint, Plugin.Config.ScreenMarkSizeScale,
                IconManager.DirectionScreenIndicatorIconColour,
                out float rotationFromUpward, out lastDrawEndPos);
            // Marker
            var markerIconDrawAction = GenerateScreenMarkerIconDrawAction(icon, lastDrawEndPos,
                iconSizeRaw, true, Plugin.Config.ScreenMarkSizeScale, iconAlpha, out lastDrawEndPos);
            // Altitude diff
            var altDiffIconDrawAction = markerIconDrawAction == null ? null
                : GenerateAltitudeDiffIconDrawAction(obj.AltitudeDiff, lastDrawEndPos, 
                    true, Plugin.Config.ScreenMarkSizeScale, iconAlpha, out _);
            // Extra info
            var extraInfoDrawAction = GenerateExtraInfoDrawAction(info, Plugin.Config.ScreenMarkSizeScale,
                infoTextColour, textShadowLightness, lastDrawEndPos, iconSizeRaw, rotationFromUpward, out _);
            return DrawAction.Combine(important, directionIconDrawAction, markerIconDrawAction, altDiffIconDrawAction, extraInfoDrawAction);
        }

        protected static DrawAction? GenerateDirectionIconDrawAction(Vector2 screenPosRaw,
            Vector4 screenMarkConstraint, float scale, uint colour, 
            out float rotationFromUpward, out Vector2 drawEndPos)
        {
            drawEndPos = screenPosRaw;
            rotationFromUpward = 0;
            var icon = IconManager.DirectionScreenIndicatorIcon;
            var iconSize = IconManager.DirectionScreenIndicatorIconSize * scale;
            rotationFromUpward = UiHelper.GetAngleOnScreen(drawEndPos);
            // Flip the direction indicator along X when not inside viewport;
            if (!UiHelper.IsScreenPosInsideMainViewport(drawEndPos))
                rotationFromUpward = -rotationFromUpward;
            drawEndPos = UiHelper.GetConstrainedScreenPos(screenPosRaw, screenMarkConstraint, iconSize / 4);
            drawEndPos -= iconSize / 2;
            (var p1, var p2, var p3, var p4) = UiHelper.GetRotatedPointsOnScreen(drawEndPos, iconSize, rotationFromUpward);
            var iconCentre = (p1 + p3) / 2;
            drawEndPos = new Vector2(iconCentre.X + iconSize.Y / 2 * MathF.Sin(rotationFromUpward),
                iconCentre.Y + iconSize.X / 2 * MathF.Cos(rotationFromUpward));
            return icon == null ? null 
                : new(() => ImGui.GetWindowDrawList().AddImageQuad(icon.ImGuiHandle,
                    p1, p2, p3, p4, new(0, 0), new(1, 0), new(1, 1), new(0, 1), colour));
        }

        protected static DrawAction? GenerateScreenMarkerIconDrawAction(
            ImGuiScene.TextureWrap? icon, Vector2 drawScreenPos, Vector2 iconSizeRaw, 
            bool posIsRaw, float scale, float alpha, out Vector2 drawEndPos)
        {
            var iconSize = iconSizeRaw * scale;
            drawEndPos = drawScreenPos;
            if (posIsRaw)
                drawEndPos -= iconSize / 2;
            var iconDrawPos = drawEndPos;
            return icon == null ? null 
                : new(() => ImGui.GetWindowDrawList().AddImage(icon.ImGuiHandle, 
                    iconDrawPos, iconDrawPos + iconSize, new(0, 0), new(1, 1), 
                    ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, alpha))));
        }

        protected static DrawAction? GenerateAltitudeDiffIconDrawAction(float altDiff, 
            Vector2 screenPos, bool posIsRaw, float scale, float alpha, out Vector2 drawEndPos)
        {
            drawEndPos = screenPos;

            ImGuiScene.TextureWrap? icon = null;
            if (altDiff > 5) icon = IconManager.AltitudeHigherIcon;
            if (altDiff < -5) icon = IconManager.AltitudeLowerIcon;
            if (icon == null) return null;
            var iconSize = IconManager.AltitudeIconSize * scale;
            if (posIsRaw)
                drawEndPos -= iconSize / 2;
            var iconDrawPos = drawEndPos;
            drawEndPos += iconSize / 2;
            return new(() => ImGui.GetWindowDrawList().AddImage(icon.ImGuiHandle, 
                iconDrawPos, iconDrawPos+ iconSize, new(0, 0), new(1, 1), 
                ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, alpha))));
        }

        protected static DrawAction? GenerateExtraInfoDrawAction(string info, float scale,
            Vector4 colour, float shadowLightness, Vector2 markerScreenPos,
            Vector2 markerSizeRaw, float directionRotationFromUpward, out Vector2 drawEndPos)
        {
            drawEndPos = markerScreenPos;
            if (string.IsNullOrEmpty(info)) return null;
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
                var size = UiHelper.GetTextSize(info, ImGui.GetFont(), fontsize);
                drawEndPos.X -= size.X + 2;
            }
            var textDrawPos = drawEndPos;
            return new(() => UiHelper.DrawTextWithShadow(ImGui.GetWindowDrawList(), info, 
                textDrawPos, ImGui.GetFont(), ImGui.GetFontSize(), scale, colour, shadowLightness));
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
