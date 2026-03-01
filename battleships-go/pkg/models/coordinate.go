package models

type Coordinate struct {
	X int
	Y int
}

func (c Coordinate) IsValid() bool {
	return c.X >= 0 && c.X < 10 && c.Y >= 0 && c.Y < 10
}
