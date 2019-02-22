using System;

namespace Parsec
{
    class ParsecMessage
    {
        private byte[] message;

        public byte[] getMessage()
        {
            return message;
        }

        public int getType()
        {
            return message[0];
        }

        public uint getTime()
        {
            return BitConverter.ToUInt32(message, 1);
        }

        public uint getData()
        {
            return BitConverter.ToUInt32(message, 5);
        }

        public ParsecMessage(byte _type, uint _time, uint _data)
        {
            message = new byte[9];
            byte[] timeBytes = BitConverter.GetBytes(_time);
            //Console.WriteLine("Time Byte Length: {0}", timeBytes.Length);
            byte[] dataBytes = BitConverter.GetBytes(_data);
            //Console.WriteLine("Data Byte Length: {0}", dataBytes.Length);
            message[0] = _type;
            message[1] = timeBytes[0];
            message[2] = timeBytes[1];
            message[3] = timeBytes[2];
            message[4] = timeBytes[3];
            message[5] = dataBytes[0];
            message[6] = dataBytes[1];
            message[7] = dataBytes[2];
            message[8] = dataBytes[3];
        }
    }
}
