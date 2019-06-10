using System;
using System.IO;
using System.Collections.Generic;

namespace midiParsec
{
    // Sequence object, stores midi sequences as a List of tracks
    // Each track in the sequence is stored as a queue in the list
    class Sequence
    {
        //Special values for midi Decoding

        //Types of events
        private const byte MIDI_TYPE_META                 = 0xFF;
        private const byte MIDI_TYPE_SYSEX1               = 0xF0;
        private const byte MIDI_TYPE_SYSEX2               = 0xF7;

        //Types of Meta events
        private const byte MIDI_META_TEMPO                = 0x51;
        private const byte MIDI_META_EOT                  = 0x2F;

        //Types of Channel Messages, which come in 2 flavors:
        //Mode and voice
        private const byte MIDI_MODE_FLAG                 = 0xB0;
        private const byte MIDI_VOICE_NOTE_OFF            = 0x80;
        private const byte MIDI_VOICE_NOTE_ON             = 0x90;
        private const byte MIDI_VOICE_POLYPHONIC_PRESSURE = 0xA0;
        private const byte MIDI_VOICE_PROGRAM_CHANGE      = 0xC0;
        private const byte MIDI_VOICE_KEY_PRESSURE        = 0xD0;
        private const byte MIDI_VOICE_PITCH_BEND          = 0xE0;



        private List<Track>     _sequenceList;
        private int             _remainingTracks;
        private double          _clockDivision;
        private double          _usecPerTick;
        private ParsecMessage[] _currentEvents;
        private long[]          _eventStartTimes;
        private string          _fileName;

        // Class needed for parsing midi variable length values 
        private class Varival
        {
            public uint NumberOfBytes;
            public uint Value;

            
            public Varival(byte[] bytes, uint start)
            {
                NumberOfBytes = 0;
                Value         = 0;

                while(true)
                {
                    Value = (uint)((Value << 7) + (bytes[start] & 0x7F));
                    ++NumberOfBytes;
                    //If first bit is 0, we are done
                    if((bytes[start] & 0x80) != 0x80)
                    {
                        break;
                    }
                    ++start;
                }
            }
        }

        public Sequence(string fileName) {
            _fileName     = fileName;
            _sequenceList = new List<Track>();

            try
            {
                PopulateSequence();
            }
            catch (FileNotFoundException e)
            {
                throw new FileNotFoundException("\nMidi file not found...", e);
            }
            
            //Ignore the conductor track for remaining tracks
            _remainingTracks = _sequenceList.Count - 1;
            _eventStartTimes = new long[_sequenceList.Count];
            _currentEvents   = new ParsecMessage[_sequenceList.Count];
            _usecPerTick     = 500000/_clockDivision;

            //Populate the current events array by getting the first 
            //event from each track
            for(int i = 0; i < _sequenceList.Count; ++i)
            {
                _currentEvents[i] = GetNextEvent(i);
            }
        }

        public void InitializeStartTimes(long startTime)
        {
            for(int i = 0; i < _sequenceList.Count; ++i)
            {
                _eventStartTimes[i] = startTime;
            }
        }

        public void TraverseSequence(long currentTime, Arduino arduino)
        {

            for(int i = 0; i < _sequenceList.Count; ++i)
            {
                if(_currentEvents[i] == null)
                {
                    continue;
                }
                    
                
                if((currentTime - _eventStartTimes[i]) >= (_usecPerTick * _currentEvents[i].GetTime()))
                {

                    //Console.Write("Track: {0} Time:{1}  ", i, currentTime);
                    //_currentEvents[i].Print();
                    //Check if event is a "conductor event" (one the arduino doesn't need to know about, but is still an event)
                    if((_currentEvents[i].GetEventCode() & 0xF0) == 0xC0)
                    {
                        
                        if(_currentEvents[i].GetEventCode() == ParsecMessage.EC_CONDUCTOR_EOT) 
                        {
                            --_remainingTracks;
                            _currentEvents[i] = null;
                        }
                        else 
                        {
                            //Tempo change event
                            if(_currentEvents[i].GetEventCode() == ParsecMessage.EC_CONDUCTOR_TEMPO)
                            {   
                                _usecPerTick   = _currentEvents[i].GetConductorData()/_clockDivision;
                            }

                            _currentEvents[i]   = GetNextEvent(i);
                            _eventStartTimes[i] = currentTime;
                        }

                        
                    }
                    else 
                    {   
                        arduino.WriteParsecMessage(_currentEvents[i]);

                        _currentEvents[i]   = GetNextEvent(i);
                        _eventStartTimes[i] = currentTime;
                        
                    }
                }
            }
        }

