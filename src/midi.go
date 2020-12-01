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
 * A track is simply a list of messages
 */
type Track []ParsecMessage

/*
 * tracks          - Stores tracks in the sequence, first is always conductor
 * remainingTracks - Number of tracks still playing music
 * clockDivision   - Midi clock division
 * usecPerTick     - Microseconds per Midi Tick (clock division)
 * eventStartTimes - Tracks the start time of the events currently being executed
 */
type Sequence struct {
	Tracks          []Track
	RemainingTracks uint8
	ClockDivision   float64
	UsecPerTick     float64
	EventStartTimes []float64
}

func initMessage(device byte, code byte, data []byte, conductorTime uint, conductorData uint) *ParsecMessage {

	var message ParsecMessage

	message.ConductorData = conductorData
	message.ConductorTime = conductorTime

	var messageLength byte
	if data == nil {
		messageLength = 0
	} else {
		messageLength = byte(len(data))
	}

	// Initialize bytes
	message.MessageBytes = make([]byte, 0)
	message.MessageBytes = append(message.MessageBytes, PARSEC_FLAG)
	message.MessageBytes = append(message.MessageBytes, device)
	message.MessageBytes = append(message.MessageBytes, code)
	message.MessageBytes = append(message.MessageBytes, messageLength)

	if messageLength > 0 {
		for _, val := range data {
			message.MessageBytes = append(message.MessageBytes, val)
		}
	}

	return &message
}

func appendMessage(track *Track, message *ParsecMessage) {
	*track = append(*track, *message)
}
