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
        North = 1<<0,
        East = 1<<1,
        South = 1<<2,
        West = 1<<3,

        NorthEast = North|East,
        NorthWest = North|West,
        SouthEast = South|East,
        SouthWest = South|West,

        NaN = byte.MaxValue
    }
}
