#include "StepperMotor.h"

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
  if(Serial.read() == 0xAE) {
    byte bytebuffer[3]; 
    //Read the important bytes
    Serial.readBytes(bytebuffer, 3);
    byte databuffer[bytebuffer[1]];
    if(bytebuffer[1] != 0) {
      Serial.readBytes(databuffer, bytebuffer[1]);
    }

    if(bytebuffer[0] == 0xFF) {
      if(bytebuffer[2] == 0xB0) {
        state = PLAY_MUSIC;
        digitalWrite(LED_BUILTIN, HIGH);
      }
      else if(bytebuffer[2] == 0xE0) {
        state = SEQUENCE_END;
        digitalWrite(LED_BUILTIN, LOW);
      }
      else if(bytebuffer[2] == 0xA1) {
        stepperMotorIdle(&steppers);
      }
      else if(bytebuffer[2] == 0xA2) {
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
      
      if(Serial.read() == 0x7F) {
        state = SEND_RESPONSE;
      }
      
      break;

    case SEND_RESPONSE:
    
      Serial.write(0x7F);
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
