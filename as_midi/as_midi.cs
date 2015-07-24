using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Xml;
using System.Collections;

using System.Diagnostics;
using System.ComponentModel;

namespace as_midi
{
    class as_midi
    {
        const double TAU = 2 * Math.PI;
        const double samplesSecond = 11025.0;
        const double sineInterval = 1;
        const int maxSamples = 11025 * 30; // 30 seconds max

        public static class GlobalVar
        {
            public static int eventsThisRun = 1000 * 16 * 1;
            public static int featureCount = (5 * eventsThisRun);
            public static int[] features = new int[350000];
            public static string arg0 = "";
            public static long[] leftmono = new long[maxSamples];
            public static int samples = 0;

            public static double[] runningWave = new double[maxSamples];
            public static long[] calcWave = new long[maxSamples];
            public static long[] diffWave = new long[maxSamples];

            public static int[] MIDIdelta = new int[eventsThisRun];
            public static int[] MIDItype = new int[eventsThisRun];
            public static int[] MIDIchannel = new int[eventsThisRun];
            public static int[] MIDIdata1 = new int[eventsThisRun];
            public static int[] MIDIdata2 = new int[eventsThisRun];
            public static bool[] MIDIvalid = new bool[eventsThisRun];
            public static bool[] MIDIwritten = new bool[eventsThisRun];


            public static int[] levelOffset = new int[eventsThisRun];
            public static int[] levelFrequency = new int[eventsThisRun];
            public static double[] levelFD = new double[eventsThisRun];
            public static int[] levelAmplitude = new int[eventsThisRun];
            public static int[] levelDirection = new int[eventsThisRun];
            public static int[] frameFirstSample = new int[eventsThisRun];
            public static int[] frameLastSample = new int[eventsThisRun];
            public static long[] frameScore = new long[eventsThisRun];
            public static bool[] frameActive = new bool[eventsThisRun];
            public static double[] freqLookup = new double[256 * 16];
            public static long[] sampleDiff = new long[maxSamples];
            public static int[,] frameSamples = new int[eventsThisRun, 410];
            public static bool[] noMore = new bool[64];

            public static int myGeneration = 0;
            public static long myScore = 0;
            public static long bestScore = 0;
            public static Random random = new Random();
            public static int soundPos = 0;
            public static int popMember = 0;
            public static int activeFeatures = 0;
            public static double lowFreq = 8000;
            public static double highFreq = 0;
            public static int lowAmp = 40000;
            public static int highAmp = 0;
            public static int lowFreqW = 8000;
            public static int highFreqW = 0;
            public static int lowAmpW = 40000;
            public static int highAmpW = 0;

            public static int highCtr = 0;
            public static double highAmpNote = 0;
            public static int scoreAll = 0;
            public static int scoreCount = 0;
            public static long worstScore = 0;
            public static int worstNDX = 0;
            public static long worstFrame = 0;
            public static long potentialDiff = 0;
            public static int lastLength = 0;
            public static int mostSamples = 0;
            public static bool allFrames = false;
            public static int startFrame = 0;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("1");
            System.Threading.Thread.Sleep(5000);
            string XMLfile = "test78.xml";
            openWav("target.wav"); 
            int nextApply = -1;
            bool someLeft = true;
            Random random = new Random();
            Console.WriteLine("2");
            System.Threading.Thread.Sleep(5000);

            for (int tx = 0; tx < 64; tx++)
                GlobalVar.noMore[tx] = false;

            if (args.Length > 0)
            {
                XMLfile = args[0];
                GlobalVar.arg0 = args[0];
            }

            if (String.IsNullOrEmpty(XMLfile))
            {
                return;
            }
            Console.WriteLine("3");
            System.Threading.Thread.Sleep(5000);

            bool openXML = false;
            int XMLTry = 0;
            while (!openXML)
            {
                XMLTry++;
                try
                {
                    openXML = true;
                    ImportXMLfile(XMLfile);
                }
                catch
                {
                    System.Threading.Thread.Sleep(10);
                    openXML = false;
                }
                if (XMLTry > 5)
                {
                    return;
                }
            }

            Console.WriteLine("4");
            System.Threading.Thread.Sleep(5000);

      //      if (!GetExistingCharacteristics(GlobalVar.popMember)) 
      //      {
                //    Console.WriteLine("input error");
      //          ExportXMLfile(XMLfile);
      //          return;
      //      }

            for (int frameX = 0; frameX < GlobalVar.eventsThisRun; frameX++)
            {
                GlobalVar.frameActive[frameX] = false;
            }

