namespace Battleship.Shared;
public enum ShipType
{
    Carrier,
    Battleship,
    Destroyer,
    Submarine,
    PatrolBoat
}
public enum Orientation
{
    Horizontal,
    Vertical
}
public enum ShotResult
{
    Miss,
    Hit,
    Sunk
}
public enum GameState
{
    Lobby,
    Placement,
    Battle,
    GameOver
}
public enum PlacementErrorType
{
    OutOfBounds,
    Overlap,
    InvalidCoordinates,
    ShipSpecific
}