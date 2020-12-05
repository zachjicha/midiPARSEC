package main

import "fmt"

/*
 * Status          - Contains current status for running status
 * IsRunningStatus - True if running status is in effect
 * IgnoredTime     - Time from events since last non-ignored event that were ignored
 * CumulativeTime  - Total time so far in the track, used for conductor events
 */
type ParseBundle struct {
	Status          byte
	IsRunningStatus bool
	PairStartIndex  uint
	IgnoredTime     uint
	CumulativeTime  uint
	ConductorTrack  *Track
}

type midiParseFunction (func([]byte, uint, byte, uint, *ParseBundle) *ParsecMessage)

var midiParseMap = map[byte]midiParseFunction{
	MIDI_NOTE_OFF:    parseMidiNoteOff,
	MIDI_NOTE_ON:     parseMidiNoteOn,
	MIDI_MODE_FLAG:   parseIgnoredDouble,
	MIDI_POLY_PRES:   parseIgnoredTriple,
	MIDI_PROG_CHANGE: parseIgnoredDouble,
	MIDI_KEY_PRES:    parseIgnoredDouble,
	MIDI_PITCH_BEND:  parseIgnoredTriple,
}

func parseEvent(bytes []byte, start uint, device byte, bundle *ParseBundle) *ParsecMessage {

	var message *ParsecMessage

	numBytes, value := parseVarival(bytes, start)
	conductorTime := uint(value) + bundle.IgnoredTime

	eventStartIndex := start + uint(numBytes)

	switch bytes[eventStartIndex] {
	case TYPE_META:
		message = parseMetaEvent(bytes, eventStartIndex, device, conductorTime, bundle)
	case TYPE_SYSEX_ONE, TYPE_SYSEX_TWO:
		message = parseSysexEvent(bytes, eventStartIndex, conductorTime, bundle)
	default:
		message = parseMidiEvent(bytes, eventStartIndex, device, conductorTime, bundle)
	}

	return message
}

/*
 * Parses an event given it is a meta event. Will return nil if the event is to be ignored
 * or otherwise a message with all fields but device byte set correctly
 */
func parseMetaEvent(bytes []byte, start uint, device byte, conductorTime uint, bundle *ParseBundle) *ParsecMessage {

	// Event to be returned
	var eventCode byte
	var conductorData uint

	// End running status
	bundle.Status = 0x00
	bundle.IsRunningStatus = false

	switch bytes[start+1] {
	case META_EOT:
		eventCode = PARSEC_EOT
		// EOT events are 3 bytes long
		bundle.PairStartIndex = 3 + start
		bundle.IgnoredTime = 0

		return initMessage(device, eventCode, nil, 0, 0)
	case META_TEMPO:
		eventCode = PARSEC_TEMPO
		conductorData = parseUint(bytes, start+3, start+5)

		conductorMessage := initMessage(0, eventCode, nil, conductorTime, conductorData)

		// Enqueue in conductor track
		appendToConductor(conductorMessage, bundle)

		// Update bundle
		bundle.IgnoredTime = conductorTime
		bundle.PairStartIndex = 6 + start

		return nil
	default:
		// varival starts 2 bytes after meta flag
		length, value := parseVarival(bytes, start+2)

		// Start + length of meta event + length of varival + 2 (meta flag and meta type)
		bundle.PairStartIndex = start + value + length + 2
		bundle.IgnoredTime = conductorTime
		return nil
	}
}

func parseSysexEvent(bytes []byte, start uint, conductorTime uint, bundle *ParseBundle) *ParsecMessage {

	// End running status
	bundle.Status = 0x00
	bundle.IsRunningStatus = false

	length, value := parseVarival(bytes, start+1)

	// Start of the event plus the sysex byte + length of event + length of varival
	bundle.PairStartIndex = start + 1 + value + length

	bundle.IgnoredTime = conductorTime
	return nil
}

func parseMidiEvent(bytes []byte, start uint, device byte, conductorTime uint, bundle *ParseBundle) *ParsecMessage {

	// Check if running status has ended
	if bytes[start]&0x80 == 0x80 {
		bundle.Status = bytes[start] & 0xF0
		bundle.IsRunningStatus = false
	} else if !bundle.IsRunningStatus {
		panic("No status byte given while not in running status")
	}

	parseFunc, ok := midiParseMap[bundle.Status]

	if !ok {
		panic(fmt.Sprintf("Unknown status byte (%X) in midi event", bundle.Status))
	}

	message := parseFunc(bytes, start, device, conductorTime, bundle)

	// Assume running status always starts
	bundle.IsRunningStatus = true

	return message
}

func parseMidiNoteOff(bytes []byte, start uint, device byte, conductorTime uint, bundle *ParseBundle) *ParsecMessage {
	bundle.PairStartIndex = start + 2 + runningStatusLength(bundle.IsRunningStatus)
	bundle.IgnoredTime = 0

	return initMessage(device, PARSEC_NOTE_OFF, nil, conductorTime, 0)
}

func parseMidiNoteOn(bytes []byte, start uint, device byte, conductorTime uint, bundle *ParseBundle) *ParsecMessage {

	velocity := bytes[start+2+runningStatusLength(bundle.IsRunningStatus)]
	var eventCode byte
	var eventData []byte = nil

	// Check if note on or off
	if velocity == 0 {
		eventCode = PARSEC_NOTE_OFF
	} else {
		eventCode = PARSEC_NOTE_ON

		pitchIndex := start + runningStatusLength(bundle.IsRunningStatus)
		eventData = []byte{bytes[pitchIndex]}
	}

	bundle.PairStartIndex = start + 2 + runningStatusLength(bundle.IsRunningStatus)
	bundle.IgnoredTime = 0

	return initMessage(device, eventCode, eventData, conductorTime, 0)
}

func parseIgnoredDouble(bytes []byte, start uint, device byte, conductorTime uint, bundle *ParseBundle) *ParsecMessage {
	bundle.PairStartIndex = 1 + start + runningStatusLength(bundle.IsRunningStatus)
	bundle.IgnoredTime = conductorTime
	return nil
}

func parseIgnoredTriple(bytes []byte, start uint, device byte, conductorTime uint, bundle *ParseBundle) *ParsecMessage {
	bundle.PairStartIndex = 2 + start + runningStatusLength(bundle.IsRunningStatus)
	bundle.IgnoredTime = conductorTime
	return nil
}

func runningStatusLength(isRunningStatus bool) uint {
	if isRunningStatus {
		return 0
	} else {
		return 1
	}
}

func parseUint(bytes []byte, start uint, end uint) (value uint) {
	if start > end {
		panic("End index less than start index")
	}

	for i := start; i <= end; i++ {
		value = (value << 8) + uint(bytes[i])
	}
	return
}

func parseVarival(bytes []byte, start uint) (numBytes uint, value uint) {

	for ; bytes[start]&0x80 == 0x80; start, numBytes = start+1, numBytes+1 {
		value = (value << 7) + uint(bytes[start]&0x7F)
	}

	numBytes++
	value = (value << 7) + uint(bytes[start]&0x7F)

	return numBytes, value
}
