typedef struct {

    byte deviceAddress;
    byte eventCode;
    byte data;
    byte enPin;
    byte stepPin;
    byte modePin;
    unsigned long pulseEndTime;

} StepperMotor;

unsigned int pitchVals[] = {
            0, //0, C-1
            0, //1, C#-1
            0, //2, D-1
            0, //3, D#-1
            0, //4, E-1
            0, //5, F-1
            0, //6, F#-1
            0, //7, G-1
            0, //8, G#-1
            0, //9, A-1
            0, //10, A#-1
            0, //11, B-1
            61162, //12, C0
            57736, //13, C#0
            54495, //14, D0
            51413, //15, D#0
            48543, //16, E0
            45808, //17, F0
            43252, //18, F#0
            40816, //19, G0
            38520, //20, G#0
            36363, //21, A0
            34317, //22, A#0
            32258, //  23  B0 31
            30303,  //  24  C1  33
            28571, //  25  CS1 35
            27027, //  26  D1  37
            25641, //  27  DS1 39
            24390,  //  28  E1  41
            22727, //  29  F1  44
            21739, //  30  FS1 46
            20408, //  31  G1  49
            19230, //  32  GS1 52
            18182, //  33  A1  55
            17241, //  34  AS1 58
            16129, //  35  B1  62
            15385, //  36  C2  65
            14493, //  37  CS2 69
            13699, //  38  D2  73
            12821, //  39  DS2 78
            12195, //  40  E2  82
            11494, //  41  F2  87
            10753, //  42  FS2 93
            10204, //  43  G2  98
            9615, //  44  GS2 104
            9091, //  45  A2  110
            8547, //  46  AS2 117
            8130, //  47  B2  123
            7634, //  48  C3  131
            7194, //  49  CS3 139
            6803, //  50  D3  147
            6410,  //  51  DS3 156
            6061, //  52  E3  165
            5714, //  53  F3  175
            5405, //  54  FS3 185
            5102, //  55  G3  196
            4808, //  56  GS3 208
            4545, //  57  A3  220
            4292, //  58  AS3 233
            4049, //  59  B3  247
            3817, //  60  C4  262
            3610, //  61  CS4 277
            3401, //  62  D4  294
            3215, //  63  DS4 311
            3030,  //  64  E4  330
            2865, //  65  F4  349
            2703, //  66  FS4 370
            2551, //  67  G4  392
            2410, //  68  GS4 415
            2273, //  69  A4  440
            2146, //  70  AS4 466
            2024, //  71  B4  494
            1912, //  72  C5  523
            1805, //  73  CS5 554
            1704, //  74  D5  587
            1608, //  75  DS5 622
            1517, //  76  E5  659
            1433, //  77  F5  698
            1351, //  78  FS5 740
            1276, //  79  G5  784
            1203, //  80  GS5 831
            1136, //  81  A5  880
            1073, //  82  AS5 932
            1012, //  83  B5  988
            955, //  84  C6  1047
            902, //  85  CS6 1109
            851, //  86  D6  1175
            803, //  87  DS6 1245
            758, //  88  E6  1319
            716, //  89  F6  1397
            676, //  90  FS6 1480
            638,  //  91  G6  1568
            602, //  92  GS6 1661
            568, //  93  A6  1760
            536, //  94  AS6 1865
            506, //  95  B6  1976
            478, //  96  C7  2093
            451,  //  97  CS7 2217
            426, //  98  D7  2349
            402, //  99  DS7 2489
            379, //  100 E7  2637
            358, //  101 F7  2794
            338, //  102 FS7 2960
            315,  //  103 G7  3136
            301, //  104 GS7 3322
            284, //  105 A7  3520
            268, //  106 AS7 3729
            253, //  107 B7  3951
            239, //  108 C8  4186
            225, //  109 CS8 4435
            213, //  110 D8  4699
            201, //  111 DS8 4978
            0, //112, E8
            0, //113, F8
            0, //114, F#8
            0, //115, G8
            0, //116, G#8
            0, //117, A8
            0, //118, A#8
            0, //119, B8
            0, //120, C9
            0, //121, C#9
            0, //122, D9
            0, //123, D#9
            0, //124, E9
            0, //125, F9
            0, //126, F#9
            0, //127, G9
        };

