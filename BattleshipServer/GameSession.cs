using Battleship.Shared;
using Battleship.Logic;
using System.Net.Sockets;
namespace BattleshipServer;

public class GameSession
{
    private readonly TcpClient[] _clients = new TcpClient[2];
    private readonly NetworkStream[] _streams = new NetworkStream[2];
    private readonly GameEngine _gameEngine = new();
    private readonly BinaryPacketReader _reader = new();
    private readonly BinaryPacketWriter _writer = new();
    private readonly int _sessionId;
    private bool _isGameOver = false;
    private GameState _currentState = GameState.Lobby;
    private bool[] _placementComplete = new bool[2];
    private DateTime[] _placementStartTimes = new DateTime[2];
    private readonly System.Threading.Timer[] _placementTimers = new System.Threading.Timer[2];
    public GameSession(int sessionId)
    {
        _sessionId = sessionId;
    }
    public bool IsFull { get; private set; } = false;
    public bool IsGameOver => _isGameOver;
    public bool TryAddPlayer(TcpClient client)
    {
        for (int i = 0; i < 2; i++)
        {
            if (_clients[i] == null)
            {
                _clients[i] = client;
                _streams[i] = client.GetStream();
                var startPacket = new GameStartPacket(i + 1);
                var data = _writer.Serialize(startPacket);
                _streams[i].Write(data, 0, data.Length);
                if (_clients[1] != null)
                {
                    IsFull = true;
                    Console.WriteLine($"Session {_sessionId}: Game starting with 2 players.");
                    _ = StartPlacementPhaseAsync();
                }
                else
                {
                    Console.WriteLine($"Session {_sessionId}: Player {i + 1} connected. Waiting for another player...");
                }
                return true;
            }
        }
        return false;
    }
    private async Task StartPlacementPhaseAsync()
    {
        Console.WriteLine($"Session {_sessionId}: Starting placement phase");
        _currentState = GameState.Placement;
        for (int i = 0; i < 2; i++)
        {
            _placementComplete[i] = false;
            _placementStartTimes[i] = DateTime.Now;
            var playerIndex = i;
            _placementTimers[playerIndex] = new System.Threading.Timer(async _ =>
            {
                if (_placementComplete[playerIndex] || _placementTimers[playerIndex] == null) return;
                var elapsed = DateTime.Now - _placementStartTimes[playerIndex];
                var remaining = TimeSpan.FromMinutes(2) - elapsed;
                if (remaining <= TimeSpan.Zero)
                {
                    Console.WriteLine($"Session {_sessionId}: Player {playerIndex + 1} time expired, using random placement");
                    await HandleTimeExpiredAsync(playerIndex);
                }
                else if (remaining.TotalSeconds <= 60 || remaining.TotalSeconds <= 30)
                {
                    var warningPacket = new TimeWarningPacket(remaining);
                    var warningData = _writer.Serialize(warningPacket);
                    await _streams[playerIndex]?.WriteAsync(warningData, 0, warningData.Length);
                }
            }, null, 0, 1000);
        }
        var remainingShips = new List<ShipType> { ShipType.Carrier, ShipType.Battleship, ShipType.Destroyer, ShipType.Submarine, ShipType.PatrolBoat };
        var gameStatePacket = new GameStateChangePacket(GameState.Placement, TimeSpan.FromMinutes(2), remainingShips);
        var gameStateData = _writer.Serialize(gameStatePacket);
        await BroadcastPacketAsync(gameStateData);
        _ = Task.Run(() => GameLoopAsync());
    }
    private async Task NotifyGameStartAsync()
    {
        await Task.Delay(100);
        var turnChangePacket = new TurnChangePacket(_gameEngine.CurrentPlayer);
        var turnChangeData = _writer.Serialize(turnChangePacket);
        await BroadcastPacketAsync(turnChangeData);
    }

