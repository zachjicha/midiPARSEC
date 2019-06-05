using System;

namespace midiParsec
{
    class ParsecMessage
    {   
        //All the different event codes
        public static byte PARSECMESSAGE_FLAG    = 0xAE;
        public static byte BROADCAST_FLAG        = 0xFF;


        public static byte EC_DEVICE_NOTEOFF     = 0xA1;
        public static byte EC_DEVICE_NOTEON      = 0xA2;
        public static byte EC_CONDUCTOR_TEMPO    = 0xC1;
        public static byte EC_CONDUCTOR_EOT      = 0xCF;
        public static byte EC_BROADCAST_IDLE     = 0xA1;
        public static byte EC_BROADCAST_STANDBY  = 0xA2;
        public static byte EC_BROADCAST_SEQBEGIN = 0xB0;
        public static byte EC_BROADCAST_SEQEND   = 0xE0;

        //Some special PARSEC messages
        public static ParsecMessage PM_SEQ_BEGIN   = 
                new ParsecMessage(BROADCAST_FLAG, EC_BROADCAST_SEQBEGIN, null, 0, 0);
        public static ParsecMessage PM_SEQ_END     = 
                new ParsecMessage(BROADCAST_FLAG, EC_BROADCAST_SEQEND  , null, 0, 0);
        public static ParsecMessage PM_ALL_IDLE    = 
                new ParsecMessage(BROADCAST_FLAG, EC_BROADCAST_IDLE    , null, 0, 0);
        public static ParsecMessage PM_ALL_STANDBY = 
                new ParsecMessage(BROADCAST_FLAG, EC_BROADCAST_STANDBY , null, 0, 0);

        
        //Private members of parsecmessage
        private byte[] _message;
        private uint   _time;
        private uint   _conductorData;

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

        public uint GetConductorData()
        {
            return _conductorData;
        }

        //Debug print method
        public void Print()
        {
            Console.WriteLine("{0:X2} {1:X2} {2:X2} {3:X2}  Time:{4}", _message[0], _message[1], _message[2], _message[3], _time);
        }

        //PARSEC message class
        public ParsecMessage(byte device, byte code, byte[] data, uint time, uint conductorData)
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
            _conductorData = conductorData;

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
