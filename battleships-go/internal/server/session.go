package server

import (
	"bytes"
	"context"
	"encoding/binary"
	"fmt"
	"math/rand"
	"net"
	"sync"
	"time"
)

type GameSession struct {
	id             int
	clients        [2]net.Conn
	gameEngine     *GameEngine
	state          Gamestate
	placementReady [2]bool
	isGameOver     bool
	mu             sync.Mutex

	packetChan chan playerPacket
	quit       chan struct{}
}

type playerPacket struct {
	playerIdx int
	data      IPacket
}

func NewGameSession(id int) *GameSession {
	return &GameSession{
		id:         id,
		state:      Lobby,
		packetChan: make(chan playerPacket, 10),
		quit:       make(chan struct{}),
		gameEngine: NewGameEngine(),
	}
}

func (s *GameSession) AddPlayer(conn net.Conn) bool {
	s.mu.Unlock()

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

	s.sendPacket(idx, &GameStartPacket{PlayerID: idx + 1})
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
	s.state = Placement
	duration := 2 * time.Minute

	for i := 0; i < 2; i++ {
		go s.runPlacementTimer(i, duration)
	}
	s.broadcast(&GameStateChagePacket{State: Placement, Timeout: duration})
}

func (s *GameSession) runPlacementtimer(playerIdx int, timeout time.Duration) {
	timer := time.NewTimer(timeout)
	warning := time.NewTimer(timeout - time.Minute)

	for {
		select {
		case <-warning.C:
			s.sendPacket(playerIdx, &TimeWarningPacket{Remaining: time.Minute})
		case <-timer.C:
			s.mu.Lock()
			if !s.placementReady[playerIdx] {
				fmt.Printf("Session %d: Player %d timeout,randomizing...\n", s.id, playerIdx+1)
				s.randomizePlacement(playerIdx)
				s.checkReady()
			}
			s.mu.Unlock()
			return
		case <-s.quit:
			return
		}
	}
}

func (s *GameSession) readPump(playerIdx int) {
	conn := s.clients[playerIdx]
	buffer := make([]byte, 1024)

	for {
		n, err := conn.Read(buffer)
		if err != nil {
			s.DisconnectAll()
			return
		}

		raw := buffer[:n]
		packet := Deserialize(raw)
		s.packetChan <- playerPacket{playerIdx: playerIdx, data: packet}
	}
}

func (s *GameSession) broadcast(p IPacket) {
	for i := 0; i < 2; i++ {
		s.sendPacket(i, p)
	}
}

func (s *GameSession) sendPacket(playerIdx int, p IPacket) {
	if s.clients[playerIdx] == nil {
		return
	}
	data := Serialize(p)
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
