package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"os/signal"
	"syscall"
)

func main() {
	// Need all arguments to run
	if len(os.Args) != 3 {
		panic("Arguments: <midi file> <comm port>")
	}

	// Extract arguments
	midiFile := os.Args[1]
	port := os.Args[2]

	// Parse midi file and open serial port to arduino
	sequence := parseSequence(midiFile)
	arduino := openPort(port)

	// Make some important messages
	seqBegin := initMessage(PARSEC_BROADCAST, PARSEC_SEQ_BEGIN, nil, 0, 0)
	seqEnd := initMessage(PARSEC_BROADCAST, PARSEC_SEQ_END, nil, 0, 0)
	idle := initMessage(PARSEC_BROADCAST, PARSEC_IDLE, nil, 0, 0)
	standby := initMessage(PARSEC_BROADCAST, PARSEC_STANDBY, nil, 0, 0)

	// Make a handler for ctrl C
	interruptChannel := make(chan os.Signal, 1)
	signal.Notify(interruptChannel, os.Interrupt, syscall.SIGINT)
	go func() {
		<-interruptChannel

		// Clean up
		arduino.SendMessage(seqEnd, sequence.NumTracks)
		arduino.ClosePort()

		os.Exit(1)
	}()

	// Make motors idle
	arduino.SendMessage(idle, sequence.NumTracks)

	// Wait for user to continue
	fmt.Println("Press enter to play")
	if _, err := bufio.NewReader(os.Stdin).ReadBytes('\n'); err != nil {
		// This error is fatal since we cannot play without keypress
		log.Fatal("Failed to record key press on Enter")
	}

	// Motors standby and begin sequence playback
	arduino.SendMessage(standby, sequence.NumTracks)
	arduino.SendMessage(seqBegin, sequence.NumTracks)

	// Play the sequence
	sequence.Play(arduino)

	// End the sequence and close the port
	arduino.SendMessage(seqEnd, sequence.NumTracks)
	arduino.ClosePort()
}
