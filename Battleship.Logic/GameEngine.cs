using Battleship.Shared;
namespace Battleship.Logic;
public class GameEngine : IGameEngine
{
    private readonly Board[] _playerBoards;
    private readonly ShipType[] _standardShips = 
    {
        ShipType.Carrier,
        ShipType.Battleship,
        ShipType.Destroyer,
        ShipType.Submarine,
        ShipType.PatrolBoat
    };
    public GameEngine()
    {
        _playerBoards = new Board[2] { new Board(), new Board() };
    }
    public Board GetPlayerBoard(int player) => _playerBoards[player - 1];
    public Board GetOpponentBoard(int player) => _playerBoards[player == 1 ? 1 : 0];
    public bool PlaceShip(ShipType type, Coordinate start, Orientation orientation)
    {
        var board = GetPlayerBoard(CurrentPlayer);
        return board.PlaceShip(type, start, orientation);
    }
    public bool PlaceShipForPlayer(ShipType type, Coordinate start, Orientation orientation, int player)
    {
        var board = GetPlayerBoard(player);
        return board.PlaceShip(type, start, orientation);
    }
    public ShotResult FireShot(Coordinate target)
    {
        var opponentBoard = GetOpponentBoard(CurrentPlayer);
        var result = opponentBoard.FireShot(target);
        if (opponentBoard.AreAllShipsSunk())
        {
        }
        else
        {
            SwitchPlayer();
        }
        return result;
    }
    public bool IsGameOver => _playerBoards.Any(board => board.AreAllShipsSunk());
    public int CurrentPlayer { get; private set; } = 1;
    public void SwitchPlayer()
    {
        CurrentPlayer = CurrentPlayer == 1 ? 2 : 1;
    }
    public bool HasPlayerCompletedPlacement(int player)
    {
        var board = GetPlayerBoard(player);
        var requiredShips = _standardShips.Length;
        return board.Ships.Count() == requiredShips;
    }
    public List<ShipType> GetRemainingShips(int player)
    {
        var board = GetPlayerBoard(player);
        var placedShipTypes = board.Ships.Select(s => s.Type).ToHashSet();
        return _standardShips.Where(shipType => !placedShipTypes.Contains(shipType)).ToList();
    }
    public bool CanPlaceShip(ShipType type, Coordinate start, Orientation orientation, int player)
    {
        var board = GetPlayerBoard(player);
        return board.CanPlaceShip(type, start, orientation);
    }
    public List<Ship> GenerateRandomPlacement(int player)
    {
        var ships = new List<Ship>();
        var random = new Random();
        var shipTypes = (ShipType[])_standardShips.Clone();
        foreach (var shipType in shipTypes)
        {
            Ship ship;
            int attempts = 0;
            do
            {
                var start = new Coordinate(random.Next(10), random.Next(10));
                var orientation = random.Next(2) == 0 ? Orientation.Horizontal : Orientation.Vertical;
                ship = new Ship(shipType, start, orientation);
                attempts++;
            } while (!CanPlaceShip(shipType, ship.Start, ship.Orientation, player) && attempts < 100);
            if (attempts < 100)
            {
                PlaceShipForPlayer(shipType, ship.Start, ship.Orientation, player);
                ships.Add(ship);
            }
            else
            {
                return new List<Ship>();
            }
        }
        return ships;
    }
    public void ResetPlayerBoard(int player)
    {
        _playerBoards[player - 1] = new Board();
    }
}