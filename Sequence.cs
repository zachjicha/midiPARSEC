using System;
using System.IO;

namespace Parsec
{
    // Sequence object, stores midi sequences as a linked list of queues
    // Each track in the sequence is stored as a queue in the list
    class Sequence
    { 
        private int numberOfTracks;
        private double clocks;
        private string filename;
        private TrackList trackList;
        private long[] eventStartTimes;
        private ParsecMessage[] currentEvents;
        private double microsPerTick;
        private int tracksLeft;
        private bool isVideo;

        public Sequence(string _filename, bool _isVideo) {
            isVideo = _isVideo;
            numberOfTracks = 0;
            filename = _filename;
            trackList = new TrackList();
            try
            {
                populateSequence();
            }
            catch (FileNotFoundException e)
            {
                throw new FileNotFoundException("\nMidi file not found...", e);
            }

            tracksLeft = numberOfTracks;
            eventStartTimes = new long[numberOfTracks];
            currentEvents = new ParsecMessage[numberOfTracks];
            microsPerTick = 500000/clocks;

            for(int i = 0; i < numberOfTracks; i++)
            {
                currentEvents[i] = getNextEvent(i);
            }
        }

        public void initializeStartTimes(long startTime)
        {
            for(int i = 0; i < numberOfTracks; i++)
            {
                eventStartTimes[i] = startTime;
            }
        }

        public void traverseSequence(long currentTime, Arduino arduino)
        {
            double tempConversion = 0;
            bool tempoEncountered = false;
            for(int i = 0; i < numberOfTracks; i++)
            {
                if(currentEvents[i] == null)
                {
                    continue;
                }
                    

                if((currentTime - eventStartTimes[i]) >= (microsPerTick * currentEvents[i].getTime()))
                {
                    //Check if event is a "silent event" (one the arduino doesn't need to know about, but is still an event)
                    if((currentEvents[i].getEventCode() & 0xF0) == 0xC0)
                    {
                        
                        if(currentEvents[i].getEventCode() == ParsecMessage.EVENTCODE_SILENT_EOT) 
                        {
                            tracksLeft--;
                            currentEvents[i] = null;
                        }
                        else 
                        {
                            //Tempo change event
                            if(currentEvents[i].getEventCode() == ParsecMessage.EVENTCODE_SILENT_TEMPO)
                            {   
                                tempConversion = currentEvents[i].getSilentData()/clocks;
                                tempoEncountered = true;
                            }
                            currentEvents[i] = getNextEvent(i);
                            eventStartTimes[i] = currentTime;
                        }

                        
                    }
                    else 
                    {
                        arduino.writeParsecMessage(currentEvents[i]);
                        currentEvents[i] = getNextEvent(i);
                        eventStartTimes[i] = currentTime;
                        
                    }
                }
            }

            if(tempoEncountered)
            {
                microsPerTick = tempConversion;
            }
        }

        public int getTracksLeft()
        {
            return tracksLeft;
        }

        public ParsecMessage getNextEvent(int track)
        {
            return trackList.getTrack(track).dequeueEvent();
        }

        public double getClocks()
        {
            return clocks;
        }

        public int getNumberOfTracks()
        {
            return numberOfTracks;
        }

        public void print()
        {
            trackList.print();
        }

        // Class needed for parsing midi variable length values 
        private class VariableLengthValue
        {
            public uint numberOfBytes;
            public uint value;

            public VariableLengthValue(byte[] bytes, int start)
            {
                numberOfBytes = 0;
                value = 0;
                int index = start;

                do 
                {
                    value = (uint)((value * 128) + (bytes[index] & 0x7F));
                    numberOfBytes++;
                } while(((bytes[index++] & 0x80) == 0x80));
                // Loop while first bit == 1
            }
        }

        private int byteArrayToUnsignedInt(byte[] bytes, int start, int end)
        {
            if(start > end)
            {
                return -1;
            }

            int sum = 0;
            for(int i = start; i <= end; i++)
            {
                sum = (sum * 256) + bytes[i];
            }

            return sum;
        }


