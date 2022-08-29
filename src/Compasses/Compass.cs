using AetherCompass.Common;
using AetherCompass.Common.Attributes;
using AetherCompass.Compasses.Objectives;
using AetherCompass.Game;
using AetherCompass.UI;
using AetherCompass.UI.GUI;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;
using ImGuiScene;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ObjectInfo = FFXIVClientStructs.FFXIV.Client.UI.UI3DModule.ObjectInfo;

namespace AetherCompass.Compasses
{
    public abstract class Compass
    {
        private bool ready = false;
        private CancellationTokenSource? cts = null;

        // Record last and 2nd last closest to prevent frequent notification when player is at a pos close to two objs
        private CachedCompassObjective? closestObj;
        private IntPtr closestObjPtrLast;
        private IntPtr closestObjPtrSecondLast;
        private DateTime closestObjLastChangedTime = DateTime.MinValue;
        private const int closestObjResetDelayInSec = 60;

        private readonly HashSet<uint> compassUsedIconIds = new();

        private CompassType _compassType = CompassType.Unknown;
        public CompassType CompassType
        {
            get
            {
                if (_compassType == CompassType.Unknown)
                    _compassType 
                        = (GetType().GetCustomAttributes(typeof(CompassTypeAttribute), false)[0] 
                            as CompassTypeAttribute)?.Type ?? CompassType.Invalid;
                return _compassType;
            }
        }

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

        public virtual bool MarkScreen => Plugin.Config.ShowScreenMark && CompassConfig.MarkScreen;
        public virtual bool ShowDetail => Plugin.Config.ShowDetailWindow && CompassConfig.ShowDetail;

        public virtual bool NotifyChat => Plugin.Config.NotifyChat && CompassConfig.NotifyChat;
        public virtual bool NotifySe => Plugin.Config.NotifySe && CompassConfig.NotifySe;
        public virtual bool NotifyToast => Plugin.Config.NotifyToast && CompassConfig.NotifyToast;


        public Compass()
        {
            _compassEnabled = CompassConfig.Enabled;   // assign to field to avoid trigger Icon manager when init
            ready = true;
        }


        #region To be overriden by children

        public abstract string CompassName { get; }
        public abstract string Description { get; }

        private protected abstract CompassConfig CompassConfig { get; }

        public abstract bool IsEnabledInCurrentTerritory();
        public unsafe abstract bool IsObjective(GameObject* o);
        
        protected unsafe virtual CachedCompassObjective CreateCompassObjective(GameObject* obj)
            => new(obj);
        protected unsafe virtual CachedCompassObjective CreateCompassObjective(ObjectInfo* info)
            => new(info);

        private protected unsafe abstract string 
            GetClosestObjectiveDescription(CachedCompassObjective objective);

        public unsafe abstract DrawAction? CreateDrawDetailsAction(CachedCompassObjective objective);
        public unsafe abstract DrawAction? CreateMarkScreenAction(CachedCompassObjective objective);

        #endregion


        #region Object processing related - Optionally overriden by children

        public unsafe virtual void UpdateClosestObjective(CachedCompassObjective objective)
        {
            if (closestObj == null) closestObj = objective;
            else if (objective.Distance3D < closestObj.Distance3D)
                closestObj = objective;
        }


        public virtual void ProcessOnLoopStart()
        {
            ProcessClosestObj();
        }

        public virtual void ProcessOnLoopEnd()
        {
            //ProcessClosestObj();
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

        #endregion


        #region Object processing during loop

        public unsafe void ProcessLoop(ObjectInfo** infoArray, int count)
        {
            cts = new();
            var token = cts.Token;
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
                    var objective = CreateCompassObjective(info);
                    ProcessObjectiveInLoop(objective);
                }
                if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();
                ProcessOnLoopEnd();
            }, token).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    foreach (var e in t.Exception!.InnerExceptions)
                    {
                        if (e is ObjectDisposedException) continue;
                        LogError(e.ToString());
                    }
                }
                ResetCancellation();
            }, CancellationToken.None);
        }