        //Getters
        public int GetRemainingTracks()
        {
            return _remainingTracks;
        }

        public ParsecMessage GetNextEvent(int track)
        {
            return _sequenceList[track].DequeueEvent();
        }

        public void Print(int track) 
        {   
            _sequenceList[track].Print();
        }

        private uint ByteArrayToUnsignedInt(byte[] bytes, uint start, uint end)
        {
            if(start > end)
            {
                throw new ArgumentOutOfRangeException("Start index comes after End index");
            }

            uint sum = 0;
            for(uint i = start; i <= end; ++i)
            {
                sum = (sum * 256) + bytes[i];
            }

            return sum;
        }

        private void EnqueueConductorEvent(byte code, uint time, uint conductorData) 
        {
            _sequenceList[0].EnqueueEvent(0, code, null, time, conductorData);
        }


        //Parses the midi file and populates the TrackList
        //Calls ParseTrack helper method to parse each track
        public void PopulateSequence()
        {

            byte[] bytes       = File.ReadAllBytes(_fileName);
            //Get the number of tracks
            uint tracksToParse = ByteArrayToUnsignedInt(bytes, 10, 11);
            //Get the pulses per quarter note
            _clockDivision     = ByteArrayToUnsignedInt(bytes, 12, 13);

            //Make the conductor track
            Track conductorTrack = new Track();
            //Wait so that this track is in line with the others
            conductorTrack.EnqueueEvent(0, ParsecMessage.EC_CONDUCTOR_NULL, null, 5750, 0);
            _sequenceList.Add(conductorTrack);

            //Variable that tracks the index of the first byte of the current track
            //Starts at 14 since the header chunk is always 14 bytes long
            uint trackStartIndex = 14;


            while(_sequenceList.Count <= tracksToParse) 
            {
                ParseTrack(bytes, ref trackStartIndex);
            }

            //Insert an EOT event into the conductor track
            //This will make traversdal of the sequence easier
            //and will also prevent segfaults due to empty tracks
            EnqueueConductorEvent(ParsecMessage.EC_CONDUCTOR_EOT, 1, 0);
        }

        //Parses a track. calls Parse
        private void ParseTrack(byte[] bytes, ref uint trackStartIndex) 
        {
            //Check to make sure its a track chunk
            //These bytes spell MTrk, which indicate a track chunk
            if(bytes[trackStartIndex+0] != 0x4D || 
               bytes[trackStartIndex+1] != 0x54 ||
               bytes[trackStartIndex+2] != 0x72 ||
               bytes[trackStartIndex+3] != 0x6B) 
            {
               //If not, update trackStartIndex and return
                trackStartIndex += 8 + ByteArrayToUnsignedInt(bytes, 
                                                              trackStartIndex+4, 
                                                              trackStartIndex+7);
                return;
            }

            //Middle C (I think)
            byte[] calibrationNote = {72};

            //Make a new track to sotre events in
            Track currentTrack = new Track();
            //Add some events to the beginning for calibration and timing purposes
            currentTrack.EnqueueEvent((byte)(_sequenceList.Count), ParsecMessage.EC_DEVICE_NOTEOFF, null           , 0   , 0);
            currentTrack.EnqueueEvent((byte)(_sequenceList.Count), ParsecMessage.EC_DEVICE_NOTEON , calibrationNote, 0   , 0);
            currentTrack.EnqueueEvent((byte)(_sequenceList.Count), ParsecMessage.EC_DEVICE_NOTEOFF, null           , 750 , 0);
            currentTrack.EnqueueEvent((byte)(_sequenceList.Count), ParsecMessage.EC_DEVICE_NOTEOFF, null           , 5000, 0);

            //Length of the data of the track
            uint trackLength = ByteArrayToUnsignedInt(bytes, trackStartIndex+4, trackStartIndex+7);

            
            //Various variables used in ParseEvent
            //Start index of the event/dt pair
            uint pairStartIndex   = trackStartIndex + 8;
            //Status byte (saved for running status)
            byte status           = 0;
            //Are we in running status or not
            bool isRunningStatus  = false;
            //Store unaccounted time for later accounting
            uint ignoredTime      = 0;
            //Store message here and then decide whether or not to add it
            ParsecMessage message = new ParsecMessage();

            

            //Loop while we are still within the bounds of the track
            while(pairStartIndex < trackLength + trackStartIndex + 8)
            {
                if(ParseEvent(bytes, ref message, ref pairStartIndex, ref status, ref isRunningStatus, ref ignoredTime, ref currentTrack.CumulativeTime))
                {
                    currentTrack.EnqueueEvent(message);
                }
            }

            //Add the track to the sequence
            _sequenceList.Add(currentTrack);
            //Update trackStartIndex
            trackStartIndex = pairStartIndex;
        }

