package main

import (
	"math"
	"math/rand"
	"testing"
	"time"

	"github.com/stretchr/testify/assert"
)

func TestParseEvent(t *testing.T) {
	rand.Seed(time.Now().UnixNano())

	t.Run("Test Sysex event with F0", func(t *testing.T) {
		bytes := []byte{0x0A, 0xF0, 0x03, 0xC0, 0xFF, 0xEE}
		bundle := ParseBundle{
			Status:         0xFF,
			PairStartIndex: 0,
			IgnoredTime:    0,
			CumulativeTime: 0,
			ConductorTrack: nil,
		}

		message := parseEvent(bytes, 0, 0, &bundle)

		assert.Equal(t, uint8(0), bundle.Status)
		assert.Equal(t, false, bundle.IsRunningStatus)
		assert.Equal(t, uint(0x0A), bundle.IgnoredTime)
		assert.Equal(t, uint(6), bundle.PairStartIndex)
		assert.Nil(t, message)
	})

	t.Run("Test Sysex event with F7", func(t *testing.T) {
		bytes := []byte{0x0A, 0xF7, 0x03, 0xC0, 0xFF, 0xEE}
		bundle := ParseBundle{
			Status:         0xFF,
			PairStartIndex: 0,
			IgnoredTime:    0,
			CumulativeTime: 0,
			ConductorTrack: nil,
		}

		message := parseEvent(bytes, 0, 0, &bundle)

		assert.Equal(t, uint8(0), bundle.Status)
		assert.Equal(t, false, bundle.IsRunningStatus)
		assert.Equal(t, uint(0x0A), bundle.IgnoredTime)
		assert.Equal(t, uint(6), bundle.PairStartIndex)
		assert.Nil(t, message)
	})

	t.Run("Test EOT Meta event", func(t *testing.T) {
		bytes := []byte{0x0A, 0xFF, 0x2F, 0x00}
		bundle := ParseBundle{
			Status:         0xFF,
			PairStartIndex: 0,
			IgnoredTime:    0,
			CumulativeTime: 0,
			ConductorTrack: nil,
		}

		message := parseEvent(bytes, 0, 3, &bundle)

		assert.Equal(t, uint8(0), bundle.Status)
		assert.Equal(t, false, bundle.IsRunningStatus)
		assert.Equal(t, uint(0), bundle.IgnoredTime)
		assert.Equal(t, uint(len(bytes)), bundle.PairStartIndex)
		assert.NotNil(t, message)
		assert.Equal(t, uint(0), message.ConductorTime)
		assert.Equal(t, uint(0), message.ConductorData)
		assert.Equal(t, uint8(PARSEC_FLAG), message.MessageBytes[0])
		assert.Equal(t, uint8(3), message.MessageBytes[1])
		assert.Equal(t, uint8(PARSEC_EOT), message.MessageBytes[2])
		assert.Equal(t, uint8(0), message.MessageBytes[3])

	})

	t.Run("Test Tempo Meta event", func(t *testing.T) {
		bytes := []byte{0x0A, 0xFF, 0x51, 0x03, 0xC0, 0xFF, 0xEE}
		var conductorTrack Track
		bundle := ParseBundle{
			Status:         0xFF,
			PairStartIndex: 0,
			IgnoredTime:    0,
			CumulativeTime: 0xF0,
			ConductorTrack: &conductorTrack,
		}

		message := parseEvent(bytes, 0, 3, &bundle)

		assert.Equal(t, uint8(0), bundle.Status)
		assert.Equal(t, false, bundle.IsRunningStatus)
		assert.Equal(t, uint(len(bytes)), bundle.PairStartIndex)
		assert.Equal(t, uint(0x0A), bundle.IgnoredTime)
		assert.Equal(t, uint(0xFA), bundle.CumulativeTime)
		assert.NotNil(t, bundle.ConductorTrack)

		assert.Nil(t, message)
		assert.Equal(t, 1, len(*(bundle.ConductorTrack)))

		cMessage := (*(bundle.ConductorTrack))[0]

		assert.Equal(t, uint(0xFA), cMessage.ConductorTime)
		assert.Equal(t, uint(0xC0FFEE), cMessage.ConductorData)
		assert.Equal(t, uint8(PARSEC_FLAG), cMessage.MessageBytes[0])
		assert.Equal(t, uint8(0), cMessage.MessageBytes[1])
		assert.Equal(t, uint8(PARSEC_TEMPO), cMessage.MessageBytes[2])
		assert.Equal(t, uint8(0), cMessage.MessageBytes[3])

	})

	t.Run("Test arbitrary meta event", func(t *testing.T) {
		bytes := []byte{0x0A, 0xFF}

		// Append fake meta type that is not tempo or eot
		metaType := byte(rand.Intn(int(META_TEMPO-META_EOT))) + META_EOT + 1
		bytes = append(bytes, metaType)

		// Fake event will be 2 bytes long
		bytes = append(bytes, byte(0x02))
		bytes = append(bytes, byte(rand.Intn(255)))
		bytes = append(bytes, byte(rand.Intn(255)))

		bundle := ParseBundle{
			Status:         0xFF,
			PairStartIndex: 0,
			IgnoredTime:    0,
			CumulativeTime: 0,
			ConductorTrack: nil,
		}

		message := parseEvent(bytes, 0, 3, &bundle)

		assert.Equal(t, byte(0), bundle.Status)
		assert.Equal(t, false, bundle.IsRunningStatus)
		assert.Equal(t, uint(len(bytes)), bundle.PairStartIndex)
		assert.Equal(t, uint(0x0A), bundle.IgnoredTime)
		assert.Equal(t, uint(0), bundle.CumulativeTime)
		assert.Nil(t, bundle.ConductorTrack)

		assert.Nil(t, message)
	})

	t.Run("Test midi note off", func(t *testing.T) {
		bytes := []byte{0x0A, 0x81, 0x3C, 0x00}

		bundle := ParseBundle{
			Status:         0xFF,
			PairStartIndex: 0,
			IgnoredTime:    0,
			CumulativeTime: 0,
			ConductorTrack: nil,
		}

		message1 := parseEvent(bytes, 0, 3, &bundle)

		assert.Equal(t, MIDI_NOTE_OFF, bundle.Status)
		assert.Equal(t, true, bundle.IsRunningStatus)
		assert.Equal(t, uint(len(bytes)), bundle.PairStartIndex)
		assert.Equal(t, uint(0), bundle.IgnoredTime)
		assert.Equal(t, uint(0), bundle.CumulativeTime)
		assert.Nil(t, bundle.ConductorTrack)

		assert.NotNil(t, message1)
		assert.Equal(t, PARSEC_FLAG, message1.MessageBytes[0])
		assert.Equal(t, byte(3), message1.MessageBytes[1])
		assert.Equal(t, PARSEC_NOTE_OFF, message1.MessageBytes[2])
		assert.Equal(t, byte(0), message1.MessageBytes[3])
		assert.Equal(t, uint(0x0A), message1.ConductorTime)
		assert.Equal(t, uint(0x00), message1.ConductorData)

		bytes = []byte{0x0A, 0x3C, 0x00}

		// test running status
		message2 := parseEvent(bytes, 0, 3, &bundle)

		assert.Equal(t, MIDI_NOTE_OFF, bundle.Status)
		assert.Equal(t, true, bundle.IsRunningStatus)
		assert.Equal(t, uint(len(bytes)), bundle.PairStartIndex)
		assert.Equal(t, uint(0), bundle.IgnoredTime)
		assert.Equal(t, uint(0), bundle.CumulativeTime)
		assert.Nil(t, bundle.ConductorTrack)

		assert.NotNil(t, message2)
		assert.Equal(t, PARSEC_FLAG, message2.MessageBytes[0])
		assert.Equal(t, byte(3), message2.MessageBytes[1])
		assert.Equal(t, PARSEC_NOTE_OFF, message2.MessageBytes[2])
		assert.Equal(t, byte(0), message2.MessageBytes[3])
		assert.Equal(t, uint(0x0A), message2.ConductorTime)
		assert.Equal(t, uint(0x00), message2.ConductorData)

	})
}

