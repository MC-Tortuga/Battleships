package models

import "battleships-go/internal/network"

type Ship struct {
	Type                ShipType
	Start               network.Coordinate
	Orientation         Orientation
	OccupiedCoordinates []network.Coordinate
	Hits                map[network.Coordinate]struct{}
}

func NewShip(shipType ShipType, start network.Coordinate, orientation Orientation) *Ship {
	s := &Ship{
		Type:        shipType,
		Start:       start,
		Orientation: orientation,
		Hits:        make(map[network.Coordinate]struct{}),
	}
	s.OccupiedCoordinates = s.calculateOccupiedCoordinates()

	return s
}
func (s *Ship) Length() int {
	return s.Type.Size()
}

func (s *Ship) calculateOccupiedCoordinates() []network.Coordinate {
	length := s.Length()
	coords := make([]network.Coordinate, 0, length)

	for i := 0; i < length; i++ {
		var coord network.Coordinate
		if s.Orientation == Horizontal {
			coord = network.Coordinate{X: s.Start.X + i, Y: s.Start.Y}
		} else {
			coord = network.Coordinate{X: s.Start.X, Y: s.Start.Y + i}

		}

		coords = append(coords, coord)
	}
	return coords
}

func (s *Ship) IsSunk() bool {
	return len(s.Hits) == s.Length()
}

func (s *Ship) ContainsCoordinate(target network.Coordinate) bool {
	for _, coord := range s.OccupiedCoordinates {
		if coord == target {
			return true
		}
	}
	return false
}

func (s *Ship) FireShot(target network.Coordinate) ShotResult {
	if !s.ContainsCoordinate(target) {
		return Miss
	}
	s.Hits[target] = struct{}{}

	if s.IsSunk() {
		return Sunk
	}

	return Hit
}
