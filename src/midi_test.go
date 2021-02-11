package main

import (
	"math/rand"
	"testing"
	"time"

	"github.com/stretchr/testify/assert"
)

func TestInitMessage(t *testing.T) {
	rand.Seed(time.Now().UnixNano())

	t.Run("Test without data segment", func(t *testing.T) {

		device := byte(rand.Intn(15))
		code := byte(rand.Intn(15))
		cTime := uint(rand.Intn(4096))
		cData := uint(rand.Intn(4096))

		result := initMessage(device, code, nil, cTime, cData)

		assert.Equal(t, cTime, result.ConductorTime)
		assert.Equal(t, cData, result.ConductorData)

		assert.Equal(t, 4, len(result.MessageBytes))
		assert.Equal(t, PARSEC_FLAG, result.MessageBytes[0])
		assert.Equal(t, device, result.MessageBytes[1])
		assert.Equal(t, code, result.MessageBytes[2])
		assert.Equal(t, byte(0), result.MessageBytes[3])
	})

	t.Run("Test with data segment", func(t *testing.T) {

		device := byte(rand.Intn(15))
		code := byte(rand.Intn(15))
		ctime := uint(rand.Intn(4096))
		cdata := uint(rand.Intn(4096))

		dataLength := rand.Intn(4) + 1
		var dataBytes []byte

		for i := 0; i < dataLength; i++ {
			dataBytes = append(dataBytes, byte(rand.Intn(15)))
		}

		result := initMessage(device, code, dataBytes, ctime, cdata)

		assert.Equal(t, ctime, result.ConductorTime)
		assert.Equal(t, cdata, result.ConductorData)

		assert.Equal(t, 4+dataLength, len(result.MessageBytes))
		assert.Equal(t, PARSEC_FLAG, result.MessageBytes[0])
		assert.Equal(t, device, result.MessageBytes[1])
		assert.Equal(t, code, result.MessageBytes[2])
		assert.Equal(t, byte(dataLength), result.MessageBytes[3])

		for i := 0; i < dataLength; i++ {
			assert.Equal(t, dataBytes[i], result.MessageBytes[i+4])
		}
	})
}

func TestAppendMessage(t *testing.T) {
	// Start with empty track

	bundle := ParseBundle{
		Status:         0x00,
		PairStartIndex: 0,
		IgnoredTime:    0,
		CumulativeTime: 0,
		ConductorTrack: nil,
	}

	var track Track
	// dummy messages
	message1 := initMessage(0, 0, nil, 10, 0)
	message2 := initMessage(5, 5, nil, 15, 0)

	appendMessage(message1, &track, &bundle)

	assert.NotNil(t, track)
	assert.Equal(t, 1, len(track))
	assert.Equal(t, *message1, track[0])
	assert.Equal(t, uint(10), bundle.CumulativeTime)

	appendMessage(message2, &track, &bundle)

	assert.Equal(t, 2, len(track))
	assert.Equal(t, *message2, track[1])
	assert.Equal(t, uint(25), bundle.CumulativeTime)
}
