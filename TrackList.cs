using System;

namespace Parsec
{
    // TrackList is a special implementation of a linked list which represents a midi sequence
    class TrackList
    {
        private int size;
        private TrackNode head;
        private TrackNode tail;

        public TrackList() 
        {
            head = tail = null;
            size = 0;
        }

        //Insert an empty track at the end of the list
        //Note there is no remove function, it is not necessary
        public void AppendTrack(EventQueue e)
        {
            if(size == 0)
            {
                head = tail = new TrackNode(e);
            }
            else {
                tail.next = new TrackNode(e);
                tail = tail.next;
            }
            size++;
        }

        //Getter
        public EventQueue GetTrack(int index) 
        {
            if(index < 0 || index >= size)
            {
                return null;
            }
            else 
            {
                TrackNode current = head;
                for(int i = 0; i < index; i++) 
                {
                    current = current.next;
                }

                return current.track;
            }
        }

        //Debug print methods
        public void Print()
        {
            TrackNode current = head;
            int i = 0;
            while(current != null)
            {
                Console.WriteLine("Track: {0}", i++);
                current.track.Print();
                current = current.next;
            }
        }

        public void Print(int track)
        {
            TrackNode current = head;
            int i = 0;
            while(current != null)
            {
                Console.WriteLine("Track: {0}", i);
                if(i == track)
                {
                    current.track.Print();
                    break;
                }
                current = current.next;
                i++;
            }
        }

        //Custom internal node class
        private class TrackNode 
        {
            public TrackNode next;
            public EventQueue track;

            public TrackNode(EventQueue e) 
            {
                next = null;
                track = e;
            }
        }
    }
}
