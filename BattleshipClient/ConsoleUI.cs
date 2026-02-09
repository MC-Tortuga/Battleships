using Battleship.Shared;
using System.Text;
namespace BattleshipClient;
public class ConsoleUI
{
    private readonly GameClient _client;
    private int _currentPlayer = -1;
    private bool _isMyTurn = false;
    private Board _myBoard = new Board();
    private List<ShipType> _remainingShips = new();
    private GameState _currentState = GameState.Lobby;
    private DateTime _placementStartTime;
    private readonly TimeSpan _placementTimeLimit = TimeSpan.FromMinutes(2);
    private System.Threading.Timer? _placementTimer;
    private TimeSpan _timeRemaining;
    private bool _lobbyMessageShown = false;
    private bool _placementCompleteMessageShown = false;
    private bool _opponentTurnMessageShown = false;
    private bool _placementHeaderShown = false;
    private readonly Dictionary<Coordinate, ShotResult> _opponentShots = new();
    private Coordinate? _lastShotCoordinate;
    public ConsoleUI(GameClient client)
    {
        _client = client;
        _client.OnGameStarted += OnGameStarted;
        _client.OnShotResult += OnShotResult;
        _client.OnTurnChanged += OnTurnChanged;
        _client.OnGameOver += OnGameOver;
        _client.OnError += OnError;
        _client.OnGameStateChange += OnGameStateChange;
        _client.OnTimeWarning += OnTimeWarning;
    }
    public async Task StartAsync()
    {
        Console.WriteLine("Battleship Client v1.0");
        Console.WriteLine("======================");
        Console.WriteLine();
        await _client.StartAsync();
        await GameLoopAsync();
    }
    private async Task GameLoopAsync()
    {
        while (true)
        {
            switch (_currentState)
            {
                case GameState.Lobby:
                    if (!_lobbyMessageShown)
                    {
                        Console.WriteLine("Waiting for opponent to join...");
                        _lobbyMessageShown = true;
                    }
                    await Task.Delay(1000);
                    break;
                case GameState.Placement:
                    await HandlePlacementPhaseAsync();
                    break;
                case GameState.Battle:
                    if (_isMyTurn)
                    {
                        await HandleBattleTurnAsync();
                    }
                    else
                    {
                        if (!_opponentTurnMessageShown)
                        {
                            Console.WriteLine("Waiting for opponent's turn...");
                            _opponentTurnMessageShown = true;
                        }
                        await Task.Delay(1000);
                    }
                    break;
                case GameState.GameOver:
                    return;
            }
        }
    }
    private async Task HandlePlacementPhaseAsync()
    {
        UpdateTimerDisplay();
        if (_remainingShips.Count == 0)
        {
            if (!_placementCompleteMessageShown)
            {
                Console.WriteLine("All ships placed! Waiting for opponent to complete placement...");
                _placementCompleteMessageShown = true;
            }
            await Task.Delay(1000);
            return;
        }
        if (!_placementHeaderShown)
        {
            Console.WriteLine("\n=== SHIP PLACEMENT PHASE ===");
            _placementHeaderShown = true;
        }
        Console.WriteLine($"Time Remaining: {FormatTime(_timeRemaining)}");
        Console.WriteLine($"\nRemaining ships ({_remainingShips.Count}):");
        for (int i = 0; i < _remainingShips.Count; i++)
        {
            var shipType = _remainingShips[i];
            var symbol = GetShipSymbol(shipType);
            Console.WriteLine($"- {shipType} ({symbol}) - {(int)shipType} spaces");
        }
        Console.WriteLine($"\nNext ship to place: {_remainingShips[0]} ({GetShipSymbol(_remainingShips[0])}) - {(int)_remainingShips[0]} spaces");
        Console.WriteLine("\nOptions:");
        Console.WriteLine("1. Place ship manually");
        Console.WriteLine("2. Random placement (all remaining ships)");
        Console.WriteLine("3. Restart placement");
        Console.WriteLine("4. View board");
        Console.Write("Enter choice: ");
        var choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                await HandleManualShipPlacementAsync();
                break;
            case "2":
                await HandleRandomPlacementAsync();
                break;
            case "3":
                await HandlePlacementRestartAsync();
                break;
            case "4":
                DrawBoards();
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                break;
            default:
                Console.WriteLine("Invalid choice. Try again.");
                break;
        }
    }
    private async Task HandleManualShipPlacementAsync()
    {
        var shipType = _remainingShips[0];
        Console.Write($"Enter X coordinate (0-9) for {shipType}: ");
        if (!int.TryParse(Console.ReadLine(), out int x) || x < 0 || x > 9)
        {
            Console.WriteLine("Invalid X coordinate.");
            return;
        }
        Console.Write($"Enter Y coordinate (0-9) for {shipType}: ");
        if (!int.TryParse(Console.ReadLine(), out int y) || y < 0 || y > 9)
        {
            Console.WriteLine("Invalid Y coordinate.");
            return;
        }
        Console.Write("Enter orientation (H/V): ");
        var orientationStr = Console.ReadLine();
        if (orientationStr?.ToUpper() != "H" && orientationStr?.ToUpper() != "V")
        {
            Console.WriteLine("Invalid orientation. Use H for Horizontal, V for Vertical.");
            return;
        }
        var orientation = orientationStr.ToUpper() == "H" ? Orientation.Horizontal : Orientation.Vertical;
        if (!_myBoard.CanPlaceShip(shipType, new Coordinate(x, y), orientation))
        {
            ShowDetailedPlacementError(shipType, new Coordinate(x, y), orientation);
            return;
        }
        await _client.PlaceShipAsync(shipType, x, y, orientation);
        _myBoard.PlaceShip(shipType, new Coordinate(x, y), orientation);
        _remainingShips.RemoveAt(0);
        Console.WriteLine($"\n{shipType} placed successfully!");
        DrawBoards();
        if (_remainingShips.Count == 0)
        {
            await SendPlacementCompleteAsync(false);
        }
    }
    private async Task HandleRandomPlacementAsync()
    {
        Console.WriteLine("\nGenerating random ship placement...");
        var random = new Random();
        var tempBoard = new Board();
        var placedShips = new List<Ship>();
        foreach (var shipType in _remainingShips.ToList())
        {
            Ship ship;
            int attempts = 0;
            do
            {
                var start = new Coordinate(random.Next(10), random.Next(10));
                var orientation = random.Next(2) == 0 ? Orientation.Horizontal : Orientation.Vertical;
                ship = new Ship(shipType, start, orientation);
                attempts++;
            } while (!tempBoard.CanPlaceShip(shipType, ship.Start, ship.Orientation) && attempts < 100);
            if (attempts < 100)
            {
                tempBoard.PlaceShip(shipType, ship.Start, ship.Orientation);
                placedShips.Add(ship);
            }
            else
            {
                Console.WriteLine("Failed to generate valid random placement. Please try manual placement.");
                return;
            }
        }
        foreach (var ship in placedShips)
        {
            _myBoard.PlaceShip(ship.Type, ship.Start, ship.Orientation);
            await _client.PlaceShipAsync(ship.Type, ship.Start.X, ship.Start.Y, ship.Orientation);
        }
        _remainingShips.Clear();
        Console.WriteLine("Random placement completed!");
        DrawBoards();
        await SendPlacementCompleteAsync(true);
    }
    private async Task HandlePlacementRestartAsync()
    {
        Console.WriteLine("Restarting ship placement...");
        _myBoard = new Board();
        _remainingShips = new List<ShipType> 
        { 
            ShipType.Carrier, ShipType.Battleship, ShipType.Destroyer, 
            ShipType.Submarine, ShipType.PatrolBoat 
        };
        await _client.RestartPlacementAsync();
        Console.WriteLine("Placement reset. You can now place all ships again.");
        await Task.Delay(1000);
    }
    private async Task SendPlacementCompleteAsync(bool usedRandomPlacement)
    {
        var placedShips = _myBoard.Ships.ToList();
        await _client.SendPlacementCompleteAsync(usedRandomPlacement, placedShips);
        Console.WriteLine("Placement complete! Waiting for opponent...");
    }
    private void ShowDetailedPlacementError(ShipType shipType, Coordinate start, Orientation orientation)
    {
        var ship = new Ship(shipType, start, orientation);
        var symbol = GetShipSymbol(shipType);
        Console.WriteLine($"\nERROR: Cannot place {shipType} ({symbol}) at ({start.X},{start.Y}) {orientation}");
        Console.WriteLine("├─ Problem:");
        var invalidCoords = ship.OccupiedCoordinates.Where(c => !c.IsValid).ToList();
        if (invalidCoords.Any())
        {
            Console.WriteLine("│  └─ Ship extends beyond board boundary");
            foreach (var coord in invalidCoords)
            {
                Console.WriteLine($"│     - Coordinate ({coord.X},{coord.Y}) is invalid (valid: 0-9)");
            }
        }
        var overlapCoords = new HashSet<Coordinate>();
        foreach (var existingShip in _myBoard.Ships)
        {
            foreach (var coord in ship.OccupiedCoordinates)
            {
                if (existingShip.OccupiedCoordinates.Contains(coord))
                {
                    overlapCoords.Add(coord);
                }
            }
        }
        if (overlapCoords.Any())
        {
            Console.WriteLine("│  └─ Ship overlaps with existing ships");
            foreach (var coord in overlapCoords)
            {
                var overlappingShip = _myBoard.Ships.FirstOrDefault(s => s.OccupiedCoordinates.Contains(coord));
                if (overlappingShip != null)
                {
                    var overlapSymbol = GetShipSymbol(overlappingShip.Type);
                    Console.WriteLine($"│     - Overlaps with {overlappingShip.Type} ({overlapSymbol}) at ({coord.X},{coord.Y})");
                }
            }
        }
        Console.WriteLine("└─ Suggestions:");
        Console.WriteLine($"   - Try placing at different coordinates");
        Console.WriteLine($"   - Try {(orientation == Orientation.Horizontal ? "Vertical" : "Horizontal")} orientation");
        var validPositions = new List<string>();
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                if (_myBoard.CanPlaceShip(shipType, new Coordinate(x, y), orientation))
                {
                    validPositions.Add($"({x},{y})");
                    if (validPositions.Count >= 3) break;
                }
            }
            if (validPositions.Count >= 3) break;
        }
        if (validPositions.Any())
        {
            Console.WriteLine($"   - Valid positions: {string.Join(", ", validPositions)}");
        }
    }
    private async Task HandleBattleTurnAsync()
    {
        Console.WriteLine("\n=== BATTLE PHASE ===");
        Console.WriteLine("Your turn! What would you like to do?");
        Console.WriteLine("1. Fire shot");
        Console.WriteLine("2. View boards");
        Console.WriteLine("3. Quit");
        Console.Write("Enter choice: ");
        var choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                await HandleFireShotAsync();
                break;
            case "2":
                DrawBoards();
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                break;
            case "3":
                _client.Disconnect();
                Environment.Exit(0);
                break;
            default:
                Console.WriteLine("Invalid choice. Try again.");
                break;
        }
    }
    private async Task HandleFireShotAsync()
    {
        Console.Write("Enter X coordinate (0-9): ");
        if (!int.TryParse(Console.ReadLine(), out int x) || x < 0 || x > 9)
        {
            Console.WriteLine("Invalid X coordinate.");
            return;
        }
        Console.Write("Enter Y coordinate (0-9): ");
        if (!int.TryParse(Console.ReadLine(), out int y) || y < 0 || y > 9)
        {
            Console.WriteLine("Invalid Y coordinate.");
            return;
        }
        _lastShotCoordinate = new Coordinate(x, y);
        await _client.FireShotAsync(x, y);
        _isMyTurn = false; 
    }
    private void UpdateTimerDisplay()
    {
        if (_currentState != GameState.Placement || _placementTimer == null)
            return;
        _timeRemaining = _placementTimeLimit - (DateTime.Now - _placementStartTime);
        if (_timeRemaining <= TimeSpan.Zero)
        {
            Console.WriteLine("\nTime's up! Using random placement...");
            _ = HandleRandomPlacementAsync();
            return;
        }
        var remainingSeconds = (int)_timeRemaining.TotalSeconds;
        if (remainingSeconds == 60 || remainingSeconds == 30)
        {
            Console.WriteLine($"\nWARNING: {remainingSeconds} seconds remaining!");
        }
    }
    private void OnGameStarted(int playerId)
    {
        _currentPlayer = playerId;
        _lobbyMessageShown = false;
        Console.WriteLine($"\n=== GAME STARTED ===");
        Console.WriteLine($"You are Player {playerId}");
    }
    private void OnShotResult(ShotResult result)
    {
        Console.WriteLine($"\nShot result: {result}");
        if (_lastShotCoordinate.HasValue)
        {
            _opponentShots[_lastShotCoordinate.Value] = result;
            _lastShotCoordinate = null;
        }
        DrawBoards();
    }
    private void OnTurnChanged(int playerId)
    {
        _isMyTurn = (playerId == _currentPlayer);
        _opponentTurnMessageShown = false;
        Console.WriteLine($"\n=== Player {playerId}'s turn ===");
        if (_isMyTurn)
        {
            Console.WriteLine("It's YOUR turn!");
            DrawBoards();
        }
    }
    private void OnGameOver(int winnerId)
    {
        _currentState = GameState.GameOver;
        Console.WriteLine($"\n=== GAME OVER ===");
        if (winnerId == _currentPlayer)
        {
            Console.WriteLine("🎉 YOU WIN! 🎉");
        }
        else
        {
            Console.WriteLine("You lose. Better luck next time!");
        }
        Environment.Exit(0);
    }
    private void OnError(string error)
    {
        Console.WriteLine($"\nERROR: {error}");
    }
    private void DrawBoards()
    {
        Console.WriteLine();
        DrawPlayerBoard();
        Console.WriteLine();
        DrawTargetBoard();
        Console.WriteLine();
    }
    private void DrawPlayerBoard()
    {
        Console.WriteLine("           YOUR SHIPS");
        Console.WriteLine("  0 1 2 3 4 5 6 7 8 9");
        Console.WriteLine(" +---------------------+");
        for (int y = 0; y < 10; y++)
        {
            Console.Write($"{y}| ");
            for (int x = 0; x < 10; x++)
            {
                var coord = new Coordinate(x, y);
                var cell = GetPlayerCellSymbol(coord);
                Console.Write($"{cell} ");
            }
            Console.WriteLine("|");
        }
        Console.WriteLine(" +---------------------+");
        Console.WriteLine("Legend: C=Carrier, B=Battleship, D=Destroyer, U=Submarine, P=PatrolBoat, ~=Water");
    }
    private void DrawTargetBoard()
    {
        Console.WriteLine("      OPPONENT'S BOARD");
        Console.WriteLine("  0 1 2 3 4 5 6 7 8 9");
        Console.WriteLine(" +---------------------+");
        for (int y = 0; y < 10; y++)
        {
            Console.Write($"{y}| ");
            for (int x = 0; x < 10; x++)
            {
                var coord = new Coordinate(x, y);
                var cell = GetTargetCellSymbol(coord);
                Console.Write($"{cell} ");
            }
            Console.WriteLine("|");
        }
        Console.WriteLine(" +---------------------+");
        Console.WriteLine("Legend: H=Hit, M=Miss, ~=Unexplored");
    }
    private string GetPlayerCellSymbol(Coordinate coord)
    {
        var ship = _myBoard.Ships.FirstOrDefault(s => s.OccupiedCoordinates.Contains(coord));
        if (ship != null)
        {
            return GetShipSymbol(ship.Type);
        }
        return "~";
    }
    private string GetTargetCellSymbol(Coordinate coord)
    {
        if (_opponentShots.TryGetValue(coord, out var result))
        {
            return result switch
            {
                ShotResult.Hit => "H",
                ShotResult.Sunk => "H",
                ShotResult.Miss => "M",
                _ => "~"
            };
        }
        return "~";
    }
    public static string GetShipSymbol(ShipType shipType)
    {
        return shipType switch
        {
            ShipType.Carrier => "C",
            ShipType.Battleship => "B",
            ShipType.Destroyer => "D",
            ShipType.Submarine => "U",
            ShipType.PatrolBoat => "P",
            _ => "?"
        };
    }
    private string FormatTime(TimeSpan timeSpan)
    {
        if (timeSpan <= TimeSpan.Zero) return "0:00";
        var minutes = Math.Max(0, (int)timeSpan.TotalMinutes);
        var seconds = Math.Max(0, timeSpan.Seconds);
        return $"{minutes}:{seconds:D2}";
    }
    public void OnGameStateChange(GameState newState, TimeSpan? timeLimit, List<ShipType>? remainingShips)
    {
        _currentState = newState;
        if (newState == GameState.Placement)
        {
            _placementStartTime = DateTime.Now;
            _timeRemaining = timeLimit ?? _placementTimeLimit;
            _remainingShips = remainingShips ?? _remainingShips;
_placementCompleteMessageShown = false; 
            _placementHeaderShown = false; 
            _placementTimer = new System.Threading.Timer(_ => 
            {
                if (_placementTimer != null)
                    UpdateTimerDisplay();
            }, null, 0, 1000);
            Console.WriteLine("\n=== SHIP PLACEMENT PHASE STARTED ===");
            Console.WriteLine($"You have {FormatTime(_timeRemaining)} to place all ships!");
        }
        else if (newState == GameState.Battle)
        {
            _placementTimer?.Dispose();
            _placementTimer = null;
            _opponentTurnMessageShown = false;
            Console.WriteLine("\n=== BATTLE PHASE STARTED ===");
            Console.WriteLine("All ships have been placed! The battle begins!");
        }
    }
    public void OnTimeWarning(TimeSpan timeRemaining)
    {
        _timeRemaining = timeRemaining;
        Console.WriteLine($"\nTIME WARNING: {FormatTime(timeRemaining)} remaining!");
    }
    public void OnPlacementRestart()
    {
        Console.WriteLine("\n=== PLACEMENT RESTARTED ===");
        _myBoard = new Board();
        _remainingShips = new List<ShipType> 
        { 
            ShipType.Carrier, ShipType.Battleship, ShipType.Destroyer, 
            ShipType.Submarine, ShipType.PatrolBoat 
        };
        _placementStartTime = DateTime.Now;
        _placementCompleteMessageShown = false;
        _placementHeaderShown = false;
    }
}
