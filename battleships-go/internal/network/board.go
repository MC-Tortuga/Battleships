package network

import ()

type Board struct {
	Ships       []*models.Ship
	MissedShots map[Coordinate]struct{}
	ShotHistory map[Coordinate]models.ShotResult
}

func NewBoard() *Board  {
	return &Board{Ships: }
	
}
