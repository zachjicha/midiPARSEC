using System;
using System.Threading;
using System.IO.Ports;
using System.IO;



namespace Parsec
{
    // Serial Encoding Client component, main class of PARSEC
    class SEC
    {
        static void Main(string[] args)
        {

            if(args.Length != 2)
            {
                throw new ArgumentException("\nIncorrect arguments!\nCorrect usage: dotnet run <portname> <file>.mid");
            }

            // Parse the midi file
            Console.WriteLine("Parsing {0}...", args[1]);
            Sequence sequence = new Sequence(args[1]);
            if(sequence.getClocks() != 480)
            {
                throw new ArgumentException(String.Format("\nIrregular clocks!\nExpect 480, got {0}!", sequence.getClocks()));
            }
            Console.WriteLine("{0} successfully parsed!", args[1]);
            // Debug print the sequence
            //sequence.print();
            Console.WriteLine("Opening serial comms with Arduino...");
            Arduino arduino = new Arduino(args[0]);
            arduino.openComms();
            Console.WriteLine("Comms established! Ready to play...");

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
                    break;
                }
                
            }
            //Console.WriteLine("Closing comms...");
            //arduino.closeComms();
            Console.WriteLine("Comms closed! Exiting...");
        }
    }
}
