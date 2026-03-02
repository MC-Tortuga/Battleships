package main

import (
	"battleships-go/internal/network"
	"battleships-go/pkg/models"
	"battleships-go/pkg/protocol"
	"bufio"
	"fmt"
	"math/rand"
	"net"
	"os"
	"strings"
	"time"
)

type Client struct {
	conn          net.Conn
	reader        *bufio.Reader
	packetReader  *protocol.BinaryPacketReader
	packetWriter  *protocol.BinaryPacketWriter
	playerID      int
	state         models.GameState
	myBoard       [][]byte
	oppBoard      [][]byte
	shipsPlaced   []models.ShipType
	placementDone bool
}

const (
	Empty = iota
	Carrier
	Battleship
	Destroyer
	Submarine
	PatrolBoat
	Miss
	Hit
	Sunk
)

func shipTypeChar(shipType models.ShipType) byte {
	switch shipType {
	case models.Carrier:
		return Carrier
	case models.Battleship:
		return Battleship
	case models.Destroyer:
		return Destroyer
	case models.Submarine:
		return Submarine
	case models.PatrolBoat:
		return PatrolBoat
	default:
		return Empty
	}
}

func NewClient() *Client {
	return &Client{
		reader:       bufio.NewReader(os.Stdin),
		packetReader: protocol.NewBinaryPacketReader(),
		packetWriter: protocol.NewBinaryPacketWriter(),
		myBoard:      makeBoard(),
		oppBoard:     makeBoard(),
		shipsPlaced:  []models.ShipType{},
	}
}

func makeBoard() [][]byte {
	board := make([][]byte, 10)
	for i := range board {
		board[i] = make([]byte, 10)
	}
	return board
}

func (c *Client) printBoards() {
	fmt.Println("\n  YOUR BOARD              OPPONENT BOARD")
	fmt.Println("  0 1 2 3 4 5 6 7 8 9    0 1 2 3 4 5 6 7 8 9")
	for i := 0; i < 10; i++ {
		fmt.Printf("%d ", i)
		for j := 0; j < 10; j++ {
			fmt.Print(cellChar(c.myBoard[i][j]), " ")
		}
		fmt.Printf("  %d ", i)
		for j := 0; j < 10; j++ {
			fmt.Print(cellChar(c.oppBoard[i][j]), " ")
		}
		fmt.Println()
	}
	fmt.Println("  ~ = water, C = Carrier, B = Battleship, D = Destroyer, U = Submarine, P = PatrolBoat, M = miss, H = hit, X = sunk")
}

func cellChar(v byte) string {
	switch v {
	case Carrier:
		return "C"
	case Battleship:
		return "B"
	case Destroyer:
		return "D"
	case Submarine:
		return "U"
	case PatrolBoat:
		return "P"
	case Miss:
		return "M"
	case Hit:
		return "H"
	case Sunk:
		return "X"
	default:
		return "~"
	}
}

func (c *Client) Connect(addr string) error {
	conn, err := net.Dial("tcp", addr)
	if err != nil {
		return err
	}
	c.conn = conn
	fmt.Println("Connected to server!")
	return nil
}

func (c *Client) Run() error {
	go c.readLoop()

	for {
		if c.playerID == 0 {
			time.Sleep(100 * time.Millisecond)
			continue
		}

		if c.state == models.Placement {
			c.doPlacement()
		} else if c.state == models.Battle {
			c.doBattle()
		} else if c.state == models.GameOver {
			fmt.Println("Game over!")
			break
		}
		time.Sleep(100 * time.Millisecond)
	}
	return nil
}

func (c *Client) readLoop() {
	buffer := make([]byte, 4096)
	bufLen := 0

	for {
		n, err := c.conn.Read(buffer[bufLen:])
		if err != nil {
			return
		}
		bufLen += n
		data := buffer[:bufLen]

		offset := 0
		for offset+3 <= bufLen {
			length := int(uint16(data[offset+1])<<8 | uint16(data[offset+2]))
			if offset+3+length > bufLen {
				break
			}
			packetData := data[offset : offset+3+length]
			offset += 3 + length

			packet, err := c.packetReader.Deserialize(packetData)
			if err != nil {
				continue
			}
			c.handlePacket(packet)
		}

		if offset > 0 {
			copy(buffer, buffer[offset:bufLen])
			bufLen -= offset
		}
	}
}

