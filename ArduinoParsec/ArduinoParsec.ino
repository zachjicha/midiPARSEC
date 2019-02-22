typedef struct {

    char track;
    int done;
    int enPin;
    int stepPin;
    int modePin;
    unsigned long eventStartTime;
    unsigned long pulseEndTime;

    unsigned int currentType;
    unsigned int currentTime;
    unsigned long currentData;

    unsigned int nextType;
    unsigned int nextTime;
    unsigned long nextData;

} StepperMotor;


//Retuns 1 if StepperMotor has reached EOT, else 0
int stepperMotorDone(StepperMotor* stepper) {
    return (*stepper).done;
}

//Inits times to start of program
void stepperMotorInitTimes(StepperMotor* stepper) {
    (*stepper).eventStartTime = micros();
    (*stepper).pulseEndTime = micros();
}

//Handles advancing to the next event
void stepperMotorAdvance(StepperMotor* stepper, double *microsPerTick, float clocks) {
    //Check type of current event
    if((*stepper).currentType == 3) {
        //EOT, disable motor
        digitalWrite((*stepper).enPin, HIGH);
        (*stepper).done = 1;
    }
    //If length of event has passed
    else if(micros() - (*stepper).eventStartTime >= ((*stepper).nextTime * (*microsPerTick))) {
        if((*stepper).currentType == 2) {
            *microsPerTick = ((*stepper).currentData)/clocks;
        }

        //Set motor to half step mode if note is especially high or low
        //Helps to combat weird noises and ugly sounds
        if((*stepper).currentData < 2800 || (*stepper).currentData > 4200) {
            digitalWrite((*stepper).modePin, HIGH);
        }
        else {
            digitalWrite((*stepper).modePin, LOW);
        }

        //(*stepper).eventStartTime += ((*stepper).nextTime * (*microsPerTick));
        (*stepper).eventStartTime = micros();

        if((*stepper).nextType == 3) {
          (*stepper).currentType = (*stepper).nextType;
          (*stepper).currentTime = (*stepper).nextTime;
          (*stepper).currentData = (*stepper).nextData;
          (*stepper).nextType = 3;
          (*stepper).nextTime = 1;
          (*stepper).nextData = 0;
        }
        else {
          (*stepper).currentType = (*stepper).nextType;
          (*stepper).currentTime = (*stepper).nextTime;
          (*stepper).currentData = (*stepper).nextData;
          unsigned char bytebuffer[] = {0,0,0,0,0,0,0,0,0};
          stepperMotorRequestNextEvent(stepper, bytebuffer);
          (*stepper).nextType = bytebuffer[0];
          (*stepper).nextTime = byteArrayToInt(bytebuffer, 1, 4);
          //(*stepper).nextTime = 5000;
          (*stepper).nextData = byteArrayToInt(bytebuffer, 5, 8);
          /*Serial.write((*stepper).track);
          (*stepper).nextType = Serial.read();
          (*stepper).nextTime = Serial.read();
          (*stepper).nextTime += 256*(Serial.read());
          (*stepper).nextTime += 65536*(Serial.read());
          (*stepper).nextTime += 16777216*(Serial.read());
          (*stepper).nextData = Serial.read();
          (*stepper).nextData += 256*(Serial.read());
          (*stepper).nextData += 65536*(Serial.read());
          (*stepper).nextData += 16777216*(Serial.read());*/
        }
        
        stepperMotorEnable(stepper);
    }
}

//Handles turning the motor on and off based on the event type
void stepperMotorEnable(StepperMotor* stepper) {
    if((*stepper).currentType == 1) {
        digitalWrite((*stepper).enPin, LOW);
    }
    else {
        digitalWrite((*stepper).enPin, HIGH);
    }
}

//Handles pulsing the motor at correct frequency
void stepperMotorPlay(StepperMotor* stepper) {
    if(micros() - (*stepper).pulseEndTime >= (*stepper).currentData) {
        (*stepper).pulseEndTime += (*stepper).currentData;
        digitalWrite((*stepper).stepPin, HIGH);
        digitalWrite((*stepper).stepPin, LOW);
    }
}
//Disables motor
void stepperMotorIdle(StepperMotor* stepper) {
    digitalWrite((*stepper).enPin, HIGH);
}

void stepperMotorRequestNextEvent(StepperMotor* stepper, unsigned char bytebuffer[]) {
    Serial.write((*stepper).track);
    //while(bytebuffer[0] == 0 && bytebuffer[1] == 0 && bytebuffer[2] == 0 && bytebuffer[3] == 0 && bytebuffer[4] == 0 && bytebuffer[5] == 0 && bytebuffer[6] == 0 && bytebuffer[7] == 0 && bytebuffer[8] == 0) {
      Serial.readBytes(bytebuffer, 9);
    //}
    
}

int byteArrayToInt(unsigned char bytes[], int start, int fin) {

    if(start > fin) {
        return -1;
    }

    int sum = 0;
    for(int i = fin; i >= start; i--) {
        sum = (sum * 256) + bytes[i];
    }

    return sum;

}


StepperMotor s0 = {1, 0, 31, 35, 33, 0, 0, 0, 0, 0, 0, 0, 0};
StepperMotor s1 = {2, 0, 37, 41, 39, 0, 0, 0, 0, 0, 0, 0, 0};
StepperMotor s2 = {3, 0, 43, 47, 45, 0, 0, 0, 0, 0, 0, 0, 0};
StepperMotor s3 = {4, 0, 49, 53, 51, 0, 0, 0, 0, 0, 0, 0, 0};

void setup() {
  // put your setup code here, to run once:

  Serial.begin(9600);
  pinMode(s0.enPin, OUTPUT);
  pinMode(s0.stepPin, OUTPUT);
  pinMode(s0.modePin, OUTPUT);
  pinMode(s1.enPin, OUTPUT);
  pinMode(s1.stepPin, OUTPUT);
  pinMode(s1.modePin, OUTPUT);
  pinMode(s2.enPin, OUTPUT);
  pinMode(s2.stepPin, OUTPUT);
  pinMode(s2.modePin, OUTPUT);
  pinMode(s3.enPin, OUTPUT);
  pinMode(s3.stepPin, OUTPUT);
  pinMode(s3.modePin, OUTPUT);

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
double clocks = 480;
double tempo = 500000;
double microsPerTick = tempo/clocks;

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
        stepperMotorInitTimes(&s0);
        stepperMotorInitTimes(&s1);
        stepperMotorInitTimes(&s2);
        stepperMotorInitTimes(&s3);
      }

      break;
    case PLAY_MUSIC:

      if(2 - stepperMotorDone(&s0) - stepperMotorDone(&s1)/* - stepperMotorDone(&s2) - stepperMotorDone(&s3) */== 0) {
        state = SEND_END_SEQUENCE;
      }
      stepperMotorAdvance(&s0, &microsPerTick, clocks);
      stepperMotorAdvance(&s1, &microsPerTick, clocks);
      //stepperMotorAdvance(&s2, &microsPerTick, clocks);
      //stepperMotorAdvance(&s3, &microsPerTick, clocks);
        
      stepperMotorPlay(&s0);
      stepperMotorPlay(&s1);
      //stepperMotorPlay(&s2);
      //stepperMotorPlay(&s3);
      break;
    case SEND_END_SEQUENCE:
      Serial.write(0xFF);
      state = DO_NOTHING;
      break;
    case DO_NOTHING:
      break;
  }


}
