package models

type Ship struct {
	Type                ShipType
	Start               Coordinate
	Orientation         Orientation
	OccupiedCoordinates []Coordinate
	Hits                map[Coordinate]struct{}
}

func NewShip(shipType ShipType, start Coordinate, orientation Orientation) *Ship {
	s := &Ship{
		Type:        shipType,
		Start:       start,
		Orientation: orientation,
		Hits:        make(map[Coordinate]struct{}),
	}
	s.OccupiedCoordinates = s.calculateOccupiedCoordinates()

	return s
}
func (s *Ship) Length() int {
	return s.Type.Size()
}

func (s *Ship) calculateOccupiedCoordinates() []Coordinate {
	length := s.Length()
	coords := make([]Coordinate, 0, length)

	for i := 0; i < length; i++ {
		var coord Coordinate
		if s.Orientation == Horizontal {
			coord = Coordinate{X: s.Start.X + i, Y: s.Start.Y}
		} else {
			coord = Coordinate{X: s.Start.X, Y: s.Start.Y + i}

		}

		coords = append(coords, coord)
	}
	return coords
}

func (s *Ship) IsSunk() bool {
	return len(s.Hits) == s.Length()
}

func (s *Ship) ContainsCoordinate(target Coordinate) bool {
	for _, coord := range s.OccupiedCoordinates {
		if coord == target {
			return true
		}
	}
	return false
}

func (s *Ship) FireShot(target Coordinate) ShotResult {
	if !s.ContainsCoordinate(target) {
		return Miss
	}
	s.Hits[target] = struct{}{}

	if s.IsSunk() {
		return Sunk
	}

	return Hit
}
