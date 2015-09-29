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

namespace as_rtcmix
{
    class as_rtcmix
    {

        const double samplesSecond = 44100.0;
        const int maxSamples = 44100 * 500; // 30 seconds max
        const int scoreFrames = 8;

        public static class GlobalVar
        {
            public static int endTime = 0;
            public static int eventsThisRun = 10000;
            public static int featureCount = (7 * eventsThisRun);
            public static int[] features = new int[350000];
            public static int[] copyFeatures = new int[350000];
            public static string arg0 = "";
            public static long[] targetWav = new long[maxSamples];
            public static int samples = 0;

            public static double[] runningWave = new double[maxSamples];
            public static long[] calcWav = new long[maxSamples];
            public static long[] diffWav = new long[maxSamples];

            //            public static int[] MIDIdelta = new int[eventsThisRun * 2];
            //            public static int[] MIDIduration = new int[eventsThisRun * 2];
            //            public static int[] MIDItypechannel = new int[eventsThisRun * 2];
            //            public static int[] MIDIdata1 = new int[eventsThisRun * 2];
            //            public static int[] MIDIdata2 = new int[eventsThisRun * 2];
            //            public static bool[] scoreValid = new bool[eventsThisRun * 2];
            //            public static bool[] MIDIwritten = new bool[eventsThisRun * 2];

            public static long[] frameScore = new long[eventsThisRun];
            public static long[] sampleDiff = new long[maxSamples];
            public static bool wavErr = false;

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

            string XMLfile = "test89.xml";
            GlobalVar.targetWav = openWav("target.wav");
            Random random = new Random();

            if (args.Length > 0)
            {
                XMLfile = args[0];
                GlobalVar.arg0 = args[0];
            }

            if (String.IsNullOrEmpty(XMLfile))
            {
                return;
            }

            try
            {
                WholeProcess(XMLfile);
            }
            catch
            {
                return;
            }

        }

        static void WholeProcess(string XMLfile)
        {

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

            if (!GetExistingCharacteristics(GlobalVar.popMember))
            {
                Console.WriteLine("input error");
                ExportXMLfile(XMLfile);
                return;
            }

            for (int i = 0; i < GlobalVar.samples; i++)
            {
                GlobalVar.sampleDiff[i] = (2 * Math.Abs(GlobalVar.targetWav[i]));
                GlobalVar.potentialDiff = GlobalVar.potentialDiff + GlobalVar.sampleDiff[i];  // worst is mirror
            }

            BuildScoreFile();

            RenderScoreToWav();

            GlobalVar.calcWav = openWav(Convert.ToString(GlobalVar.popMember) + ".wav");

            if (GlobalVar.wavErr)
            {
                return;
            }

            // at this point I have both wav files, and the rest of the process should be identical

            GlobalVar.myScore = (GlobalVar.potentialDiff - AlternateScore(0, GlobalVar.samples)) + TotalSound(0, GlobalVar.samples);
            Console.WriteLine("myScore: " + GlobalVar.myScore + " " + GlobalVar.samples + " " + GlobalVar.potentialDiff);

            if (GlobalVar.myScore > GlobalVar.bestScore)
            {
                //x          WriteBestFile();
            }

            // there is nothing in the framescore array
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

            char[] buildChars;
            buildChars = new char[350000];

            for (int i = 0; i < GlobalVar.featureCount; i++)
                buildChars[i] = (char)GlobalVar.copyFeatures[i];

            string bs = new string(buildChars);
            bs = bs.Substring(0, GlobalVar.featureCount);

            string fn = "";
            fn = "mx" + Convert.ToString(GlobalVar.popMember);
            File.WriteAllText(fn, bs);
        }