            Console.WriteLine("5");
            System.Threading.Thread.Sleep(5000);

            for (int i = 0; i < GlobalVar.samples; i++)
            {
                GlobalVar.sampleDiff[i] = (2 * Math.Abs(GlobalVar.leftmono[i]));
                GlobalVar.potentialDiff = GlobalVar.potentialDiff + GlobalVar.sampleDiff[i];  // worst is mirror
            }

            Console.WriteLine("6");
            System.Threading.Thread.Sleep(5000);

            //at this point we have the target wav file, and the XML of our MIDI file

            BuildMIDIFile();

            // build parameters
            // call external program
            RenderMIDIToWav();

            // at this point I have both wav files, and the rest of the process should be identical

            GlobalVar.myScore = GlobalVar.potentialDiff - AlternateScore(0, GlobalVar.samples);
            //       Console.WriteLine("score " + GlobalVar.myScore.ToString());

            if (GlobalVar.myScore > GlobalVar.bestScore)
            //            if (GlobalVar.myScore > -999999)
            {
                WriteBestFile();
            }

            WriteScoreFile();

            // write out xml
            bool outputXML = false;
            int XMLOut = 0;
            while (!outputXML)
            {
                XMLOut++;
                try
                {
                    outputXML = true;
                    if (ExportXMLfile(XMLfile) > 0)
                    {
                        outputXML = false;
                        System.Threading.Thread.Sleep(10);
                    }
                }
                catch
                {
                    System.Threading.Thread.Sleep(10);
                    outputXML = false;
                }
                if (XMLOut > 5)
                    return;
            }

            ExportXMLfile(XMLfile);

        }


        static void BuildMIDIFile()
        {
            // GetExistingCharacteristics should already have turned XML into MIDI events
            // need to sort all events based on time (any event can have any time)
            // New routine to write header and all events into tracks
            // Have a valid MIDI file by the end of this

            // open output midi file

            for (int eventX = 0; eventX < GlobalVar.eventsThisRun; eventX++)
            {
                GlobalVar.MIDIvalid[eventX] = true;
                GlobalVar.MIDIwritten[eventX] = false;
            }

            byte[] buildMIDI = new byte[GlobalVar.featureCount];
            int buildNDX = 0;

            byte[] MThd = new byte["MThd".Length * sizeof(char)];
            System.Buffer.BlockCopy("MThd".ToCharArray(), 0, MThd, 0, MThd.Length);
            System.Buffer.BlockCopy(MThd, 0, buildMIDI, 0, MThd.Length);
            buildNDX += MThd.Length;

            buildMIDI[buildNDX] = 0;buildNDX++;
            buildMIDI[buildNDX] = 0; buildNDX++;
            buildMIDI[buildNDX] = 0; buildNDX++;
            buildMIDI[buildNDX] = 6; buildNDX++;

            buildMIDI[buildNDX] = 0; buildNDX++;
            buildMIDI[buildNDX] = 0; buildNDX++;

            buildMIDI[buildNDX] = 0; buildNDX++;
            buildMIDI[buildNDX] = 1; buildNDX++;

            buildMIDI[buildNDX] = (256-25); buildNDX++;
            buildMIDI[buildNDX] = 40; buildNDX++;

            bool noMoreEvents = false;


            while (noMoreEvents) 
            {
                int nextDelta = 256 * 256;
                int nextEvent = -1;
                for (int eventX = 0; eventX < GlobalVar.eventsThisRun; eventX++)
                {
                    if ((GlobalVar.MIDIdelta[eventX] < nextDelta) && (GlobalVar.MIDIvalid[eventX]) && (!GlobalVar.MIDIwritten[eventX]))
                    {
                        nextEvent = eventX;
                        nextDelta = GlobalVar.MIDIdelta[eventX];
                    }
                }
                if (nextEvent > -1)
                {
                    GlobalVar.MIDIwritten[nextEvent] = true;
                    while (GlobalVar.MIDIdelta[nextEvent] > 127)
                    {
                        buildMIDI[buildNDX] = 0; buildNDX++;

                    }
                    buildMIDI[buildNDX] = 0; buildNDX++;
                }
                else
                {
                    noMoreEvents = true;
                }
            }

            byte[] midiValues = new byte[buildNDX];

            Array.Copy(buildMIDI, midiValues, buildNDX);

            string fn = Convert.ToString(GlobalVar.popMember) + ".midi";

            if (File.Exists(fn))
            {
                File.Delete(fn);
            }

            File.WriteAllBytes(fn, midiValues);

            // write midi header

            // loop through features - write an event if valid

            // close file

        }

