namespace AetherCompass.Common
{
    public static class MathUtil
    {
        public const float PI2 = MathF.PI * 2;
        public const float PIOver2 = MathF.PI / 2;

        public static bool IsBetween(float x, float min, float max, bool inclusive = false)
            => inclusive ? (x >= min && x <= max) : (x > min && x < max);

        public static float Clamp(float x, float min, float max)
            => MathF.Min(max, MathF.Max(min, x));

        public static float TruncateToOneDecimalPlace(float v)
            => MathF.Truncate(v * 10) / 10f;
    }
}
