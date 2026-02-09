namespace Battleship.Shared;
public static class ShipTypeExtensions
{
    public static int GetSize(this ShipType shipType) => shipType switch
    {
        ShipType.Carrier => 5,
        ShipType.Battleship => 4,
        ShipType.Destroyer => 3,
        ShipType.Submarine => 3,
        ShipType.PatrolBoat => 2,
        _ => 1
    };
}