package main

import (
	"fmt"
	"os"
)

func main() {
	if len(os.Args) != 3 {
		panic("Wrong arguments")
	}

	midiFile := os.Args[1]

	sequence := parseSequence(midiFile)
	fmt.Printf("%v\n", sequence)
}
