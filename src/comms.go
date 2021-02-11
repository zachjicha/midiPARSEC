package main

import (
	"fmt"
	"io"
	"log"
	"time"

	"github.com/huin/goserial"
)

type Arduino struct {
	Port   *io.ReadWriteCloser
	Motors uint
}

/*
 * Opens the serial port to an arduino and returns a corresponding arduino struct
 * name: name of the serial port
 */
func openPort(name string) *Arduino {
	config := goserial.Config{
		Name: name,
		Baud: 9600,
	}

	serialPort, err := goserial.OpenPort(&config)

	if err != nil {
		panic("Error opening port")
	}

	// Arduino restarts when you connect to it, we wait a second so it is ready to accept messages
	fmt.Println("Waiting for arduino to restart...")
	time.Sleep(time.Second)

	// Write query byte to arduino to start handshake
	if _, err := serialPort.Write([]byte{PARSEC_QUERY}); err != nil {
		//Fatal error if writes fail
		log.Fatal(err)
	}

	// We will (usually) receive two bytes from arduino in response
	var buf = make([]byte, 2)
	bytesRead := 0

	// Read two bytes
	for bytesRead < 2 {
		if n, err := serialPort.Read(buf); err != nil {
			panic("Error receiving data")
		} else {
			bytesRead += n
		}
	}

	var numMotors byte
	// Sometimes the response byte is lost, this accounts for that since it doesn't carry important info
	if buf[0] != PARSEC_RESPONSE {
		numMotors = buf[0]
	} else {
		numMotors = buf[1]
	}

	fmt.Printf("Connection established to arduino with %d motors\n", numMotors)

	return &Arduino{
		Port:   &serialPort,
		Motors: uint(numMotors),
	}
}

// Close arduino's port
func (a *Arduino) ClosePort() {
	if err := (*a.Port).Close(); err != nil {
		// non-Fatal error, just it
		log.Println(err)
	}
}

// Send a parsec message to the arduino
func (a *Arduino) SendMessage(m *ParsecMessage, tracks uint) {
	// Subtract one to ignore conductor track
	if _, err := (*a.Port).Write(formatMessage(m, tracks-1, a.Motors)); err != nil {
		//Fatal error if writes fail
		log.Fatal(err)
	}
}
