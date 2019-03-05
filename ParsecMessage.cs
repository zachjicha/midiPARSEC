using System;

namespace Parsec
{
    class ParsecMessage
    {
        private byte[] message;
        private uint time;
        private uint silentData;
        
        public static byte PARSECMESSAGE_FLAG = 0xAE;
        public static byte EVENTCODE_DEVICE_NOTEOFF = 0xA1;
        public static byte EVENTCODE_DEVICE_NOTEON = 0xA2;
        public static byte EVENTCODE_DEVICE_IDLE = 0xA3;
        public static byte EVENTCODE_DEVICE_STANDBY = 0xA4;
        public static byte EVENTCODE_MULTI_NOTEOFF = 0xB1;
        public static byte EVENTCODE_MULTI_NOTEON = 0xB2;
        public static byte EVENTCODE_SILENT_TEMPO = 0xC1;
        public static byte EVENTCODE_SILENT_EOT = 0xCF;
        public static ParsecMessage SequenceBeginMessage = new ParsecMessage(0xFF, 0xB0, null, 0, 0);
        public static ParsecMessage SequenceEndMessage = new ParsecMessage(0xFF, 0xE0, null, 0, 0);
        public static ParsecMessage AllDevicesIdle = new ParsecMessage(0xFF, 0xA1, null, 0, 0);
        public static ParsecMessage AllDevicesStandby = new ParsecMessage(0xFF, 0xA2, null, 0, 0);

        public byte[] getMessage()
        {
            return message;
        }

        public uint getTime()
        {
            return time;
        }  

        public byte getDeviceAddress()
        {
            return message[1];
        }

        public byte getLength()
        {
            return (byte)(4 + message[2]);
        }

        public byte getEventCode()
        {
            return message[3];
        }

        public byte getData()
        {
            return message[4];
        }

        public uint getSilentData()
        {
            return silentData;
        }

        public void print()
        {
            Console.WriteLine("{0:X2} {1:X2} {2:X2} {3:X2}", message[0], message[1], message[2], message[3]);
        }

        public void print(int track)
        {
            Console.WriteLine("Track: {5}   {0:X2} {1:X2} {2:X2} {3:X2} {4:X2}", message[0], message[1], message[2], message[3], message[4], track);
        }

        public ParsecMessage(byte device, byte code, byte[] data, uint _time, uint _silentData)
        {   

            if(data == null)
            {
                message = new byte[4];
            }
            else {
                message = new byte[4 + data.Length];
            }
            time = _time;
            silentData = _silentData;
            message[0] = PARSECMESSAGE_FLAG;
            message[1] = device;
            message[2] = (byte)(message.Length - 4);
            message[3] = code;

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
