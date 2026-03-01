package protocol

import (
	"battleships-go/internal/network"
	"battleships-go/pkg/models"
	"encoding/binary"
	"errors"
	"io"
	"time"
)

type BinaryPacketReader struct{}

func NewBinaryPacketReader() *BinaryPacketReader {
	return &BinaryPacketReader{}
}

func (r *BinaryPacketReader) Deserialize(data []byte) (models.IPacket, error) {
	if len(data) < 3 {
		return nil, errors.New("packet data must be at least 3 bytes for header")
	}

	packetType := models.PacketType(data[0])
	length := uint16(data[1])<<8 | uint16(data[2])

	if len(data) != 3+int(length) {
		return nil, errors.New("packet length mismatch")
	}

	payload := data[3:]

	switch packetType {
	case models.PktFireShot:
		return r.readShotPacket(payload), nil
	case models.PktShotResult:
		return r.readShotResultPacket(payload), nil
	case models.PktPlaceShip:
		return r.readPlaceShipPacket(payload), nil
	case models.PktGameStart:
		return r.readGameStartPacket(payload), nil
	case models.PktTurnChange:
		return r.readTurnChangePacket(payload), nil
	case models.PktGameOver:
		return r.readGameOverPacket(payload), nil
	case models.PktError:
		return r.readErrorPacket(payload), nil
	case models.PktGameStateChange:
		return r.readGameStateChangePacket(payload), nil
	case models.PktPlacementComplete:
		return r.readPlacementCompletePacket(payload), nil
	case models.PktPlacementRestart:
		return network.NewPlacementRestartPacket(), nil
	case models.PktTimeWarning:
		return r.readTimeWarningPacket(payload), nil
	default:
		return nil, errors.New("unknown packet type")
	}
}

func (r *BinaryPacketReader) readShotPacket(payload []byte) *network.ShotPacket {
	if len(payload) != 8 {
		return &network.ShotPacket{Coordinate: models.Coordinate{}}
	}
	x := int32(binary.BigEndian.Uint32(payload[0:4]))
	y := int32(binary.BigEndian.Uint32(payload[4:8]))
	return network.NewShotPacket(models.Coordinate{X: int(x), Y: int(y)})
}

func (r *BinaryPacketReader) readShotResultPacket(payload []byte) *network.ShotResultPacket {
	result := models.ShotResult(payload[0])
	var sunkShipType *models.ShipType
	if len(payload) > 1 && result == models.Sunk {
		st := models.ShipType(payload[1])
		sunkShipType = &st
	}
	return network.NewShotResultPacket(result, sunkShipType)
}

func (r *BinaryPacketReader) readPlaceShipPacket(payload []byte) *network.PlaceShipPacket {
	if len(payload) != 10 {
		return &network.PlaceShipPacket{}
	}
	shipType := models.ShipType(payload[0])
	x := int32(binary.BigEndian.Uint32(payload[1:5]))
	y := int32(binary.BigEndian.Uint32(payload[5:9]))
	orientation := models.Orientation(payload[9])
	return network.NewPlaceShipPacket(shipType, models.Coordinate{X: int(x), Y: int(y)}, orientation)
}

func (r *BinaryPacketReader) readGameStartPacket(payload []byte) *network.GameStartPacket {
	if len(payload) != 1 {
		return network.NewGameStartPacket(0)
	}
	return network.NewGameStartPacket(int(payload[0]))
}

func (r *BinaryPacketReader) readTurnChangePacket(payload []byte) *network.TurnChangePacket {
	if len(payload) != 1 {
		return network.NewTurnChangePacket(0)
	}
	return network.NewTurnChangePacket(int(payload[0]))
}

func (r *BinaryPacketReader) readGameOverPacket(payload []byte) *network.GameOverPacket {
	if len(payload) != 1 {
		return network.NewGameOverPacket(0)
	}
	return network.NewGameOverPacket(int(payload[0]))
}

func (r *BinaryPacketReader) readErrorPacket(payload []byte) *network.ErrorPacket {
	if len(payload) < 2 {
		return network.NewErrorPacket(0, "")
	}
	errorCode := uint16(payload[0])<<8 | uint16(payload[1])
	message := ""
	if len(payload) > 2 {
		message = string(payload[2:])
	}
	return network.NewErrorPacket(errorCode, message)
}

func (r *BinaryPacketReader) readGameStateChangePacket(payload []byte) *network.GameStateChangePacket {
	if len(payload) == 0 {
		return network.NewGameStateChangePacket(models.Lobby, nil, nil)
	}

	index := 0
	newState := models.GameState(payload[index])
	index++

	var timeLimit *time.Duration
	if index < len(payload) && payload[index] == 1 {
		index++
		if index+4 <= len(payload) {
			totalSeconds := int32(binary.BigEndian.Uint32(payload[index : index+4]))
			ts := time.Duration(totalSeconds) * time.Second
			timeLimit = &ts
			index += 4
		}
	} else if index < len(payload) {
		index++
	}

	var remainingShips []models.ShipType
	if index < len(payload) {
		shipCount := int(payload[index])
		index++
		remainingShips = make([]models.ShipType, 0, shipCount)
		for i := 0; i < shipCount && index < len(payload); i++ {
			remainingShips = append(remainingShips, models.ShipType(payload[index]))
			index++
		}
	}

	return network.NewGameStateChangePacket(newState, timeLimit, remainingShips)
}

func (r *BinaryPacketReader) readPlacementCompletePacket(payload []byte) *network.PlacementCompletePacket {
	if len(payload) < 2 {
		return network.NewPlacementCompletePacket(false, nil)
	}

	index := 0
	usedRandom := payload[index] == 1
	index++

	shipCount := int(payload[index])
	index++

	ships := make([]*models.Ship, 0, shipCount)
	for i := 0; i < shipCount && index+10 <= len(payload); i++ {
		shipType := models.ShipType(payload[index])
		index++
		x := int32(binary.BigEndian.Uint32(payload[index : index+4]))
		index += 4
		y := int32(binary.BigEndian.Uint32(payload[index : index+4]))
		index += 4
		orientation := models.Orientation(payload[index])
		index++

		ships = append(ships, models.NewShip(shipType, models.Coordinate{X: int(x), Y: int(y)}, orientation))
	}

	return network.NewPlacementCompletePacket(usedRandom, ships)
}

func (r *BinaryPacketReader) readTimeWarningPacket(payload []byte) *network.TimeWarningPacket {
	if len(payload) < 4 {
		return network.NewTimeWarningPacket(0)
	}
	totalSeconds := int32(binary.BigEndian.Uint32(payload[0:4]))
	return network.NewTimeWarningPacket(time.Duration(totalSeconds) * time.Second)
}

func ReadFullPacket(reader io.Reader) (models.IPacket, error) {
	header := make([]byte, 3)
	_, err := io.ReadFull(reader, header)
	if err != nil {
		return nil, err
	}

	length := uint16(header[1])<<8 | uint16(header[2])
	payload := make([]byte, length)
	_, err = io.ReadFull(reader, payload)
	if err != nil {
		return nil, err
	}

	bpr := NewBinaryPacketReader()
	return bpr.Deserialize(append(header, payload...))
}
