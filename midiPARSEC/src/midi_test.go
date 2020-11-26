package main

import (
	"math/rand"
	"testing"
	"time"

	"github.com/stretchr/testify/assert"
)

func printFailure(t *testing.T, unit string, expected interface{}, got interface{}) {
	t.Errorf("%v failed: Expected %v but got %v", unit, expected, got)
}

func TestReadVarival(t *testing.T) {

	rand.Seed(time.Now().UnixNano())
	// TEST ZERO
	t.Run("Test zero Varival", func(t *testing.T) {
		zeroBytes, zeroValue := readVarival([]byte{0x00}, 0)

		assert.Equal(t, uint8(1), zeroBytes)
		assert.Equal(t, uint32(0), zeroValue)
	})
	// TEST 3 different lengths

	// Test single byte value
	t.Run("Test a single byte Varival", func(t *testing.T) {
		randNum := rand.Intn(15) + 1
		randBytes, randValue := readVarival([]byte{uint8(randNum)}, 0)

		assert.Equal(t, uint8(1), randBytes)
		assert.Equal(t, uint32(randNum), randValue)
	})

	//3 byte test
	t.Run("Test a three byte Varival", func(t *testing.T) {
		longBytes, longValue := readVarival([]byte{0x00, 0x01, 0xA5, 0x87, 0x7F, 0x00, 0x02}, 2)

		assert.Equal(t, uint8(3), longBytes)
		assert.Equal(t, uint32(0x943FF), longValue)
	})

	// Max value test
	t.Run("Test max Varival", func(t *testing.T) {
		maxBytes, maxValue := readVarival([]byte{0xFF, 0xFF, 0xFF, 0x7F}, 0)

		assert.Equal(t, uint8(4), maxBytes)
		assert.Equal(t, uint32(0xFFFFFFF), maxValue)
	})
}
