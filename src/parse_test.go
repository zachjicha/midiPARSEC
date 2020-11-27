package main

import (
	"math"
	"math/rand"
	"testing"
	"time"

	"github.com/stretchr/testify/assert"
)

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
