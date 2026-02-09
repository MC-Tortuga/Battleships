namespace Battleship.Shared;
public class Ship
{
    public ShipType Type { get; }
    public Coordinate Start { get; }
    public Orientation Orientation { get; }
    public int Length => Type.GetSize();
    public List<Coordinate> OccupiedCoordinates { get; }
    public HashSet<Coordinate> Hits { get; } = new();
    public Ship(ShipType type, Coordinate start, Orientation orientation)
    {
        Type = type;
        Start = start;
        Orientation = orientation;
        OccupiedCoordinates = CalculateOccupiedCoordinates();
    }
    private List<Coordinate> CalculateOccupiedCoordinates()
    {
        var coordinates = new List<Coordinate>();
        for (int i = 0; i < Length; i++)
        {
            var coord = Orientation == Orientation.Horizontal
                ? new Coordinate(Start.X + i, Start.Y)
                : new Coordinate(Start.X, Start.Y + i);
            coordinates.Add(coord);
        }
        return coordinates;
    }
    public bool IsSunk => Hits.Count == Length;
    public ShotResult FireShot(Coordinate target)
    {
        if (!OccupiedCoordinates.Contains(target))
        {
            return ShotResult.Miss;
        }
        Hits.Add(target);
        return IsSunk ? ShotResult.Sunk : ShotResult.Hit;
    }
}