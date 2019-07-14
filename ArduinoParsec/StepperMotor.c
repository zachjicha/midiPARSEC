#include "ParsecInstrument.h"
#include "StepperMotor.h"
#include <Arduino.h>


void stepperMotorSetup(StepperMotors* stepper) {
    stepper->enPins[0] = 31;
    stepper->enPins[1] = 37;
    stepper->enPins[2] = 43;
    stepper->enPins[3] = 49;
    stepper->enPins[4] = 25;
    stepper->stepPins[0] = 35;
    stepper->stepPins[1] = 41;
    stepper->stepPins[2] = 47;
    stepper->stepPins[3] = 53;
    stepper->stepPins[4] = 29;
    stepper->modePins[0] = 33;
    stepper->modePins[1] = 39;
    stepper->modePins[2] = 45;
    stepper->modePins[3] = 51;
    stepper->modePins[4] = 27;

    for(int i = 0; i < MAX_STEPPER_MOTORS; i++) {
        pinMode(stepper->enPins[i], OUTPUT);
        pinMode(stepper->stepPins[i], OUTPUT);
        pinMode(stepper->modePins[i], OUTPUT);
    }
    
}

void stepperMotorPlay(StepperMotors* stepper) {
    if(stepper->notePeriod[0] > 0) {
        stepper->cycle[0] += TIMER_INTERRUPT_INTERVAL;
        if(stepper->cycle[0] >= stepper->notePeriod[0]) {
            stepperMotorPulse(stepper, 0);
            stepper->cycle[0] = 0;
        }
    }

    if(stepper->notePeriod[1] > 0) {
        stepper->cycle[1] += TIMER_INTERRUPT_INTERVAL;
        if(stepper->cycle[1] >= stepper->notePeriod[1]) {
            stepperMotorPulse(stepper, 1);
            stepper->cycle[1] = 0;
        }
    }

    if(stepper->notePeriod[2] > 0) {
        stepper->cycle[2] += TIMER_INTERRUPT_INTERVAL;
        if(stepper->cycle[2] >= stepper->notePeriod[2]) {
            stepperMotorPulse(stepper, 2);
            stepper->cycle[2] = 0;
        }
    }

    if(stepper->notePeriod[3] > 0) {
        stepper->cycle[3] += TIMER_INTERRUPT_INTERVAL;
        if(stepper->cycle[3] >= stepper->notePeriod[3]) {
            stepperMotorPulse(stepper, 3);
            stepper->cycle[3] = 0;
        }
    }

    if(stepper->notePeriod[4] > 0) {
        stepper->cycle[4] += TIMER_INTERRUPT_INTERVAL;
        if(stepper->cycle[4] >= stepper->notePeriod[4]) {
            stepperMotorPulse(stepper, 4);
            stepper->cycle[4] = 0;
        }
    }
}

void stepperMotorAutoMode(StepperMotors* stepper, int noteIndex, int motor) {
    if(noteIndex < MIN_FULLSTEP_INDEX || noteIndex > MAX_FULLSTEP_INDEX){
        digitalWrite(stepper->modePins[motor], HIGH);
    }
    else {
        digitalWrite(stepper->modePins[motor], LOW);
    }
}

void stepperMotorPulse(StepperMotors* stepper, int index) {
    digitalWrite(stepper->stepPins[index], HIGH);
    digitalWrite(stepper->stepPins[index], LOW);
}

void stepperMotorIdle(StepperMotors* stepper) {
    for(int i = 0; i < MAX_STEPPER_MOTORS; i++) {
      digitalWrite(stepper->enPins[i], HIGH);
    }
}

void stepperMotorStandby(StepperMotors* stepper) {
    for(int i = 0; i < MAX_STEPPER_MOTORS; i++) {
      digitalWrite(stepper->enPins[i], LOW);
    }
}
void stepperMotorHandleMessage(StepperMotors* stepper, byte deviceAddress, byte eventCode, byte data[]) {
    switch(eventCode){
        case 0xA1:
            stepper->notePeriod[deviceAddress] = 0;
            break;

        case 0xA2:
            stepper->notePeriod[deviceAddress] = pitchVals[data[0]];
            stepperMotorAutoMode(stepper, data[0], deviceAddress);
            break;
    }
}
