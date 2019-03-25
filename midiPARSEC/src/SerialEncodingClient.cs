using System;
using System.Threading;
using System.IO.Ports;
using System.IO;
using System.Windows.Input;



namespace midiParsec
{
    // Serial Encoding Client component, main class of PARSEC
    class SerialEncodingClient
    {
        static void Main(string[] args)
        {
            
            //Check for correct argument usage
            if(args.Length != 2)
            {
                throw new ArgumentException("\nIncorrect arguments!\nCorrect usage: dotnet run <portname> <file>.mid");
            }

            // Parse the midi file
            Console.WriteLine("Parsing {0}...", args[1]);
            Sequence _sequence = new Sequence(args[1]);
            Console.WriteLine("{0} successfully parsed!", args[1]);

            // Debug print the sequence
            //sequence.print();

            //Make arduino object
            Console.WriteLine("Opening serial comms with Arduino...");
            Arduino _arduino = new Arduino(args[0]);

            //Open serial port to arduino
            _arduino.OpenComms();
            Console.WriteLine("Comms established! Ready to play...");

            //Idle all devices
            Console.WriteLine("All devices idling! Press ENTER to begin...");
            _arduino.WriteParsecMessage(ParsecMessage.AllDevicesIdle);
            
            //Wait for user to press enter
            while(true)
            {
                //Wait for enter
                if(Console.ReadKey(true).Key == ConsoleKey.Enter)
                {
                    break;
                }
            }

            //Standby all devices
            Console.WriteLine("All devices standing by...");
            _arduino.WriteParsecMessage(ParsecMessage.AllDevicesStandby);

            //Tell the arduino to begin the sequence
            _arduino.WriteParsecMessage(ParsecMessage.SequenceBeginMessage);

            Console.WriteLine("Now playing! Press ENTER again to stop...");

            //Get a reference point in time to calculate delta time
            DateTime past = new DateTime(1999, 8, 30);

            //Calculate current time is microseconds
            long elapsedTicks = DateTime.Now.Ticks - past.Ticks;
            long currentMicros = elapsedTicks/10;

            //Intiialize sequence with correct start time
            _sequence.InitializeStartTimes(currentMicros);

            //Loop until sequence is done
            while(true)
            {
                //Calculate current time is microseconds
                elapsedTicks = DateTime.Now.Ticks - past.Ticks;
                currentMicros = elapsedTicks/10;

                //Traverse the sequence and check for pending midi events
                _sequence.TraverseSequence(currentMicros, _arduino);
             
                //Check if no tracks are left (sequence is done)
                if(_sequence.GetTracksLeft() == 0)
                {
                    //Send the sequence end message and break from the loop
                    _arduino.WriteParsecMessage(ParsecMessage.SequenceEndMessage);
                    break;
                }

                //Check for key press
                if(Console.KeyAvailable)
                {
                    //Exit on enter
                    if(Console.ReadKey().Key == ConsoleKey.Enter)
                    {
                        _arduino.WriteParsecMessage(ParsecMessage.SequenceEndMessage);
                        break;
                    }
                }
            }
            //Close comms with arduino
            Console.WriteLine("Closing comms...");
            _arduino.CloseComms();
            Console.WriteLine("Comms closed! Exiting...");
        }
    }
}
