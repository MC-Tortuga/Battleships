package logic

import (
	"math/rand"
	"time"
)

type GameEngine struct {
	playerBoards  [2]*network.Board
	standardShips []models.ShipType
	currentPlayer int
}

func NewGameEngine() *GameEngine {
	return &GameEngine{playerBoards: [2]*network.Board{
		network.NewBoard(),
		network.NewBoard(),
	},
	standardShips:[]models.ShipType{
models.Carrier,
			models.Battleship,
			models.Destroyer,
			models.Submarine,
			models.PatrolBboat
		} ,
	currentPlayer: 1,
	}
}

