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
            //This option ensures the arduino restarts when we run the program
            commsPort.DtrEnable = true;
            commsPort.ReadTimeout = 500;
            commsPort.WriteTimeout = 500;
        }
        
        private void handShake()
        {
            // This handshake establishes comms with the arduino
            // It works just like a TCP handshake
            // The three stages are named Query, Response, Start instead of SYN, SYN+ACK, ACK

            bool waitingForResponse = true;
            byte[] nums = {0x7F, 0xAB};

            //Query the arduino to make sure it is ready
            commsPort.Write(nums, 0, 1);

            //Wait for the Response from the Arduino to indicate it is ready
            while(waitingForResponse)
            {
                if(commsPort.ReadByte() == 0x7F)
                {
                    waitingForResponse = false;
                }
            }

            //Send the Start message, which tells the arduino we are ready to start the music
            commsPort.Write(nums, 1, 1);
        }

        public void openComms()
        {
            commsPort.Open();
            Console.WriteLine("Waiting for arduino to restart...");
            System.Threading.Thread.Sleep(5000);
            handShake();
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