namespace Battleship.Shared;
public class Board
{
    private readonly List<Ship> _ships = new();
    private readonly HashSet<Coordinate> _missedShots = new();
    private readonly Dictionary<Coordinate, ShotResult> _shotHistory = new();
    public IReadOnlyCollection<Ship> Ships => _ships.AsReadOnly();
    public IReadOnlyCollection<Coordinate> MissedShots => _missedShots.AsReadOnly();
    public bool CanPlaceShip(ShipType type, Coordinate start, Orientation orientation)
    {
        var newShip = new Ship(type, start, orientation);
        if (!newShip.OccupiedCoordinates.All(c => c.IsValid))
        {
            return false;
        }
        foreach (var ship in _ships)
        {
            if (newShip.OccupiedCoordinates.Any(ship.OccupiedCoordinates.Contains))
            {
                return false;
            }
        }
        return true;
    }
    public bool PlaceShip(ShipType type, Coordinate start, Orientation orientation)
    {
        if (!CanPlaceShip(type, start, orientation))
        {
            return false;
        }
        _ships.Add(new Ship(type, start, orientation));
        return true;
    }
    public ShotResult FireShot(Coordinate target)
    {
        if (!target.IsValid || _shotHistory.ContainsKey(target))
        {
            return ShotResult.Miss;
        }
        foreach (var ship in _ships)
        {
            var result = ship.FireShot(target);
            if (result != ShotResult.Miss)
            {
                _shotHistory[target] = result;
                return result;
            }
        }
        _missedShots.Add(target);
        _shotHistory[target] = ShotResult.Miss;
        return ShotResult.Miss;
    }
    public bool AreAllShipsSunk() => _ships.Any() && _ships.All(ship => ship.IsSunk);
}