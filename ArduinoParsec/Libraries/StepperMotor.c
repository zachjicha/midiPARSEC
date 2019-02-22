#include "ParsecInstrument.h"
#include <TimerOne.h>
#include <Arduino.h>


void stepperMotorSetup(StepperMotors* stepper) {
    stepper->enPins = {31, 37, 43, 53};
    stepper->stepPins = {35, 41, 47, 53};
    stepper->modePins = {33, 39, 45, 51};

    for(int i = 0; i < MAX_STEPPER_MOTORS; i++) {
        pinMode(enPins[i], OUTPUT);
        pinMode(stepPins[i], OUTPUT);
        pinMode(modePins[i], OUTPUT);
    }

    Timer1.initialize(TIMER_RESOLUTION);
    Timer1.attachInterrupt(stepperMotorPlay);
}

void stepperMotorPlay(StepperMotors* stepper) {
    if(stepper->notePeriod[0] > 0) {
        stepper->cycle[0] += 40;
        if(stepper->cycle[0] >= stepper->notePeriod[0]) {
            stepperMotorPulse(stepper, 0);
            stepper->cycle[0] = 0;
        }
    }

    if(stepper->notePeriod[1] > 0) {
        stepper->cycle[1] += 40;
        if(stepper->cycle[1] >= stepper->notePeriod[1]) {
            stepperMotorPulse(stepper, 1);
            stepper->cycle[1] = 0;
        }
    }

    if(stepper->notePeriod[2] > 0) {
        stepper->cycle[2] += 40;
        if(stepper->cycle[2] >= stepper->notePeriod[2]) {
            stepperMotorPulse(stepper, 2);
            stepper->cycle[2] = 0;
        }
    }

    if(stepper->notePeriod[3] > 0) {
        stepper->cycle[3] += 40;
        if(stepper->cycle[3] >= stepper->notePeriod[3]) {
            stepperMotorPulse(stepper, 3);
            stepper->cycle[3] = 0;
        }
    }
}

void stepperMotorAutoMode(StepperMotors* stepper, int index) {
    if(notePeriod[index] < 2800 || notePeriod[index] > 4200){
        digitalWrite(modePins[index], HIGH);
    }
    else {
        digitalWrite(modePins[index], LOW);
    }
}

void stepperMotorPulse(Stepper* stepper, int index) {
    digitalWrite(stepPins[index], HIGH);
    digitalWrite(stepPins[index], LOW);
}

void stepperMotorHandleMessage(StepperMotors* stepper, byte deviceAddress, byte eventCode, byte data) {
    switch(eventCode){
        case 0xA1:
            notePeriod[deviceAddress] = 0;
            break;

        case 0xA2:
            notePeriod[deviceAddress] = pitchVals[data];
            break;

        case 0xAF:
            notePeriod[deviceAddress] = 0;
            break;
    }
}
