package network

import "battleships-go/pkg/models"

type Board struct {
	Ships       []*models.Ship
	MissedShots map[Coordinate]struct{}
	ShotHistory map[Coordinate]models.ShotResult
}

func NewBoard() *Board {
	return &Board{

		Ships:       make([]*models.Ship, 0),
		MissedShots: make(map[Coordinate]struct{}),
		ShotHistory: make(map[Coordinate]models.ShotResult),
	}
}

func (b *Board) CanPlaceShip(shipType models.ShipType, start Coordinate, orientation models.Orientation) bool {
	newShip := models.NewShip(shipType, start, orientation)
	for _, c := range newShip.OccupiedCoordinates {
		if !c.IsValid() {
			return false
		}
	}

	for _, existingShip := range b.Ships {
		for _, newCoord := range newShip.OccupiedCoordinates {
			if existingShip.ContainsCoordinate(newCoord) {
				return false
			}
		}
	}

	return true
}

func (b *Board) PlaceShip(shipType models.ShipType, start Coordinate, orientation models.Orientation) bool {
	if !b.CanPlaceShip(shipType, start, orientation) {
		return false
	}

	b.Ships = append(b.Ships, models.NewShip(shipType, start, orientation))
	return true
}

func (b *Board) FireShot(target Coordinate) models.ShotResult {
	if !target.IsValid() {
		return models.Miss
	}
	if _, alreadyShot := b.ShotHistory[target]; alreadyShot {
		return models.Miss
	}

	for _, ship := range b.Ships {
		result := ship.FireShot(target)

		if result != models.Miss {
			b.ShotHistory[target] = result
			return result
		}
	}

	b.MissedShots[target] = struct{}{}
	b.ShotHistory[target] = models.Miss

	return models.Miss
}

func (b *Board) AreAllShipsSunk() bool {
	if len(b.Ships) == 0 {
		return false
	}

	for _, ship := range b.Ships {
		if !ship.IsSunk() {
			return false
		}
	}

	return true
}
