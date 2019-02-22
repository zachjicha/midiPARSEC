using System;

namespace Parsec
{
    class ParsecMessage
    {
        private byte[] message;
        private uint time;

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
            return message[2];
        }

        public byte getEventCode()
        {
            return message[3];
        }

        public byte getData()
        {
            return message[4];
        }

        public void print()
        {
            Console.WriteLine("{0} {1} {2} {3} {4}", message[0], message[1], message[2], message[3], message[4]);
        }

        public void print(int track)
        {
            Console.WriteLine("Track: {5} {0} {1} {2} {3} {4}", message[0], message[1], message[2], message[3], message[4], track);
        }

        public ParsecMessage(byte device, byte code, byte data, uint _time)
        {
            time = _time;
            message = new byte[8];
            message[0] = 0xAE;
            message[1] = device;
            //Byte 2 (Message Length) is handled below
            message[3] = code;
            message[4] = data;

            if(device == 0xFF)
            {
                message[2] = 1;
            }
            else {
                //Only note on events carry any extra data
                if(code == 0xA2)
                {
                    message[2] = 2;
                }
                else
                {
                    message[2] = 1;
                }
            }
        }
    }
}
