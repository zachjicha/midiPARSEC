package main

import (
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

	time.Sleep(time.Second)

	var buf = make([]byte, 2)
	totalBytes := 0

	for totalBytes < 2 {
		if n, err := serialPort.Read(buf); err != nil && buf[0] == PARSEC_RESPONSE {
			totalBytes += n
		}
	}

	return &Arduino{
		Port:   &serialPort,
		Motors: uint(buf[1]),
	}
}

func (a *Arduino) SendMessage(m *ParsecMessage) {
	(*a.Port).Write(formatMessage(m, a.Tracks, a.Motors))
}
