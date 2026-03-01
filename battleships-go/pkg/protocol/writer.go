package protocol

import (
	"battleships-go/internal/network"
	"battleships-go/pkg/models"
	"bytes"
	"encoding/binary"
	"errors"
	"io"
)

type BinaryPacketWriter struct {
	buffer *bytes.Buffer
}

func NewBinaryPacketWriter() *BinaryPacketWriter {
	return &BinaryPacketWriter{
		buffer: new(bytes.Buffer),
	}
}

func (w *BinaryPacketWriter) Serialize(packet models.IPacket) ([]byte, error) {
	w.buffer.Reset()

	w.buffer.WriteByte(byte(packet.GetType()))
	w.buffer.WriteByte(0)
	w.buffer.WriteByte(0)

	payloadStart := w.buffer.Len()

	switch p := packet.(type) {
	case *network.ShotPacket:
		w.writeInt32(int32(p.Coordinate.X))
		w.writeInt32(int32(p.Coordinate.Y))
	case *network.ShotResultPacket:
		w.buffer.WriteByte(byte(p.Result))
		if p.Result == models.Sunk && p.SunkShipType != nil {
			w.buffer.WriteByte(byte(*p.SunkShipType))
		}
	case *network.PlaceShipPacket:
		w.buffer.WriteByte(byte(p.ShipType))
		w.writeInt32(int32(p.Start.X))
		w.writeInt32(int32(p.Start.Y))
		if p.Orientation == models.Vertical {
			w.buffer.WriteByte(1)
		} else {
			w.buffer.WriteByte(0)
		}
	case *network.GameStartPacket:
		w.buffer.WriteByte(byte(p.PlayerId))
	case *network.TurnChangePacket:
		w.buffer.WriteByte(byte(p.PlayerId))
	case *network.GameOverPacket:
		w.buffer.WriteByte(byte(p.WinnerId))
	case *network.ErrorPacket:
		w.buffer.WriteByte(byte(p.ErrorCode >> 8))
		w.buffer.WriteByte(byte(p.ErrorCode & 0xFF))
		w.buffer.WriteString(p.Message)
	case *network.GameStateChangePacket:
		w.buffer.WriteByte(byte(p.NewState))
		if p.TimeLimit != nil {
			w.buffer.WriteByte(1)
			w.writeInt32(int32(p.TimeLimit.Seconds()))
		} else {
			w.buffer.WriteByte(0)
		}
		if p.RemainingShips != nil {
			w.buffer.WriteByte(byte(len(p.RemainingShips)))
			for _, st := range p.RemainingShips {
				w.buffer.WriteByte(byte(st))
			}
		}
	case *network.PlacementCompletePacket:
		if p.UsedRandomPlacement {
			w.buffer.WriteByte(1)
		} else {
			w.buffer.WriteByte(0)
		}
		w.buffer.WriteByte(byte(len(p.PlacedShips)))
		for _, ship := range p.PlacedShips {
			w.buffer.WriteByte(byte(ship.Type))
			w.writeInt32(int32(ship.Start.X))
			w.writeInt32(int32(ship.Start.Y))
			if ship.Orientation == models.Vertical {
				w.buffer.WriteByte(1)
			} else {
				w.buffer.WriteByte(0)
			}
		}
	case *network.PlacementRestartPacket:
		break
	case *network.TimeWarningPacket:
		w.writeInt32(int32(p.TimeRemaining.Seconds()))
	default:
		return nil, errors.New("unknown packet type")
	}

	payloadLength := w.buffer.Len() - payloadStart
	w.buffer.Bytes()[1] = byte(payloadLength >> 8)
	w.buffer.Bytes()[2] = byte(payloadLength & 0xFF)

	return w.buffer.Bytes(), nil
}

func (w *BinaryPacketWriter) writeInt32(value int32) {
	b := make([]byte, 4)
	binary.BigEndian.PutUint32(b, uint32(value))
	w.buffer.Write(b)
}

func WritePacket(writer io.Writer, packet models.IPacket) error {
	bpw := NewBinaryPacketWriter()
	data, err := bpw.Serialize(packet)
	if err != nil {
		return err
	}
	_, err = writer.Write(data)
	return err
}