        //Parses an event
        private bool ParseEvent(byte[] bytes, ref ParsecMessage message, ref uint pairStartIndex, 
                                ref byte status, ref bool isRunningStatus, ref uint ignoredTime, ref uint cumulativeTime)
        {
            
            //Here are the members of the event we will create
            byte   eventDevice   = (byte)(_sequenceList.Count);
            byte   eventCode     = 0;
            byte[] eventData     = null;
            uint   conductorTime = 0;
            uint   conductorData = 0;
            
            //Read the dt for the current event
            Varival deltaTime    = new Varival(bytes, pairStartIndex);
            //Time for this event is the read we just did plus any ignored time
            conductorTime        = deltaTime.Value + ignoredTime;
            //Record the start of the devent in the dt/event pair
            uint eventStartIndex = pairStartIndex + deltaTime.NumberOfBytes;
            
            //If the event is a meta event
            if(bytes[eventStartIndex] == MIDI_TYPE_META)
            {
                //Meta events always end running status
                isRunningStatus = false;

                //Switch over the next byte (type of meta event)
                switch(bytes[eventStartIndex + 1]) {
                    case MIDI_META_EOT:
                        //Set eventCode appropriately
                        eventCode      = ParsecMessage.EC_CONDUCTOR_EOT;
                        //Update new pair start index
                        //EOT events are always 3 bytes long
                        pairStartIndex = 3 + eventStartIndex;
                        ignoredTime    = 0;

                        //Update message
                        message = new ParsecMessage(eventDevice,
                                                    eventCode,
                                                    null,
                                                    conductorTime,
                                                    conductorData);
                        
                        //Return true so that the message is added to the track
                        return true;
                    case MIDI_META_TEMPO:
                        //Set eventCode appropriately
                        eventCode      = ParsecMessage.EC_CONDUCTOR_TEMPO;
                        //Get the tempo data
                        conductorData  = ByteArrayToUnsignedInt(bytes, 
                                                               eventStartIndex + 3,
                                                               eventStartIndex + 5);
                        //Update new pair start index
                        //Tempo events are always 6 bytes long
                        pairStartIndex = 6 + eventStartIndex;
                        //Add the event to the conductor track
                        //5750 is the time spent calibrating
                        EnqueueConductorEvent(eventCode, cumulativeTime + conductorTime - 5750, conductorData);
                        //Technically, we are ignoring this event, since it goes in the conductor track
                        //So, we need to account for its time later
                        ignoredTime    = conductorTime;
                        //Return false becuase we handle this in the conductor track
                        return false;
                    //Default case is all meta events we dont care about
                    default:
                        //Read the length of the meta event data
                        Varival metaLength = new Varival(bytes, eventStartIndex + 2);
                        //The index of the next event pair is the sum of (in order of adding):
                        //Length of the event data
                        //Length of the Length (how many bytes were used to store the length)
                        //The current eventStartIndex
                        //2 (1 is for the byte that marks the event as meta, 1 for the type of meta event)
                        pairStartIndex     = metaLength.Value +
                                             metaLength.NumberOfBytes +
                                             eventStartIndex +
                                             2;
                        
                        //Account for this ignored event's time
                        ignoredTime        = conductorTime;
                        //Return false since we don't want to add this event to track
                        return false;
                }
            }
            //If the event is a SYSEX event we don't care
            else if(bytes[eventStartIndex] == MIDI_TYPE_SYSEX1 ||
                    bytes[eventStartIndex] == MIDI_TYPE_SYSEX2)
            {
                
                //SYSEX always cancels running status
                isRunningStatus     = false;
                //Read the length of the SYSEX data
                Varival sysexLength = new Varival(bytes, eventStartIndex + 2);
                //The index of ther next event pair is the sum of (in order of adding):
                //Length of the event data
                //Length of the Length (how many bytes were used to store the length)
                //The current eventStartIndex
                //1 (for the byte that marks the event as SYSEX)
                pairStartIndex      = sysexLength.Value +
                                      sysexLength.NumberOfBytes +
                                      eventStartIndex +
                                      1;

                //Account for the time of this ignored event
                ignoredTime         = conductorTime;
                //Return false since we don't want to add this event to track
                return false;
            }
            //Here means we are at a Channel event
            //The good stuff starts here
            else 
            {
                //First, check if we are in running status
                if(bytes[eventStartIndex] > 0x7F) {
                    //If byte is > 0x7F running status has ended
                    status          = bytes[eventStartIndex];
                    isRunningStatus = false;
                }
                //If this was skipped, it is running status
                //and the old status will do

                //We switch over the first 4 bits of the status
                //The low order 4 bits are channel number info that we dont need
                switch(status & 0xF0) {
                    case MIDI_VOICE_NOTE_OFF:
                        //Set event code appropriately
                        eventCode       = ParsecMessage.EC_DEVICE_NOTEOFF;
                        //Advance pairStartIndex
                        pairStartIndex  = eventStartIndex +
                                          3 -
                                          (uint)(isRunningStatus ? 1 : 0);

                        //Always assume running status will start
                        isRunningStatus = true;
                        ignoredTime     = 0;
                        break;
                    case MIDI_VOICE_NOTE_ON:
                        //Get velocity to check if this is really a note on event
                        
                        byte velocity = bytes[eventStartIndex + 2 - (isRunningStatus ? 1 : 0)];

                        //Note on events with velocity = 0 are actually
                        //note off events, used to utilize running status
                        if(velocity == 0)
                        {
                            //Set event code appropriately
                            eventCode = ParsecMessage.EC_DEVICE_NOTEOFF;
                        }
                        //Velcity > 0, so note on
                        else 
                        {
                            //Set event code appropriately
                            eventCode       = ParsecMessage.EC_DEVICE_NOTEON;
                            //Index where pitch data is found
                            uint pitchIndex = eventStartIndex +
                                              1 - 
                                              (uint)(isRunningStatus ? 1 : 0);

                            //Store pitch data
                            eventData       = new byte[1];
                            eventData[0]    = bytes[pitchIndex];
                        }

                        //Advance pairStartIndex
                        pairStartIndex  = eventStartIndex +
                                          3 -
                                          (uint)(isRunningStatus ? 1 : 0);

                        //Always assume running status will start
                        isRunningStatus = true;
                        ignoredTime     = 0;
                        break;
                    //We ignore every event starting here
                    case MIDI_MODE_FLAG:
                        //If we are here, then this is either a Channel Mode Message or Controller Change message
                        //None of these events are relevant
                        //All events in this grouping have a length of 3
                        pairStartIndex  = 3 + eventStartIndex - (uint)(isRunningStatus ? 1 : 0);
                        //Always assume running status will start
                        isRunningStatus = true;
                        //Account for this ignored event's time
                        ignoredTime     = conductorTime;
                        //Return false as we skip this event
                        return false;
                    case MIDI_VOICE_POLYPHONIC_PRESSURE:
                        //Length of event is 3
                        pairStartIndex  = 3 + eventStartIndex - (uint)(isRunningStatus ? 1 : 0);
                        //Always assume running status will start
                        isRunningStatus = true;
                        //Account for this ignored event's time
                        ignoredTime     = conductorTime;
                        return false;
                    case MIDI_VOICE_PROGRAM_CHANGE:
                        //Length of event is 2
                        pairStartIndex  = 2 + eventStartIndex - (uint)(isRunningStatus ? 1 : 0);
                        //Always assume running status will start
                        isRunningStatus = true;
                        //Account for this ignored event's time
                        ignoredTime     = conductorTime;
                        return false;
                    case MIDI_VOICE_KEY_PRESSURE:
                        //Length of event is 2
                        pairStartIndex  = 2 + eventStartIndex - (uint)(isRunningStatus ? 1 : 0);
                        //Always assume running status will start
                        isRunningStatus = true;
                        //Account for this ignored event's time
                        ignoredTime     = conductorTime;
                        return false;
                    case MIDI_VOICE_PITCH_BEND:
                        //Length of event is 3
                        pairStartIndex  = 3 + eventStartIndex - (uint)(isRunningStatus ? 1 : 0);
                        //Always assume running status will start
                        isRunningStatus = true;
                        //Account for this ignored event's time
                        ignoredTime     = conductorTime;
                        return false;
                    default:
                        throw new InvalidOperationException("Unknown midi event encountered");
                }

                //Instantiate message to contain the info we parsed
                message = new ParsecMessage(eventDevice, 
                                            eventCode, 
                                            eventData, 
                                            conductorTime, 
                                            conductorData);
                return true;
            }
        }
    }
}
