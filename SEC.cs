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

            Console.WriteLine( "IsLittleEndian:  {0}", 
            BitConverter.IsLittleEndian );

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
            sequence.print();
            Console.WriteLine("Opening serial comms with Arduino...");
            Arduino arduino = new Arduino(args[0]);
            arduino.openComms();
            Console.WriteLine("Comms established! Ready to play...");

            while(true)
            {
                int request = arduino.listenForRequest();
                

                if(request == 0xFF)
                {
                    Console.WriteLine("Song finished playing!");
                    break;
                }
                    

                if(request <= 0)
                    continue;

                //Console.WriteLine("Cool Request: {0}", request);
                //Request - 1 is the track, as the 0 request is usually nothing
                if(request < 5) {
                    Console.WriteLine("Track Request: {0}", request - 1);
                    ParsecMessage message = sequence.getNextEvent(request - 1);
                    if(message.getType() == 2) 
                    {
                        message = sequence.getNextEvent(request - 1);
                    }
                    arduino.writeParsecMessage(message);
                    //Console.WriteLine("Sent!");
                }
                
            }
            Console.WriteLine("Closing comms...");
            arduino.closeComms();
            Console.WriteLine("Comms closed! Exiting...");
        }
    }
}
