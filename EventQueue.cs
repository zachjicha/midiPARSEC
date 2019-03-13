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
        public void EnqueueEvent(byte device, byte code, byte[] data, uint time, uint silentData)
        {
            EventNode node = new EventNode(device, code, data, time, silentData, null);
            if(size == 0)
            {
                front = end = node;
            }
            else {
                end.next = node;
                end = end.next;
            } 
            size++;
        }

        // Dequeue an event and return its contents as a tuple
        public ParsecMessage DequeueEvent()
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
                return toReturn.message;
            }
        }

        //Debugging print function
        public void Print()
        {
            EventNode current = front;

            while(current != null)
            {
                current.message.Print();
                current = current.next;
            }
        }

        //Custom Node class, wraps around a PARSEC message
        private class EventNode 
        {
            public EventNode next;
            public ParsecMessage message;
            public EventNode(byte device, byte code, byte[] data, uint _time, uint _silentData, EventNode _next) 
            {
                message = new ParsecMessage(device, code, data, _time, _silentData);
                next = _next;
            }
        }
    }
}
