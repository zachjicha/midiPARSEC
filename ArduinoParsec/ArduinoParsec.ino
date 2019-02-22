#include "StepperMotor.h"

StepperMotors steppers;

void parseMessage() {
  //If message flag is found
  if(Serial.read() == 0xAE) {
    byte bytebuffer[4]; 
    //Read the important bits
    Serial.readBytes(bytebuffer, 4);

    stepperMotorHandleMessage(&steppers, bytebuffer[0]-1, bytebuffer[2], bytebuffer[3]);
  }
}

void setup() {
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

  //Set compare value to 640 (40 micros)
  OCR1A = 640;

  //Enable Timer1 compare interrupt
  TIMSK1 = (1 << OCIE1A);

  //Enable Global Interrupts
  sei();

}

enum State {
  RECEIVE_QUERY,
  SEND_RESPONSE,
  WAITING_FOR_START,
  PLAY_MUSIC,
  SEND_END_SEQUENCE,
  DO_NOTHING
};

State state = RECEIVE_QUERY;

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
    
      if(Serial.read() == 0xAB) {
        state = PLAY_MUSIC;
      }

      break;
    case PLAY_MUSIC:
      parseMessage();
      
      break;
    case SEND_END_SEQUENCE:
      state = DO_NOTHING;
      break;
    case DO_NOTHING:
      break;
  }
}

ISR(TIMER1_COMPA_vect) {
  stepperMotorPlay(&steppers);
}
