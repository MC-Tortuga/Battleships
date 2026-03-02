package network

import "battleships-go/pkg/models"

type Board struct {
	Ships       []*models.Ship
	MissedShots map[models.Coordinate]struct{}
	ShotHistory map[models.Coordinate]models.ShotResult
}

func NewBoard() *Board {
	return &Board{

		Ships:       make([]*models.Ship, 0),
		MissedShots: make(map[models.Coordinate]struct{}),
		ShotHistory: make(map[models.Coordinate]models.ShotResult),
	}
}

func (b *Board) CanPlaceShip(shipType models.ShipType, start models.Coordinate, orientation models.Orientation) bool {
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

func (b *Board) PlaceShip(shipType models.ShipType, start models.Coordinate, orientation models.Orientation) bool {
	if !b.CanPlaceShip(shipType, start, orientation) {
		return false
	}

	b.Ships = append(b.Ships, models.NewShip(shipType, start, orientation))
	return true
}

func (b *Board) FireShot(target models.Coordinate) models.ShotResult {
	result, _ := b.FireShotWithType(target)
	return result
}

func (b *Board) FireShotWithType(target models.Coordinate) (models.ShotResult, *models.ShipType) {
	if !target.IsValid() {
		return models.Miss, nil
	}
	if _, alreadyShot := b.ShotHistory[target]; alreadyShot {
		return models.Miss, nil
	}

	for _, ship := range b.Ships {
		result := ship.FireShot(target)

		if result != models.Miss {
			b.ShotHistory[target] = result
			if result == models.Sunk {
				st := ship.Type
				return result, &st
			}
			return result, nil
		}
	}

	b.MissedShots[target] = struct{}{}
	b.ShotHistory[target] = models.Miss

	return models.Miss, nil
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
