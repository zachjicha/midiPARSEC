package main

import "testing"

func printFailure(t *testing.T, unit string, expected interface{}, got interface{}) {
	t.Errorf("%v failed: Expected %v but got %v", unit, expected, got)
}

func TestReadVarival(t *testing.T) {

	unit := "readVarival()"

	// TEST ZERO
	zeroResult := readVarival([]byte{0x00}, 0)
	zeroTrue := Varival{
		numBytes: 1,
		value:    0,
	}

	if *zeroResult != zeroTrue {
		printFailure(t, unit, zeroTrue, *zeroResult)
	}

	// TEST 3 different lengths\
	// 5
	fiveResult := readVarival([]byte{0x05}, 0)
	fiveTrue := Varival{
		numBytes: 1,
		value:    5,
	}

	if *fiveResult != fiveTrue {
		printFailure(t, unit, fiveTrue, *fiveResult)
	}

	//3 byte test
	longResult := readVarival([]byte{0x00, 0x01, 0xA5, 0x87, 0x7F, 0x00, 0x02}, 2)
	longTrue := Varival{
		numBytes: 3,
		value:    0x943FF,
	}

	if *longResult != longTrue {
		printFailure(t, unit, longTrue, *longResult)
	}

	// Max value test
	maxResult := readVarival([]byte{0xFF, 0xFF, 0xFF, 0x7F}, 0)
	maxTrue := Varival{
		numBytes: 4,
		value:    0xFFFFFFF,
	}

	if *maxResult != maxTrue {
		printFailure(t, unit, maxTrue, *maxResult)
	}
}
