using System;

namespace midiParsec
{
    // TrackList is a special implementation of a linked list which represents a midi sequence
    class TrackList
    {
        private int _size;
        private TrackNode _head;
        private TrackNode _tail;

        public TrackList() 
        {
            _head = _tail = null;
            _size = 0;
        }

        //Insert an empty track at the end of the list
        //Note there is no remove function, it is not necessary
        public void AppendTrack(EventQueue e)
        {
            if(_size == 0)
            {
                _head = _tail = new TrackNode(e);
            }
            else {
                _tail.Next = new TrackNode(e);
                _tail = _tail.Next;
            }
            _size++;
        }

        //Getter
        public EventQueue GetTrack(int index) 
        {
            if(index < 0 || index >= _size)
            {
                return null;
            }
            else 
            {
                TrackNode current = _head;
                for(int i = 0; i < index; i++) 
                {
                    current = current.Next;
                }

                return current.Track;
            }
        }

        //Debug print methods
        public void Print()
        {
            TrackNode current = _head;
            int i = 0;
            while(current != null)
            {
                Console.WriteLine("Track: {0}", i++);
                current.Track.Print();
                current = current.Next;
            }
        }

        public void Print(int track)
        {
            TrackNode current = _head;
            int i = 0;
            while(current != null)
            {
                Console.WriteLine("Track: {0}", i);
                if(i == track)
                {
                    current.Track.Print();
                    break;
                }
                current = current.Next;
                i++;
            }
        }

        //Custom internal node class
        private class TrackNode 
        {
            public TrackNode Next;
            public EventQueue Track;

            public TrackNode(EventQueue e) 
            {
                Next = null;
                Track = e;
            }
        }
    }
}
