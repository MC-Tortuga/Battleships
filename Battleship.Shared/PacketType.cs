namespace Battleship.Shared;
public enum PacketType : byte
{
    FireShot = 0x01,
    ShotResult = 0x02,
    PlaceShip = 0x03,
    GameStart = 0x04,
    TurnChange = 0x05,
    GameOver = 0x06,
    Error = 0x07,
    Heartbeat = 0x08,
    GameStateChange = 0x09,
    PlacementComplete = 0x0A,
    PlacementRestart = 0x0B,
    TimeWarning = 0x0C
}