func TestParseUint(t *testing.T) {

	rand.Seed(time.Now().UnixNano())

	t.Run("Test panic case", func(t *testing.T) {
		defer func() {
			if r := recover(); r == nil {
				assert.Fail(t, "Did not panic")
			}
		}()
		parseUint(nil, 1, 0)
	})

	t.Run("Test on one byte random number", func(t *testing.T) {
		randNum := byte(rand.Intn(255))

		var bytes []byte
		bytes = append(bytes, randNum)

		result := parseUint(bytes, 0, 0)

		assert.Equal(t, uint(randNum), result)
	})

	t.Run("Test on multi byte random number", func(t *testing.T) {
		randNum := uint(rand.Intn(int(math.Pow(256, 4)) + 256))

		var bytes []byte

		for copy := randNum; copy > 0; {
			bytes = append([]byte{byte(copy & 0xFF)}, bytes...)
			copy = copy >> 8
		}

		result := parseUint(bytes, 0, uint(len(bytes)-1))

		assert.Equal(t, randNum, result)
	})
}

func TestParseVarival(t *testing.T) {

	rand.Seed(time.Now().UnixNano())
	// TEST ZERO
	t.Run("Test zero Varival", func(t *testing.T) {
		zeroBytes, zeroValue := parseVarival([]byte{0x00}, 0)

		assert.Equal(t, uint(1), zeroBytes)
		assert.Equal(t, uint(0), zeroValue)
	})
	// TEST 3 different lengths

	// Test single byte value
	t.Run("Test a single byte Varival", func(t *testing.T) {
		randNum := rand.Intn(15) + 1
		randBytes, randValue := parseVarival([]byte{uint8(randNum)}, 0)

		assert.Equal(t, uint(1), randBytes)
		assert.Equal(t, uint(randNum), randValue)
	})

	//3 byte test
	t.Run("Test a three byte Varival", func(t *testing.T) {
		longBytes, longValue := parseVarival([]byte{0x00, 0x01, 0xA5, 0x87, 0x7F, 0x00, 0x02}, 2)

		assert.Equal(t, uint(3), longBytes)
		assert.Equal(t, uint(0x943FF), longValue)
	})

	// Max value test
	t.Run("Test max Varival", func(t *testing.T) {
		maxBytes, maxValue := parseVarival([]byte{0xFF, 0xFF, 0xFF, 0x7F}, 0)

		assert.Equal(t, uint(4), maxBytes)
		assert.Equal(t, uint(0xFFFFFFF), maxValue)
	})
}
