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
	PARSEC_NULL     byte = 0x00
)

// Warmup constants
const (
	WARMUP_NOTE       byte = 0x3C
	WARMUP_TIME_ONE   uint = 0
	WARMUP_TIME_TWO   uint = 0
	WARMUP_TIME_THREE uint = 750
	WARMUP_TIME_FOUR  uint = 5000
	WARMUP_LENGTH     uint = WARMUP_TIME_ONE + WARMUP_TIME_TWO + WARMUP_TIME_THREE + WARMUP_TIME_FOUR
)

// Constants of positions in midi file
const (
	FILE_TRACKS_START      uint = 10
	FILE_TRACKS_END        uint = 11
	FILE_CLOCK_START       uint = 12
	FILE_CLOCK_END         uint = 13
	FILE_FIRST_TRACK_START uint = 14
)
