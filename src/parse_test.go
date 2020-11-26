package main

import (
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
}