func (c *Client) handlePacket(packet models.IPacket) {
	switch p := packet.(type) {
	case *network.GameStartPacket:
		c.playerID = p.PlayerId
		fmt.Printf("You are Player %d\n", c.playerID)

	case *network.GameStateChangePacket:
		c.state = p.NewState
		if c.state == models.Placement {
			c.placementDone = false
		}
		fmt.Printf("Game state changed to: %v\n", c.state)
		if c.state == models.Battle {
			fmt.Println("Battle phase started!")
		}

	case *network.TurnChangePacket:
		fmt.Printf("It's now Player %d's turn\n", p.PlayerId)

	case *network.ShotResultPacket:
		if p.Coordinate.X >= 0 && p.Coordinate.X < 10 && p.Coordinate.Y >= 0 && p.Coordinate.Y < 10 {
			switch p.Result {
			case Hit:
				c.oppBoard[p.Coordinate.Y][p.Coordinate.X] = Hit
			case Sunk:
				c.oppBoard[p.Coordinate.Y][p.Coordinate.X] = Sunk
			case Miss:
				c.oppBoard[p.Coordinate.Y][p.Coordinate.X] = Miss
			}
		}
		fmt.Printf("Shot result: %v\n", p.Result)

	case *network.PlacementCompletePacket:
		if p.PlacedShips != nil {
			for _, ship := range p.PlacedShips {
				for _, coord := range ship.OccupiedCoordinates {
					if coord.X >= 0 && coord.X < 10 && coord.Y >= 0 && coord.Y < 10 {
						c.myBoard[coord.Y][coord.X] = shipTypeChar(ship.Type)
					}
				}
			}
		}
		c.printBoards()
		fmt.Println("Ships placed!")

	case *network.GameOverPacket:
		c.state = models.GameOver
		fmt.Printf("Game Over! Winner: Player %d\n", p.WinnerId)

	case *network.ErrorPacket:
		fmt.Printf("Error: %s\n", p.Message)
	}
}

func (c *Client) doPlacement() {
	if c.placementDone {
		return
	}

	c.printBoards()
	ships := []models.ShipType{
		models.Carrier,
		models.Battleship,
		models.Destroyer,
		models.Submarine,
		models.PatrolBoat,
	}

	remaining := filterShips(ships, c.shipsPlaced)
	if len(remaining) == 0 {
		c.placementDone = true
		c.sendPlacementComplete()
		return
	}

	fmt.Printf("Remaining ships: %v\n", remaining)
	fmt.Println("Enter 'r' for random placement or coordinates (x y orientation): ")

	text, _ := c.reader.ReadString('\n')
	text = strings.TrimSpace(text)

	if text == "r" || text == "random" {
		c.randomPlacement()
		c.placementDone = true
		c.sendPlacementComplete()
		return
	}

	var x, y int
	var oriStr string
	fmt.Sscanf(text, "%d %d %s", &x, &y, &oriStr)
	orientation := models.Horizontal
	if oriStr == "v" || oriStr == "vertical" {
		orientation = models.Vertical
	}

	shipType := remaining[0]
	coord := models.Coordinate{X: x, Y: y}
	c.placeShip(shipType, coord, orientation)
	c.sendPlacementComplete()
}

func filterShips(all []models.ShipType, placed []models.ShipType) []models.ShipType {
	placedSet := make(map[models.ShipType]bool)
	for _, s := range placed {
		placedSet[s] = true
	}
	var result []models.ShipType
	for _, s := range all {
		if !placedSet[s] {
			result = append(result, s)
		}
	}
	return result
}

func (c *Client) randomPlacement() {
	r := rand.New(rand.NewSource(time.Now().UnixNano()))
	ships := []models.ShipType{
		models.Carrier,
		models.Battleship,
		models.Destroyer,
		models.Submarine,
		models.PatrolBoat,
	}

	for _, shipType := range ships {
		for attempts := 0; attempts < 100; attempts++ {
			start := models.Coordinate{X: r.Intn(10), Y: r.Intn(10)}
			orientation := models.Orientation(r.Intn(2))
			c.placeShip(shipType, start, orientation)
			break
		}
	}
	c.shipsPlaced = ships
}

func (c *Client) placeShip(shipType models.ShipType, start models.Coordinate, orientation models.Orientation) {
	packet := network.NewPlaceShipPacket(shipType, start, orientation)
	data, _ := c.packetWriter.Serialize(packet)
	c.conn.Write(data)
	c.shipsPlaced = append(c.shipsPlaced, shipType)
}

func (c *Client) sendPlacementComplete() {
	packet := network.NewPlacementCompletePacket(true, nil)
	data, _ := c.packetWriter.Serialize(packet)
	c.conn.Write(data)
}

func (c *Client) doBattle() {
	c.printBoards()
	fmt.Println("Enter coordinates to fire (x y): ")
	text, _ := c.reader.ReadString('\n')
	text = strings.TrimSpace(text)

	var x, y int
	fmt.Sscanf(text, "%d %d", &x, &y)

	packet := network.NewShotPacket(models.Coordinate{X: x, Y: y})
	data, _ := c.packetWriter.Serialize(packet)
	c.conn.Write(data)
}

func main() {
	fmt.Println("Battleship Client v1.0")
	fmt.Println("======================")

	addr := "localhost:42069"
	if len(os.Args) > 1 {
		addr = os.Args[1]
	}

	client := NewClient()
	if err := client.Connect(addr); err != nil {
		fmt.Printf("Failed to connect: %v\n", err)
		return
	}

	client.Run()
}