    private async Task GameLoopAsync()
    {
        Console.WriteLine($"Session {_sessionId}: Game loop started.");
        var tasks = new Task[2];
        for (int i = 0; i < 2; i++)
        {
            int playerIndex = i;
            tasks[i] = Task.Run(async () => await HandleClientAsync(playerIndex));
        }
        await Task.WhenAll(tasks);
        Console.WriteLine($"Session {_sessionId}: Game loop ended.");
    }
    private async Task HandleClientAsync(int playerIndex)
    {
        var stream = _streams[playerIndex];
        var buffer = new byte[1024];
        var receiveBuffer = new List<byte>();
        try
        {
            while (!_isGameOver && _clients[playerIndex].Connected)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;
                receiveBuffer.AddRange(buffer[..bytesRead]);
                while (receiveBuffer.Count >= 3)
                {
                    var packetLength = (receiveBuffer[1] << 8) | receiveBuffer[2];
                    var totalPacketLength = 3 + packetLength;
                    if (receiveBuffer.Count >= totalPacketLength)
                    {
                        var packetData = receiveBuffer.GetRange(0, totalPacketLength).ToArray();
                        receiveBuffer.RemoveRange(0, totalPacketLength);
                        try
                        {
                            var packet = _reader.Deserialize(packetData);
                            await ProcessPacketAsync(playerIndex, packet);
                        }
                        catch (Exception packetEx)
                        {
                            Console.WriteLine($"Session {_sessionId}: Error processing packet from player {playerIndex + 1}: {packetEx.Message}");
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Session {_sessionId}: Error handling player {playerIndex + 1}: {ex.Message}");
        }
        finally
        {
            _isGameOver = true;
            Console.WriteLine($"Session {_sessionId}: Player {playerIndex + 1} disconnected.");
        }
    }
    private async Task ProcessPacketAsync(int playerIndex, IPacket packet)
    {
        switch (packet)
        {
            case ShotPacket shot:
                if (_currentState != GameState.Battle || playerIndex + 1 != _gameEngine.CurrentPlayer)
                {
                    var errorPacket = new ErrorPacket(0x0003, "Cannot shoot during placement phase or when not your turn");
                    var errorData = _writer.Serialize(errorPacket);
                    await _streams[playerIndex]?.WriteAsync(errorData, 0, errorData.Length);
                    return;
                }
                await HandleShotAsync(playerIndex, shot);
                break;
            case PlaceShipPacket place:
                if (_currentState != GameState.Placement)
                {
                    var errorPacket = new ErrorPacket(0x0004, "Cannot place ships during battle phase");
                    var errorData = _writer.Serialize(errorPacket);
                    await _streams[playerIndex]?.WriteAsync(errorData, 0, errorData.Length);
                    return;
                }
                await HandlePlaceShipAsync(playerIndex, place);
                break;
            case PlacementCompletePacket placementComplete:
                await HandlePlacementCompleteAsync(playerIndex, placementComplete);
                break;
            case PlacementRestartPacket:
                await HandlePlacementRestartAsync(playerIndex);
                break;
            case GameStateChangePacket:
            case TurnChangePacket:
            case GameStartPacket:
            case GameOverPacket:
            case ShotResultPacket:
            case ErrorPacket:
            case TimeWarningPacket:
                break;
        }
    }
    private async Task HandleShotAsync(int playerIndex, ShotPacket shotPacket)
    {
        var result = _gameEngine.FireShot(shotPacket.Coordinate);
        var resultPacket = new ShotResultPacket(result, result == ShotResult.Sunk ? GetSunkShipType() : null);
        var data = _writer.Serialize(resultPacket);
        await BroadcastPacketAsync(data);
        if (_gameEngine.IsGameOver)
        {
            _isGameOver = true;
            var gameOverPacket = new GameOverPacket(_gameEngine.CurrentPlayer);
            var gameOverData = _writer.Serialize(gameOverPacket);
            await BroadcastPacketAsync(gameOverData);
        }
        else
        {
            var turnChangePacket = new TurnChangePacket(_gameEngine.CurrentPlayer);
            var turnChangeData = _writer.Serialize(turnChangePacket);
            await BroadcastPacketAsync(turnChangeData);
        }
    }
    private async Task HandlePlaceShipAsync(int playerIndex, PlaceShipPacket placePacket)
    {
        var success = _gameEngine.PlaceShipForPlayer(placePacket.ShipType, placePacket.Start, placePacket.IsVertical, playerIndex + 1);
        if (!success)
        {
            var errorMessage = CreateDetailedPlacementError(playerIndex + 1, placePacket.ShipType, placePacket.Start, placePacket.IsVertical);
            var errorPacket = new ErrorPacket(0x0002, errorMessage);
            var errorData = _writer.Serialize(errorPacket);
            await _streams[playerIndex].WriteAsync(errorData, 0, errorData.Length);
        }
        else
        {
            Console.WriteLine($"Session {_sessionId}: Player {playerIndex + 1} placed {placePacket.ShipType} at ({placePacket.Start.X},{placePacket.Start.Y})");
        }
    }
    private string CreateDetailedPlacementError(int player, ShipType shipType, Coordinate start, Orientation orientation)
    {
        var ship = new Ship(shipType, start, orientation);
        var board = _gameEngine.GetPlayerBoard(player);
        var invalidCoords = ship.OccupiedCoordinates.Where(c => !c.IsValid).ToList();
        if (invalidCoords.Any())
        {
            return $"Cannot place {shipType} at ({start.X},{start.Y}) {orientation}: Ship extends beyond board. Invalid coordinates: {string.Join(", ", invalidCoords.Select(c => $"({c.X},{c.Y})"))}";
        }
        var overlapCoords = new HashSet<Coordinate>();
        foreach (var existingShip in board.Ships)
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
            var overlappingShip = board.Ships.FirstOrDefault(s => s.OccupiedCoordinates.Any(overlapCoords.Contains));
            return $"Cannot place {shipType} at ({start.X},{start.Y}) {orientation}: Overlaps with {overlappingShip?.Type} at ({string.Join(", ", overlapCoords.Select(c => $"({c.X},{c.Y})"))})";
        }
        return $"Cannot place {shipType} at ({start.X},{start.Y}) {orientation}: Invalid placement";
    }
    private async Task BroadcastPacketAsync(byte[] data)
    {
        var tasks = new Task[2];
        for (int i = 0; i < 2; i++)
        {
            if (_streams[i] != null)
            {
                tasks[i] = _streams[i].WriteAsync(data, 0, data.Length);
            }
        }
        await Task.WhenAll(tasks);
    }
    private ShipType? GetSunkShipType()
    {
        return null;
    }
    private async Task HandleTimeExpiredAsync(int playerIndex)
    {
        _placementTimers[playerIndex]?.Dispose();
        var remainingShips = new List<ShipType> { ShipType.Carrier, ShipType.Battleship, ShipType.Destroyer, ShipType.Submarine, ShipType.PatrolBoat };
        foreach (var shipType in remainingShips.ToList())
        {
            bool placed = false;
            int attempts = 0;
            while (!placed && attempts < 100)
            {
                var random = new Random();
                Orientation orientation = random.Next(2) == 0 ? Orientation.Horizontal : Orientation.Vertical;
                int x = random.Next(10);
                int y = random.Next(10);
                var start = new Coordinate(x, y);
                placed = _gameEngine.PlaceShipForPlayer(shipType, start, orientation, playerIndex + 1);
                attempts++;
            }
            if (!placed)
            {
                Console.WriteLine($"Session {_sessionId}: Failed to place {shipType} for player {playerIndex + 1} after timeout");
            }
        }
        _placementComplete[playerIndex] = true;
        await CheckBothPlayersReadyAsync();
    }
    private async Task HandlePlacementCompleteAsync(int playerIndex, PlacementCompletePacket packet)
    {
        _placementTimers[playerIndex]?.Dispose();
        _placementComplete[playerIndex] = true;
        await CheckBothPlayersReadyAsync();
    }
    private async Task HandlePlacementRestartAsync(int playerIndex)
    {
        _gameEngine.ResetPlayerBoard(playerIndex + 1);
        _placementComplete[playerIndex] = false;
        _placementStartTimes[playerIndex] = DateTime.Now;
        _placementTimers[playerIndex]?.Dispose();
        var remainingShips = new List<ShipType> { ShipType.Carrier, ShipType.Battleship, ShipType.Destroyer, ShipType.Submarine, ShipType.PatrolBoat };
        var gameStatePacket = new GameStateChangePacket(GameState.Placement, TimeSpan.FromMinutes(2), remainingShips);
        var gameStateData = _writer.Serialize(gameStatePacket);
        await _streams[playerIndex].WriteAsync(gameStateData, 0, gameStateData.Length);
    }
    private async Task CheckBothPlayersReadyAsync()
    {
        if (_placementComplete[0] && _placementComplete[1])
        {
            for (int i = 0; i < 2; i++)
            {
                _placementTimers[i]?.Dispose();
            }
            _currentState = GameState.Battle;
            Console.WriteLine($"Session {_sessionId}: Both players ready. Starting battle phase.");
            var gameStatePacket = new GameStateChangePacket(GameState.Battle, TimeSpan.Zero, null);
            var gameStateData = _writer.Serialize(gameStatePacket);
            await BroadcastPacketAsync(gameStateData);
            var turnChangePacket = new TurnChangePacket(_gameEngine.CurrentPlayer);
            var turnChangeData = _writer.Serialize(turnChangePacket);
            await BroadcastPacketAsync(turnChangeData);
        }
    }
    public void DisconnectAll()
    {
        foreach (var client in _clients)
        {
            client?.Close();
        }
    }
}
