package models

type ShipType int

const (
	Carrier ShipType = iota
	Battleship
	Destroyer
	Submarine
	PatrolBoat
)

func (s ShipType) Size() int {
	switch s {
	case Carrier:
		return 5
	case Battleship:
		return 4
	case Destroyer:
		return 3
	case Submarine:
		return 3
	case PatrolBoat:
		return 2
	default:
		return 0

	}
}

type Orientation int

const (
	Horizontal Orientation = iota
	Vertical
)

type ShotResult int

const (
	Miss ShotResult = iota
	Hit
	Sunk
)

type GameState int

const (
	Lobby GameState = iota
	Placement
	Battle
	GameOver
)

type PlacementErrorType int

const (
	OutOfBounds PlacementErrorType = iota
	Overlap
	InvalidCoordinates
	ShipSpecific
)
