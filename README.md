# Distributed Battleship: A High-Performance Multiplayer Engine

A professional-grade implementation of the classic Battleship game, designed to showcase advanced software engineering principles for technical interviews.

## 🎯 Project Vision

This project demonstrates the ability to build a **scalable, testable, and high-performance** multiplayer system. It moves beyond a simple game to highlight expertise in:
- **Custom Binary Protocol Design** for low-latency networking
- **SOLID Principles** and **Design Patterns** for maintainable architecture
- **Test-Driven Development (TDD)** for robust, verifiable logic
- **Asynchronous Concurrency** for handling multiple game sessions
- **Robust Packet Framing** for reliable TCP communication
- **Interactive Console UI** with real-time shot tracking

## 🏗️ Architecture Overview

The solution is structured into four key components:

```
Battleship/
├── Battleship.Shared/     # Domain Models & Binary Protocol
├── Battleship.Logic/      # Pure Game Engine (100% Testable)
├── Battleship.Server/     # Multiplayer Matchmaker & Session Manager
├── Battleship.Client/     # Networked Game Client with Console UI
└── Battleship.Tests/      # Comprehensive xUnit Test Suite
```

- **`Battleship.Shared`**: Contains all domain entities (`Board`, `Ship`, `Coordinate`) and the high-performance binary serialization engine.
- **`Battleship.Logic`**: A pure C# library with zero external dependencies, containing all game rules. This separation enables **100% unit test coverage** of game logic without network mocking.
- **`Battleship.Server`**: An asynchronous TCP server that manages matchmaking and multiple concurrent game sessions using `Task`-based concurrency with proper packet framing.
- **`Battleship.Client`**: A console-based client with interactive UI, shot tracking, and clean console output management.

## 🚀 Key Technical Features

### Custom Binary Protocol with Packet Framing
- **Why Binary?** To demonstrate mastery of data serialization, memory efficiency, and network optimization over standard formats like JSON.
- **Specification**: A 3-byte header `[Type:1][Length:2]` followed by a payload, ensuring minimal overhead and fast parsing.
- **Packet Framing**: Robust TCP stream handling that correctly processes multiple packets in single reads and partial packets across multiple reads.
- **Implementation**: Hand-rolled binary writers using `Span<byte>` for zero-allocation performance.

### Interactive Console UI
- **Dual Board Display**: Shows both player's ships and opponent's targeting board
- **Shot Tracking**: Visual feedback with 'H' for hits, 'M' for misses, and '~' for unexplored areas
- **Clean Output**: Smart message display that prevents console clutter and duplicate messages
- **Real-time Updates**: Immediate board updates when shot results are received
- **Game State Management**: Clear transitions through placement, battle, and game over phases

### SOLID Design Principles
- **Single Responsibility**: Game logic is completely isolated from networking and UI code.
- **Open/Closed**: The packet system is extensible for future packet types without modifying existing code.
- **Dependency Inversion**: All network operations are abstracted behind interfaces, enabling clean unit testing.

### Design Patterns
- **State Pattern**: Manages the game lifecycle (`Lobby` -> `Placement` -> `Battle` -> `GameOver`).
- **Observer Pattern**: Decouples the UI from network events for a reactive client experience.
- **Strategy Pattern**: Encapsulates different ship placement strategies (manual vs random).

## 🛠️ Tech Stack

- **.NET 10**: The latest platform for high-performance C# development.
- **xUnit**: The preferred testing framework for .NET, used for both unit and integration tests.
- **System.Net.Sockets**: Low-level TCP networking for full control over the communication layer.
- **System.Threading.Timer**: For accurate placement phase timing.

## 📖 How to Run

1. **Clone the Repository**
   ```bash
   git clone <repository-url>
   cd Battleships
   ```

2. **Build the Solution**
   ```bash
   dotnet build
   ```

3. **Run the Server**
   ```bash
   dotnet run --project BattleshipServer
   ```

4. **Run Two Client Instances**
   ```bash
   # Terminal 1
   dotnet run --project BattleshipClient

   # Terminal 2
   dotnet run --project BattleshipClient
   ```

## 🎮 Game Features

### Ship Placement Phase
- **Manual Placement**: Place ships one by one with coordinate input and orientation selection
- **Random Placement**: Automatically place all ships with valid positioning
- **Placement Timer**: 2-minute time limit with warnings at 60 and 30 seconds
- **Validation**: Real-time placement validation with detailed error messages
- **Restart Option**: Reset placement and start over if needed

### Battle Phase
- **Turn-based Combat**: Players alternate firing shots at opponent's board
- **Shot Tracking**: Visual record of all fired shots and their results
- **Hit/Miss Indicators**: Clear marking of successful hits and misses
- **Win Detection**: Game ends when all ships of one player are sunk

### Console Interface
- **Player Board**: Shows your ships with legend (C, B, D, U, P, ~)
- **Target Board**: Shows shot history with legend (H, M, ~)
- **Clean Messages**: Non-intrusive status updates without console clutter
- **Interactive Menus**: Clear choice-based interaction for all game actions

## 🧪 Testing

The project emphasizes quality through a comprehensive testing strategy:

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Coverage
- **Unit Tests**: Complete coverage of game engine logic (`GameEngineTests.cs`)
- **Board Tests**: Comprehensive board and ship functionality (`BoardTests.cs`)
- **Protocol Tests**: Binary packet serialization/deserialization (`BinaryProtocolTests.cs`)
- **Packet Framing Tests**: TCP stream handling and packet boundaries (`PacketFramingTests.cs`)
- **Coordinate Tests**: Value type behavior and dictionary usage (`CoordinateTests.cs`)

### Test Categories
- **Game Logic**: Ship placement, shot resolution, turn management, win conditions
- **Network Protocol**: Packet serialization, deserialization, and framing
- **Domain Models**: Board state, ship positioning, coordinate validation
- **Edge Cases**: Boundary conditions, error scenarios, concurrent operations

## 📚 Documentation

For a deep dive into the technical design, see the `/docs` folder:
- [`docs/architecture/overview.md`](docs/architecture/overview.md) - Detailed architecture and design patterns
- [`docs/protocol/binary_spec.md`](docs/protocol/binary_spec.md) - Formal binary protocol specification
- [`docs/testing/strategy.md`](docs/testing/strategy.md) - Testing strategy and TDD approach

## 💡 Key Learnings

### Technical Insights
- **Binary Protocol Design**: Custom 3-byte header `[Type:1][Length:2]` enables efficient TCP communication with minimal overhead
- **Packet Framing**: Robust TCP stream handling requires buffering to handle multiple packets in single reads and partial packets across reads
- **State Management**: Clean game state transitions prevent resource leaks and ensure proper UI updates
- **Console UI Architecture**: Smart flag-based message management prevents console clutter while maintaining real-time feedback

### Architecture Principles
- **Separation of Concerns**: Game logic isolated from networking enables 100% testable core engine
- **Interface-Driven Design**: Abstractions allow clean unit testing and future extensibility
- **Asynchronous Concurrency**: Task-based patterns efficiently handle multiple game sessions
- **Error Resilience**: Proper exception isolation prevents cascading failures in multiplayer sessions
