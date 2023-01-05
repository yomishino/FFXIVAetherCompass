using Dalamud.Interface;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
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

        internal static unsafe bool WorldToScreen(Vector3 worldPos, out Vector2 screenPos, out Vector3 pCoordsRaw)
        {
            if (getMatrixSingleton == null)
                throw new InvalidOperationException("getMatrixSingleton did not initiate correctly");

            var matrixSingleton = getMatrixSingleton();
            if (matrixSingleton == IntPtr.Zero)
                throw new InvalidOperationException("Cannot get matrixSingleton");

            var windowPos = ImGuiHelpers.MainViewport.Pos;

            var viewProjectionMatrix = *(Matrix4x4*)(matrixSingleton + 0x1b4);
            var device = Device.Instance();
            float width = device->Width;
            float height = device->Height;

            var worldPosDx = worldPos.ToSharpDX();

            var pCoords = Vector3.Transform(worldPos, viewProjectionMatrix);
            pCoordsRaw = pCoords;

            // NOTE: Tweak the formula that was originally used in Dalamud
            // Using abs to make the markers projected to hopefully a more intuitive position
            //  when off-screen, esp. when it's right behind the camera.
            screenPos = new Vector2(pCoords.X / MathF.Abs(pCoords.Z), pCoords.Y / MathF.Abs(pCoords.Z));

            screenPos.X = (0.5f * width * (screenPos.X + 1f)) + windowPos.X;
            screenPos.Y = (0.5f * height * (1f - screenPos.Y)) + windowPos.Y;

            return pCoords.Z > 0 
                && screenPos.X > windowPos.X && screenPos.X < windowPos.X + width
                && screenPos.Y > windowPos.Y && screenPos.Y < windowPos.Y + height;
        }
    }
}
