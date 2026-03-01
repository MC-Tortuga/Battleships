package server

import (
	"battleships-go/internal/logic"
	"battleships-go/internal/network"
	"battleships-go/pkg/models"
	"battleships-go/pkg/protocol"
	"fmt"
	"math/rand"
	"net"
	"sync"
	"time"
)

type GameSession struct {
	id             int
	clients        [2]net.Conn
	gameEngine     *logic.GameEngine
	state          models.GameState
	placementReady [2]bool
	isGameOver     bool
	mu             sync.Mutex
	placementStart [2]time.Time

	packetChan chan playerPacket
	quit       chan struct{}
}

type playerPacket struct {
	playerIdx int
	data      models.IPacket
}

func NewGameSession(id int) *GameSession {
	return &GameSession{
		id:         id,
		state:      models.Lobby,
		packetChan: make(chan playerPacket, 10),
		quit:       make(chan struct{}),
		gameEngine: logic.NewGameEngine(),
	}
}

func (s *GameSession) AddPlayer(conn net.Conn) bool {
	s.mu.Lock()
	defer s.mu.Unlock()

	idx := -1
	if s.clients[0] == nil {
		idx = 0
	} else if s.clients[1] == nil {
		idx = 1
	}

	if idx == -1 {
		return false
	}

	s.clients[idx] = conn

	s.sendPacket(idx, network.NewGameStartPacket(idx+1))

	s.state = models.Placement
	s.placementReady[0] = false
	s.placementReady[1] = false

	if s.clients[1] != nil {
		go s.runGameLoop()
	}
	return true
}

func (s *GameSession) runGameLoop() {
	fmt.Printf("Session %d: Game Starting\n", s.id)

	for i := 0; i < 2; i++ {
		go s.readPump(i)
	}

	s.startPlacementPhase()

	for {
		select {
		case p := <-s.packetChan:
			s.handlePacket(p.playerIdx, p.data)
		case <-s.quit:
			return
		}
	}
}

func (s *GameSession) startPlacementPhase() {
	s.state = models.Placement
	duration := 2 * time.Minute

	remainingShips := []models.ShipType{
		models.Carrier,
		models.Battleship,
		models.Destroyer,
		models.Submarine,
		models.PatrolBoat,
	}

	for i := 0; i < 2; i++ {
		s.placementStart[i] = time.Now()
		go s.runPlacementTimer(i, duration)
	}

	statePacket := network.NewGameStateChangePacket(models.Placement, &duration, remainingShips)
	s.broadcast(statePacket)
}

func (s *GameSession) runPlacementTimer(playerIdx int, timeout time.Duration) {
	timer := time.NewTimer(timeout)

	for {
		select {
		case <-timer.C:
			s.mu.Lock()
			if !s.placementReady[playerIdx] {
				fmt.Printf("Session %d: Player %d timeout, randomizing...\n", s.id, playerIdx+1)
				s.randomizePlacement(playerIdx)
				s.placementReady[playerIdx] = true
				s.checkReady()
			}
			s.mu.Unlock()
			return
		case <-s.quit:
			return
		}
	}
}

func (s *GameSession) randomizePlacement(playerIdx int) {
	r := rand.New(rand.NewSource(time.Now().UnixNano()))
	ships := []models.ShipType{
		models.Carrier,
		models.Battleship,
		models.Destroyer,
		models.Submarine,
		models.PatrolBoat,
	}

	for _, shipType := range ships {
		placed := false
		for attempts := 0; attempts < 100 && !placed; attempts++ {
			start := models.Coordinate{X: r.Intn(10), Y: r.Intn(10)}
			orientation := models.Orientation(r.Intn(2))
			placed = s.gameEngine.PlaceShipForPlayer(shipType, start, orientation, playerIdx+1)
		}
	}
	s.placementReady[playerIdx] = true
	s.checkReady()
}

func (s *GameSession) checkReady() {
	if s.placementReady[0] && s.placementReady[1] {
		s.state = models.Battle
		statePacket := network.NewGameStateChangePacket(models.Battle, nil, nil)
		s.broadcast(statePacket)

		turnPacket := network.NewTurnChangePacket(s.gameEngine.CurrentPlayer())
		s.broadcast(turnPacket)
	}
}

func (s *GameSession) readPump(playerIdx int) {
	conn := s.clients[playerIdx]
	reader := protocol.NewBinaryPacketReader()
	buffer := make([]byte, 4096)
	bufLen := 0

	for {
		n, err := conn.Read(buffer[bufLen:])
		if err != nil {
			s.DisconnectAll()
			return
		}
		bufLen += n
		data := buffer[:bufLen]

		offset := 0
		for offset+3 <= bufLen {
			length := int(uint16(data[offset+1])<<8 | uint16(data[offset+2]))
			if offset+3+length > bufLen {
				break
			}
			packetData := data[offset : offset+3+length]
			offset += 3 + length

			packet, err := reader.Deserialize(packetData)
			if err != nil {
				fmt.Printf("Session %d: Error deserializing packet: %v\n", s.id, err)
				continue
			}
			s.packetChan <- playerPacket{playerIdx: playerIdx, data: packet}
		}

		if offset > 0 {
			copy(buffer, buffer[offset:bufLen])
			bufLen -= offset
		}
	}
}