StepperMotor s1 = {1, 0xA1, 0, 31, 35, 33, 0};
StepperMotor s2 = {2, 0xA1, 0, 37, 41, 39, 0};
StepperMotor s3 = {3, 0xA1, 0, 43, 47, 45, 0};
StepperMotor s4 = {4, 0xA1, 0, 49, 53, 51, 0};

//Inits times to start of program
void stepperMotorInitTimes(StepperMotor* stepper) {
    (*stepper).pulseEndTime = micros();
}

void stepperMotorAutoStep(StepperMotor* stepper) {
    if((*stepper).data < 2800 || (*stepper).data > 4200) {
            digitalWrite((*stepper).modePin, HIGH);
        }
        else {
            digitalWrite((*stepper).modePin, LOW);
        }
}

//Handles turning the motor on and off based on the event type
void stepperMotorEnable(StepperMotor* stepper) {
    if((*stepper).eventCode == 0xA2) {
        digitalWrite((*stepper).enPin, LOW);
    }
    else {
        digitalWrite((*stepper).enPin, HIGH);
    }
}

//Handles pulsing the motor at correct frequency
void stepperMotorPlay(StepperMotor* stepper) {
    if(micros() - (*stepper).pulseEndTime >= pitchVals[(*stepper).data]) {
        (*stepper).pulseEndTime = micros();
        digitalWrite((*stepper).stepPin, HIGH);
        digitalWrite((*stepper).stepPin, LOW);
    }
}
//Disables motor
void stepperMotorIdle(StepperMotor* stepper) {
    digitalWrite((*stepper).enPin, HIGH);
}

void handoffEvent(StepperMotor* stepper, byte eventCode, byte data) {
  (*stepper).eventCode = eventCode;
  (*stepper).data = data;
  stepperMotorEnable(stepper);
  stepperMotorAutoStep(stepper);
}

byte messageBuffer[4];

void parseMessage() {
  //If message flag is found
  if(Serial.read() == 0xAE) {
    //Read the important bits
    Serial.readBytes(messageBuffer, 4);
/*
    Serial.write(0x80);
    Serial.write(messageBuffer[0]);
    Serial.write(messageBuffer[1]);
    Serial.write(messageBuffer[2]);
    Serial.write(messageBuffer[3]);
*/

    if(messageBuffer[0] == 1) {
      handoffEvent(&s1, messageBuffer[2], messageBuffer[3]);
    }
    else if(messageBuffer[0] == 2) {
      handoffEvent(&s2, messageBuffer[2], messageBuffer[3]);
    }
    else if(messageBuffer[0] == 3) {
      handoffEvent(&s3, messageBuffer[2], messageBuffer[3]);
    }
    else if(messageBuffer[0] == 4) {
      handoffEvent(&s4, messageBuffer[2], messageBuffer[3]);
    }
    
  }
}

void setup() {
  // put your setup code here, to run once:

  Serial.begin(9600);
  pinMode(s1.enPin, OUTPUT);
  pinMode(s1.stepPin, OUTPUT);
  pinMode(s1.modePin, OUTPUT);
  pinMode(s2.enPin, OUTPUT);
  pinMode(s2.stepPin, OUTPUT);
  pinMode(s2.modePin, OUTPUT);
  pinMode(s3.enPin, OUTPUT);
  pinMode(s3.stepPin, OUTPUT);
  pinMode(s3.modePin, OUTPUT);
  pinMode(s4.enPin, OUTPUT);
  pinMode(s4.stepPin, OUTPUT);
  pinMode(s4.modePin, OUTPUT);

}

enum State {
  RECEIVE_QUERY,
  SEND_RESPONSE,
  WAITING_FOR_START,
  PLAY_MUSIC,
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
        stepperMotorInitTimes(&s1);
        stepperMotorInitTimes(&s2);
        stepperMotorInitTimes(&s3);
        stepperMotorInitTimes(&s4);
      }

      break;
    case PLAY_MUSIC:

      //if() {
        //state = DO_NOTHING;
      //}

      parseMessage();
        
      stepperMotorPlay(&s1);
      stepperMotorPlay(&s2);
      stepperMotorPlay(&s3);
      stepperMotorPlay(&s4);
      break;
    case DO_NOTHING:
      break;
  }


}
