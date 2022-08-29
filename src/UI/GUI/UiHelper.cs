using AetherCompass.Common;
using AetherCompass.Game.SeFunctions;
using Dalamud.Interface;
using ImGuiNET;
using System.Numerics;


namespace AetherCompass.UI.GUI
{
    public static class UiHelper
    {
        public static Vector2 GetScreenCentre()
            => ImGuiHelpers.MainViewport.GetCenter();

        // offset is L/B/R/T, added directly to the position of each side of viewport
        public static bool IsScreenPosInsideMainViewport(
            Vector2 screenPos, Vector4 offset)
        {
            var pos = ImGuiHelpers.MainViewport.Pos;
            var size = ImGuiHelpers.MainViewport.Size;
            return MathUtil.IsBetween(screenPos.X, pos.X + offset.X, pos.X + size.X + offset.Z) 
                && MathUtil.IsBetween(screenPos.Y, pos.Y + offset.W, pos.Y + size.Y + offset.Y);
        }

        public static bool IsScreenPosInsideMainViewport(Vector2 screenPos)
            => IsScreenPosInsideMainViewport(screenPos, new(0, 0, 0, 0));

        public static bool WorldToScreenPos(Vector3 worldPos, out Vector2 screenPos)
            => Projection.WorldToScreen(worldPos, out screenPos);

        internal static bool WorldToScreenPos(Vector3 worldPos, out Vector2 screenPos, out Vector3 pCoordsRaw)
            => Projection.WorldToScreen(worldPos, out screenPos, out pCoordsRaw);

        // NDC used in UI3DModule.ObjectInfo for object screen positions
        public static Vector3 TranslateNormalisedCoordinates(
            Vector3 pos3norm, bool reverseY = true)
        {
            var mSizeHalf = ImGuiHelpers.MainViewport.Size / 2;
            var mCentrePos = ImGuiHelpers.MainViewport.Pos + mSizeHalf;
            return new(mCentrePos.X + pos3norm.X * mSizeHalf.X, 
                reverseY ? mCentrePos.Y - pos3norm.Y * mSizeHalf.Y 
                    : mCentrePos.Y + pos3norm.Y * mSizeHalf.Y, 
                pos3norm.Z);
        }

        public static Vector2 GetConstrainedScreenPos(
            Vector2 screenPos, Vector4 screenConstraint, Vector2 extraConstraint)
        {
            var constraintUL = ImGuiHelpers.MainViewport.Pos + extraConstraint;
            var constraintBR = ImGuiHelpers.MainViewport.Pos + ImGuiHelpers.MainViewport.Size - extraConstraint;
            var x = Math.Clamp(screenPos.X, constraintUL.X + screenConstraint.X, constraintBR.X - screenConstraint.Z);
            var y = Math.Clamp(screenPos.Y, constraintUL.Y + screenConstraint.W, constraintBR.Y - screenConstraint.Y);
            return new Vector2(x, y);
        }

        public static bool IsScreenPosInsideConstraint(
            Vector2 screenPos, Vector4 screenConstraint, Vector2 extraConstraint)
        {
            var constraintUL = ImGuiHelpers.MainViewport.Pos 
                + new Vector2(screenConstraint.X, screenConstraint.W) + extraConstraint;
            var constraintBR = ImGuiHelpers.MainViewport.Pos + ImGuiHelpers.MainViewport.Size 
                - new Vector2(screenConstraint.Z, screenConstraint.Y) - extraConstraint;
            return MathUtil.IsBetween(screenPos.X, constraintUL.X, constraintBR.X, true)
                && MathUtil.IsBetween(screenPos.Y, constraintUL.Y, constraintBR.Y, true);
        }

        public static float GetAngleOnScreen(Vector2 origin, Vector2 point, bool flipped = false)
            => flipped ? MathF.Atan2(origin.X - point.X, origin.Y - point.Y)
            : MathF.Atan2(point.X - origin.X, point.Y - origin.Y);

        public static float GetAngleOnScreen(Vector2 point, bool flipped = false)
            => GetAngleOnScreen(GetScreenCentre(), point, flipped);

        public static (Vector2 P1, Vector2 P2, Vector2 P3, Vector2 P4)
            GetRectPointsOnScreen(Vector2 screenPos, Vector2 rectHalfSize)
        {
            // p1~p4 is UL, UR, BR, BL of the image
            Vector2 p1 = screenPos - rectHalfSize;
            Vector2 p2 = new(screenPos.X + rectHalfSize.X, screenPos.Y - rectHalfSize.Y);
            Vector2 p3 = screenPos + rectHalfSize;
            Vector2 p4 = new(screenPos.X - rectHalfSize.X, screenPos.Y + rectHalfSize.Y);
            return (p1, p2, p3, p4);
        }

