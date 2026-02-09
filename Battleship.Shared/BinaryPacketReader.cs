using System.Buffers;
namespace Battleship.Shared;
public class BinaryPacketReader
{
    public IPacket Deserialize(byte[] data)
    {
        if (data.Length < 3)
            throw new ArgumentException("Packet data must be at least 3 bytes for header.", nameof(data));
        var type = (PacketType)data[0];
        var length = (data[1] << 8) | data[2];
        if (data.Length != 3 + length)
            throw new ArgumentException($"Packet length mismatch. Expected {3 + length}, got {data.Length}.");
        var payload = data[3..];
        return type switch
        {
            PacketType.FireShot => new ShotPacket(payload),
            PacketType.ShotResult => new ShotResultPacket(payload),
            PacketType.PlaceShip => new PlaceShipPacket(payload),
            PacketType.GameStart => new GameStartPacket(payload),
            PacketType.TurnChange => new TurnChangePacket(payload),
            PacketType.GameOver => new GameOverPacket(payload),
            PacketType.Error => new ErrorPacket(payload),
            PacketType.GameStateChange => new GameStateChangePacket(payload),
            PacketType.PlacementComplete => new PlacementCompletePacket(payload),
            PacketType.PlacementRestart => new PlacementRestartPacket(payload),
            PacketType.TimeWarning => new TimeWarningPacket(payload),
            _ => throw new InvalidOperationException($"Unknown packet type: {type}")
        };
    }
}