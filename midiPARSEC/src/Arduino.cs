using System;
using System.Threading;
using System.IO.Ports;

namespace midiParsec
{

    // This class holds the serial ports and comms functions
    class Arduino
    {

        private SerialPort commsPort;

        public Arduino(string port)
        {
            //Open serial port with baud rate 9600
            commsPort = new SerialPort(port, 9600);
            //This option ensures the arduino restarts when we run the program
            commsPort.DtrEnable = true;
            //Default timeouts half a second, don't really matter
            commsPort.ReadTimeout = 500;
            commsPort.WriteTimeout = 500;
        }
        
        //Handshake with arduino to open comms
        private void HandShake()
        {
            // This handshake establishes comms with the arduino
            // It works just like a TCP handshake, but only 2 steps
            // The two stages are named Query, Response

            bool waitingForResponse = true;
            byte[] queryByte = {0x7F};

            //Query the arduino to make sure it is ready
            commsPort.Write(queryByte, 0, 1);

            //Wait for the Response from the Arduino to indicate it is ready
            while(waitingForResponse)
            {
                if(commsPort.ReadByte() == 0x7F)
                {
                    waitingForResponse = false;
                }
            }
        }

        //Method to open serial port
        public void OpenComms()
        {
            //OPen the port
            commsPort.Open();
            Console.WriteLine("Waiting for arduino to restart...");

            //Opening the serial port causes the arduino to restart, so wait a second for it to do that
            System.Threading.Thread.Sleep(1000);

            //Shake hands with arduino
            Console.WriteLine("Shaking hands with arduino...");
            HandShake();
        }

        //Method to close serial port
        public void CloseComms()
        {
            //Close the port
            commsPort.Close();
        } 

        //Method to write the bytes of a PARSEC message to the serial port
        public void WriteParsecMessage(ParsecMessage message)
        {
            //Write the message bytes
            commsPort.Write(message.GetMessage(), 0, message.GetLength());
        }
    }
}