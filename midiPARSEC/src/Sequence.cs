using System;
using System.IO;

namespace midiParsec
{
    // Sequence object, stores midi sequences as a linked list of queues
    // Each track in the sequence is stored as a queue in the list
    class Sequence
    { 
        private int _numberOfTracks;
        private double _clocks;
        private string _fileName;
        private TrackList _trackList;
        private long[] _eventStartTimes;
        private ParsecMessage[] _currentEvents;
        private double _microsPerTick;
        private int _tracksLeft;

        public Sequence(string fileName) {
            _numberOfTracks = 0;
            _fileName = fileName;
            _trackList = new TrackList();
            try
            {
                PopulateSequence();
            }
            catch (FileNotFoundException e)
            {
                throw new FileNotFoundException("\nMidi file not found...", e);
            }

            _tracksLeft = _numberOfTracks;
            _eventStartTimes = new long[_numberOfTracks];
            _currentEvents = new ParsecMessage[_numberOfTracks];
            _microsPerTick = 500000/_clocks;

            //Populate the current events array by getting the first event from each track
            for(int i = 0; i < _numberOfTracks; i++)
            {
                _currentEvents[i] = GetNextEvent(i);
            }
        }

        public void InitializeStartTimes(long startTime)
        {
            for(int i = 0; i < _numberOfTracks; i++)
            {
                _eventStartTimes[i] = startTime;
            }
        }

        public void TraverseSequence(long currentTime, Arduino arduino)
        {
            double tempConversion = 0;
            bool tempoEncountered = false;
            for(int i = 0; i < _numberOfTracks; i++)
            {
                if(_currentEvents[i] == null)
                {
                    continue;
                }
                    
                
                if((currentTime - _eventStartTimes[i]) >= (_microsPerTick * _currentEvents[i].GetTime()))
                {
                    //Check if event is a "silent event" (one the arduino doesn't need to know about, but is still an event)
                    if((_currentEvents[i].GetEventCode() & 0xF0) == 0xC0)
                    {
                        
                        if(_currentEvents[i].GetEventCode() == ParsecMessage.EVENTCODE_SILENT_EOT) 
                        {
                            _tracksLeft--;
                            _currentEvents[i] = null;
                        }
                        else 
                        {
                            //Tempo change event
                            if(_currentEvents[i].GetEventCode() == ParsecMessage.EVENTCODE_SILENT_TEMPO)
                            {   
                                tempConversion = _currentEvents[i].GetSilentData()/_clocks;
                                tempoEncountered = true;
                            }

                            _currentEvents[i] = GetNextEvent(i);
                            _eventStartTimes[i] = currentTime;
                        }

                        
                    }
                    else 
                    {   
                        arduino.WriteParsecMessage(_currentEvents[i]);
                        _currentEvents[i] = GetNextEvent(i);
                        _eventStartTimes[i] = currentTime;
                        
                    }
                }
            }
            
            if(tempoEncountered)
            {
                _microsPerTick = tempConversion;
            }
        }

        //Getters
        public int GetTracksLeft()
        {
            return _tracksLeft;
        }

        public ParsecMessage GetNextEvent(int track)
        {
            return _trackList.GetTrack(track).DequeueEvent();
        }

        public double GetClocks()
        {
            return _clocks;
        }

        public int GetNumberOfTracks()
        {
            return _numberOfTracks;
        }

        //Debug print methods
        public void Print()
        {
            _trackList.Print();
        }

        public void Print(int track)
        {
            _trackList.Print(track);
        }

        // Class needed for parsing midi variable length values 
        private class VariableLengthValue
        {
            public uint NumberOfBytes;
            public uint Value;

            public VariableLengthValue(byte[] bytes, int start)
            {
                NumberOfBytes = 0;
                Value = 0;
                int index = start;

                while(true)
                {
                    Value = (uint)((Value << 7) + (bytes[index] & 0x7F));
                    NumberOfBytes++;
                    //If first bit is 0, we are done
                    if((bytes[index] & 0x80) != 0x80)
                    {
                        break;
                    }
                    index++;
                
                }
            }
        }

