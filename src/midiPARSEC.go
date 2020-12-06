package main

import (
	"os"
)

func main() {
	if len(os.Args) != 3 {
		panic("Wrong arguments")
	}

	midiFile := os.Args[1]
	port := os.Args[2]

	sequence := parseSequence(midiFile)

	arduino := openPort(port)

	sbegin := initMessage(PARSEC_BROADCAST, PARSEC_SEQ_BEGIN, nil, 0, 0)

	arduino.SendMessage(sbegin)
}
