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
	ConductorTrack *Track
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
		//message = parseMidiEvent()
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

		conductorMessage := initMessage(0, eventCode, nil, bundle.CumulativeTime+conductorTime-WARMUP_LENGTH, conductorData)

		// Enqueue in conductor track
		appendMessage(bundle.ConductorTrack, conductorMessage)

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

	length, value := parseVarival(bytes, start+1)

	// Start of the event plus the sysex byte + length of event + length of varival
	bundle.PairStartIndex = start + 1 + value + length

	bundle.IgnoredTime = conductorTime
	return nil
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
