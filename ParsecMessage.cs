using System;

namespace Parsec
{
    class ParsecMessage
    {
        private byte[] message;
        private byte type;
        private uint time;
        private uint data;

        public byte[] getMessage()
        {
            return message;
        }

        public byte getType()
        {
            return type;
        }

        public uint getTime()
        {
            return time;
        }

        public uint getData()
        {
            return data;
        }

        public void print()
        {
            Console.WriteLine("Type: {0} Time: {1} Data: {2}", type, time, data);
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
            type = _type;
            time = _time;
            data = _data;
        }
    }
}
