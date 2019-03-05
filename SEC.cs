using System;
using System.Threading;
using System.IO.Ports;
using System.IO;
using System.Windows.Input;



namespace Parsec
{
    // Serial Encoding Client component, main class of PARSEC
    class SEC
    {
        static void Main(string[] args)
        {
            

            if(args.Length < 2)
            {
                throw new ArgumentException("\nIncorrect arguments!\nCorrect usage: dotnet run <portname> <file>.mid <options>\nAcceptable options: v [Idle motors for 20 secs before playback]");
            }

            bool isVideo = false;

            if(args.Length == 3) 
            {
                if(args[2] == "v") 
                {
                    isVideo = true;
                }
            }

            // Parse the midi file
            Console.WriteLine("Parsing {0}...", args[1]);
            Sequence sequence = new Sequence(args[1], isVideo);
            Console.WriteLine("{0} successfully parsed!", args[1]);
            // Debug print the sequence
            //sequence.print();
            Console.WriteLine("Opening serial comms with Arduino...");
            Arduino arduino = new Arduino(args[0]);
            arduino.openComms();
            Console.WriteLine("Comms established! Ready to play...");

            Console.WriteLine("Press ENTER to begin...");

            if(isVideo)
            {
                Console.WriteLine("All devices idling...");
                arduino.writeParsecMessage(ParsecMessage.AllDevicesIdle);
            }
            
            while(true)
            {
                //Wait for enter
                if(Console.ReadKey(true).Key == ConsoleKey.Enter)
                {
                    break;
                }
            }

            if(isVideo)
            {
                Console.WriteLine("All devices standing by...");
                arduino.writeParsecMessage(ParsecMessage.AllDevicesStandby);
            }

            arduino.writeParsecMessage(ParsecMessage.SequenceBeginMessage);

            Console.WriteLine("Now playing! Press ENTER again to stop...");

            

            DateTime past = new DateTime(1999, 8, 30);
            long elapsedTicks = DateTime.Now.Ticks - past.Ticks;
            long currentMicros = elapsedTicks/10;

            sequence.initializeStartTimes(currentMicros);

            while(true)
            {
                elapsedTicks = DateTime.Now.Ticks - past.Ticks;
                currentMicros = elapsedTicks/10;

                sequence.traverseSequence(currentMicros, arduino);
             
                if(sequence.getTracksLeft() == 0)
                {
                    arduino.writeParsecMessage(ParsecMessage.SequenceEndMessage);
                    break;
                }

                //Check for key press
                if(Console.KeyAvailable)
                {
                    //Exit on enter
                    if(Console.ReadKey().Key == ConsoleKey.Enter)
                    {
                        arduino.writeParsecMessage(ParsecMessage.SequenceEndMessage);
                        break;
                    }
                }
                
                
            }
            Console.WriteLine("Closing comms...");
            arduino.closeComms();
            Console.WriteLine("Comms closed! Exiting...");
        }
    }
}
