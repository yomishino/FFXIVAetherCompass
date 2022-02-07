using Dalamud.Interface;
using Dalamud.Utility;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace AetherCompass.Game.SeFunctions
{
    internal static class Projection
    {
        private delegate IntPtr GetMatrixSingletonDelegate();
        private static readonly GetMatrixSingletonDelegate getMatrixSingleton;

        static Projection()
        {
            IntPtr addr = Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4c 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??");
            getMatrixSingleton ??= Marshal.GetDelegateForFunctionPointer<GetMatrixSingletonDelegate>(addr);
        }

        // Rewrite a bit of Dalamud's WorldToScreen because the result when object is off-screen is quite counter-intuitive for our purpose
        public static bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos) => WorldToScreen(worldPos, out screenPos, out _);

        internal static bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos, out Vector3 pCoordsRaw)
        {
            if (getMatrixSingleton == null)
                throw new InvalidOperationException("getMatrixSingleton did not initiate correctly");

            // Get base object with matrices
            var matrixSingleton = getMatrixSingleton();
            if (matrixSingleton == IntPtr.Zero)
                throw new InvalidOperationException("Cannot get matrixSingleton");

            // Read current ViewProjectionMatrix plus game window size
            var viewProjectionMatrix = default(SharpDX.Matrix);
            float width, height;
            var windowPos = ImGuiHelpers.MainViewport.Pos;

            unsafe
            {
                var rawMatrix = (float*)(matrixSingleton + 0x1b4);
                if (rawMatrix == null) throw new InvalidOperationException("Cannot get rawMatrix");

                for (var i = 0; i < 16; i++, rawMatrix++)
                    viewProjectionMatrix[i] = *rawMatrix;

                width = *rawMatrix;
                height = *(rawMatrix + 1);
            }

            var worldPosDx = worldPos.ToSharpDX();
            SharpDX.Vector3.Transform(ref worldPosDx, ref viewProjectionMatrix, out SharpDX.Vector3 pCoords);

            pCoordsRaw = pCoords.ToSystem();

            // NOTE: Kind of a dirty fix. Using abs to make the markers projected to hopefully a more intuitive position
            //  when off-screen, esp. when it's right behind the camera.
            // On Y-axis this makes it always points to a direction respecting the altitude difference
            //  between camera and the obj, so basically the marker looks higher on screen when the obj is higher than camera and vice versa;
            //  (altho it may seem in reversed Y when camera is lower than obj but character is not, because this is camera-based not character-based).
            // Also did this on X-axis so that when altitude difference is small and obj's right behind the camera,
            //  you can make it be in front of the camera by simply following the marker's direction on X-axis;
            //  without the abs the marker will suddenly flip to the other side when you follow it past the 180-degree,
            //  which i think is more confusing for navigation purpose.
            screenPos = new Vector2(pCoords.X / MathF.Abs(pCoords.Z), pCoords.Y / MathF.Abs(pCoords.Z));

            screenPos.X = (0.5f * width * (screenPos.X + 1f)) + windowPos.X;
            screenPos.Y = (0.5f * height * (1f - screenPos.Y)) + windowPos.Y;

            return pCoords.Z > 0 
                && screenPos.X > windowPos.X && screenPos.X < windowPos.X + width
                && screenPos.Y > windowPos.Y && screenPos.Y < windowPos.Y + height;
        }
    }
}
