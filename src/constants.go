package main

// Event Type bytes
const (
	TYPE_META      byte = 0xFF
	TYPE_SYSEX_ONE byte = 0xF0
	TYPE_SYSEX_TWO byte = 0xF7
)

// Meta status bytes
const (
	META_EOT   byte = 0x2F
	META_TEMPO byte = 0x51
)

// Midi Channel Messages
// There are Mode and Voice messages
// All mode messages are ignored and start with 0xB
const (
	MIDI_MODE_FLAG   byte = 0xB0
	MIDI_NOTE_OFF    byte = 0x80
	MIDI_NOTE_ON     byte = 0x90
	MIDI_POLY_PRES   byte = 0xA0
	MIDI_PROG_CHANGE byte = 0xC0
	MIDI_KEY_PRES    byte = 0xD0
	MIDI_PITCH_BEND  byte = 0xE0
)

// Values from the PARSEC message protocol)
const (
	PARSEC_FLAG     byte = 0xAE
	PARSEC_EOT      byte = 0x00
	PARSEC_TEMPO    byte = 0x00
	PARSEC_NOTE_OFF byte = 0x00
	PARSEC_NOTE_ON  byte = 0x00
)

// Warmup constants
const WARMUP_LENGTH uint = 5750
