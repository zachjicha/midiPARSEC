using System;

namespace Parsec
{
    // EventList is a special implementation of a linked list which represents a midi track
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
        public void appendTrack(EventQueue e)
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

        public EventQueue getTrack(int index) 
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

        public void print()
        {
            TrackNode current = head;
            int i = 0;
            while(current != null)
            {
                Console.WriteLine("Track: {0}", i++);
                current.track.print();
                current = current.next;
            }
        }

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