        // rotation = 0 points upwards to make things intuitive
        public static (Vector2 P1, Vector2 P2, Vector2 P3, Vector2 P4)
            GetRotatedRectPointsOnScreen(Vector2 screenPos, Vector2 rectHalfSize, float rotation)
        {
            var (p1, p2, p3, p4) = GetRectPointsOnScreen(screenPos, rectHalfSize);

            // Rotate
            p1 = RotatePointOnPlane(p1, screenPos, rotation);
            p2 = RotatePointOnPlane(p2, screenPos, rotation);
            p3 = RotatePointOnPlane(p3, screenPos, rotation);
            p4 = RotatePointOnPlane(p4, screenPos, rotation);

            return (p1, p2, p3, p4);
        }

        public static Vector2 RotatePointOnPlane(Vector2 p, Vector2 rotationCentre, float rotation)
        {
            p -= rotationCentre;
            var a = MathF.Atan2(p.X, p.Y);
            var di = Vector2.Distance(p, Vector2.Zero);
            return new Vector2(
                di * MathF.Sin(a + rotation) + rotationCentre.X,
                di * MathF.Cos(a + rotation) + rotationCentre.Y);
        }

        public static Vector4 GenerateShadowColour(Vector4 colour, float lightness)
        {
            ImGui.ColorConvertRGBtoHSV(colour.X, colour.Y, colour.Z, out float h, out float _, out float _);
            float s = -lightness * lightness + 1;
            float v = lightness;
            ImGui.ColorConvertHSVtoRGB(h, s, v, out float r, out float g, out float b);
            return new Vector4(r, g, b, colour.W);
        }


        public static Vector2 GetTextSize(string text, ImFontPtr font, float fontsize)
        {
            var split = text.Split('\n');
            float maxLineW = 0;
            int lineCount = 0;
            foreach (var s in split)
            {
                float lineW = 0;
                foreach (var c in s)
                    lineW += font.GetCharAdvance(c);
                maxLineW = MathF.Max(maxLineW, lineW);
                lineCount++;
            }
            return new Vector2(maxLineW * fontsize / font.FontSize, fontsize * lineCount);
        }

        public static void DrawTextWithShadow(ImDrawListPtr drawList, string text, Vector2 pos,
            ImFontPtr font, float fontsizeRaw, float scale, Vector4 colour, float shadowLightness)
        {
            var fontsize = fontsizeRaw * scale;
            var col_uint = ImGui.ColorConvertFloat4ToU32(colour);
            var shadowCol_uint = ImGui.ColorConvertFloat4ToU32(GenerateShadowColour(colour, shadowLightness));
            // showdow R
            pos.X += scale;
            drawList.AddText(font, fontsize, pos, shadowCol_uint, text);
            // showdow D
            pos.X -= scale;
            pos.Y += scale;
            drawList.AddText(font, fontsize, pos, shadowCol_uint, text);
            // content
            pos.Y -= scale;
            drawList.AddText(font, fontsize, pos, col_uint, text);
        }

        public static void DrawMultilineTextWithShadow(ImDrawListPtr drawList, string text, 
            Vector2 pos, ImFontPtr font, float fontsizeRaw, float scale, Vector4 colour, 
            float shadowLightness, bool rightAligned = false)
        {
            if (!rightAligned)
            {
                DrawTextWithShadow(drawList, text, pos, font, fontsizeRaw, scale, colour, shadowLightness);
                return;
            }
            var fontsize = fontsizeRaw * scale;
            var lines = text.Split('\n');
            var lineTextSize = new Vector2[lines.Length];
            var maxSizeX = float.MinValue;
            for (int i = 0; i < lines.Length; i++)
            {
                lineTextSize[i] = GetTextSize(lines[i], font, fontsize);
                maxSizeX = MathF.Max(maxSizeX, lineTextSize[i].X);
            }
            var linePos = pos;
            for (int i = 0; i < lines.Length; i++)
            {
                linePos.X = pos.X + maxSizeX - lineTextSize[i].X;
                DrawTextWithShadow(drawList, lines[i], linePos, font, fontsizeRaw, scale, colour, shadowLightness);
                linePos.Y += lineTextSize[i].Y;
            }
        }
    }

}