        static void BuildScoreFile()
        {
            // GetExistingCharacteristics should already have turned XML into MIDI events
            // need to sort all events based on time (any event can have any time)
            // New routine to write header and all events into tracks
            // Have a valid MIDI file by the end of this

            // open output midi file

            byte[] buildScore = new byte[GlobalVar.featureCount];
            int buildNDX = 0;
            int lastDelta = 0;

            buildScore[buildNDX] = 77; buildNDX++;
            buildScore[buildNDX] = 84; buildNDX++;
            buildScore[buildNDX] = 104; buildNDX++;
            buildScore[buildNDX] = 100; buildNDX++;

            buildScore[buildNDX] = 0; buildNDX++;
            buildScore[buildNDX] = 0; buildNDX++;
            buildScore[buildNDX] = 0; buildNDX++;
            buildScore[buildNDX] = 6; buildNDX++;

            buildScore[buildNDX] = 0; buildNDX++;
            buildScore[buildNDX] = 0; buildNDX++;

            buildScore[buildNDX] = 0; buildNDX++;
            buildScore[buildNDX] = 1; buildNDX++;

            //            buildScore[buildNDX] = (256-25); buildNDX++; // failed attempt at SMPTE timing
            //            buildScore[buildNDX] = 40; buildNDX++;

            buildScore[buildNDX] = 2; buildNDX++;
            buildScore[buildNDX] = 213; buildNDX++; // trying to get 1000 ticks per second

            // write MIDI track header
            buildScore[buildNDX] = 77; buildNDX++;
            buildScore[buildNDX] = 84; buildNDX++;
            buildScore[buildNDX] = 114; buildNDX++;
            buildScore[buildNDX] = 107; buildNDX++;

            // just holding space,track length will be entered when it is known
            int MIDItrackLocation = buildNDX;
            buildScore[buildNDX] = 0; buildNDX++;
            buildScore[buildNDX] = 0; buildNDX++;
            buildScore[buildNDX] = 0; buildNDX++;
            buildScore[buildNDX] = 0; buildNDX++;

            bool MoreEvents = true;
            int trackBytes = 0;
            int copyX = 0;

            //loop through events
            while (MoreEvents)
            {
                int nextDelta = 256 * 256;
                int nextEvent = -1;
                for (int eventX = 0; eventX < (2 * GlobalVar.eventsThisRun); eventX++)
                {
                    if ((GlobalVar.MIDIdelta[eventX] < nextDelta) && (!GlobalVar.MIDIwritten[eventX]))
                    {
                        nextEvent = eventX;
                        nextDelta = GlobalVar.MIDIdelta[eventX];
                    }
                }

                if (nextEvent < 0)
                {
                    MoreEvents = false;
                }

                if (nextEvent > -1)
                {
                    GlobalVar.MIDIwritten[nextEvent] = true;
                    GlobalVar.copyFeatures[copyX + 1] = GlobalVar.MIDIdelta[nextEvent] / 256;
                    GlobalVar.copyFeatures[copyX] = GlobalVar.MIDIdelta[nextEvent] - (GlobalVar.copyFeatures[copyX + 1] / 256);
                    GlobalVar.copyFeatures[copyX + 3] = GlobalVar.MIDIduration[nextEvent] / 256; copyX++;
                    GlobalVar.copyFeatures[copyX + 2] = GlobalVar.MIDIduration[nextEvent] - (GlobalVar.copyFeatures[copyX + 3] / 256);
                    GlobalVar.copyFeatures[copyX + 4] = GlobalVar.MIDItypechannel[nextEvent];
                    GlobalVar.copyFeatures[copyX + 5] = GlobalVar.MIDIdata1[nextEvent];
                    GlobalVar.copyFeatures[copyX + 6] = GlobalVar.MIDIdata2[nextEvent];

                    if (GlobalVar.scoreValid[nextEvent])
                    {
                        int workDelta = GlobalVar.MIDIdelta[nextEvent] - lastDelta;
                        int tempDelta = 0;
                        Console.WriteLine("workDelta: " + workDelta + " " + tempDelta);

                        if (workDelta >= (128 * 128 * 128))
                        {
                            tempDelta = workDelta / (128 * 128 * 128);
                            buildScore[buildNDX] = Convert.ToByte(128 + tempDelta); buildNDX++; trackBytes++;
                            workDelta = workDelta - (tempDelta * 128 * 128 * 128);
                            Console.WriteLine("workDelta: " + workDelta + " " + tempDelta);
                        }
                        if (workDelta >= (128 * 128))
                        {
                            tempDelta = workDelta / (128 * 128);
                            buildScore[buildNDX] = Convert.ToByte(128 + tempDelta); buildNDX++; trackBytes++;
                            workDelta = workDelta - (tempDelta * 128 * 128);
                            Console.WriteLine("workDelta: " + workDelta + " " + tempDelta);
                        }
                        if (workDelta >= 128)
                        {
                            tempDelta = workDelta / 128;
                            buildScore[buildNDX] = Convert.ToByte(128 + tempDelta); buildNDX++; trackBytes++;
                            workDelta = workDelta - (tempDelta * 128);
                            Console.WriteLine("workDelta: " + workDelta + " " + tempDelta);
                        }
                        buildScore[buildNDX] = Convert.ToByte(workDelta); buildNDX++; trackBytes++;
                        Console.WriteLine("workDelta: " + workDelta + " " + tempDelta);

                        lastDelta = GlobalVar.MIDIdelta[nextEvent];

                        copyX = copyX + 7;

                        buildScore[buildNDX] = Convert.ToByte(GlobalVar.MIDItypechannel[nextEvent]); buildNDX++; trackBytes++;
                        //                 Console.WriteLine("MIDItypechannel: " + GlobalVar.MIDItypechannel[nextEvent]);

                        if (GlobalVar.MIDIdata1[nextEvent] > 127)
                            GlobalVar.MIDIdata1[nextEvent] = GlobalVar.MIDIdata1[nextEvent] - 128;
                        buildScore[buildNDX] = Convert.ToByte(GlobalVar.MIDIdata1[nextEvent]); buildNDX++; trackBytes++;
                        //                 Console.WriteLine("MIDIdata1: " + GlobalVar.MIDIdata1[nextEvent]);
                        if (GlobalVar.MIDIdata2[nextEvent] > 127)
                            GlobalVar.MIDIdata2[nextEvent] = GlobalVar.MIDIdata2[nextEvent] - 128;
                        if ((GlobalVar.MIDItypechannel[nextEvent] < 192) || (GlobalVar.MIDItypechannel[nextEvent] > 223))
                        {
                            buildScore[buildNDX] = Convert.ToByte(GlobalVar.MIDIdata2[nextEvent]); buildNDX++; trackBytes++;
                            //                 Console.WriteLine("MIDIdata2: " + GlobalVar.MIDIdata2[nextEvent]);
                        }
                    }
                }
            }

            // EOF marker
            buildScore[buildNDX] = 0; buildNDX++; trackBytes++;
            buildScore[buildNDX] = 255; buildNDX++; trackBytes++;
            buildScore[buildNDX] = 47; buildNDX++; trackBytes++;
            buildScore[buildNDX] = 0; buildNDX++; trackBytes++;

            buildScore[MIDItrackLocation + 0] = 0;
            buildScore[MIDItrackLocation + 1] = 0;
            buildScore[MIDItrackLocation + 2] = 0;
            buildScore[MIDItrackLocation + 3] = 0;

            int tempBytes = 0;

            Console.WriteLine("trackBytes: " + trackBytes + " " + tempBytes);

            if (trackBytes > 255)
            {
                tempBytes = trackBytes / 256;
                buildScore[MIDItrackLocation + 2] = Convert.ToByte(tempBytes);
                trackBytes = trackBytes - (tempBytes * 256);
                Console.WriteLine("trackBytes: " + trackBytes + " " + tempBytes);
            }
            buildScore[MIDItrackLocation + 3] = Convert.ToByte(trackBytes);
            Console.WriteLine("trackBytes: " + trackBytes + " " + tempBytes);

            byte[] scoreValues = new byte[buildNDX];

            Array.Copy(buildScore, scoreValues, buildNDX);

            string fn = Convert.ToString(GlobalVar.popMember) + ".sco";

            if (File.Exists(fn))
            {
                File.Delete(fn);
            }

            File.WriteAllBytes(fn, scoreValues);

            // write midi header

            // loop through features - write an event if valid

            // close file

        }

