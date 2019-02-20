void setup() {
  // put your setup code here, to run once:

  Serial.begin(9600);

}

enum State {
  WAITING_FOR_HANDSHAKE,
  ACK_HANDSHAKE,
  WAITING_FOR_ACK,
  TRANSMITTING
};
byte byteBuffer[9];
State state = WAITING_FOR_HANDSHAKE;

void loop() {
  // put your main code here, to run repeatedly:

  switch(state){
    case WAITING_FOR_HANDSHAKE:
      
      if(Serial.read() == 0x7F) {
        state = ACK_HANDSHAKE;
      }
      
      break;

    case ACK_HANDSHAKE:
    
      Serial.write(0x7F);
      state = WAITING_FOR_ACK;

      break;
    case WAITING_FOR_ACK:
      if(Serial.read() == 0xAB) {
        state = TRANSMITTING;
      }

      break;
    case TRANSMITTING:
      Serial.write(0x80);
      break;
  }


}
