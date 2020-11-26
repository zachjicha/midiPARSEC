package main

// Event Type bytes
const TYPE_META byte = 0xFF
const TYPE_SYSEX_ONE byte = 0xF0
const TYPE_SYSEX_TWO byte = 0xF7

// Meta status bytes
const META_EOT byte = 0x2F
const META_TEMPO byte = 0x51

// Midi Channel Messages
// There are Mode and Voice messages
// All mode messages are ignored and start with 0xB
const MIDI_MODE_FLAG byte = 0xB0
const MIDI_NOTE_OFF byte = 0x80
const MIDI_NOTE_ON byte = 0x90
const MIDI_POLY_PRES byte = 0xA0
const MIDI_PROG_CHANGE byte = 0xC0
const MIDI_KEY_PRES byte = 0xD0
const MIDI_PITCH_BEND byte = 0xE0

// Values from the PARSEC message protocol)
const PARSEC_EOT byte = 0x00
