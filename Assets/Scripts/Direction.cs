using UnityEngine;

public enum Direction : int
{
    Invalid = -1,
    NegativeX = 0,
    PositiveX = 1,
    NegativeY = 2,
    PositiveY = 3,
    NegativeZ = 4,
    PositiveZ = 5
}

public static class DirectionHelper
{
    public static Direction Flip(Direction original)
    {
        if ((int)original > 5 || (int)original < 0)
        {
            return Direction.Invalid;
        }
        if ((int)original % 2 == 1)
        {
            return (Direction)(original - 1);
        }
        else if ((int)original % 2 == 0)
        {
            return (Direction)(original + 1);
        }
        return Direction.Invalid;
    }
}
