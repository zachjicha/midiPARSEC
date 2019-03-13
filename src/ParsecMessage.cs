using System;

namespace Parsec
{
    class ParsecMessage
    {
        private byte[] message;
        private uint time;
        private uint silentData;
        
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
            return message;
        }

        public uint GetTime()
        {
            return time;
        }  

        public byte GetDeviceAddress()
        {
            return message[1];
        }

        public byte GetLength()
        {
            return (byte)(4 + message[2]);
        }

        public byte GetEventCode()
        {
            return message[3];
        }

        public byte GetData()
        {
            return message[4];
        }

        public uint GetSilentData()
        {
            return silentData;
        }

        //Debug print method
        public void Print()
        {
            Console.WriteLine("{0:X2} {1:X2} {2:X2} {3:X2}  Time:{4}", message[0], message[1], message[2], message[3], time);
        }

        //PARSEC message class
        public ParsecMessage(byte device, byte code, byte[] data, uint _time, uint _silentData)
        {   
            //If no data, length = 0
            if(data == null)
            {
                message = new byte[4];
            }
            //Else length = data.Length
            else {
                message = new byte[4 + data.Length];
            }

            //midi dt
            time = _time;
            //Data for silent events
            silentData = _silentData;

            //Set actual bytes of message
            message[0] = PARSECMESSAGE_FLAG;
            message[1] = device;
            message[2] = (byte)(message.Length - 4);
            message[3] = code;
            
            //Append data array
            if(data != null) 
            {
                for(int i = 4; i < message.Length; i++)
                {
                    message[i] = data[i - 4];
                }
            }
            
        }
    }
}
