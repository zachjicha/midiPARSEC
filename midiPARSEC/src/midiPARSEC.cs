using System;
using System.Threading;
using System.IO.Ports;
using System.IO;
using System.Windows.Input;



namespace midiParsec
{
    // Serial Encoding Client component, main class of PARSEC
    class midiPARSEC
    {
        static void Main(string[] args)
        {
            
            //Check for correct argument usage
            if(args.Length != 2)
            {
                throw new ArgumentException("\nIncorrect arguments!\nCorrect usage: dotnet run <portname> <file>.mid");
            }

            // Parse the midi file
            Sequence sequence = new Sequence(args[1]);
            Console.WriteLine("{0} successfully parsed!", args[1]);

            //Make arduino object
            Arduino arduino = new Arduino(args[0]);

            //Open serial port to arduino
            arduino.OpenComms();
            Console.WriteLine("Comms established! Ready to play...");

            //Idle all devices
            Console.WriteLine("All devices idling! Press ENTER to begin...");
            arduino.WriteParsecMessage(ParsecMessage.PM_ALL_IDLE, sequence.NumberOfTracks);
            
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
            arduino.WriteParsecMessage(ParsecMessage.PM_ALL_STANDBY, sequence.NumberOfTracks);

            //Tell the arduino to begin the sequence
            arduino.WriteParsecMessage(ParsecMessage.PM_SEQ_BEGIN, sequence.NumberOfTracks);

            Console.WriteLine("Now playing! Press ENTER again to stop...");

            //Get a reference point in time to calculate delta time
            DateTime past = new DateTime(1999, 8, 30);

            //Calculate current time is microseconds
            long elapsedTicks = DateTime.Now.Ticks - past.Ticks;
            long currentMicros = elapsedTicks/10;

            //Intiialize sequence with correct start time
            sequence.InitializeStartTimes(currentMicros);

            //Loop until sequence is done
            while(true)
            {
                //Calculate current time is microseconds
                elapsedTicks = DateTime.Now.Ticks - past.Ticks;
                currentMicros = elapsedTicks/10;

                //Traverse the sequence and check for pending midi events
                sequence.TraverseSequence(currentMicros, arduino);
             
                //Check if no tracks are left (sequence is done)
                if(sequence.RemainingTracks == 0)
                {
                    //Send the sequence end message and break from the loop
                    arduino.WriteParsecMessage(ParsecMessage.PM_SEQ_END, sequence.NumberOfTracks);
                    break;
                }

                //Check for key press
                if(Console.KeyAvailable)
                {
                    //Exit on enter
                    if(Console.ReadKey().Key == ConsoleKey.Enter)
                    {
                        arduino.WriteParsecMessage(ParsecMessage.PM_SEQ_END, sequence.NumberOfTracks);
                        break;
                    }
                }
            }
            //Close comms with arduino
            Console.WriteLine("Closing comms...");
            arduino.CloseComms();
            Console.WriteLine("Comms closed! Exiting...");
        }
    }
}