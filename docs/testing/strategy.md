# Testing Strategy

This document outlines the comprehensive testing strategy for the Distributed Battleship project, emphasizing Test-Driven Development (TDD), quality assurance, and how testing reinforces the architectural design.

## Philosophy

Our testing strategy is built on three core principles:

1. **Isolation**: Test the smallest possible units of logic in isolation.
2. **Confidence**: Tests should give us the confidence to refactor and add new features.
3. **Clarity**: Tests should serve as living documentation of the system's behavior.

## 💡 Key Testing Learnings

### Test Architecture Insights
- **Isolation Principle**: Game logic separated from networking enables 100% unit test coverage without mocking
- **Protocol Testing**: Binary serialization requires round-trip tests to ensure byte-level compatibility
- **Packet Framing Tests**: TCP stream handling needs dedicated tests for partial reads and multiple packets
- **Value Type Testing**: Coordinate behavior as dictionary keys requires explicit testing of equality and hashing

### Testing Realities
- **Coverage vs Value**: Focus testing on complex business logic, not simple property accessors
- **Mock Usage**: Use mocks for external dependencies (network) but fakes for simple data structures
- **Performance Testing**: Binary protocol efficiency requires benchmarking to validate design choices
- **Manual Testing**: Console applications need manual E2E testing to validate user experience

## Test Pyramid

We follow a classic test pyramid model, with a strong foundation of unit tests and a smaller number of integration tests.

```
      /\
     /  \   E2E Tests (Manual)
    /____\
   /      \
  /        \  Integration Tests
 /__________\
/____________\
 Unit Tests (xUnit)
```

### 1. Unit Tests (The Foundation)

The majority of our tests are unit tests, which are fast, isolated, and deterministic. They focus on the business logic in the `Battleship.Logic` library.

**Target Library:** `Battleship.Tests` (xUnit)

**Key Areas of Coverage:**
- **Game Rules**: Validating ship placement, hit detection, and win conditions (`GameEngineTests.cs`).
- **Board State**: Ensuring the board model correctly tracks ships and shots (`BoardTests.cs`).
- **Domain Models**: Testing the behavior of `Ship`, `Coordinate`, and `GameState` (`CoordinateTests.cs`).
- **Packet Framing**: Robust TCP stream handling and packet boundary detection (`PacketFramingTests.cs`).
- **Binary Protocol**: Complete serialization/deserialization coverage (`BinaryProtocolTests.cs`).

**Example Test Case:**
```csharp
[Fact]
public void FireShot_WhenShipIsPresent_ReturnsHit()
{
    // Arrange
    var board = new Board();
    var ship = new Ship(ShipType.Destroyer, new Coordinate(2, 2), Orientation.Horizontal);
    board.PlaceShip(ship);
    var gameLogic = new GameEngine(board);

    // Act
    var result = gameLogic.FireShot(new Coordinate(2, 2));

    // Assert
    Assert.Equal(ShotResult.Hit, result);
}
```

### 2. Integration Tests (The Glue)

Integration tests verify that different parts of the system work together correctly. Our primary focus for integration testing is the **Binary Protocol**.

**Key Areas of Coverage:**
- **Protocol Serialization**: Ensuring a packet serializes to the correct byte array.
- **Protocol Deserialization**: Ensuring a byte array deserializes back to the correct packet object.
- **Round-Trip Tests**: Serialize -> Deserialize -> Assert equality.

**Example Test Case:**
```csharp
[Fact]
public void ShotPacket_SerializeAndDeserialize_RoundTripIsSuccessful()
{
    // Arrange
    var originalPacket = new ShotPacket(new Coordinate(5, 3));
    var writer = new BinaryPacketWriter();
    var reader = new BinaryPacketReader();

    // Act
    var data = writer.Serialize(originalPacket);
    var deserializedPacket = reader.Deserialize<ShotPacket>(data);

    // Assert
    Assert.Equal(originalPacket.Coordinate.X, deserializedPacket.Coordinate.X);
    Assert.Equal(originalPacket.Coordinate.Y, deserializedPacket.Coordinate.Y);
}
```

### 3. End-to-End Tests (Manual)

E2E tests are performed manually by running the client and server and playing a full game. These tests are not automated but are crucial for validating the user experience.

**Manual Test Checklist:**
- [ ] Two clients can connect to the server.
- [ ] Players can place all their ships (manual and random placement).
- [ ] Players can fire shots and see results with hit/miss tracking.
- [ ] The game correctly identifies a winner.
- [ ] The client handles a server disconnect gracefully.
- [ ] Packet framing handles multiple packets in single reads.
- [ ] Console output is clean without duplicate messages.
- [ ] Timer cleanup works properly after phase transitions.

