using Battleship.Shared;
using Battleship.Logic;
using Xunit;
namespace Battleship.Tests;
public class GameEngineTests
{
    [Fact]
    public void GameEngine_InitialState_IsCorrect()
    {
        var engine = new GameEngine();
        Assert.Equal(1, engine.CurrentPlayer);
        Assert.False(engine.IsGameOver);
    }
    [Fact]
    public void PlaceShip_ValidPlacement_ReturnsTrue()
    {
        var engine = new GameEngine();
        var result = engine.PlaceShipForPlayer(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal, 1);
        Assert.True(result);
    }
    [Fact]
    public void PlaceShip_InvalidPlacement_ReturnsFalse()
    {
        var engine = new GameEngine();
        engine.PlaceShipForPlayer(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal, 1);
        var result = engine.PlaceShipForPlayer(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal, 1);
        Assert.False(result);
    }
    [Fact]
    public void FireShot_Hit_ReturnsHit()
    {
        var engine = new GameEngine();
        engine.PlaceShipForPlayer(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal, 2);
        var result = engine.FireShot(new Coordinate(0, 0));
        Assert.Equal(ShotResult.Hit, result);
    }
    [Fact]
    public void FireShot_Miss_ReturnsMiss()
    {
        var engine = new GameEngine();
        engine.PlaceShipForPlayer(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal, 2);
        var result = engine.FireShot(new Coordinate(5, 5));
        Assert.Equal(ShotResult.Miss, result);
    }
    [Fact]
    public void FireShot_SinksShip_ReturnsSunk()
    {
        var engine = new GameEngine();
        engine.PlaceShipForPlayer(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal, 2);
        var result1 = engine.FireShot(new Coordinate(0, 0));
        Assert.Equal(ShotResult.Hit, result1);
        engine.SwitchPlayer();
        var result2 = engine.FireShot(new Coordinate(1, 0));
        Assert.Equal(ShotResult.Sunk, result2);
    }
    [Fact]
    public void FireShot_TurnSwitches_AfterShot()
    {
        var engine = new GameEngine();
        Assert.Equal(1, engine.CurrentPlayer);
        engine.PlaceShipForPlayer(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal, 1);
        engine.FireShot(new Coordinate(5, 5)); 
        Assert.Equal(2, engine.CurrentPlayer);
    }
    [Fact]
    public void SwitchPlayer_AlternatesPlayers()
    {
        var engine = new GameEngine();
        Assert.Equal(1, engine.CurrentPlayer);
        engine.SwitchPlayer();
        Assert.Equal(2, engine.CurrentPlayer);
        engine.SwitchPlayer();
        Assert.Equal(1, engine.CurrentPlayer);
    }
    [Fact]
    public void PlaceShipForPlayer_ValidPlacement_ReturnsTrue()
    {
        var engine = new GameEngine();
        var result = engine.PlaceShipForPlayer(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal, 1);
        Assert.True(result);
    }
    [Fact]
    public void PlaceShipForPlayer_SeparateBoards_DoesNotAffectOtherPlayer()
    {
        var engine = new GameEngine();
        engine.PlaceShipForPlayer(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal, 1);
        var player2Board = engine.GetPlayerBoard(2);
        Assert.Empty(player2Board.Ships);
    }
    [Fact]
    public void HasPlayerCompletedPlacement_FalseWhenEmpty()
    {
        var engine = new GameEngine();
        Assert.False(engine.HasPlayerCompletedPlacement(1));
    }
    [Fact]
    public void HasPlayerCompletedPlacement_TrueWhenAllShipsPlaced()
    {
        var engine = new GameEngine();
        var placements = new[]
        {
            new { Type = ShipType.Carrier, X = 0, Y = 0, Orient = Orientation.Horizontal },
            new { Type = ShipType.Battleship, X = 0, Y = 1, Orient = Orientation.Horizontal },
            new { Type = ShipType.Destroyer, X = 0, Y = 2, Orient = Orientation.Horizontal },
            new { Type = ShipType.Submarine, X = 0, Y = 3, Orient = Orientation.Horizontal },
            new { Type = ShipType.PatrolBoat, X = 0, Y = 4, Orient = Orientation.Horizontal }
        };
        foreach (var placement in placements)
        {
            engine.PlaceShipForPlayer(placement.Type, new Coordinate(placement.X, placement.Y), placement.Orient, 1);
        }
        Assert.True(engine.HasPlayerCompletedPlacement(1));
    }
    [Fact]
    public void GetRemainingShips_ReturnsAllWhenEmpty()
    {
        var engine = new GameEngine();
        var remaining = engine.GetRemainingShips(1);
        Assert.Equal(5, remaining.Count);
        Assert.Contains(ShipType.Carrier, remaining);
        Assert.Contains(ShipType.Battleship, remaining);
        Assert.Contains(ShipType.Destroyer, remaining);
        Assert.Contains(ShipType.Submarine, remaining);
        Assert.Contains(ShipType.PatrolBoat, remaining);
    }
    [Fact]
    public void GetRemainingShips_ExcludesPlacedShips()
    {
        var engine = new GameEngine();
        engine.PlaceShipForPlayer(ShipType.Carrier, new Coordinate(0, 0), Orientation.Horizontal, 1);
        var remaining = engine.GetRemainingShips(1);
        Assert.Equal(4, remaining.Count);
        Assert.DoesNotContain(ShipType.Carrier, remaining);
        Assert.Contains(ShipType.Battleship, remaining);
    }
    [Fact]
    public void CanPlaceShip_ValidCoordinates_ReturnsTrue()
    {
        var engine = new GameEngine();
        var result = engine.CanPlaceShip(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal, 1);
        Assert.True(result);
    }
    [Fact]
    public void CanPlaceShip_OutOfBounds_ReturnsFalse()
    {
        var engine = new GameEngine();
        var result = engine.CanPlaceShip(ShipType.Carrier, new Coordinate(8, 0), Orientation.Horizontal, 1);
        Assert.False(result);
    }
    [Fact]
    public void GenerateRandomPlacement_CreatesAllShips()
    {
        var engine = new GameEngine();
        var ships = engine.GenerateRandomPlacement(1);
        Assert.Equal(5, ships.Count);
        Assert.Contains(ships, s => s.Type == ShipType.Carrier);
        Assert.Contains(ships, s => s.Type == ShipType.Battleship);
        Assert.Contains(ships, s => s.Type == ShipType.Destroyer);
        Assert.Contains(ships, s => s.Type == ShipType.Submarine);
        Assert.Contains(ships, s => s.Type == ShipType.PatrolBoat);
    }
    [Fact]
    public void ResetPlayerBoard_ClearsAllShips()
    {
        var engine = new GameEngine();
        engine.PlaceShipForPlayer(ShipType.PatrolBoat, new Coordinate(0, 0), Orientation.Horizontal, 1);
        engine.ResetPlayerBoard(1);
        Assert.Empty(engine.GetPlayerBoard(1).Ships);
        Assert.False(engine.HasPlayerCompletedPlacement(1));
    }
}