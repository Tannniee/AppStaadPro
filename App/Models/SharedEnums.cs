namespace StaadSteelApp.Core
{
    public enum eFramePropType
    {
        I = 1,
        Channel = 2,
        T = 3,
        Angle = 4,
        DblAngle = 5,
        Box = 6,
        Pipe = 7,
        Rectangular = 8,
        Circle = 9,
        General = 10,
        DbChannel = 11,
        AutoSelect = 12,
        Variable = 14,
        ISection = 15,
        Prismatic = 16,
        Trapezoidal = 17,
        Legacy = 99
    }

    public enum TaperedTubeShape
    {
        Unknown = -1,
        Round = 0,
        Square = 5
    }
}
