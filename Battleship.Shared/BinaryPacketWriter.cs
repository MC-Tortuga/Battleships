using System.Buffers;
using System.Text;
namespace Battleship.Shared;
public class BinaryPacketWriter
{
    private readonly MemoryStream _stream = new();
    public byte[] Serialize(IPacket packet)
    {
        _stream.SetLength(0);
        _stream.WriteByte((byte)packet.Type);
        _stream.WriteByte(0); 
        _stream.WriteByte(0); 
        var payloadStart = _stream.Position;
        switch (packet)
        {
            case ShotPacket shot:
                WriteInt32(shot.Coordinate.X);
                WriteInt32(shot.Coordinate.Y);
                break;
            case ShotResultPacket result:
                _stream.WriteByte((byte)result.Result);
                if (result.Result == ShotResult.Sunk && result.SunkShipType.HasValue)
                {
                    _stream.WriteByte((byte)result.SunkShipType.Value);
                }
                break;
            case PlaceShipPacket place:
                _stream.WriteByte((byte)place.ShipType);
                WriteInt32(place.Start.X);
                WriteInt32(place.Start.Y);
                _stream.WriteByte(place.IsVertical == Orientation.Vertical ? (byte)1 : (byte)0);
                break;
            case GameStartPacket start:
                _stream.WriteByte((byte)start.PlayerId);
                break;
            case TurnChangePacket turn:
                _stream.WriteByte((byte)turn.PlayerId);
                break;
            case GameOverPacket gameOver:
                _stream.WriteByte((byte)gameOver.WinnerId);
                break;
            case ErrorPacket error:
                _stream.WriteByte((byte)(error.ErrorCode >> 8));
                _stream.WriteByte((byte)(error.ErrorCode & 0xFF));
                var messageBytes = System.Text.Encoding.UTF8.GetBytes(error.Message);
                _stream.Write(messageBytes, 0, messageBytes.Length);
                break;
            case GameStateChangePacket gameStateChange:
                _stream.WriteByte((byte)gameStateChange.NewState);
                if (gameStateChange.TimeLimit.HasValue)
                {
                    _stream.WriteByte(1); 
                    var totalSeconds = (int)gameStateChange.TimeLimit.Value.TotalSeconds;
                    WriteInt32(totalSeconds);
                }
                else
                {
                    _stream.WriteByte(0); 
                }
                if (gameStateChange.RemainingShips != null)
                {
                    _stream.WriteByte((byte)gameStateChange.RemainingShips.Count);
                    foreach (var shipType in gameStateChange.RemainingShips)
                    {
                        _stream.WriteByte((byte)shipType);
                    }
                }
                break;
            case PlacementCompletePacket placementComplete:
                _stream.WriteByte(placementComplete.UsedRandomPlacement ? (byte)1 : (byte)0);
                _stream.WriteByte((byte)placementComplete.PlacedShips.Count);
                foreach (var ship in placementComplete.PlacedShips)
                {
                    _stream.WriteByte((byte)ship.Type);
                    WriteInt32(ship.Start.X);
                    WriteInt32(ship.Start.Y);
                    _stream.WriteByte(ship.Orientation == Orientation.Vertical ? (byte)1 : (byte)0);
                }
                break;
            case PlacementRestartPacket:
                break;
            case TimeWarningPacket timeWarning:
                var totalSecondsTime = (int)timeWarning.TimeRemaining.TotalSeconds;
                WriteInt32(totalSecondsTime);
                break;
            default:
                throw new InvalidOperationException($"Unknown packet type: {packet.Type}");
        }
        var payloadLength = (int)(_stream.Position - payloadStart);
        _stream.Position = 1;
        _stream.WriteByte((byte)(payloadLength >> 8));
        _stream.WriteByte((byte)(payloadLength & 0xFF));
        return _stream.ToArray();
    }
    private void WriteInt32(int value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        _stream.Write(bytes, 0, 4);
    }
}