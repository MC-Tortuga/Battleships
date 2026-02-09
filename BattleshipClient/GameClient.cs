using Battleship.Shared;
using System.Net.Sockets;
using System.Text;
namespace BattleshipClient;

public class GameClient
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly BinaryPacketReader _reader = new();
    private readonly BinaryPacketWriter _writer = new();
    private int _playerId = -1;
    private bool _isConnected = false;
    public event Action<int>? OnGameStarted;
    public event Action<ShotResult>? OnShotResult;
    public event Action<int>? OnTurnChanged;
    public event Action<int>? OnGameOver;
    public event Action<string>? OnError;
    public event Action<GameState, TimeSpan?, List<ShipType>?>? OnGameStateChange;
    public event Action<TimeSpan>? OnTimeWarning;
    public GameClient(string server, int port)
    {
        _client = new TcpClient();
        _client.Connect(server, port);
        _stream = _client.GetStream();
        _isConnected = true;
    }
    public async Task StartAsync()
    {
        Console.WriteLine("Connected to server. Waiting for game start...");
        _ = Task.Run(ListenForServerMessagesAsync);
    }
    private async Task ListenForServerMessagesAsync()
    {
        var buffer = new byte[1024];
        var receiveBuffer = new List<byte>();
        try
        {
            while (_isConnected && _client.Connected)
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
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
                            ProcessServerPacket(packet);
                        }
                        catch (Exception packetEx)
                        {
                            Console.WriteLine($"Error processing server packet: {packetEx.Message}");
                            OnError?.Invoke($"Packet processing error: {packetEx.Message}");
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
            Console.WriteLine($"Error receiving data: {ex.Message}");
            OnError?.Invoke($"Connection error: {ex.Message}");
        }
        finally
        {
            _isConnected = false;
            Console.WriteLine("Disconnected from server.");
        }
    }
    private void ProcessServerPacket(IPacket packet)
    {
        switch (packet)
        {
            case GameStartPacket start:
                _playerId = start.PlayerId;
                OnGameStarted?.Invoke(_playerId);
                break;
            case ShotResultPacket result:
                OnShotResult?.Invoke(result.Result);
                break;
            case TurnChangePacket turn:
                OnTurnChanged?.Invoke(turn.PlayerId);
                break;
            case GameOverPacket gameOver:
                OnGameOver?.Invoke(gameOver.WinnerId);
                break;
            case ErrorPacket error:
                OnError?.Invoke(error.Message);
                break;
            case GameStateChangePacket gameStateChange:
                OnGameStateChange?.Invoke(gameStateChange.NewState, gameStateChange.TimeLimit, gameStateChange.RemainingShips);
                break;
            case PlacementCompletePacket placementComplete:
                break;
            case PlacementRestartPacket:
                break;
            case TimeWarningPacket timeWarning:
                OnTimeWarning?.Invoke(timeWarning.TimeRemaining);
                break;
        }
    }
    public async Task FireShotAsync(int x, int y)
    {
        var packet = new ShotPacket(new Coordinate(x, y));
        var data = _writer.Serialize(packet);
        await _stream.WriteAsync(data, 0, data.Length);
    }
    public async Task PlaceShipAsync(ShipType shipType, int x, int y, Orientation orientation)
    {
        var packet = new PlaceShipPacket(shipType, new Coordinate(x, y), orientation);
        var data = _writer.Serialize(packet);
        await _stream.WriteAsync(data, 0, data.Length);
    }
    public void Disconnect()
    {
        _isConnected = false;
        _client.Close();
    }
    public async Task RestartPlacementAsync()
    {
        var packet = new PlacementRestartPacket();
        var data = _writer.Serialize(packet);
        await _stream.WriteAsync(data, 0, data.Length);
    }
    public async Task SendPlacementCompleteAsync(bool usedRandomPlacement, List<Ship> placedShips)
    {
        var packet = new PlacementCompletePacket(usedRandomPlacement, placedShips);
        var data = _writer.Serialize(packet);
        await _stream.WriteAsync(data, 0, data.Length);
    }
}