        static void RenderScoreToWav()
        {
            Process scoreProcess = new Process();
            String scoreFile = Convert.ToString(GlobalVar.popMember) + ".sco";
            String wavFile = Convert.ToString(GlobalVar.popMember) + ".wav"; // wav file name is buried in score file

            scoreProcess = new Process();

            scoreProcess.StartInfo.CreateNoWindow = true;
            scoreProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            scoreProcess.StartInfo.FileName = "./RTCmix/bin/CMIX";
            scoreProcess.StartInfo.Arguments = " < " + scoreFile + " ";
            scoreProcess.Start();
            scoreProcess.WaitForExit();
            System.Threading.Thread.Sleep(500);
            scoreProcess.Dispose();
        }


        static long AlternateScore(int startX, int endX)
        {
            long runningScore = 0;

            for (int i = startX; i < endX; i++)
            {
                runningScore = runningScore + (Math.Abs(GlobalVar.targetWav[i] - GlobalVar.calcWav[i]));
            }
            return (runningScore);
        }

        static long TotalSound(int startX, int endX)
        {
            long runningScore = 0;

            for (int i = startX; i < endX; i++)
            {
                runningScore = runningScore + (Math.Abs(GlobalVar.calcWav[i]));
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
                    GlobalVar.diffWav[i] = GlobalVar.targetWav[i] - GlobalVar.calcWav[i];

                    if (GlobalVar.diffWav[i] < 0)
                    {
                        GlobalVar.diffWav[i] = GlobalVar.diffWav[i] + (256 * 256);
                    }
                    if (GlobalVar.diffWav[i] >= (256 * 256))
                    {
                        GlobalVar.diffWav[i] = (256 * 256) - 1;
                    }
                    if (GlobalVar.calcWav[i] < 0)
                    {
                        GlobalVar.calcWav[i] = GlobalVar.calcWav[i] + (256 * 256);
                    }
                    if (GlobalVar.calcWav[i] >= (256 * 256))
                    {
                        GlobalVar.calcWav[i] = (256 * 256) - 1;
                    }
                    bigint = GlobalVar.calcWav[i] / (256);
                    smallInt = GlobalVar.calcWav[i] - (256 * bigint);

                    best[pos] = Convert.ToByte(smallInt);
                    best[pos + 1] = Convert.ToByte(bigint);

                    bigint = GlobalVar.diffWav[i] / (256);
                    smallInt = GlobalVar.diffWav[i] - (256 * bigint);

                    //     bigint = GlobalVar.targetWav[i] / (256);
                    //      smallInt = GlobalVar.targetWav[i] - (256 * bigint);

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

            }
        }

