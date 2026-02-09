using Battleship.Shared;
using Xunit;
namespace Battleship.Tests;
public class PacketFramingTests
{
    [Fact]
    public void BinaryPacketReader_MultiplePacketsInBuffer_ProcessesCorrectly()
    {
        var writer = new BinaryPacketWriter();
        var reader = new BinaryPacketReader();
        var shotPacket = new ShotPacket(new Coordinate(1, 2));
        var resultPacket = new ShotResultPacket(ShotResult.Hit);
        var turnPacket = new TurnChangePacket(2);
        var shotData = writer.Serialize(shotPacket);
        var resultData = writer.Serialize(resultPacket);
        var turnData = writer.Serialize(turnPacket);
        var combinedBuffer = new List<byte>();
        combinedBuffer.AddRange(shotData);
        combinedBuffer.AddRange(resultData);
        combinedBuffer.AddRange(turnData);
        var combinedData = combinedBuffer.ToArray();
        var firstPacket = reader.Deserialize(combinedData[..shotData.Length]);
        Assert.IsType<ShotPacket>(firstPacket);
        var secondOffset = shotData.Length;
        var secondLength = resultData.Length;
        var secondPacket = reader.Deserialize(combinedData[secondOffset..(secondOffset + secondLength)]);
        Assert.IsType<ShotResultPacket>(secondPacket);
        var thirdOffset = shotData.Length + resultData.Length;
        var thirdPacket = reader.Deserialize(combinedData[thirdOffset..]);
        Assert.IsType<TurnChangePacket>(thirdPacket);
    }
    [Fact]
    public void BinaryPacketReader_PartialPacket_ThrowsException()
    {
        var writer = new BinaryPacketWriter();
        var reader = new BinaryPacketReader();
        var packet = new ShotPacket(new Coordinate(5, 3));
        var fullData = writer.Serialize(packet);
        var partialData = fullData[..(fullData.Length - 2)];
        Assert.Throws<ArgumentException>(() => reader.Deserialize(partialData));
    }
    [Fact]
    public void BinaryPacketReader_EmptyBuffer_ThrowsException()
    {
        var reader = new BinaryPacketReader();
        Assert.Throws<ArgumentException>(() => reader.Deserialize(Array.Empty<byte>()));
    }
    [Fact]
    public void PlacementCompletePacket_MultipleShips_SerializesCorrectly()
    {
        var ships = new List<Ship>
        {
            new Ship(ShipType.Carrier, new Coordinate(0, 0), Orientation.Horizontal),
            new Ship(ShipType.Battleship, new Coordinate(1, 1), Orientation.Vertical),
            new Ship(ShipType.Destroyer, new Coordinate(2, 2), Orientation.Horizontal),
            new Ship(ShipType.Submarine, new Coordinate(3, 3), Orientation.Vertical),
            new Ship(ShipType.PatrolBoat, new Coordinate(4, 4), Orientation.Horizontal)
        };
        var originalPacket = new PlacementCompletePacket(true, ships);
        var writer = new BinaryPacketWriter();
        var reader = new BinaryPacketReader();
        var data = writer.Serialize(originalPacket);
        var deserializedPacket = reader.Deserialize(data) as PlacementCompletePacket;
        Assert.NotNull(deserializedPacket);
        Assert.True(deserializedPacket.UsedRandomPlacement);
        Assert.Equal(5, deserializedPacket.PlacedShips.Count);
        Assert.Contains(deserializedPacket.PlacedShips, s => s.Type == ShipType.Carrier);
        Assert.Contains(deserializedPacket.PlacedShips, s => s.Type == ShipType.Battleship);
        Assert.Contains(deserializedPacket.PlacedShips, s => s.Type == ShipType.Destroyer);
        Assert.Contains(deserializedPacket.PlacedShips, s => s.Type == ShipType.Submarine);
        Assert.Contains(deserializedPacket.PlacedShips, s => s.Type == ShipType.PatrolBoat);
    }
}