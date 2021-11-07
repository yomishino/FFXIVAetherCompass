using AetherCompass.UI.SeFunctions;
using Dalamud.Interface;
using System;
using System.Numerics;


namespace AetherCompass.UI
{
    public static class CompassUi
    {
        public static Vector2 GetScreenCentre()
            => ImGuiHelpers.MainViewport.GetCenter();

        public static bool IsScreenPosInsideMainViewport(Vector2 screenPos)
        {
            var pos = ImGuiHelpers.MainViewport.Pos;
            var size = ImGuiHelpers.MainViewport.Size;
            return screenPos.X > pos.X && screenPos.X < pos.X + size.X
                && screenPos.Y > pos.Y && screenPos.Y < pos.Y + size.Y;
        }

        public static bool WorldToScreenPos(Vector3 worldPos, out Vector2 screenPos)
            => Projection.WorldToScreen(worldPos, out screenPos);

        internal static bool WorldToScreenPos(Vector3 worldPos, out Vector2 screenPos, out Vector3 pCoordsRaw)
            => Projection.WorldToScreen(worldPos, out screenPos, out pCoordsRaw);

        public static Vector2 GetConstrainedScreenPos(Vector2 screenPosUL, Vector4 screenConstraint, Vector2 extraConstraint)
        {
            var constraintUL = ImGuiHelpers.MainViewport.Pos + extraConstraint;
            var constraintBR = ImGuiHelpers.MainViewport.Pos + ImGuiHelpers.MainViewport.Size - extraConstraint;
            var x = MathF.Max(constraintUL.X + screenConstraint.X, MathF.Min(constraintBR.X - screenConstraint.Z, screenPosUL.X));
            var y = MathF.Max(constraintUL.Y + screenConstraint.Z, MathF.Min(constraintBR.Y - screenConstraint.Y, screenPosUL.Y));
            return new Vector2(x, y);
        }

        // upwards = true if rotation = 0 points upwards
        public static float GetAngleOnScreen(Vector2 origin, Vector2 point, bool upwards = true)
            => MathF.Atan2(point.X - origin.X, upwards ? (origin.Y - point.Y) : (point.Y - origin.Y));

        public static float GetAngleOnScreen(Vector2 point, bool upwards = true)
            => GetAngleOnScreen(GetScreenCentre(), point, upwards);

        // rotation = 0 points upwards to make things intuitive
        public static (Vector2 P1, Vector2 P2, Vector2 P3, Vector2 P4)
            GetRotatedPointsOnScreen(Vector2 screenPosUL, Vector2 size, float rotation)
        {
            // p1~p4 is UL, DL, DR, DU of the image
            Vector2 p1 = screenPosUL;
            Vector2 p2 = new(screenPosUL.X + size.X, screenPosUL.Y);
            Vector2 p3 = screenPosUL + size;
            Vector2 p4 = new(screenPosUL.X, screenPosUL.Y + size.Y);

            Vector2 p0 = screenPosUL + size / 2;

            // Rotate
            p1 = RotatePointOnPlane(p1, p0, rotation);
            p2 = RotatePointOnPlane(p2, p0, rotation);
            p3 = RotatePointOnPlane(p3, p0, rotation);
            p4 = RotatePointOnPlane(p4, p0, rotation);

            return (p1, p2, p3, p4);
        }

        public static Vector2 RotatePointOnPlane(Vector2 p, Vector2 rotationCentre, float rotation)
        {
            p -= rotationCentre;
            var a = MathF.Atan2(p.X, p.Y);
            var di = MathF.Sqrt(p.X * p.X + p.Y * p.Y);
            return new Vector2(
                di * MathF.Sin(a + rotation) + rotationCentre.X,
                di * MathF.Cos(a + rotation) + rotationCentre.Y);
        }

        //public static Direction GetDirectionOnScreen(Vector2 origin, Vector2 point)
        //{
        //    var theta = GetAngleOnScreen(origin, point);
        //    if (float.IsNaN(theta)) return Direction.O;
        //    Direction d = 0;
        //    if (MathF.Abs(theta) <= 3 * MathF.PI / 8) d |= Direction.Down;
        //    if (MathF.Abs(theta) > 5 * MathF.PI / 8) d |= Direction.Up;
        //    if (MathF.PI / 8 < theta && theta <= 7 * MathF.PI / 8) d |= Direction.Right;
        //    if (-7 * MathF.PI / 8 < theta && theta <= -MathF.PI / 8) d |= Direction.Left;
        //    return d;
        //}

        //public static Direction GetDirectionOnScreen(Vector2 point)
        //    => GetDirectionOnScreen(GetScreenCentre(), point);

        public static Vector2 GetTextSize(string text, float fontsize)
        {
            var split = text.Split('\n');
            float maxLineW = 0;
            foreach (var s in split)
            {
                float lineW = 0;
                foreach (var c in s)
                    //ImFontPtr.FindGlyph(c).AdvanceX will not get the correct result, it usually gives larger result; idk why
                    lineW += c < ImGuiNET.ImGui.GetFont().IndexAdvanceX.Size ? ImGuiNET.ImGui.GetFont().IndexAdvanceX[c] : ImGuiNET.ImGui.GetFont().FallbackAdvanceX;
                maxLineW = MathF.Max(maxLineW, lineW);
            }
            return new Vector2(maxLineW * fontsize / ImGuiNET.ImGui.GetFontSize(), fontsize);
        }


    }
}
