using System;

namespace midiParsec
{
    class ParsecMessage
    {
        private byte[] _message;
        private uint _time;
        private uint _silentData;
        
        //All the different event codes
        public static byte PARSECMESSAGE_FLAG = 0xAE;
        public static byte EVENTCODE_DEVICE_NOTEOFF = 0xA1;
        public static byte EVENTCODE_DEVICE_NOTEON = 0xA2;
        public static byte EVENTCODE_SILENT_TEMPO = 0xC1;
        public static byte EVENTCODE_SILENT_EOT = 0xCF;
        public static byte EVENTCODE_BROADCAST_IDLE = 0xA1;
        public static byte EVENTCODE_BROADCAST_STANDBY = 0xA2;
        public static byte EVENTCODE_BROADCAST_SEQUENCEBEGIN = 0xB0;
        public static byte EVENTCODE_BROADCAST_SEQUENCEEND = 0xE0;

        //Some special PARSEC messages
        public static ParsecMessage SequenceBeginMessage = new ParsecMessage(0xFF, EVENTCODE_BROADCAST_SEQUENCEBEGIN, null, 0, 0);
        public static ParsecMessage SequenceEndMessage = new ParsecMessage(0xFF, EVENTCODE_BROADCAST_SEQUENCEEND, null, 0, 0);
        public static ParsecMessage AllDevicesIdle = new ParsecMessage(0xFF, EVENTCODE_BROADCAST_IDLE, null, 0, 0);
        public static ParsecMessage AllDevicesStandby = new ParsecMessage(0xFF, EVENTCODE_BROADCAST_STANDBY, null, 0, 0);

        //Getters
        public byte[] GetMessage()
        {
            return _message;
        }

        public uint GetTime()
        {
            return _time;
        }  

        public byte GetDeviceAddress()
        {
            return _message[1];
        }

        public byte GetLength()
        {
            return (byte)(4 + _message[2]);
        }

        public byte GetEventCode()
        {
            return _message[3];
        }

        public byte GetData()
        {
            return _message[4];
        }

        public uint GetSilentData()
        {
            return _silentData;
        }

        //Debug print method
        public void Print()
        {
            Console.WriteLine("{0:X2} {1:X2} {2:X2} {3:X2}  Time:{4}", _message[0], _message[1], _message[2], _message[3], _time);
        }

        //PARSEC message class
        public ParsecMessage(byte device, byte code, byte[] data, uint time, uint silentData)
        {   
            //If no data, length = 0
            if(data == null)
            {
                _message = new byte[4];
            }
            //Else length = data.Length
            else {
                _message = new byte[4 + data.Length];
            }

            //midi dt
            _time = time;
            //Data for silent events
            _silentData = silentData;

            //Set actual bytes of message
            _message[0] = PARSECMESSAGE_FLAG;
            _message[1] = device;
            _message[2] = (byte)(_message.Length - 4);
            _message[3] = code;
            
            //Append data array
            if(data != null) 
            {
                for(int i = 4; i < _message.Length; i++)
                {
                    _message[i] = data[i - 4];
                }
            }
            
        }
    }
}
