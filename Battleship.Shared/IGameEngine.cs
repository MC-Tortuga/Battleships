namespace Battleship.Shared;
public interface IGameEngine
{
    bool PlaceShip(ShipType type, Coordinate start, Orientation orientation);
    ShotResult FireShot(Coordinate target);
    bool IsGameOver { get; }
    int CurrentPlayer { get; }
    void SwitchPlayer();
}