#if DEBUG
        public unsafe void ProcessLoopDebugAllObjects(GameObject** GameObjectList, int count)
        {
            cts = new();
            var token = cts.Token;
            Task.Run(() =>
            {
                ProcessOnLoopStart();
                for (int i = 0; i < count; i++)
                {
                    if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();
                    var obj = GameObjectList[i];
                    if (obj == null) continue;
                    if (!IsObjective(obj)) continue;
                    // no info about nameplate pos here though
                    var objective = CreateCompassObjective(obj);
                    ProcessObjectiveInLoop(objective);
                }
                if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();
                ProcessOnLoopEnd();
            }, token).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    foreach (var e in t.Exception!.InnerExceptions)
                    {
                        if (e is ObjectDisposedException) continue;
                        LogError(e.ToString());
                    }
                }
                ResetCancellation();
            }, CancellationToken.None);
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
                if (
#if DEBUG
                    Plugin.Config.DebugTestAllGameObjects ||
#endif
                    !Plugin.Config.HideScreenMarkIfNameplateInsideDisplayArea 
                    || !ShouldHideMarkerFor(objective))
                {
                    var action = CreateMarkScreenAction(objective);
                    Plugin.Overlay.AddDrawAction(action);
                }
            }
        }

        private unsafe void ProcessClosestObj()
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
                        Notifier.TryNotifyByChat(msg, NotifySe, CompassConfig.NotifySeId);
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
            closestObj = null;
        }

        public void CancelLastUpdate()
            => cts?.Cancel();

        private void ResetCancellation()
        {
            cts?.Dispose();
            cts = null;
        }

        #endregion


        #region Config UI
        public void DrawConfigUi()
        {
            var name = CompassType is CompassType.Experimental or CompassType.Debug
                ? $"[{CompassType}] ".ToUpper() + CompassName : CompassName;
            ImGuiEx.Checkbox(name, ref CompassConfig.Enabled);
            // Also dispose icons if disabled
            if (CompassConfig.Enabled != _compassEnabled) CompassEnabled = CompassConfig.Enabled;
            ImGui.Indent();
            ImGuiEx.IconTextCompass(nextSameLine: true);
            ImGui.TextWrapped(Description);
            if (CompassType == CompassType.Experimental)
                ImGui.TextDisabled("Experimental compasses may not work as expected.\nPlease enable with caution.");
            ImGui.Unindent();
            if (CompassConfig.Enabled)
            {
                ImGui.PushID($"{CompassName}");
                if (ImGui.TreeNode($"Compass settings"))
                {
                    ImGui.BulletText("UI:");
                    ImGui.Indent();
                    if (Plugin.Config.ShowScreenMark)
                        ImGuiEx.Checkbox("Mark detected objects on screen", ref CompassConfig.MarkScreen,
                            "Mark objects detected by this compass on screen, showing the direction and distance.");
                    else ImGui.TextDisabled("Mark-on-screen disabled in Plugin Settings");
                    if (Plugin.Config.ShowDetailWindow)
                        ImGuiEx.Checkbox("Show objects details", ref CompassConfig.ShowDetail,
                            "List details of objects detected by this compass in the Details Window.");
                    else ImGui.TextDisabled("Detail Window disabled in Plugin Settings");
                    ImGui.Unindent();

                    ImGui.BulletText("Notifications:");
                    ImGui.Indent();
                    if (Plugin.Config.NotifyChat)
                    {
                        ImGuiEx.Checkbox("Chat", ref CompassConfig.NotifyChat,
                            "Allow this compass to send a chat message about an object detected.");
                        if (Plugin.Config.NotifySe)
                        {
                            ImGuiEx.Checkbox("Sound", ref CompassConfig.NotifySe,
                                "Also allow this compass to make sound when sending chat message notification.");
                            if (CompassConfig.NotifySe)
                            {
                                ImGui.Indent();
                                ImGuiEx.InputInt("Sound Effect ID", 100, ref CompassConfig.NotifySeId,
                                    "Input the Sound Effect ID for sound notification, from 1 to 16.\n\n" +
                                    "Sound Effect ID is the same as the game's macro sound effects <se.1>~<se.16>. " +
                                    "For example, if <se.1> is to be used, then enter \"1\" here.");
                                if (CompassConfig.NotifySeId < 1) CompassConfig.NotifySeId = 1;
                                if (CompassConfig.NotifySeId > 16) CompassConfig.NotifySeId = 16;
                                ImGui.Unindent();
                            }
                        }
                        else ImGui.TextDisabled("Sound notification disabled in Plugin Settings");
                    }
                    else ImGui.TextDisabled("Chat notification disabled in Plugin Settings");
                    if (Plugin.Config.NotifyToast)
                    {
                        ImGuiEx.Checkbox("Toast", ref CompassConfig.NotifyToast,
                            "Allow this compass to make a Toast notification about an object detected.");
                    }
                    else ImGui.TextDisabled("Toast notification disabled in Plugin Settings");
                    ImGui.Unindent();

                    DrawConfigUiExtra();
                    ImGui.TreePop();
                }
                ImGui.PopID();
            }
        }

        public virtual void DrawConfigUiExtra() { }
        #endregion


        #region Drawing Helpers

        protected void DrawFlagButton(string id, Vector3 mapCoordToFlag)
        {
            if (ImGui.Button($"Set flag on map##{CompassName}_{id}"))
                Plugin.CompassManager.RegisterMapFlag(new(mapCoordToFlag.X, mapCoordToFlag.Y));
        }

        internal static DrawAction? GenerateConfigDummyMarkerDrawAction(string info, float markerSizeScale, float textRelSizeScale)
        {
            var icon = Plugin.IconManager.ConfigDummyMarkerIcon;
            if (icon == null) info = "(Failed to load icon)\n" + info;
            var drawPos = UiHelper.GetScreenCentre();
            return DrawAction.Combine(important: true,
                GenerateScreenMarkerIconDrawAction(icon, drawPos, IconManager.MarkerIconSize, markerSizeScale, 1, out drawPos),
                GenerateExtraInfoDrawAction(info, markerSizeScale, textRelSizeScale, 
                    new(1, 1, 1, 1), 0, drawPos, IconManager.MarkerIconSize, 0, out _));
        }

        protected static Vector2 DefaultMarkerIconSize
            => IconManager.MarkerIconSize;

        private static readonly Vector2 BaseMarkerSize 
            = DefaultMarkerIconSize + IconManager.DirectionScreenIndicatorIconSize;
        
        protected static DrawAction? GenerateDefaultScreenMarkerDrawAction(CachedCompassObjective obj,
            uint iconId, Vector2 iconSizeRaw, float iconAlpha, string info,
            Vector4 infoTextColour, float textShadowLightness, out Vector2 lastDrawEndPos, 
            bool important = false, bool showIfOutOfScreen = true)
        {
            Vector3 hitboxPosAdjusted = new(obj.Position.X, obj.Position.Y + obj.GameObjectHeight + .5f, obj.Position.Z);
            bool inFrontOfCamera = UiHelper.WorldToScreenPos(hitboxPosAdjusted, out var screenPos);
            if (!showIfOutOfScreen)
            {
                // Allow some extra space;
                // Also exclude those having screen position calculated to be
                // inside viewport but actually are at the back
                if (!UiHelper.IsScreenPosInsideMainViewport(screenPos, new(-20, 50, 20, -20))
                    || !inFrontOfCamera && UiHelper.IsScreenPosInsideMainViewport(screenPos))
                {
                    lastDrawEndPos = new();
                    return null;
                }
            }
            screenPos = PushToSideOnXIfNeeded(screenPos, inFrontOfCamera);
            float flippedOnScreenRotation = UiHelper.GetAngleOnScreen(screenPos, true);

            var scaledBaseMarkerSize = BaseMarkerSize * Plugin.Config.ScreenMarkSizeScale;

            lastDrawEndPos = UiHelper.GetConstrainedScreenPos(screenPos, Plugin.Config.ScreenMarkConstraint, scaledBaseMarkerSize / 4);


            // Direction indicator
            var directionIconDrawAction = GenerateDirectionIconDrawAction(lastDrawEndPos,
                flippedOnScreenRotation, Plugin.Config.ScreenMarkSizeScale, 
                IconManager.DirectionScreenIndicatorIconColour, out lastDrawEndPos);
            // Marker
            var icon = Plugin.IconManager.GetIcon(iconId);
            var markerIconDrawAction = GenerateScreenMarkerIconDrawAction(icon, lastDrawEndPos,
                iconSizeRaw, Plugin.Config.ScreenMarkSizeScale, iconAlpha, out lastDrawEndPos);
            // Altitude diff
            var altDiffIconDrawAction = markerIconDrawAction == null ? null
                : GenerateAltitudeDiffIconDrawAction(obj.AltitudeDiff, lastDrawEndPos, 
                    Plugin.Config.ScreenMarkSizeScale, iconAlpha, out _);
            // Extra info
            var extraInfoDrawAction = GenerateExtraInfoDrawAction(info, 
                Plugin.Config.ScreenMarkSizeScale, Plugin.Config.ScreenMarkTextRelSizeScale,
                infoTextColour, textShadowLightness, lastDrawEndPos, iconSizeRaw, flippedOnScreenRotation, out _);
            return DrawAction.Combine(important, directionIconDrawAction, markerIconDrawAction, altDiffIconDrawAction, extraInfoDrawAction);
        }

        protected static DrawAction? GenerateDirectionIconDrawAction(Vector2 drawPos, 
            float rotation, float scale, uint colour, out Vector2 drawEndPos)
        {
            var icon = Plugin.IconManager.DirectionScreenIndicatorIcon;
            var iconHalfSize = IconManager.DirectionScreenIndicatorIconSize * scale / 2;
            (var p1, var p2, var p3, var p4) = UiHelper.GetRotatedRectPointsOnScreen(
                drawPos, iconHalfSize, rotation);
            //var iconCentre = (p1 + p3) / 2;
            drawEndPos = new Vector2(drawPos.X + iconHalfSize.X * MathF.Sin(rotation),
                drawPos.Y + iconHalfSize.Y * MathF.Cos(rotation));
            return icon == null ? null
                : new(() => ImGui.GetWindowDrawList().AddImageQuad(icon.ImGuiHandle,
                    p1, p2, p3, p4, new(0, 0), new(1, 0), new(1, 1), new(0, 1), colour));
        }

        protected static DrawAction? GenerateScreenMarkerIconDrawAction(
            ImGuiScene.TextureWrap? icon, Vector2 screenPosRaw, Vector2 iconSizeRaw, 
            float scale, float alpha, out Vector2 drawEndPos)
        {
            var iconSize = iconSizeRaw * scale;
            drawEndPos = screenPosRaw - iconSize / 2;
            var iconDrawPos = drawEndPos;
            return icon == null ? null 
                : new(() => ImGui.GetWindowDrawList().AddImage(icon.ImGuiHandle, 
                    iconDrawPos, iconDrawPos + iconSize, new(0, 0), new(1, 1), 
                    ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, alpha))));
        }

        protected static DrawAction? GenerateAltitudeDiffIconDrawAction(float altDiff, 
            Vector2 screenPosRaw, float scale, float alpha, out Vector2 drawEndPos)
        {
            drawEndPos = screenPosRaw;
            ImGuiScene.TextureWrap? icon = null;
            if (altDiff > 5) icon = Plugin.IconManager.AltitudeHigherIcon;
            if (altDiff < -5) icon = Plugin.IconManager.AltitudeLowerIcon;
            if (icon == null) return null;
            var iconHalfSize = IconManager.AltitudeIconSize * scale / 2;
            return new(() => ImGui.GetWindowDrawList().AddImage(icon.ImGuiHandle,
                screenPosRaw - iconHalfSize, screenPosRaw + iconHalfSize, new(0, 0), new(1, 1), 
                ImGui.ColorConvertFloat4ToU32(new(1, 1, 1, alpha))));
        }

        protected static DrawAction? GenerateExtraInfoDrawAction(string info, 
            float markerSizeScale, float textRelSizeScale,
            Vector4 colour, float shadowLightness, Vector2 markerScreenPos,
            Vector2 markerSizeRaw, float rotation, out Vector2 drawEndPos)
        {
            drawEndPos = markerScreenPos;
            if (string.IsNullOrEmpty(info)) return null;
            var textSizeScale = markerSizeScale * textRelSizeScale;
            var fontsize = ImGui.GetFontSize() * textSizeScale;
            var textsize = UiHelper.GetTextSize(info, ImGui.GetFont(), fontsize);
            drawEndPos.Y += (markerSizeRaw.Y * markerSizeScale - textsize.Y) / 2 + markerSizeScale / textRelSizeScale;  // make it slighly lower
            bool rightAligned = false;
            if (rotation > -.2f)
            {
                // direction indicator would be on left side, so just draw text on right
                drawEndPos.X += markerSizeRaw.X * markerSizeScale + 2;
            }
            else
            {
                // direction indicator would be on right side, so draw text on the left
                drawEndPos.X -= textsize.X + 2;
                rightAligned = true;
            }
            var textDrawPos = drawEndPos;
            return new(() => UiHelper.DrawMultilineTextWithShadow(ImGui.GetWindowDrawList(), info,
                textDrawPos, ImGui.GetFont(), ImGui.GetFontSize(), textSizeScale, colour, shadowLightness, rightAligned));
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

        private TextureWrap? GetMarkerIcon(uint iconId)
        {
            compassUsedIconIds.Add(iconId);
            return Plugin.IconManager.GetIcon(iconId);
        }

        #endregion


        // y-axis is reversed (up is + instead of -) in NDC for nameplates
        private static Vector3 TranslateNormalisedNameplatePos3D(Vector3 pos3norm)
            => UiHelper.TranslateNormalisedCoordinates(pos3norm, true);

        private static Vector2 TranslateNormalisedNameplatePos2D(Vector3 pos3norm)
        {
            var pos = TranslateNormalisedNameplatePos3D(pos3norm);
            return new(pos.X, pos.Y);
        }

        private static bool IsNameplatePosInsideConstraint(CachedCompassObjective objective)
            => UiHelper.IsScreenPosInsideConstraint(
                TranslateNormalisedNameplatePos2D(objective.NormalisedNameplatePos),
                Plugin.Config.ScreenMarkConstraint, new(0, 0));

        private static bool ShouldHideMarkerFor(CachedCompassObjective objective)
            => IsNameplatePosInsideConstraint(objective)
            && objective.Distance3D < Plugin.Config.HideScreenMarkEnabledDistance;

        private void DisposeCompassUsedIcons()
        {
            Plugin.IconManager.DisposeIcons(compassUsedIconIds);
            compassUsedIconIds.Clear();
        }
    }
}
