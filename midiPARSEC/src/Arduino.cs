using System;
using System.Threading;
using System.IO.Ports;

namespace midiParsec
{

    // This class holds the serial ports and comms functions
    class Arduino
    {

        //Bytes used in handshake with arduino
        //Not a bad choice of numbers my dude
        private const byte QUERY_BYTE    = 0x90;
        private const byte RESPONSE_BYTE = 0x26;

        private int        _maxSteppers;
        private SerialPort _commsPort;

        public Arduino(string port)
        {
            //Open serial port with baud rate 9600
            _commsPort = new SerialPort(port, 9600);
            //This option ensures the arduino restarts when we run the program
            _commsPort.DtrEnable = true;
            //Default timeouts half a second, don't really matter
            _commsPort.ReadTimeout = 500;
            _commsPort.WriteTimeout = 500;
        }
        
        //Handshake with arduino to open comms
        private void HandShake()
        {
            // This handshake establishes comms with the arduino
            // It works just like a TCP handshake, but only 2 steps
            // The two stages are named Query, Response

            bool waitingForResponse = true;
            byte[] queryByte        = {QUERY_BYTE};

            //Query the arduino to make sure it is ready
            _commsPort.Write(queryByte, 0, 1);

            //Wait for the Response from the Arduino to indicate it is ready
            while(waitingForResponse)
            {
                if(_commsPort.ReadByte() == RESPONSE_BYTE)
                {
                    waitingForResponse = false;
                }
            }

            //Read the second response byte which tells us how many steppers are connected to the arduino
            _maxSteppers = _commsPort.ReadByte();
        }

        //Method to open serial port
        public void OpenComms()
        {
            //OPen the port
            _commsPort.Open();
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
            _commsPort.Close();
        } 

        //Method to write the bytes of a PARSEC message to the serial port
        public void WriteParsecMessage(ParsecMessage message, uint numberOfTracks)
        {
            //Write the message bytes
            _commsPort.Write(message.FormatAndGetMessage(_maxSteppers, numberOfTracks), 0, message.Length);
        }
    }
}