package main

import (
	"os"
	"time"
)

func main() {
	if len(os.Args) != 3 {
		panic("Arguments: <midi file> <comm port>")
	}

	//midiFile := os.Args[1]
	port := os.Args[2]

	//sequence := parseSequence(midiFile)

	arduino := openPort(port)

	// Make some important messages
	seqBegin := initMessage(PARSEC_BROADCAST, PARSEC_SEQ_BEGIN, nil, 0, 0)
	seqEnd := initMessage(PARSEC_BROADCAST, PARSEC_SEQ_END, nil, 0, 0)
	idle := initMessage(PARSEC_BROADCAST, PARSEC_IDLE, nil, 0, 0)
	standby := initMessage(PARSEC_BROADCAST, PARSEC_STANDBY, nil, 0, 0)

	arduino.SendMessage(idle)

	time.Sleep(time.Second)

	arduino.SendMessage(standby)

	arduino.SendMessage(seqBegin)
	time.Sleep(time.Second)
	arduino.SendMessage(seqEnd)
	arduino.ClosePort()
}
