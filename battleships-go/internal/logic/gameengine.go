package logic

import (
	"battleships-go/internal/network"
	"battleships-go/pkg/models"
	"math/rand"
	"time"
)

type GameEngine struct {
	playerBoards  [2]*network.Board
	standardShips []models.ShipType
	currentPlayer int
}

func NewGameEngine() *GameEngine {
	return &GameEngine{
		playerBoards: [2]*network.Board{
			network.NewBoard(),
			network.NewBoard(),
		},
		standardShips: []models.ShipType{
			models.Carrier,
			models.Battleship,
			models.Destroyer,
			models.Submarine,
			models.PatrolBoat,
		},
		currentPlayer: 1,
	}
}

func (e *GameEngine) GetPlayerBoard(player int) *network.Board {
	return e.playerBoards[player-1]
}

func (e *GameEngine) GetOpponentBoard(player int) *network.Board {
	if player == 1 {
		return e.playerBoards[1]
	}
	return e.playerBoards[0]
}

func (e *GameEngine) CurrentPlayer() int {
	return e.currentPlayer
}

func (e *GameEngine) PlaceShipForPlayer(shipType models.ShipType, start models.Coordinate, orientation models.Orientation, player int) bool {
	board := e.GetPlayerBoard(player)
	return board.PlaceShip(shipType, start, orientation)
}

func (e *GameEngine) FireShot(target models.Coordinate) models.ShotResult {
	opponentBoard := e.GetOpponentBoard(e.currentPlayer)
	result := opponentBoard.FireShot(target)

	if !opponentBoard.AreAllShipsSunk() {
		e.SwitchPlayer()
	}
	return result
}

func (e *GameEngine) IsGameOver() bool {
	for _, board := range e.playerBoards {
		if board.AreAllShipsSunk() {
			return true
		}
	}
	return false
}

func (e *GameEngine) SwitchPlayer() {
	if e.currentPlayer == 1 {
		e.currentPlayer = 2
	} else {
		e.currentPlayer = 1
	}
}

func (e *GameEngine) HasPlayerCompletedPlacement(player int) bool {
	board := e.GetPlayerBoard(player)
	return len(board.Ships) == len(e.standardShips)
}

func (e *GameEngine) GenerateRandomPlacement(player int) []*models.Ship {
	r := rand.New(rand.NewSource(time.Now().UnixNano()))
	var placedShips []*models.Ship

	for _, shipType := range e.standardShips {
		success := false
		for attempts := 0; attempts < 100; attempts++ {
			start := models.Coordinate{X: r.Intn(10), Y: r.Intn(10)}
			orientation := models.Orientation(r.Intn(2))

			if e.playerBoards[player-1].CanPlaceShip(shipType, start, orientation) {
				e.PlaceShipForPlayer(shipType, start, orientation, player)
				placedShips = append(placedShips, &models.Ship{
					Type:        shipType,
					Start:       start,
					Orientation: orientation,
				})
				success = true
				break

			}
		}
		if !success {
			return nil
		}
	}
	return placedShips
}

func (e *GameEngine) ResetPlayerBoard(player int) {
	e.playerBoards[player-1] = network.NewBoard()
}
