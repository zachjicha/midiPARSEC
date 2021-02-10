package main

import (
	"fmt"
	"io"
	"time"

	"github.com/huin/goserial"
)

type Arduino struct {
	Port   *io.ReadWriteCloser
	Motors uint
	Tracks uint
}

func openPort(name string) *Arduino {
	config := goserial.Config{
		Name: name,
		Baud: 9600,
	}

	serialPort, err := goserial.OpenPort(&config)

	if err != nil {
		panic("Error opening port")
	}

	fmt.Println("Waiting for arduino to restart...")
	time.Sleep(time.Second)

	serialPort.Write([]byte{0x90})

	var buf = make([]byte, 2)
	bytesRead := 0

	for bytesRead < 2 {
		if n, err := serialPort.Read(buf); err != nil {
			panic("Error receiving data")
		} else {
			bytesRead += n
		}
	}

	var numMotors byte
	if buf[0] != 0x26 {
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

func (a *Arduino) ClosePort() {
	(*a.Port).Close()
}

func (a *Arduino) SendMessage(m *ParsecMessage) {
	(*a.Port).Write(formatMessage(m, a.Tracks, a.Motors))
}
