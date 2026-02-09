using Battleship.Shared;
using Xunit;
namespace Battleship.Tests;
public class BinaryProtocolTests
{
    [Fact]
    public void ShotPacket_SerializeAndDeserialize_RoundTripIsSuccessful()
    {
        var originalPacket = new ShotPacket(new Coordinate(5, 3));
        var writer = new BinaryPacketWriter();
        var reader = new BinaryPacketReader();
        var data = writer.Serialize(originalPacket);
        var deserializedPacket = reader.Deserialize(data) as ShotPacket;
        Assert.NotNull(deserializedPacket);
        Assert.Equal(originalPacket.Coordinate.X, deserializedPacket.Coordinate.X);
        Assert.Equal(originalPacket.Coordinate.Y, deserializedPacket.Coordinate.Y);
    }
    [Fact]
    public void ShotResultPacket_Hit_SerializeAndDeserialize_RoundTripIsSuccessful()
    {
        var originalPacket = new ShotResultPacket(ShotResult.Hit);
        var writer = new BinaryPacketWriter();
        var reader = new BinaryPacketReader();
        var data = writer.Serialize(originalPacket);
        var deserializedPacket = reader.Deserialize(data) as ShotResultPacket;
        Assert.NotNull(deserializedPacket);
        Assert.Equal(ShotResult.Hit, deserializedPacket.Result);
        Assert.Null(deserializedPacket.SunkShipType);
    }
    [Fact]
    public void ShotResultPacket_Sunk_SerializeAndDeserialize_RoundTripIsSuccessful()
    {
        var originalPacket = new ShotResultPacket(ShotResult.Sunk, ShipType.Carrier);
        var writer = new BinaryPacketWriter();
        var reader = new BinaryPacketReader();
        var data = writer.Serialize(originalPacket);
        var deserializedPacket = reader.Deserialize(data) as ShotResultPacket;
        Assert.NotNull(deserializedPacket);
        Assert.Equal(ShotResult.Sunk, deserializedPacket.Result);
        Assert.Equal(ShipType.Carrier, deserializedPacket.SunkShipType);
    }
    [Fact]
    public void PlaceShipPacket_SerializeAndDeserialize_RoundTripIsSuccessful()
    {
        var originalPacket = new PlaceShipPacket(ShipType.Destroyer, new Coordinate(2, 3), Orientation.Vertical);
        var writer = new BinaryPacketWriter();
        var reader = new BinaryPacketReader();
        var data = writer.Serialize(originalPacket);
        var deserializedPacket = reader.Deserialize(data) as PlaceShipPacket;
        Assert.NotNull(deserializedPacket);
        Assert.Equal(ShipType.Destroyer, deserializedPacket.ShipType);
        Assert.Equal(2, deserializedPacket.Start.X);
        Assert.Equal(3, deserializedPacket.Start.Y);
        Assert.Equal(Orientation.Vertical, deserializedPacket.IsVertical);
    }
    [Fact]
    public void GameStartPacket_SerializeAndDeserialize_RoundTripIsSuccessful()
    {
        var originalPacket = new GameStartPacket(2);
        var writer = new BinaryPacketWriter();
        var reader = new BinaryPacketReader();
        var data = writer.Serialize(originalPacket);
        var deserializedPacket = reader.Deserialize(data) as GameStartPacket;
        Assert.NotNull(deserializedPacket);
        Assert.Equal(2, deserializedPacket.PlayerId);
    }
    [Fact]
    public void TurnChangePacket_SerializeAndDeserialize_RoundTripIsSuccessful()
    {
        var originalPacket = new TurnChangePacket(1);
        var writer = new BinaryPacketWriter();
        var reader = new BinaryPacketReader();
        var data = writer.Serialize(originalPacket);
        var deserializedPacket = reader.Deserialize(data) as TurnChangePacket;
        Assert.NotNull(deserializedPacket);
        Assert.Equal(1, deserializedPacket.PlayerId);
    }
    [Fact]
    public void GameOverPacket_SerializeAndDeserialize_RoundTripIsSuccessful()
    {
        var originalPacket = new GameOverPacket(2);
        var writer = new BinaryPacketWriter();
        var reader = new BinaryPacketReader();
        var data = writer.Serialize(originalPacket);
        var deserializedPacket = reader.Deserialize(data) as GameOverPacket;
        Assert.NotNull(deserializedPacket);
        Assert.Equal(2, deserializedPacket.WinnerId);
    }
    [Fact]
    public void ErrorPacket_SerializeAndDeserialize_RoundTripIsSuccessful()
    {
        var originalPacket = new ErrorPacket(0x0001, "Test error message");
        var writer = new BinaryPacketWriter();
        var reader = new BinaryPacketReader();
        var data = writer.Serialize(originalPacket);
        var deserializedPacket = reader.Deserialize(data) as ErrorPacket;
        Assert.NotNull(deserializedPacket);
        Assert.Equal((ushort)0x0001, deserializedPacket.ErrorCode);
        Assert.Equal("Test error message", deserializedPacket.Message);
    }
    [Fact]
    public void GameStateChangePacket_Placement_SerializeAndDeserialize_RoundTripIsSuccessful()
    {
        var remainingShips = new List<ShipType> { ShipType.Carrier, ShipType.Battleship };
        var originalPacket = new GameStateChangePacket(GameState.Placement, TimeSpan.FromMinutes(2), remainingShips);
        var writer = new BinaryPacketWriter();
        var reader = new BinaryPacketReader();
        var data = writer.Serialize(originalPacket);
        var deserializedPacket = reader.Deserialize(data) as GameStateChangePacket;
        Assert.NotNull(deserializedPacket);
        Assert.Equal(GameState.Placement, deserializedPacket.NewState);
        Assert.Equal(TimeSpan.FromMinutes(2), deserializedPacket.TimeLimit);
        Assert.Equal(2, deserializedPacket.RemainingShips?.Count);
        Assert.NotNull(deserializedPacket.RemainingShips);
        Assert.Contains(ShipType.Carrier, deserializedPacket.RemainingShips!);
        Assert.Contains(ShipType.Battleship, deserializedPacket.RemainingShips!);
    }
    [Fact]
    public void PlacementCompletePacket_SerializeAndDeserialize_RoundTripIsSuccessful()
    {
        var ships = new List<Ship>
        {
            new Ship(ShipType.Carrier, new Coordinate(0, 0), Orientation.Horizontal),
            new Ship(ShipType.PatrolBoat, new Coordinate(5, 5), Orientation.Vertical)
        };
        var originalPacket = new PlacementCompletePacket(false, ships);
        var writer = new BinaryPacketWriter();
        var reader = new BinaryPacketReader();
        var data = writer.Serialize(originalPacket);
        var deserializedPacket = reader.Deserialize(data) as PlacementCompletePacket;
        Assert.NotNull(deserializedPacket);
        Assert.False(deserializedPacket.UsedRandomPlacement);
        Assert.Equal(2, deserializedPacket.PlacedShips.Count);
        Assert.Equal(ShipType.Carrier, deserializedPacket.PlacedShips[0].Type);
        Assert.Equal(ShipType.PatrolBoat, deserializedPacket.PlacedShips[1].Type);
    }
    [Fact]
    public void PlacementRestartPacket_SerializeAndDeserialize_RoundTripIsSuccessful()
    {
        var originalPacket = new PlacementRestartPacket();
        var writer = new BinaryPacketWriter();
        var reader = new BinaryPacketReader();
        var data = writer.Serialize(originalPacket);
        var deserializedPacket = reader.Deserialize(data) as PlacementRestartPacket;
        Assert.NotNull(deserializedPacket);
        Assert.Equal(PacketType.PlacementRestart, deserializedPacket.Type);
    }
    [Fact]
    public void TimeWarningPacket_SerializeAndDeserialize_RoundTripIsSuccessful()
    {
        var timeRemaining = TimeSpan.FromSeconds(45);
        var originalPacket = new TimeWarningPacket(timeRemaining);
        var writer = new BinaryPacketWriter();
        var reader = new BinaryPacketReader();
        var data = writer.Serialize(originalPacket);
        var deserializedPacket = reader.Deserialize(data) as TimeWarningPacket;
        Assert.NotNull(deserializedPacket);
        Assert.Equal(TimeSpan.FromSeconds(45), deserializedPacket.TimeRemaining);
    }
}