        private int ByteArrayToUnsignedInt(byte[] bytes, int start, int end)
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
        public void PopulateSequence()
        {

            byte[] bytes = File.ReadAllBytes(_fileName);
            //Get the number of tracks
            _numberOfTracks = ByteArrayToUnsignedInt(bytes, 10, 11);
            //Get the pulses per quarter note
            _clocks = ByteArrayToUnsignedInt(bytes, 12, 13);

            //Variable that tracks the index of the first byte of the current track
            //Starts at 14 since the header chunk is always 14 bytes long
            int trackStartIndex = 14;

            //Status is the type of Midi message event, seperate from meta and SYSEX events
            byte status = 0;

            //Boolean to track whether we are in running status or not
            int isRunningStatus = 0;

            //Loop through each track
            for(int i = 0; i < _numberOfTracks; i++) 
            {

                //Check to make sure its a track chunk
                if(bytes[trackStartIndex] != 0x4D || bytes[trackStartIndex+1] != 0x54 || bytes[trackStartIndex+2] != 0x72 || bytes[trackStartIndex+3] != 0x6B) 
                {
                    //If not, continue
                    i--;
                    trackStartIndex += 8 + ByteArrayToUnsignedInt(bytes, trackStartIndex+4, trackStartIndex+7);
                    continue;
                }

                //Make a queue that represents the current track
                EventQueue currentTrack = new EventQueue();
                _trackList.AppendTrack(currentTrack);

                //Add some events to the beginning for calibration and timing purposes
                currentTrack.EnqueueEvent((byte)(i+1), ParsecMessage.EVENTCODE_DEVICE_NOTEOFF, null, 0, 0);
                byte[] calibrationNote = {72};
                currentTrack.EnqueueEvent((byte)(i+1), ParsecMessage.EVENTCODE_DEVICE_NOTEON, calibrationNote, 0, 0);
                currentTrack.EnqueueEvent((byte)(i+1), ParsecMessage.EVENTCODE_DEVICE_NOTEOFF, null, 750, 0);
                //Wait a little
                currentTrack.EnqueueEvent((byte)(i+1), ParsecMessage.EVENTCODE_DEVICE_NOTEOFF, null, 5000, 0);

                //Length of the data of the chunk
                int chunkLength = ByteArrayToUnsignedInt(bytes, trackStartIndex+4, trackStartIndex+7);

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

                //This is for dts from ignored events
                uint ignoredDT = 0;

                while(pairStartIndex < chunkLength + trackStartIndex + 8) 
                {

                    //Here are the members of the event we will create
                    byte eventDevice = (byte)(i+1);
                    byte eventCode = 0;
                    byte[] eventData = null;
                    uint eventTime = 0;
                    uint eventSilentData = 0;
                    
                    //Set the members of the event
                    VariableLengthValue deltaRead = new VariableLengthValue(bytes, pairStartIndex);
                    eventTime = deltaRead.Value + ignoredDT;

                    //eventStartIndex is the index of the first byte of the event in the current delta time/event pair
                    int eventStartIndex = (int)(pairStartIndex + deltaRead.NumberOfBytes);

                    //If this condition is true, then it is a meta event
                    if(bytes[eventStartIndex] == 0xFF) 
                    {
                        isRunningStatus = 0;

                        //This byte holds the type of meta event
                        //It is analogous to status
                        byte type = bytes[eventStartIndex + 1];

                        //Check the type of meta event
                        if(type == 0x2F) 
                        {
                            //This is an EOT event
                            eventData = null;
                            eventCode = ParsecMessage.EVENTCODE_SILENT_EOT;
                            //Record the length of the dt/event pair
                            //EOT event is always 3 long
                            nextPairStartIndex = 3 + eventStartIndex;
                            ignoredDT = 0;
                        }
                        else if(type == 0x51) 
                        {
                            //this is a tempo meta event
                            uint tempo = (uint)(ByteArrayToUnsignedInt(bytes, eventStartIndex+3, eventStartIndex+5));
                            //int tempo = 500000;
                            eventSilentData = tempo;
                            eventCode = ParsecMessage.EVENTCODE_SILENT_TEMPO;
                            //Record the length of the dt/event pair
                            //Tempo event is always 6 long
                            nextPairStartIndex = 6 + eventStartIndex;
                            ignoredDT = 0;
                        }
                        else 
                        {
                            //This is for events we dont care about
                            VariableLengthValue variableLengthRead = new VariableLengthValue(bytes, eventStartIndex + 2);
                            //The index of ther next event pair is the sum of (in order of adding):
                            //Length of the event data
                            //Length of the Length (how many bytes were used to store the length)
                            //The current eventStartIndex
                            //2 (1 is for the byte that marks the event as meta, 1 for the type of meta event)
                            nextPairStartIndex = (int)(variableLengthRead.Value + variableLengthRead.NumberOfBytes + eventStartIndex + 2);
                            //Skip the rest of this loop
                            pairStartIndex = nextPairStartIndex;
                            ignoredDT = eventTime;
                            continue;
                        }
                    }

                    //This case is for system exclusive events, irrelevant
                    else if(bytes[eventStartIndex] == 0xF0 || bytes[eventStartIndex] == 0xF7) 
                    {
                        isRunningStatus = 0;
                        //Record the length of the dt/event pair
                        VariableLengthValue variableLengthRead = new VariableLengthValue(bytes, eventStartIndex + 2);
                        //The index of ther next event pair is the sum of (in order of adding):
                        //Length of the event data
                        //Length of the Length (how many bytes were used to store the length)
                        //The current eventStartIndex
                        //1 (for the byte that marks the event as SYSEX)
                        nextPairStartIndex = (int)(variableLengthRead.Value + variableLengthRead.NumberOfBytes + eventStartIndex + 1);
                        //Skip the rest of this loop
                        pairStartIndex = nextPairStartIndex;
                        ignoredDT = eventTime;
                        continue;
                    }

                    //If we got here, that means we have a regular midi event
                    else 
                    {

                        //Check if we are in running status
                        if(bytes[eventStartIndex] > 0x7F) 
                        {
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
                            ignoredDT = eventTime;
                            continue;
                        }

                        //If we reach this condition, then the event is a Midi Voice Message
                        //Finally the good stuff
                        else 
                        {

                            //Check the status byte
                            if((status & 0xF0) == 0x80) 
                            {   
                                //Note off message
                                eventData = null;
                                eventCode = ParsecMessage.EVENTCODE_DEVICE_NOTEOFF;
                                //Record the length of the dt/event pair
                                nextPairStartIndex = 3 + eventStartIndex - isRunningStatus;
                                isRunningStatus = 1;
                                ignoredDT = 0;
                            }
                            else if((status & 0xF0) == 0x90) 
                            {
                                //Note on message
                                byte pitchIndex = bytes[eventStartIndex + 1 - isRunningStatus];

                                int velocity = bytes[eventStartIndex + 2 - isRunningStatus];
                                if(velocity == 0) 
                                {
                                    //Velocity of zero is really a note off event to sustain running status
                                    eventData = null;
                                    eventCode = ParsecMessage.EVENTCODE_DEVICE_NOTEOFF;
                                }
                                else 
                                {
                                    eventData = new byte[1];
                                    eventData[0] = pitchIndex;
                                    eventCode = ParsecMessage.EVENTCODE_DEVICE_NOTEON;
                                }
                                //Record the length of the dt/event pair
                                nextPairStartIndex = 3 + eventStartIndex - isRunningStatus;
                                isRunningStatus = 1;
                                ignoredDT = 0;
                            }
                            else if((status & 0xF0) == 0xA0) 
                            {
                                //Polyphonic pressure
                                nextPairStartIndex = 3 + eventStartIndex;
                                isRunningStatus = 1;
                                pairStartIndex = nextPairStartIndex;
                                ignoredDT = eventTime;
                                continue;
                            }
                            else if((status & 0xF0) == 0xC0) 
                            {
                                //Program Change
                                //Record the length of the dt/event pair
                                nextPairStartIndex = 2 + eventStartIndex;
                                isRunningStatus = 1;
                                pairStartIndex = nextPairStartIndex;
                                ignoredDT = eventTime;
                                continue;
                            }
                            else if((status & 0xF0) == 0xD0) 
                            {
                                //Channel Key Pressure
                                //Record the length of the dt/event pair
                                nextPairStartIndex = 2 + eventStartIndex;
                                isRunningStatus = 1;
                                pairStartIndex = nextPairStartIndex;
                                ignoredDT = eventTime;
                                continue;
                            }
                            else if((status & 0xF0) == 0xE0) 
                            {
                                //Pitch Bend
                                //TODO This one might actually be useful at some time
                                //Record the length of the dt/event pair
                                nextPairStartIndex = 3 + eventStartIndex;
                                isRunningStatus = 1;
                                pairStartIndex = nextPairStartIndex;
                                ignoredDT = eventTime;
                                continue;
                            }
                        }
                    }
                    //Finally! Bless your soul if you got here!

                    //Now if we are here that means we got one of the handful of relevant events
                    //So we can add it to our track
                    currentTrack.EnqueueEvent(eventDevice, eventCode, eventData, eventTime, eventSilentData);
                    pairStartIndex = nextPairStartIndex;
                }
                trackStartIndex = nextPairStartIndex;
            }
        }
    }
}
