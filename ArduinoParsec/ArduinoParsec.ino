#include "StepperMotor.h"

#define QUERY_BYTE      0x90
#define RESPONSE_BYTE   0x26

#define MESSAGE_FLAG    0xAE
#define BROADCAST_FLAG  0xFF
#define BEGIN_FLAG      0xB0
#define END_FLAG        0xE0
#define IDLE_FLAG       0xA1
#define STANDBY_FLAG    0xA2 

StepperMotors steppers;

enum State {
  RECEIVE_QUERY,
  SEND_RESPONSE,
  WAITING_FOR_START,
  PLAY_MUSIC,
  SEQUENCE_END,
};

State state = RECEIVE_QUERY;
int light = LOW;
long mils = 0;

void parseMessage() {
  //If message flag is found
  if(Serial.read() == MESSAGE_FLAG) {
    byte bytebuffer[3]; 
    //Read the important bytes
    Serial.readBytes(bytebuffer, 3);
    byte databuffer[bytebuffer[1]];
    if(bytebuffer[1] != 0) {
      Serial.readBytes(databuffer, bytebuffer[1]);
    }

    //Ignore messages for motors that don't exist
    //but don't throw out broadast messages
    if(bytebuffer[0] > MAX_STEPPER_MOTORS && bytebuffer[0] != BROADCAST_FLAG) {
      return;
    }

    if(bytebuffer[0] == BROADCAST_FLAG) {
      if(bytebuffer[2] == BEGIN_FLAG) {
        state = PLAY_MUSIC;
        digitalWrite(LED_BUILTIN, HIGH);
      }
      else if(bytebuffer[2] == END_FLAG) {
        state = SEQUENCE_END;
        digitalWrite(LED_BUILTIN, LOW);
      }
      else if(bytebuffer[2] == IDLE_FLAG) {
        stepperMotorIdle(&steppers);
      }
      else if(bytebuffer[2] == STANDBY_FLAG) {
        stepperMotorStandby(&steppers);
      }
    }
    else {
      stepperMotorHandleMessage(&steppers, bytebuffer[0]-1, bytebuffer[2], databuffer);
    }
  }
}

void setup() {
  pinMode(LED_BUILTIN, OUTPUT);
  digitalWrite(LED_BUILTIN, LOW);
  Serial.begin(9600);
  // put your setup code here, to run once:
  stepperMotorSetup(&steppers);

  //Timer1 Stuff
  //Reset Timer1 Control Reg A 
  TCCR1A = 0;

  //Set Timer1 mode to CTC (reset upon reaching 640)
  TCCR1B &= ~(1 << WGM13);
  TCCR1B |= (1 << WGM12);

  //Set Prescaler to 1
  TCCR1B &= ~(1 << CS12);
  TCCR1B &= ~(1 << CS11);
  TCCR1B |= (1 << CS10);

  //Reset Timer1
  TCNT1 = 0;

  //Set compare value to 320 (20 micros)
  OCR1A = 320;

  //Enable Timer1 compare interrupt
  TIMSK1 = (1 << OCIE1A);

  //Enable Global Interrupts
  sei();

  mils = millis();
}

void loop() {
  switch(state){
    case RECEIVE_QUERY:
      
      if(Serial.read() == QUERY_BYTE) {
        state = SEND_RESPONSE;
      }
      
      break;

    case SEND_RESPONSE:
    
      Serial.write(RESPONSE_BYTE);
      Serial.write(MAX_STEPPER_MOTORS);
      state = WAITING_FOR_START;

      break;
    case WAITING_FOR_START:
      if(millis() - mils >= 500) {
        light = !light;
        mils = millis();
      }
      digitalWrite(LED_BUILTIN, light);
      parseMessage();
      break;
    case PLAY_MUSIC:
      parseMessage();
      break;
    case SEQUENCE_END:
      stepperMotorIdle(&steppers);
      break;
  }
}

ISR(TIMER1_COMPA_vect) {
  stepperMotorPlay(&steppers);
}
