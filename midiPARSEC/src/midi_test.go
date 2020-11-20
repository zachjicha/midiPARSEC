package main

import (
	"math/rand"
	"testing"
)

func printFailure(t *testing.T, unit string, expected interface{}, got interface{}) {
	t.Errorf("%v failed: Expected %v but got %v", unit, expected, got)
}

func TestReadVarival(t *testing.T) {

	unit := "readVarival()"

	// TEST ZERO
	t.Run("Test zero Varival ", func(t *testing.T) {
		zeroResult := readVarival([]byte{0x00}, 0)
		zeroTrue := Varival{
			numBytes: 1,
			value:    0,
		}

		if *zeroResult != zeroTrue {
			printFailure(t, unit, zeroTrue, *zeroResult)
		}
	})

	// TEST 3 different lengths

	// Test single byte value
	t.Run("Test a single byte Varival ", func(t *testing.T) {

		randNum := rand.Intn(15) + 1
		randResult := readVarival([]byte{uint8(randNum)}, 0)
		randTrue := Varival{
			numBytes: 1,
			value:    uint32(randNum),
		}

		if *randResult != randTrue {
			printFailure(t, unit, randTrue, *randResult)
		}
	})

	//3 byte test
	t.Run("Test a thrre byte Varival ", func(t *testing.T) {
		longResult := readVarival([]byte{0x00, 0x01, 0xA5, 0x87, 0x7F, 0x00, 0x02}, 2)
		longTrue := Varival{
			numBytes: 3,
			value:    0x943FF,
		}

		if *longResult != longTrue {
			printFailure(t, unit, longTrue, *longResult)
		}
	})

	// Max value test
	t.Run("Test max Varival ", func(t *testing.T) {
		maxResult := readVarival([]byte{0xFF, 0xFF, 0xFF, 0x7F}, 0)
		maxTrue := Varival{
			numBytes: 4,
			value:    0xFFFFFFF,
		}

		if *maxResult != maxTrue {
			printFailure(t, unit, maxTrue, *maxResult)
		}
	})
}
