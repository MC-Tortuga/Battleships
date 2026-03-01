package server

import (
	"fmt"
	"net"
	"sync/atomic"
)

type Matchmaker struct {
	joinQueue      chan net.Conn
	cleanupQueue   chan int
	activeSessions map[int]*GameSession
	nextSessionID  int32
}

func NewMatchMaker() *Matchmaker {
	m := &Matchmaker{
		joinQueue:      make(chan net.Conn),
		cleanupQueue:   make(chan int),
		activeSessions: make(map[int]*GameSession),
		nextSessionID:  1,
	}
	go m.run()

	return m
}

func (m *Matchmaker) AddClient(conn net.Conn) {
	fmt.Println("Matmaker: Client added to queue.")
	m.joinQueue <- conn
}

func (m *Matchmaker) run() {
	var waitingClient net.Conn

	for {
		select {
		case conn := <-m.joinQueue:
			if waitingClient == nil {
				waitingClient = conn
				fmt.Println("Matchmaker: Waiting for a second player...")
			} else {
				sID := int(atomic.AddInt32(&m.nextSessionID, 1) - 1)
				session := NewGameSession(sID)

				session.AddPlayer(waitingClient)
				session.AddPlayer(conn)

				m.activeSessions[sID] = session
				fmt.Printf("Matchmaker: Created session %d for 2 players.\n", sID)

				waitingClient = nil
			}

		case sID := <-m.cleanupQueue:
			if session, exists := m.activeSessions[sID]; exists {
				session.DisconnectAll()
				delete(m.activeSessions, sID)
				fmt.Printf("Matchmaker: Cleaned up session %d.\n", sID)
			}

		}
	}
}
