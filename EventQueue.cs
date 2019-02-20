using System;

namespace Parsec
{
    // EventList is a special implementation of a queue which represents a midi track
    class EventQueue
    {
        private int size;
        private EventNode front;
        private EventNode end;

        public EventQueue() 
        {
            front = end = null;
            size = 0;
        }

        //Insert an event at the end of the queue
        public void enqueueEvent(byte _type, uint _time, uint _data)
        {
            if(size == 0)
            {
                front = end = new EventNode(_type, _time, _data, null);
            }
            else {
                end.next = new EventNode(_type, _time, _data, null);
                end = end.next;
            }
            size++;
        }

        // Dequeue an event and return its contents as a tuple
        public Tuple<byte, uint, uint> dequeueEvent()
        {
            if(size == 0)
            {
                return null;
            }
            else
            {
                EventNode toReturn = front;
                front = front.next;
                if(size == 1)
                {
                    end = null;
                }
                size--;
                return Tuple.Create(toReturn.type, toReturn.time, toReturn.data);
            }
        }

        public void print()
        {
            EventNode current = front;

            while(current != null)
            {
                Console.WriteLine("Type: {0}  Time: {1}  Data: {2}", current.type, current.time, current.data);
                current = current.next;
            }
        }

        private class EventNode 
        {
            public EventNode next;
            public byte type;
            public uint time;
            public uint data;

            public EventNode(byte _type, uint _time, uint _data, EventNode _next) 
            {
                type = _type;
                time = _time;
                data = _data;
                next = _next;
            }
        }
    }
}