        static void RenderMIDIToWav()
        {

            Process midiProcess = new Process();
            String midiFile = Convert.ToString(GlobalVar.popMember) + ".midi";
            String wavFile = Convert.ToString(GlobalVar.popMember) + ".wav";

            // call Fluidity passing new MIDI file
            // import new WAV file into
            // can rewrite openWav() to read either target or new 
            // can write this part first and used a pre-created MIDI file

            midiProcess = new Process();

            midiProcess.StartInfo.CreateNoWindow = true;
            midiProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            midiProcess.StartInfo.FileName = "fluidsynth.exe";
            midiProcess.StartInfo.Arguments = " " + midiFile + " " + wavFile;
            midiProcess.Start();
            midiProcess.WaitForExit();
            midiProcess.Dispose();
        }

        static long AlternateScore(int startX, int endX)
        {
            long runningScore = 0;

            for (int i = startX; i < endX; i++)
            {
                runningScore = runningScore + (Math.Abs(GlobalVar.leftmono[i] - GlobalVar.calcWave[i]));
            }
            return (runningScore);
        }

        static void WriteBestFile()
        {
            if (GlobalVar.myScore < 1)
                return;

            if (GlobalVar.random.Next(0, 1000) < 100) // dsm
            {
                byte[] best = File.ReadAllBytes("target.wav");
                byte[] diff = File.ReadAllBytes("target.wav");
                long i = 0;
                long pos = GlobalVar.soundPos;
                long smallInt = 0;
                long bigint = 0;

                while (i < (GlobalVar.samples))
                {
                    GlobalVar.diffWave[i] = GlobalVar.leftmono[i] - GlobalVar.calcWave[i];

                    if (GlobalVar.diffWave[i] < 0)
                    {
                        GlobalVar.diffWave[i] = GlobalVar.diffWave[i] + (256 * 256);
                    }
                    if (GlobalVar.diffWave[i] >= (256 * 256))
                    {
                        GlobalVar.diffWave[i] = (256 * 256) - 1;
                    }
                    if (GlobalVar.calcWave[i] < 0)
                    {
                        GlobalVar.calcWave[i] = GlobalVar.calcWave[i] + (256 * 256);
                    }
                    if (GlobalVar.calcWave[i] >= (256 * 256))
                    {
                        GlobalVar.calcWave[i] = (256 * 256) - 1;
                    }
                    bigint = GlobalVar.calcWave[i] / (256);
                    smallInt = GlobalVar.calcWave[i] - (256 * bigint);

                    best[pos] = Convert.ToByte(smallInt);
                    best[pos + 1] = Convert.ToByte(bigint);

                    bigint = GlobalVar.diffWave[i] / (256);
                    smallInt = GlobalVar.diffWave[i] - (256 * bigint);

                    //     bigint = GlobalVar.leftmono[i] / (256);
                    //      smallInt = GlobalVar.leftmono[i] - (256 * bigint);

                    diff[pos] = Convert.ToByte(smallInt);
                    diff[pos + 1] = Convert.ToByte(bigint);

                    pos = pos + 2;
                    i++;
                }

                string fn = "B" + GlobalVar.myScore.ToString() + ".wav";

                if (File.Exists(fn))
                {
                    File.Delete(fn);
                }

                File.WriteAllBytes(fn, best);

                fn = "D" + GlobalVar.myScore.ToString() + ".wav";

                if (File.Exists(fn))
                {
                    File.Delete(fn);
                }

                File.WriteAllBytes(fn, diff);

                fn = "B" + GlobalVar.myScore.ToString() + ".xml";
                ExportXMLfile(fn);

                fn = "B" + GlobalVar.myScore.ToString() + ".csv";
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(fn))
                {
                    string line = "frame, active, freq, amp, phase, direction, weight";
                    file.WriteLine(line);
                    for (int frameX = 0; frameX < GlobalVar.eventsThisRun; frameX++)
                    {
                        if (GlobalVar.frameActive[frameX])
                        {
                            line = frameX.ToString() + ",";
                            line = line + GlobalVar.frameActive[frameX].ToString() + ",";
                            line = line + GlobalVar.levelFD[frameX].ToString() + ",";
                            line = line + GlobalVar.levelAmplitude[frameX].ToString() + ",";
                            line = line + GlobalVar.levelOffset[frameX].ToString() + ",";
                            line = line + GlobalVar.levelDirection[frameX].ToString();
                            file.WriteLine(line);
                        }
                    }
                }
            }
        }

