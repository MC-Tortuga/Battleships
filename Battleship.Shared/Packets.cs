using System.Text;
namespace Battleship.Shared;
public class ShotPacket : IPacket
{
    public PacketType Type => PacketType.FireShot;
    public Coordinate Coordinate { get; }
    public ShotPacket(Coordinate coordinate)
    {
        Coordinate = coordinate;
    }
    public ShotPacket(byte[] payload)
    {
        if (payload.Length != 8)
            throw new ArgumentException("ShotPacket payload must be 8 bytes.", nameof(payload));
        var x = BitConverter.ToInt32(payload, 0);
        var y = BitConverter.ToInt32(payload, 4);
        Coordinate = new Coordinate(x, y);
    }
}
public class ShotResultPacket : IPacket
{
    public PacketType Type => PacketType.ShotResult;
    public ShotResult Result { get; }
    public ShipType? SunkShipType { get; }
    public ShotResultPacket(ShotResult result, ShipType? sunkShipType = null)
    {
        Result = result;
        SunkShipType = sunkShipType;
    }
    public ShotResultPacket(byte[] payload)
    {
        Result = (ShotResult)payload[0];
        if (payload.Length > 1 && Result == ShotResult.Sunk)
        {
            SunkShipType = (ShipType)payload[1];
        }
    }
}
public class PlaceShipPacket : IPacket
{
    public PacketType Type => PacketType.PlaceShip;
    public ShipType ShipType { get; }
    public Coordinate Start { get; }
    public Orientation IsVertical { get; }
    public PlaceShipPacket(ShipType shipType, Coordinate start, Orientation isVertical)
    {
        ShipType = shipType;
        Start = start;
        IsVertical = isVertical;
    }
    public PlaceShipPacket(byte[] payload)
    {
        if (payload.Length != 10)
            throw new ArgumentException("PlaceShipPacket payload must be 10 bytes.", nameof(payload));
        ShipType = (ShipType)payload[0];
        var x = BitConverter.ToInt32(payload, 1);
        var y = BitConverter.ToInt32(payload, 5);
        Start = new Coordinate(x, y);
        IsVertical = payload[9] == 1 ? Orientation.Vertical : Orientation.Horizontal;
    }
}
public class GameStartPacket : IPacket
{
    public PacketType Type => PacketType.GameStart;
    public int PlayerId { get; }
    public GameStartPacket(int playerId)
    {
        PlayerId = playerId;
    }
    public GameStartPacket(byte[] payload)
    {
        if (payload.Length != 1)
            throw new ArgumentException("GameStartPacket payload must be 1 byte.", nameof(payload));
        PlayerId = payload[0];
    }
}
public class TurnChangePacket : IPacket
{
    public PacketType Type => PacketType.TurnChange;
    public int PlayerId { get; }
    public TurnChangePacket(int playerId)
    {
        PlayerId = playerId;
    }
    public TurnChangePacket(byte[] payload)
    {
        if (payload.Length != 1)
            throw new ArgumentException("TurnChangePacket payload must be 1 byte.", nameof(payload));
        PlayerId = payload[0];
    }
}
public class GameOverPacket : IPacket
{
    public PacketType Type => PacketType.GameOver;
    public int WinnerId { get; }
    public GameOverPacket(int winnerId)
    {
        WinnerId = winnerId;
    }
    public GameOverPacket(byte[] payload)
    {
        if (payload.Length != 1)
            throw new ArgumentException("GameOverPacket payload must be 1 byte.", nameof(payload));
        WinnerId = payload[0];
    }
}
public class ErrorPacket : IPacket
{
    public PacketType Type => PacketType.Error;
    public ushort ErrorCode { get; }
    public string Message { get; }
    public ErrorPacket(ushort errorCode, string message)
    {
        ErrorCode = errorCode;
        Message = message ?? string.Empty;
    }
    public ErrorPacket(byte[] payload)
    {
        if (payload.Length < 2)
            throw new ArgumentException("ErrorPacket payload must be at least 2 bytes.", nameof(payload));
        ErrorCode = (ushort)((payload[0] << 8) | payload[1]);
        if (payload.Length > 2)
        {
            Message = System.Text.Encoding.UTF8.GetString(payload, 2, payload.Length - 2);
        }
    }
}
public class GameStateChangePacket : IPacket
{
    public PacketType Type => PacketType.GameStateChange;
    public GameState NewState { get; }
    public TimeSpan? TimeLimit { get; }
    public List<ShipType>? RemainingShips { get; }
    public GameStateChangePacket(GameState newState, TimeSpan? timeLimit = null, List<ShipType>? remainingShips = null)
    {
        NewState = newState;
        TimeLimit = timeLimit;
        RemainingShips = remainingShips;
    }
    public GameStateChangePacket(byte[] payload)
    {
        var index = 0;
        NewState = (GameState)payload[index++];
        if (index < payload.Length && payload[index] == 1)
        {
            index++;
            var totalSeconds = BitConverter.ToInt32(payload, index);
            TimeLimit = TimeSpan.FromSeconds(totalSeconds);
            index += 4;
        }
        if (index < payload.Length)
        {
            var shipCount = payload[index++];
            RemainingShips = new List<ShipType>();
            for (int i = 0; i < shipCount; i++)
            {
                if (index < payload.Length)
                {
                    RemainingShips.Add((ShipType)payload[index++]);
                }
            }
        }
    }
}
public class PlacementCompletePacket : IPacket
{
    public PacketType Type => PacketType.PlacementComplete;
    public bool UsedRandomPlacement { get; }
    public List<Ship> PlacedShips { get; }
    public PlacementCompletePacket(bool usedRandomPlacement, List<Ship> placedShips)
    {
        UsedRandomPlacement = usedRandomPlacement;
        PlacedShips = placedShips;
    }
    public PlacementCompletePacket(byte[] payload)
    {
        var index = 0;
        UsedRandomPlacement = payload[index++] == 1;
        var shipCount = payload[index++];
        PlacedShips = new List<Ship>();
        for (int i = 0; i < shipCount; i++)
        {
            var shipType = (ShipType)payload[index++];
            var startX = BitConverter.ToInt32(payload, index);
            index += 4;
            var startY = BitConverter.ToInt32(payload, index);
            index += 4;
            var orientation = (Orientation)payload[index++];
            PlacedShips.Add(new Ship(shipType, new Coordinate(startX, startY), orientation));
        }
    }
}
public class PlacementRestartPacket : IPacket
{
    public PacketType Type => PacketType.PlacementRestart;
    public PlacementRestartPacket()
    {
    }
    public PlacementRestartPacket(byte[] payload)
    {
    }
}
public class TimeWarningPacket : IPacket
{
    public PacketType Type => PacketType.TimeWarning;
    public TimeSpan TimeRemaining { get; }
    public TimeWarningPacket(TimeSpan timeRemaining)
    {
        TimeRemaining = timeRemaining;
    }
    public TimeWarningPacket(byte[] payload)
    {
        var totalSeconds = BitConverter.ToInt32(payload, 0);
        TimeRemaining = TimeSpan.FromSeconds(totalSeconds);
    }
}