func (s *GameSession) handlePacket(playerIdx int, packet models.IPacket) {
	s.mu.Lock()
	defer s.mu.Unlock()

	switch p := packet.(type) {
	case *network.ShotPacket:
		s.handleShot(playerIdx, p)
	case *network.PlaceShipPacket:
		s.handlePlaceShip(playerIdx, p)
	case *network.PlacementCompletePacket:
		s.handlePlacementComplete(playerIdx, p)
	case *network.PlacementRestartPacket:
		s.handlePlacementRestart(playerIdx)
	}
}

func (s *GameSession) handleShot(playerIdx int, shot *network.ShotPacket) {
	if s.state != models.Battle || playerIdx+1 != s.gameEngine.CurrentPlayer() {
		errPacket := network.NewErrorPacket(0x0003, "Cannot shoot during placement phase or when not your turn")
		s.sendPacket(playerIdx, errPacket)
		return
	}

	result := s.gameEngine.FireShot(shot.Coordinate)
	var sunkShipType *models.ShipType
	if result == models.Sunk {
		st := s.getSunkShipType()
		sunkShipType = &st
	}

	resultPacket := network.NewShotResultPacket(result, sunkShipType)
	s.broadcast(resultPacket)

	if s.gameEngine.IsGameOver() {
		s.isGameOver = true
		gameOverPacket := network.NewGameOverPacket(s.gameEngine.CurrentPlayer())
		s.broadcast(gameOverPacket)
		close(s.quit)
	} else {
		turnPacket := network.NewTurnChangePacket(s.gameEngine.CurrentPlayer())
		s.broadcast(turnPacket)
	}
}

func (s *GameSession) handlePlaceShip(playerIdx int, place *network.PlaceShipPacket) {
	if s.state != models.Placement {
		errPacket := network.NewErrorPacket(0x0004, "Cannot place ships during battle phase")
		s.sendPacket(playerIdx, errPacket)
		return
	}

	s.gameEngine.PlaceShipForPlayer(place.ShipType, place.Start, place.Orientation, playerIdx+1)
}

func (s *GameSession) createPlacementError(player int, shipType models.ShipType, start models.Coordinate, orientation models.Orientation) string {
	ship := models.NewShip(shipType, start, orientation)
	board := s.gameEngine.GetPlayerBoard(player)

	var invalidCoords []string
	for _, c := range ship.OccupiedCoordinates {
		if !c.IsValid() {
			invalidCoords = append(invalidCoords, fmt.Sprintf("(%d,%d)", c.X, c.Y))
		}
	}
	if len(invalidCoords) > 0 {
		return fmt.Sprintf("Cannot place %v at (%d,%d) %v: Ship extends beyond board. Invalid coordinates: %s",
			shipType, start.X, start.Y, orientation, joinStrings(invalidCoords))
	}

	var overlapCoords []string
	for _, existingShip := range board.Ships {
		for _, coord := range ship.OccupiedCoordinates {
			if existingShip.ContainsCoordinate(coord) {
				overlapCoords = append(overlapCoords, fmt.Sprintf("(%d,%d)", coord.X, coord.Y))
			}
		}
	}
	if len(overlapCoords) > 0 {
		return fmt.Sprintf("Cannot place %v at (%d,%d) %v: Overlaps with existing ship at %s",
			shipType, start.X, start.Y, orientation, joinStrings(overlapCoords))
	}

	return fmt.Sprintf("Cannot place %v at (%d,%d) %v: Invalid placement", shipType, start.X, start.Y, orientation)
}

func joinStrings(strs []string) string {
	result := ""
	for i, s := range strs {
		if i > 0 {
			result += ", "
		}
		result += s
	}
	return result
}

func (s *GameSession) getSunkShipType() models.ShipType {
	return models.Carrier
}

func (s *GameSession) handlePlacementComplete(playerIdx int, packet *network.PlacementCompletePacket) {
	board := s.gameEngine.GetPlayerBoard(playerIdx + 1)
	confirmPacket := network.NewPlacementCompletePacket(packet.UsedRandomPlacement, board.Ships)
	s.sendPacket(playerIdx, confirmPacket)

	s.placementReady[playerIdx] = true
	s.checkReady()
}

func (s *GameSession) handlePlacementRestart(playerIdx int) {
	s.gameEngine.ResetPlayerBoard(playerIdx + 1)
	s.placementReady[playerIdx] = false
	s.placementStart[playerIdx] = time.Now()

	remainingShips := []models.ShipType{
		models.Carrier,
		models.Battleship,
		models.Destroyer,
		models.Submarine,
		models.PatrolBoat,
	}
	duration := 2 * time.Minute
	statePacket := network.NewGameStateChangePacket(models.Placement, &duration, remainingShips)
	s.sendPacket(playerIdx, statePacket)
}

func (s *GameSession) broadcast(p models.IPacket) {
	writer := protocol.NewBinaryPacketWriter()
	data, err := writer.Serialize(p)
	if err != nil {
		return
	}

	for i := 0; i < 2; i++ {
		if s.clients[i] != nil {
			s.clients[i].Write(data)
		}
	}
}

func (s *GameSession) sendPacket(playerIdx int, p models.IPacket) {
	if s.clients[playerIdx] == nil {
		return
	}
	writer := protocol.NewBinaryPacketWriter()
	data, err := writer.Serialize(p)
	if err != nil {
		return
	}
	s.clients[playerIdx].Write(data)
}

func (s *GameSession) DisconnectAll() {
	s.mu.Lock()
	defer s.mu.Unlock()
	if s.isGameOver {
		return
	}

	s.isGameOver = true
	close(s.quit)
	for _, c := range s.clients {
		if c != nil {
			c.Close()
		}
	}
}