        static void WriteScoreFile()
        {
            string fn = "sx" + Convert.ToString(GlobalVar.popMember);
            BinaryWriter scoreFile = new BinaryWriter(File.Open(fn, FileMode.Create));

            for (int fx = 0; fx < GlobalVar.eventsThisRun; fx++)
            {
                scoreFile.Write(Convert.ToInt32(GlobalVar.frameScore[fx]));
            }
            scoreFile.Close();
        }

        static int ExportXMLfile(string XMLfile)
        {
            XmlTextWriter xml;
            string filename = XMLfile;

            if (File.Exists(XMLfile))
            {
                try
                {
                    File.Delete(XMLfile);
                }
                catch
                {
                    return (1);
                }
            }

            try
            {
                xml = new XmlTextWriter(filename, null);
            }
            catch
            {
                return (1);
            }

            try
            {
                if (File.Exists(XMLfile))
                {
                    xml.WriteStartDocument();
                    xml.WriteStartElement("Features");
                    xml.WriteWhitespace("\n");

                    xml.WriteElementString("Score", GlobalVar.myScore.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("Best", GlobalVar.bestScore.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("Pop", GlobalVar.popMember.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("Generation", GlobalVar.myGeneration.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("Active", GlobalVar.activeFeatures.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("LowFreq", GlobalVar.lowFreq.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("LowFreqW", GlobalVar.lowFreqW.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("HighFreq", GlobalVar.highFreq.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("HighFreqW", GlobalVar.highFreqW.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("LowAmp", GlobalVar.lowAmp.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("LowAmpW", GlobalVar.lowAmpW.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("HighAmp", GlobalVar.highAmp.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("HighAmpW", GlobalVar.highAmpW.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("HighNote", GlobalVar.highAmpNote.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("SuperHighs", GlobalVar.highCtr.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("ScoreAll", GlobalVar.scoreAll.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("ScoreCount", GlobalVar.scoreCount.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("samples", GlobalVar.samples.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("frames", GlobalVar.eventsThisRun.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("features", GlobalVar.featureCount.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("worstscore", GlobalVar.worstScore.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteElementString("worstndx", GlobalVar.worstNDX.ToString());
                    xml.WriteWhitespace("\n  ");

                    xml.WriteEndElement();
                    xml.WriteWhitespace("\n");

                    xml.WriteEndDocument();
                }

            }
            catch
            {
                return (1);
            }

            try
            {
                //Write the XML to file and close the writer.
                if (File.Exists(XMLfile))
                {
                    xml.Flush();
                    xml.Close();
                }
            }
            catch
            {
                return (1);
            }

            return (0);
        }

        static void ImportXMLfile(string XMLfile)
        {

            if (XMLfile.Equals("solution.xml"))
                return;
            string elementString = "";

            System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(XMLfile);

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // The node is an element.
                        elementString = reader.Name;
                        break;
                    case XmlNodeType.Text: //Display the text in each element.

                        if (elementString.Equals("Generation"))
                        {
                            GlobalVar.myGeneration = Convert.ToInt32(reader.Value);
                        }
                        if (elementString.Equals("Best"))
                        {
                            GlobalVar.bestScore = Convert.ToInt64(reader.Value);
                        }
                        if (elementString.Equals("Pop"))
                        {
                            GlobalVar.popMember = Convert.ToInt32(reader.Value);
                        }
                        break;
                    case XmlNodeType.EndElement: //Display the end of the element.
                        break;
                }

            }
            reader.Close();
        }

        static bool GetExistingCharacteristics(int popMember)
        {
            char[] buildChars;
            buildChars = new char[350000];
            string featureString = "";
            string fn = "";
            int zeroCount = 0;
            bool zeroString = false;
            try
            {
                fn = "mx" + Convert.ToString(popMember);

                featureString = File.ReadAllText(fn);

                buildChars = featureString.ToCharArray();

                Array.Resize(ref buildChars, GlobalVar.featureCount);

                for (int i = 0; i < GlobalVar.featureCount; i++)
                {

                    GlobalVar.features[i] = buildChars[i];

                    if (GlobalVar.features[i] > 255)
                        GlobalVar.features[i] = 255;
                    if (GlobalVar.features[i] < 0)
                        GlobalVar.features[i] = 0;
                    if (!GlobalVar.features[i].Equals(0))
                        zeroCount = 0;
                    if ((GlobalVar.features[i].Equals(0)) && (zeroString))
                        zeroCount++;
                    if (zeroCount > 300)
                    {
                        GlobalVar.myScore = 0;
                        return (false);
                    }
                    zeroString = false;
                    if (GlobalVar.features[i].Equals(0))
                        zeroString = true;
                }
                AssignToParamaters();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return (false);
            }
            return (true);
        }

        static void AssignToParamaters()
        {
            int MIDISize = 5;

            for (int i = 0; i < GlobalVar.eventsThisRun; i++)
            {

                GlobalVar.MIDIdelta[i] = GlobalVar.features[(i * MIDISize)] + (256 * GlobalVar.features[1 + (i * MIDISize)]);

                GlobalVar.MIDItype[i] = GlobalVar.features[1 + (i * MIDISize)];
                if (GlobalVar.MIDItype[i] >= (256 * 128))
                {
                    GlobalVar.levelOffset[i] = GlobalVar.levelOffset[i] + 1;
                    GlobalVar.levelFrequency[i] = GlobalVar.levelFrequency[i] - (256 * 128);
                }

    //        public static int[] MIDItype = new int[eventsThisRun];
    //        public static int[] MIDIdata1 = new int[eventsThisRun];
    //        public static int[] MIDIdata2 = new int[eventsThisRun];
//            public static int[] MIDItype = new int[eventsThisRun];
//            public static int[] MIDIchannel = new int[eventsThisRun];
//            public static int[] MIDIdata1 = new int[eventsThisRun];
//            public static int[] MIDIdata2 = new int[eventsThisRun];
//            public static bool[] MIDIvalid = new bool[eventsThisRun];
//            public static bool[] MIDIwritten = new bool[eventsThisRun];


   //             GlobalVar.levelOffset[i] = 0;

   //             GlobalVar.levelFrequency[i] = GlobalVar.features[(i * waveSize)] + (256 * GlobalVar.features[1 + (i * waveSize)]);

   //             if (GlobalVar.levelFrequency[i] >= (256 * 128))
   //             {
   //                 GlobalVar.levelOffset[i] = GlobalVar.levelOffset[i] + 1;
   //                 GlobalVar.levelFrequency[i] = GlobalVar.levelFrequency[i] - (256 * 128);
   //             }
   //             if (GlobalVar.levelFrequency[i] >= (256 * 64))
   //             {
   //                 GlobalVar.levelOffset[i] = GlobalVar.levelOffset[i] + 2;
   //                 GlobalVar.levelFrequency[i] = GlobalVar.levelFrequency[i] - (256 * 64);
   //             }

            }
        }

        static long bytesToInteger(byte firstByte, byte secondByte)
        {
            long r = 0;
            // convert two bytes to one short (little endian)
            long s = (Convert.ToInt32(secondByte) * 256) + Convert.ToInt32(firstByte);
            // convert to range from -1 to (just below) 1

            r = s;

            if (s > ((128 * 256) - 1))
            {
                r = s - (256 * 256);
            }

            return r;
        }

        // Returns left and right double arrays. 'right' will be null if sound is mono.
        static void openWav(string filename)
        {
            byte[] wav = File.ReadAllBytes(filename);

            // Determine if mono or stereo
            int channels = wav[22];     // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels

            // Get past all the other sub chunks to get to the data subchunk:
            int pos = 12;   // First Subchunk ID from 12 to 16

            // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
            while (!(wav[pos] == 100 && wav[pos + 1] == 97 && wav[pos + 2] == 116 && wav[pos + 3] == 97))
            {
                pos += 4;
                int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
                pos += 4 + chunkSize;
            }
            //      pos += 8;

            pos += 4;
            int wavSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
            pos += 4;

            GlobalVar.soundPos = pos;

            // Pos is now positioned to start of actual sound data.
            GlobalVar.samples = (wavSize) / 2;     // more accurate, get actual chunk size

            // here can set frame size etc.

            GlobalVar.eventsThisRun = 1000 * 16 * 1;
            GlobalVar.featureCount = (5 * GlobalVar.eventsThisRun);

            GlobalVar.leftmono = new long[GlobalVar.samples];

            // Write to double array/s:
            int i = 0;

            // pos++;

            while (i < (GlobalVar.samples))
            {
                GlobalVar.leftmono[i] = bytesToInteger(wav[pos], wav[pos + 1]);
                //                Console.WriteLine(GlobalVar.soundPos.ToString() + " " + pos.ToString() + " " +
                //                    i.ToString() + " " + wav[pos] + " " + wav[pos + 1] + " " + GlobalVar.leftmono[i].ToString());
                //                System.Threading.Thread.Sleep(5);
                pos += 2;

                i++;
            }
        }

    }
}
