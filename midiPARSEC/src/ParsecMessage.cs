using System;

namespace midiParsec
{
    class ParsecMessage
    {   
        //All the different event codes
        public const byte PARSECMESSAGE_FLAG    = 0xAE;
        public const byte BROADCAST_FLAG        = 0xFF;
        public const byte EC_DEVICE_NOTEOFF     = 0xA1;
        public const byte EC_DEVICE_NOTEON      = 0xA2;
        public const byte EC_CONDUCTOR_NULL     = 0xC0;
        public const byte EC_CONDUCTOR_TEMPO    = 0xC1;
        public const byte EC_CONDUCTOR_EOT      = 0xCF;
        public const byte EC_BROADCAST_IDLE     = 0xA1;
        public const byte EC_BROADCAST_STANDBY  = 0xA2;
        public const byte EC_BROADCAST_SEQBEGIN = 0xB0;
        public const byte EC_BROADCAST_SEQEND   = 0xE0;

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
        private uint   _conductorTime;
        private uint   _conductorData;

        //Default constructor
        public ParsecMessage() 
        {
            //Do nothing, this will always be overwritten
        }

        //PARSEC message constructor
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
            _conductorTime = time;
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

        //Getters
        public byte[] GetMessage()
        {
            return _message;
        }

        public byte[] FormatAndGetMessage(int numberOfSteppers, uint numberOfTracks) 
        {
            //No need to modify the message if it is a broadcast message
            if(_message[1] == 0xFF) 
            {
                return  _message;
            }

            //If the number of motors is odd
            if(numberOfSteppers % 2 == 1)
            {
                byte offset = (byte)((numberOfSteppers - numberOfTracks)/2);
                //If the number of tracks is odd
                if(numberOfTracks % 2 == 1)
                {
                    //Offset the motors so the middle ones play
                    _message[1] += offset;
                }
                //If there are an even number of tracks
                else {
                    //Offset the first half of the motors
                    if(_message[1] <= numberOfTracks/2) 
                    {
                        _message[1] += offset;
                    }
                    //Skip the middle motor, it will not play
                    else 
                    {
                        _message[1] += offset;
                        _message[1] += 1;
                    }
                }
            }
            //If number of motors is even
            else 
            {
                byte offset = (byte)((numberOfSteppers - numberOfTracks)/2);
                //If the number of tracks is odd
                if(numberOfTracks % 2 == 0)
                {
                    //Offset the motors so the middle ones play
                    _message[1] += offset;
                }
                //Nothing to be done if odd number of tracks, they can't be arranged 
                //nicely on an even number of motors
            }

            return _message;
        }

        public uint GetTime()
        {
            return _conductorTime;
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
            Console.Write("{0:X2} {1:X2} {2:X2} {3:X2} ", _message[0], _message[1], _message[2], _message[3], _conductorTime, _conductorData);
            //Print payload if it exists
            if(_message[2] > 0) 
            {
                for(int i = 4; i < _message[2] + 4; ++i)
                {
                    Console.Write("Data<{0}>: {1:X2} ", i-4, _message[i]);
                }
            }
            Console.WriteLine("   Time:{0} Data:{1}", _conductorTime, _conductorData);
        }
    }
}
