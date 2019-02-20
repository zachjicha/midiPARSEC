using System;
using System.Threading;
using System.IO.Ports;

namespace Parsec
{

    // This class holds the serial ports and comms functions
    class Arduino
    {

        private SerialPort commsPort;


        public Arduino(string port)
        {
            commsPort = new SerialPort(port, 9600);
            commsPort.DtrEnable = true;
            commsPort.ReadTimeout = 500;
            commsPort.WriteTimeout = 500;
        }
        
        public void openComms()
        {
            commsPort.Open();
            Console.WriteLine("Waiting for arduino to restart...");
            System.Threading.Thread.Sleep(5000);

            int state = 0;
            byte[] nums = {0x7F, 0xAB};
            commsPort.Write(nums, 0, 1);

            while(state == 0)
            {
                
                if(commsPort.ReadByte() == 0x7F)
                {
                    state = 1;
                }
            }

            commsPort.Write(nums, 1, 1);
        }

        public void closeComms()
        {
            commsPort.Close();
        }  

        public int listenForRequest()
        {
            try{
                return commsPort.ReadByte();
            }
            catch
            {
                //In case of a timeout
                return -1;
            }
        } 

        public void writeParsecMessage(ParsecMessage message)
        {
            commsPort.Write(message.getMessage(), 0, 9);
        }


    }


}