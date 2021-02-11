package main

import (
	"time"
)

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
type MidiSequence struct {
	Tracks          []*Track
	NumTracks       uint
	ClockDivision   float64
	EventStartTimes []float64
}

/*
 * RemainingTracks - A map where keys are the indices of tracks still playing (values ignored, used as set)
 * CurrentEvents   - Event at index i is the event waiting to be played on track i
 * StartTimes      - Time at index i is the start time for the currently playing event on track i
 * UsecPerTick     - Conversion from microseconds to midi ticks
 */
type PlaybackBundle struct {
	RemainingTracks map[int]bool
	CurrentEvents   []int
	StartTimes      []int64
	UsecPerTick     float64
}

/*
	Constructor for parsec messages
*/
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

	// Account for 1 indexing
	// TODO: CHANGE TO 0 indexing
	if device != 0xFF {
		device++
	}

	// Initialize bytes
	message.MessageBytes = make([]byte, 0)
	message.MessageBytes = append(message.MessageBytes, PARSEC_FLAG)
	message.MessageBytes = append(message.MessageBytes, device)
	message.MessageBytes = append(message.MessageBytes, messageLength)
	message.MessageBytes = append(message.MessageBytes, code)

	if messageLength > 0 {
		for _, val := range data {
			message.MessageBytes = append(message.MessageBytes, val)
		}
	}

	return &message
}

// Append a message to a given track
func appendMessage(message *ParsecMessage, track *Track, bundle *ParseBundle) {
	*track = append(*track, *message)
	bundle.CumulativeTime += message.ConductorTime
}

// Append a message to the conductor track
func appendToConductor(message *ParsecMessage, bundle *ParseBundle) {
	deltaTime := message.ConductorTime
	message.ConductorTime += bundle.CumulativeTime
	*(bundle.ConductorTrack) = append(*(bundle.ConductorTrack), *message)
	bundle.CumulativeTime += deltaTime
}

// Format a message for sending to the arduino
// All this does is center the motors so it looks nice
func formatMessage(m *ParsecMessage, numTracks uint, numMotors uint) []byte {
	if m.MessageBytes[1] == 0xFF {
		return m.MessageBytes
	}

	if numMotors%2 == 1 {
		offset := byte((numMotors - numTracks) / 2)

		if numTracks%2 == 1 {
			m.MessageBytes[1] += offset
		} else {
			if uint(m.MessageBytes[1]) <= numTracks/2 {
				m.MessageBytes[1] += offset
			} else {
				m.MessageBytes[1] += offset + 1
			}
		}
	} else {
		offset := byte((numMotors - numTracks) / 2)
		if numTracks%2 == 0 {
			m.MessageBytes[1] += offset
		}
	}

	return m.MessageBytes
}

// Get current unix time in micro seconds
func getCurrentMicro() int64 {
	return time.Now().UnixNano() / MICROS_PER_NANO
}

// Playback a midi sequence
func (s MidiSequence) Play(a *Arduino) {

	remainingTracks := make(map[int]bool)
	currentEvents := make([]int, s.NumTracks)
	startTimes := make([]int64, s.NumTracks)

	past := getCurrentMicro()

	for i := 0; i < int(s.NumTracks); i++ {
		remainingTracks[i] = true
		currentEvents[i] = 0
		startTimes[i] = past - getCurrentMicro()
	}

	pbBundle := &PlaybackBundle{
		RemainingTracks: remainingTracks,
		CurrentEvents:   currentEvents,
		StartTimes:      startTimes,
		// Default Tempo
		UsecPerTick: float64(500000) / s.ClockDivision,
	}

	for len(pbBundle.RemainingTracks) > 0 {
		elapsedTime := getCurrentMicro() - past
		s.CheckEvents(elapsedTime, a, pbBundle)
	}
}

func (s MidiSequence) CheckEvents(currentTime int64, a *Arduino, pb *PlaybackBundle) {
	for currTrack, _ := range pb.RemainingTracks {
		track := *(s.Tracks[currTrack])
		trackEvent := track[pb.CurrentEvents[currTrack]]
		eventStart := pb.StartTimes[currTrack]

		// Check for end of track
		if trackEvent.MessageBytes[CODE_BYTE] == PARSEC_EOT {
			delete(pb.RemainingTracks, currTrack)
			continue
		}

		// Check if enough time has elapsed for next event
		if currentTime-eventStart >= (int64(pb.UsecPerTick) * int64(trackEvent.ConductorTime)) {

			// If the event is a conductor event
			if trackEvent.MessageBytes[CODE_BYTE]&0xF0 == 0xC0 {
				if trackEvent.MessageBytes[CODE_BYTE] == PARSEC_TEMPO {
					//update tempo
					pb.UsecPerTick = float64(trackEvent.ConductorData) / s.ClockDivision
				}
			} else {
				// For every other track
				a.SendMessage(&trackEvent, s.NumTracks)
			}

			// Update events and times
			pb.CurrentEvents[currTrack]++
			pb.StartTimes[currTrack] = currentTime
		}
	}
}
