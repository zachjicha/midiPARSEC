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
            Console.WriteLine("{0} successfully parsed!", args[1]);
            // Debug print the sequence
            //s.print();
            Console.WriteLine("Opening serial comms with Arduino...");
            Arduino arduino = new Arduino(args[0]);
            arduino.openComms();
            Console.WriteLine("Comms established! Ready to play...");

            while(true)
            {
                int request = arduino.listenForRequest();
                Console.WriteLine("Cool Request: {0}", request);

                if(request == 0)
                    break;

                if(request < 0)
                    continue;

                //Request - 1 is the track, as the 0 request is reserved for end of song
                //arduino.writeParsecMessage(sequence.getNextEvent(request - 1));
            }
            
            arduino.closeComms();

        }
    }
}