## Test-Driven Development (TDD) Workflow

We use a Red-Green-Refactor TDD cycle for developing new features in the `Battleship.Logic` library.

1.  **Red**: Write a failing test for a new piece of functionality.
2.  **Green**: Write the minimum amount of code to make the test pass.
3.  **Refactor**: Clean up the code while ensuring the test still passes.

**Example: Adding a "Sunk" state**

1.  **Red**: Write a test that expects `FireShot` to return `ShotResult.Sunk` when the last part of a ship is hit.
2.  **Green**: Modify `FireShot` to check if the ship has any remaining un-hit parts.
3.  **Refactor**: Extract the ship health logic into its own method for clarity.

## Mocking and Fakes

To achieve true unit test isolation, we use mocks and fakes for external dependencies.

### `INetworkHandler` Mock

The `GameEngine` depends on an `INetworkHandler` to send packets. In our unit tests, we use a mock to verify that the correct packets are being sent without needing a real network connection.

```csharp
// Using Moq library
var mockNetworkHandler = new Mock<INetworkHandler>();
var gameEngine = new GameEngine(mockNetworkHandler.Object);

// Act
gameEngine.FireShot(new Coordinate(5, 5));

// Assert
mockNetworkHandler.Verify(x => x.SendPacket(It.Is<ShotPacket>(p => p.Coordinate.X == 5)), Times.Once);
```

### In-Memory Fakes

For simple data dependencies, we use in-memory fakes instead of a full mocking framework.

```csharp
public class FakeBoard : IBoard
{
    private readonly List<Ship> _ships = new();
    public void PlaceShip(Ship ship) => _ships.Add(ship);
    public ShotResult FireShot(Coordinate coord) { /* ... */ }
}
```

## Code Coverage

We aim for **high code coverage** in the `Battleship.Logic` library, but we recognize that 100% coverage is not always practical or valuable.

**Coverage Goals:**
- **`Battleship.Logic`**: >95% coverage. This is the core business logic and must be thoroughly tested.
- **`Battleship.Shared`**: >90% coverage, focusing on the binary protocol and domain models.
- **`Battleship.Server/Client`**: ~80% coverage. Focus on the core networking and session management logic.

**Test Files Overview:**
- `GameEngineTests.cs`: 212 lines covering all game engine functionality
- `BoardTests.cs`: Comprehensive board and ship behavior testing
- `BinaryProtocolTests.cs`: 197 lines covering packet serialization/deserialization
- `PacketFramingTests.cs`: TCP stream handling and packet boundary validation
- `CoordinateTests.cs`: Value type behavior and dictionary usage testing

**Running Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Performance Testing

While not a primary focus, we include basic performance tests to ensure our binary protocol is efficient.

```csharp
[Fact]
public void SerializeShotPacket_Performance_IsWithinThreshold()
{
    var packet = new ShotPacket(new Coordinate(5, 5));
    var writer = new BinaryPacketWriter();
    var stopwatch = Stopwatch.StartNew();

    for (int i = 0; i < 10000; i++)
    {
        writer.Serialize(packet);
    }

    stopwatch.Stop();
    Assert.True(stopwatch.ElapsedMilliseconds < 100, "Serialization was too slow.");
}
```

## Continuous Integration (CI)

In a production environment, this project would include a CI pipeline (e.g., GitHub Actions) that automatically runs all tests on every push.

**Example CI Steps:**
1.  **Build**: `dotnet build`
2.  **Unit Tests**: `dotnet test Battleship.Tests`
3.  **Integration Tests**: `dotnet test Battleship.Tests --filter Category=Integration`
4.  **Code Coverage**: Generate and publish a coverage report.
5.  **Linting**: Run a linter (e.g., `dotnet format --verify`) to ensure code style.

---

## 💡 Testing Strategy Takeaways

### Core Testing Principles
- **Test-Driven Development**: Red-Green-Refactor cycle ensures code is testable from the start
- **Test Pyramid**: Strong unit test foundation with selective integration testing provides best ROI
- **Living Documentation**: Tests serve as executable specifications of system behavior
- **Quality Gates**: Automated testing enables confident refactoring and feature additions

### Professional Testing Practices
- **Isolation Testing**: Pure unit tests enable fast, deterministic validation of business logic
- **Integration Testing**: Protocol testing ensures correct byte-level communication between components
- **Manual Testing**: User experience validation requires manual testing of console interfaces
- **Performance Testing**: Binary protocols need benchmarking to validate efficiency claims