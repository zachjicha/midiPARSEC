using System;

namespace midiParsec
{
    // EventList is a special implementation of a queue which represents a midi track
    class EventQueue
    {
        private int _size;
        private EventNode _front;
        private EventNode _end;

        public EventQueue() 
        {
            _front = _end = null;
            _size = 0;
        }

        //Insert an event at the end of the queue
        public void EnqueueEvent(byte device, byte code, byte[] data, uint time, uint silentData)
        {
            EventNode node = new EventNode(device, code, data, time, silentData, null);
            if(_size == 0)
            {
                _front = _end = node;
            }
            else {
                _end.Next = node;
                _end = _end.Next;
            } 
            _size++;
        }

        // Dequeue an event and return its contents as a tuple
        public ParsecMessage DequeueEvent()
        {
            if(_size == 0)
            {
                return null;
            }
            else
            {
                EventNode toReturn = _front;
                _front = _front.Next;
                if(_size == 1)
                {
                    _end = null;
                }
                _size--;
                return toReturn.Message;
            }
        }

        //Debugging print function
        public void Print()
        {
            EventNode current = _front;

            while(current != null)
            {
                current.Message.Print();
                current = current.Next;
            }
        }

        //Custom Node class, wraps around a PARSEC message
        private class EventNode 
        {
            public EventNode Next;
            public ParsecMessage Message;
            public EventNode(byte device, byte code, byte[] data, uint time, uint silentData, EventNode next) 
            {
                Message = new ParsecMessage(device, code, data, time, silentData);
                Next = next;
            }
        }
    }
}
