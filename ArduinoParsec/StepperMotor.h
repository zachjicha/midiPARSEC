#ifndef Stepper_Motor_h
#define Stepper_Motor_h

#include "ParsecInstrument.h"
#include <Arduino.h>

#define MAX_STEPPER_MOTORS 5
#define MIN_FULLSTEP_INDEX 54
#define MAX_FULLSTEP_INDEX 72

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
    unsigned long pulseEndTimes[MAX_STEPPER_MOTORS];
    unsigned int cycle[MAX_STEPPER_MOTORS];
    unsigned int notePeriod[MAX_STEPPER_MOTORS];
    byte enPins[MAX_STEPPER_MOTORS];
    byte stepPins[MAX_STEPPER_MOTORS];
    byte modePins[MAX_STEPPER_MOTORS];
} StepperMotors;

void stepperMotorSetup(StepperMotors* stepper);
void stepperMotorPlay(StepperMotors* stepper);
void stepperMotorAutoMode(StepperMotors* stepper, int noteIndex, int motor);
void stepperMotorPulse(StepperMotors* stepper, int index);
void stepperMotorIdle(StepperMotors* stepper);
void stepperMotorStandby(StepperMotors* stepper);
void stepperMotorHandleMessage(StepperMotors* stepper, byte deviceAddress, byte eventCode, byte data[]);

#ifdef __cplusplus
}
#endif

#endif
