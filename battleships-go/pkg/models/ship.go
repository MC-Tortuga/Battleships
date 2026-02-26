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
	coords :=make([]network.Coordinate,0,length)

	for i := 0; i < length; i++ {
		
	}
}