        // Parses the midi file and populates the TrackList
        // Strap in, the PAR in PARSEC is long
        public void populateSequence()
        {

            byte[] bytes = File.ReadAllBytes(filename);
            //Get the number of tracks
            numberOfTracks = byteArrayToUnsignedInt(bytes, 10, 11);
            //Get the pulses per quarter note
            clocks = byteArrayToUnsignedInt(bytes, 12, 13);

            //Variable that tracks the index of the first byte of the current track
            //Starts at 14 since the header chunk is always 14 bytes long
            int trackStartIndex = 14;

            //Status is the type of Midi message event, seperate from meta and SYSEX events
            byte status = 0;

            //Boolean to track whether we are in running status or not
            int isRunningStatus = 0;

            //Loop through each track
            for(int i = 0; i < numberOfTracks; i++) {

                //Check to make sure its a track chunk
                if(bytes[trackStartIndex] != 0x4D || bytes[trackStartIndex+1] != 0x54 || bytes[trackStartIndex+2] != 0x72 || bytes[trackStartIndex+3] != 0x6B) {
                    //If not, continue
                    i--;
                    trackStartIndex += 8 + byteArrayToUnsignedInt(bytes, trackStartIndex+4, trackStartIndex+7);
                    continue;
                }

                //Make a queue that represents the current track
                EventQueue currentTrack = new EventQueue();
                trackList.appendTrack(currentTrack);

                //Add some events to the beginning for calibration and timing purposes
                currentTrack.enqueueEvent((byte)(i+1), ParsecMessage.EVENTCODE_DEVICE_NOTEOFF, null, 0, 0);
                /*if(isVideo) {
                    currentTrack.enqueueEvent((byte)(i+1), ParsecMessage.EVENTCODE_DEVICE_IDLE, null, 0, 0);
                    currentTrack.enqueueEvent((byte)(i+1), ParsecMessage.EVENTCODE_DEVICE_STANDBY, null, 5000, 0);
                }*/
                byte[] calibrationNote = {72};
                currentTrack.enqueueEvent((byte)(i+1), ParsecMessage.EVENTCODE_DEVICE_NOTEON, calibrationNote, 0, 0);
                currentTrack.enqueueEvent((byte)(i+1), ParsecMessage.EVENTCODE_DEVICE_NOTEOFF, null, 500, 0);
                currentTrack.enqueueEvent((byte)(i+1), ParsecMessage.EVENTCODE_DEVICE_NOTEOFF, null, 1500, 0);

                //Length of the data of the chunk
                int chunkLength = byteArrayToUnsignedInt(bytes, trackStartIndex+4, trackStartIndex+7);

                //MTrk chunks are split into three main parts: the MTrk tag, length, and data
                //The data contains the delta time event pairs

                //pairStartIndex is the index of the first byte of a delta time/event pair
                //it starts at the first byte of the data
                int pairStartIndex = trackStartIndex + 8;

                //Index of the next event pair
                int nextPairStartIndex = 0;

                //chunkLength + trackStartIndex + 8 is the first byte of the next chunk
                //this is since the start of the chunk is s, the header is 8 bytes and the rest is chunkLength bytes
                //Loop through each dt/event pair

                while(pairStartIndex < chunkLength + trackStartIndex + 8) {

                    //printf("%d\n", pairStartIndex);

                    //Here are the members of the event we will create
                    byte eventDevice = (byte)(i+1);
                    byte eventCode = 0;
                    byte[] eventData = null;
                    uint eventTime = 0;
                    uint eventSilentData = 0;
                    
                    //Set the members of the event
                    VariableLengthValue deltaRead = new VariableLengthValue(bytes, pairStartIndex);
                    eventTime = deltaRead.value;

                    //eventStartIndex is the index of the first byte of the event in the current delta time/event pair
                    int eventStartIndex = (int)(pairStartIndex + deltaRead.numberOfBytes);

                    //If this condition is true, then it is a meta event
                    if(bytes[eventStartIndex] == 0xFF) {
                        isRunningStatus = 0;

                        //This byte holds the type of meta event
                        //It is analogous to status
                        byte type = bytes[eventStartIndex + 1];

                        //Check the type of meta event
                        if(type == 0x2F) {
                            //This is an EOT event
                            eventData = null;
                            eventCode = ParsecMessage.EVENTCODE_SILENT_EOT;
                            //Record the length of the dt/event pair
                            //EOT event is always 3 long
                            nextPairStartIndex = 3 + eventStartIndex;
                        }
                        else if(type == 0x51) {
                            //this is a tempo meta event
                            uint tempo = (uint)(byteArrayToUnsignedInt(bytes, eventStartIndex+3, eventStartIndex+5));
                            //int tempo = 500000;
                            eventSilentData = tempo;
                            eventCode = ParsecMessage.EVENTCODE_SILENT_TEMPO;
                            //Record the length of the dt/event pair
                            //Tempo event is always 6 long
                            nextPairStartIndex = 6 + eventStartIndex;
                        }
                        else {
                            //This is for events we dont care about
                            VariableLengthValue variableLengthRead = new VariableLengthValue(bytes, eventStartIndex + 2);
                            //The index of ther next event pair is the sum of (in order of adding):
                            //Length of the event data
                            //Length of the Length (how many bytes were used to store the length)
                            //The current eventStartIndex
                            //2 (1 is for the byte that marks the event as meta, 1 for the type of meta event)
                            nextPairStartIndex = (int)(variableLengthRead.value + variableLengthRead.numberOfBytes + eventStartIndex + 2);
                            //Skip the rest of this loop
                            pairStartIndex = nextPairStartIndex;
                            continue;
                        }
                    }

                    //This case is for system exclusive events, irrelevant
                    else if(bytes[eventStartIndex] == 0xF0 || bytes[eventStartIndex] == 0xF7) {
                        isRunningStatus = 0;
                        //Record the length of the dt/event pair
                        VariableLengthValue variableLengthRead = new VariableLengthValue(bytes, eventStartIndex + 2);
                        //The index of ther next event pair is the sum of (in order of adding):
                        //Length of the event data
                        //Length of the Length (how many bytes were used to store the length)
                        //The current eventStartIndex
                        //1 (for the byte that marks the event as SYSEX)
                        nextPairStartIndex = (int)(variableLengthRead.value + variableLengthRead.numberOfBytes + eventStartIndex + 1);
                        //Skip the rest of this loop
                        pairStartIndex = nextPairStartIndex;
                        continue;
                    }

                    //If we got here, that means we have a regular midi event
                    else {

                        //Check if we are in running status
                        if(bytes[eventStartIndex] > 0x7F) {
                            //If bytes[k] is greater than 0x7F, then it is a new status, so we record it
                            status = bytes[eventStartIndex];
                            isRunningStatus = 0;
                        }

                        //If we skipped the prior if statement, then we are in running status, so keep using the previous status

                        //Check which status we are in
                        if((status & 0xF0) == 0xB0) 
                        {

                            //If we are here, then this is either a Channel Mode Message or Controller Change message
                            //None of these events are relevant
                            //All events in this grouping have a length of 3
                            nextPairStartIndex = 3 + eventStartIndex - isRunningStatus;
                            isRunningStatus = 1;
                            pairStartIndex = nextPairStartIndex;
                            continue;
                        }

                        //If we reach this condition, then the event is a Midi Voice Message
                        //Finally the good stuff
                        else {

                            //Check the status byte
                            if((status & 0xF0) == 0x80) 
                            {
                                //Note off message
                                eventData = null;
                                eventCode = ParsecMessage.EVENTCODE_DEVICE_NOTEOFF;
                                //Record the length of the dt/event pair
                                nextPairStartIndex = 3 + eventStartIndex - isRunningStatus;
                                isRunningStatus = 1;
                            }
                            else if((status & 0xF0) == 0x90) {
                                //Note on message
                                byte pitchIndex = bytes[eventStartIndex + 1 - isRunningStatus];

                                int velocity = bytes[eventStartIndex + 2 - isRunningStatus];
                                if(velocity == 0) 
                                {
                                    //Velocity of zero is really a note off event to sustain running status
                                    eventData = null;
                                    eventCode = ParsecMessage.EVENTCODE_DEVICE_NOTEOFF;
                                }
                                else {
                                    eventData = new byte[1];
                                    eventData[0] = pitchIndex;
                                    eventCode = ParsecMessage.EVENTCODE_DEVICE_NOTEON;
                                }
                                //Record the length of the dt/event pair
                                nextPairStartIndex = 3 + eventStartIndex - isRunningStatus;
                                isRunningStatus = 1;
                            }
                            else if((status & 0xF0) == 0xA0) {
                                //Polyphonic pressure
                                nextPairStartIndex = 3 + eventStartIndex;
                                isRunningStatus = 1;
                                pairStartIndex = nextPairStartIndex;
                                continue;
                            }
                            else if((status & 0xF0) == 0xC0) {
                                //Program Change
                                //Record the length of the dt/event pair
                                nextPairStartIndex = 2 + eventStartIndex;
                                isRunningStatus = 1;
                                pairStartIndex = nextPairStartIndex;
                                continue;
                            }
                            else if((status & 0xF0) == 0xD0) {
                                //Channel Key Pressure
                                //Record the length of the dt/event pair
                                nextPairStartIndex = 2 + eventStartIndex;
                                isRunningStatus = 1;
                                pairStartIndex = nextPairStartIndex;
                                continue;
                            }
                            else if((status & 0xF0) == 0xE0) {
                                //Pitch Bend
                                //TODO This one might actually be useful at some time
                                //Record the length of the dt/event pair
                                nextPairStartIndex = 3 + eventStartIndex;
                                isRunningStatus = 1;
                                pairStartIndex = nextPairStartIndex;
                                continue;
                            }
                        }
                    }
                    //Finally! Bless your soul if you got here!

                    //Now if we are here that means we got one of the handful of relevant events
                    //So we can add it to our track
                    currentTrack.enqueueEvent(eventDevice, eventCode, eventData, eventTime, eventSilentData);
                    pairStartIndex = nextPairStartIndex;
                }
                trackStartIndex = nextPairStartIndex;
            }
        }
    }
}
