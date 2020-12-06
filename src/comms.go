package main

import "github.com/huin/goserial"

func makeSerialConfig(name string) *goserial.Config {
	config := goserial.Config{
		Name: name,
		Baud: 9600,
	}
	return &config
}
