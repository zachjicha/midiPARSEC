package main

/*
 * MessageBytes  - Stores bytes to be sent over serial to arduino
 * ConductorTime - Stores time from beginning of preceding message
 * ConductorData - Stores any info the conductor will need, such as tempo change
 */
type ParsecMessage struct {
	MessageBytes  []byte
	ConductorTime uint
	ConductorData uint
}

/*
 * events         - Stores events in the track
 * cumulativeTime - Stores the total delta time of all events in the track
 */
type Track struct {
	messages       []ParsecMessage
	cumulativeTime uint64
}

/*
 * tracks          - Stores tarcks in the sequence
 * remainingTracks - Number of tracks still playing music
 * clockDivision   - Midi clock division
 * usecPerTick     - Microseconds per Midi Tick (clock division)
 * eventStartTimes - Tracks the start time of the events currently being executed
 */
type Sequence struct {
	tracks          []Track
	remainingTracks uint8
	clockDivision   float64
	usecPerTick     float64
	eventStartTimes []float64
}

/*
 * numBytes - Number of bytes used to store the variable length value
 * value    - Numerical value of the variable length value
 */
type Varival struct {
	numBytes uint8
	value    uint32
}

func readVarival(bytes []byte, start uint32) *Varival {
	var numBytes uint8 = 0
	var value uint32 = 0

	for ; bytes[start]&0x80 == 0x80; start, numBytes = start+1, numBytes+1 {
		value = (value << 7) + uint32(bytes[start]&0x7F)
	}

	numBytes++
	value = (value << 7) + uint32(bytes[start]&0x7F)

	return &Varival{
		numBytes: numBytes,
		value:    value,
	}

}
