namespace Battleship.Shared;
public readonly record struct Coordinate(int X, int Y)
{
    public bool IsValid => X >= 0 && X < 10 && Y >= 0 && Y < 10;
}