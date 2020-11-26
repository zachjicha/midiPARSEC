package main

/*
 * Status         - 0 indicates no running status, otherwise conatins the current running status
 * IgnoredTime    - Time from events since last non-ignored event that were ignored
 * CumulativeTime - Total time so far in the track, used for conductor events
 */
type ParseBundle struct {
	Status         byte
	PairStartIndex uint
	IgnoredTime    uint
	CumulativeTime uint
}

func parseEvent(bytes []byte, start uint, bundle *ParseBundle) (message *ParsecMessage) {

	numBytes, value := readVarival(bytes, start)
	conductorTime = uint(value) + bundle.IgnoredTime

	eventStartIndex := start + uint(numBytes)

	switch bytes[eventStartIndex] {
	case TYPE_META:
		message = parseMetaEvent(bytes, eventStartIndex, bundle)
	case TYPE_SYSEX_ONE, TYPE_SYSEX_TWO:
		message = parseSysexEvent()
	default:
		message = parseMidiEvent()
	}
	return
}

func parseMetaEvent(bytes []byte, start uint, bundle *ParseBundle) *ParsecMessage {

	// Event to be returned
	var eventDevice byte
	var eventCode byte
	var conductorTime uint
	var conductorData uint

	switch bytes[start+1] {
	case META_EOT:
		eventCode = PARSEC_EOT
		// EOT events are 3 bytes long
		bundle.PairStartIndex = 3 + start
		bundle.IgnoredTime = 0

	case META_TEMPO:
		eventCode = PARSEC_EOT
		conductorData = parseUint(bytes, start+3, start+5)
		bundle.PairStartIndex = 6 + start

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
