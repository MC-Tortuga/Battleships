package network

import (
	"battleships-go/pkg/models"
	"time"
)

type ShotPacket struct {
	Coordinate models.Coordinate
}

func NewShotPacket(coord models.Coordinate) *ShotPacket {
	return &ShotPacket{Coordinate: coord}
}

func (p *ShotPacket) GetType() models.PacketType {
	return models.PktFireShot
}

type ShotResultPacket struct {
	Result       models.ShotResult
	SunkShipType *models.ShipType
}

func NewShotResultPacket(result models.ShotResult, sunkShipType *models.ShipType) *ShotResultPacket {
	return &ShotResultPacket{Result: result, SunkShipType: sunkShipType}
}

func (p *ShotResultPacket) GetType() models.PacketType {
	return models.PktShotResult
}

type PlaceShipPacket struct {
	ShipType    models.ShipType
	Start       models.Coordinate
	Orientation models.Orientation
}

func NewPlaceShipPacket(shipType models.ShipType, start models.Coordinate, orientation models.Orientation) *PlaceShipPacket {
	return &PlaceShipPacket{ShipType: shipType, Start: start, Orientation: orientation}
}

func (p *PlaceShipPacket) GetType() models.PacketType {
	return models.PktPlaceShip
}

type GameStartPacket struct {
	PlayerId int
}

func NewGameStartPacket(playerId int) *GameStartPacket {
	return &GameStartPacket{PlayerId: playerId}
}

func (p *GameStartPacket) GetType() models.PacketType {
	return models.PktGameStart
}

type TurnChangePacket struct {
	PlayerId int
}

func NewTurnChangePacket(playerId int) *TurnChangePacket {
	return &TurnChangePacket{PlayerId: playerId}
}

func (p *TurnChangePacket) GetType() models.PacketType {
	return models.PktTurnChange
}

type GameOverPacket struct {
	WinnerId int
}

func NewGameOverPacket(winnerId int) *GameOverPacket {
	return &GameOverPacket{WinnerId: winnerId}
}

func (p *GameOverPacket) GetType() models.PacketType {
	return models.PktGameOver
}

type ErrorPacket struct {
	ErrorCode uint16
	Message   string
}

func NewErrorPacket(errorCode uint16, message string) *ErrorPacket {
	return &ErrorPacket{ErrorCode: errorCode, Message: message}
}

func (p *ErrorPacket) GetType() models.PacketType {
	return models.PktError
}

type GameStateChangePacket struct {
	NewState       models.GameState
	TimeLimit      *time.Duration
	RemainingShips []models.ShipType
}

func NewGameStateChangePacket(newState models.GameState, timeLimit *time.Duration, remainingShips []models.ShipType) *GameStateChangePacket {
	return &GameStateChangePacket{NewState: newState, TimeLimit: timeLimit, RemainingShips: remainingShips}
}

func (p *GameStateChangePacket) GetType() models.PacketType {
	return models.PktGameStateChange
}

type PlacementCompletePacket struct {
	UsedRandomPlacement bool
	PlacedShips         []*models.Ship
}

func NewPlacementCompletePacket(usedRandom bool, placedShips []*models.Ship) *PlacementCompletePacket {
	return &PlacementCompletePacket{UsedRandomPlacement: usedRandom, PlacedShips: placedShips}
}

func (p *PlacementCompletePacket) GetType() models.PacketType {
	return models.PktPlacementComplete
}

type PlacementRestartPacket struct{}

func NewPlacementRestartPacket() *PlacementRestartPacket {
	return &PlacementRestartPacket{}
}

func (p *PlacementRestartPacket) GetType() models.PacketType {
	return models.PktPlacementRestart
}

type TimeWarningPacket struct {
	TimeRemaining time.Duration
}

func NewTimeWarningPacket(timeRemaining time.Duration) *TimeWarningPacket {
	return &TimeWarningPacket{TimeRemaining: timeRemaining}
}

func (p *TimeWarningPacket) GetType() models.PacketType {
	return models.PktTimeWarning
}