        static void WriteScoreFile()
        {
            string fn = "sx" + Convert.ToString(GlobalVar.popMember);
            BinaryWriter scoreFile = new BinaryWriter(File.Open(fn, FileMode.Create));

            for (int fx = 0; fx < scoreFrames; fx++)
            {
                int startX = fx * (GlobalVar.samples / scoreFrames);
                int endX = startX + (GlobalVar.samples / scoreFrames);

                GlobalVar.frameScore[fx] = (GlobalVar.potentialDiff / scoreFrames) - AlternateScore(startX, endX);
                GlobalVar.frameScore[fx] = GlobalVar.frameScore[fx] + TotalSound(startX, endX);

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
            int MIDISize = 7;

            for (int i = 0; i < GlobalVar.eventsThisRun; i++)
            {

                GlobalVar.MIDIdelta[i] = GlobalVar.features[(i * MIDISize)] + (256 * GlobalVar.features[1 + (i * MIDISize)]);
                GlobalVar.MIDIduration[i] = GlobalVar.features[2 + (i * MIDISize)] + (256 * GlobalVar.features[3 + (i * MIDISize)]);

                GlobalVar.MIDItypechannel[i] = GlobalVar.features[4 + (i * MIDISize)];

                GlobalVar.MIDIdata1[i] = GlobalVar.features[5 + (i * MIDISize)];
                GlobalVar.MIDIdata2[i] = GlobalVar.features[6 + (i * MIDISize)];

                GlobalVar.MIDIwritten[i] = false;
                GlobalVar.scoreValid[i] = false;
                if ((GlobalVar.MIDItypechannel[i] <= (144 + 15)) && (GlobalVar.MIDItypechannel[i] >= (144 + 0)))
                {
                    if (GlobalVar.MIDIdelta[i] < GlobalVar.endTime)
                        GlobalVar.scoreValid[i] = true; // note on
                    //create a note off at delta + duration
                    GlobalVar.MIDIdelta[i + GlobalVar.eventsThisRun] = GlobalVar.MIDIdelta[i] +
                        GlobalVar.features[2 + (i * MIDISize)] + (256 * GlobalVar.features[3 + (i * MIDISize)]);

                    if (GlobalVar.MIDIdelta[i] + GlobalVar.MIDIdelta[i + GlobalVar.eventsThisRun] > GlobalVar.endTime)
                        GlobalVar.MIDIdelta[i + GlobalVar.eventsThisRun] = GlobalVar.endTime - GlobalVar.MIDIdelta[i];

                    GlobalVar.MIDItypechannel[i + GlobalVar.eventsThisRun] = GlobalVar.features[4 + (i * MIDISize)] - 16;

                    GlobalVar.MIDIdata1[i + GlobalVar.eventsThisRun] = GlobalVar.features[5 + (i * MIDISize)];
                    GlobalVar.MIDIdata2[i + GlobalVar.eventsThisRun] = GlobalVar.features[6 + (i * MIDISize)];

                    GlobalVar.scoreValid[i + GlobalVar.eventsThisRun] = GlobalVar.scoreValid[i];
                }
                if ((GlobalVar.MIDItypechannel[i] <= (160 + 15)) && (GlobalVar.MIDItypechannel[i] >= (160 + 0)))
                {
                    GlobalVar.scoreValid[i] = true; // key pressure
                }
                if ((GlobalVar.MIDItypechannel[i] <= (176 + 15)) && (GlobalVar.MIDItypechannel[i] >= (176 + 0)))
                {
                    GlobalVar.scoreValid[i] = true; // control change
                }
                if ((GlobalVar.MIDItypechannel[i] <= (192 + 8)) && (GlobalVar.MIDItypechannel[i] >= (192 + 0)))
                {
                    GlobalVar.scoreValid[i] = true; // program change
                }
                if ((GlobalVar.MIDItypechannel[i] <= (208 + 15)) && (GlobalVar.MIDItypechannel[i] >= (208 + 0)))
                {
                    GlobalVar.scoreValid[i] = true; // after touch
                }
                if ((GlobalVar.MIDItypechannel[i] <= (224 + 15)) && (GlobalVar.MIDItypechannel[i] >= (224 + 0)))
                {
                    GlobalVar.scoreValid[i] = true; // pitch change
                }

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
        static long[] openWav(string filename)
        {
            byte[] wav = File.ReadAllBytes(filename);
            long[] wavArray = new long[maxSamples];

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
            if (GlobalVar.samples < 1)
            {
                GlobalVar.samples = (wavSize / 2);     // more accurate, get actual chunk size
                GlobalVar.endTime = (Convert.ToInt32((GlobalVar.samples / samplesSecond) * 1000)) / 1;
            }

            int genSamples = 0;
            if (GlobalVar.samples > 0)
            {
                genSamples = wavSize / 2;
            }

            if (genSamples < 0.95 * GlobalVar.samples)
            {
                GlobalVar.wavErr = true;
                return null;
            }



            Console.WriteLine("samples: " + GlobalVar.samples);

            // here can set frame size etc.

            GlobalVar.eventsThisRun = 960;
            GlobalVar.featureCount = (7 * GlobalVar.eventsThisRun);

            wavArray = new long[GlobalVar.samples];

            // Write to double array/s:
            int i = 0;

            // pos++;

            int retSize = Math.Min(genSamples, GlobalVar.samples);
            while (i < (retSize))
            {
                wavArray[i] = bytesToInteger(wav[pos], wav[pos + 1]);
                pos += 2;
                i++;
            }
            return wavArray;
        }

    }
}
