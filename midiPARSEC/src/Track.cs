using System;
using System.Collections.Generic;

namespace midiParsec
{
    // EventList is a special implementation of a queue which represents a midi track
    class Track
    {
        private Queue<ParsecMessage> _trackQueue;
        public  uint CumulativeTime;

        public Track() 
        {
            _trackQueue    = new Queue<ParsecMessage>();
            CumulativeTime = 0;
        }

        //Insert an event at the end of the queue
        public void EnqueueEvent(byte device, byte code, byte[] data, uint time, uint conductorData)
        {
            //Make a new message and enqueue it
            _trackQueue.Enqueue(new ParsecMessage(device, code, data, time, conductorData));
            //Add time to to the cumulative sum
            CumulativeTime += time;
        }

        //Insert an event at the end of the queue
        public void EnqueueEvent(ParsecMessage message)
        {
            //Make a new message and enqueue it
            _trackQueue.Enqueue(message);
            //Add time to to the cumulative sum
            CumulativeTime += message.GetTime();
        }

        // Dequeue an event and return its contents as a tuple
        public ParsecMessage DequeueEvent()
        {
            return _trackQueue.Dequeue();
        }

        //Debugging print function
        public void Print()
        {
            foreach (ParsecMessage message in _trackQueue)  
            {
                message.Print();
            }
        }
    }
}
