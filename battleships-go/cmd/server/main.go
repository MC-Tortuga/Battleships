package main

import (
	"battleships-go/internal/server"
	"context"
	"fmt"
	"log"
	"net"
	"os"
	"os/signal"
	"syscall"
)

func main() {
	fmt.Println("Battleship Server v1.0")
	fmt.Println("=====================")

	const port = ":42069"

	matchmaker := server.NewMatchMaker()

	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	listener, err := net.Listen("tcp", port)
	if err != nil {
		log.Fatalf("Server error: %v", err)
	}

	go func() {
		<-ctx.Done()
		fmt.Println("\nShutting down server...")
		listener.Close()

	}()

	fmt.Printf("Server Started on port %s. Waiting for clients...\n", port)

	for {
		conn, err := listener.Accept()
		if err != nil {
			select {
			case <-ctx.Done():
				fmt.Println("Server stopped.")
				return
			default:
				fmt.Printf("Error accepting client: %v\n", err)
				continue

			}
		}

		fmt.Printf("New client connected from %s\n", conn.RemoteAddr())

		matchmaker.AddClient(conn)
	}

}
