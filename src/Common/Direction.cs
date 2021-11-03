using System;

namespace AetherCompass.Common
{
    [Flags]
    public enum Direction : byte
    {
        O = 0,
        Up = 1<<0,
        Right = 1<<1,
        Down = 1<<2,
        Left = 1<<3,

        UpperRight = Up | Right,
        UpperLeft = Up | Left,
        BottomRight = Down | Right,
        BottomLeft = Down | Left,

        NaN = byte.MaxValue
    }
    
    [Flags]
    public enum CompassDirection : byte
    {
        O = 0,
        N = 1<<0,
        E = 1<<1,
        S = 1<<2,
        W = 1<<3,

        NE = N|E,
        NW = N|W,
        SE = S|E,
        SW = S|W,

        NaN = byte.MaxValue
    }
}
