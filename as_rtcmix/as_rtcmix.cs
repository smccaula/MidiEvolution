﻿using System;
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
        const int bytesPerEvent = 4;
        const double samplesSecond = 44100.0;
        const int maxSamples = 44100 * 30; // 30 seconds max
        const int scoreFrames = 8;

        public static class GlobalVar
        {
            public static int endTime = 0;
            public static int eventsThisRun = 8192;
            public static int featureCount = (bytesPerEvent * eventsThisRun);
            public static int[] features = new int[350000];
            public static int[] copyFeatures = new int[350000];
            public static string arg0 = "";
            public static long[] targetWav = new long[maxSamples];
            public static int samples = 0;

            public static double[] runningWave = new double[maxSamples];
            public static long[] calcWav = new long[maxSamples];
            public static long[] diffWav = new long[maxSamples];
            // need start, dur, amp, freq, pan

            public static int[] CMIXstart = new int[eventsThisRun];
            public static int[] CMIXdur = new int[eventsThisRun];
            public static int[] CMIXamp = new int[eventsThisRun];
            public static int[] CMIXfreq = new int[eventsThisRun];
            public static int[] CMIXpan = new int[eventsThisRun];

            public static long[] frameScore = new long[scoreFrames];
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
            public static long totalDiff = 0;
            public static long[] potentialDiff = new long[scoreFrames];
            public static int lastLength = 0;
            public static int mostSamples = 0;
            public static bool allFrames = false;
            public static int startFrame = 0;
            public static double[] freqLookup = new double[256 * 256];
        }

        static void Main(string[] args)
        {

            double freqInterval = 1.027777778;

            GlobalVar.freqLookup[0] = 13.75;
            for (int i = 1; i < (256); i++)
            {
                GlobalVar.freqLookup[i] = GlobalVar.freqLookup[i - 1] * freqInterval;
            }

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

            bool processSuccess = false;
            int processTry = 0;

            while (!processSuccess)
            {
                processSuccess = PreProcess(XMLfile);
                if (!processSuccess)
                {
                    //                   Console.WriteLine("retry " + processTry.ToString() + " wc-"
                    //                       + GlobalVar.popMember.ToString() + " gen-" +  GlobalVar.myGeneration.ToString() + " sco-" 
                    //                       + GlobalVar.myScore.ToString());
                    System.Threading.Thread.Sleep(1000);
                }
                processTry++;
                if (processTry > 2)
                {
                    // error recovery routines
                    //                   Console.WriteLine("quitting");
                    return;
                }

            }
            OutputAllFiles(XMLfile);
        }

        static void OutputAllFiles(string XMLfile)
        {
            // write sx, mx and xml files, only on successful completion of process
            WriteScoreFile();
            if (GlobalVar.myScore > GlobalVar.bestScore)
            {
                //x          WriteBestFile();
            }
            ExportXMLfile(XMLfile);

            char[] buildChars;
            buildChars = new char[350000];

            for (int i = 0; i < GlobalVar.featureCount; i++)
            {
                buildChars[i] = (char)GlobalVar.features[i];
            }

            string bs = new string(buildChars);
            bs = bs.Substring(0, GlobalVar.featureCount);

            string fn = "";
            fn = "mx" + Convert.ToString(GlobalVar.popMember);
            File.WriteAllText(fn, bs);
        }

        static bool PreProcess(string XMLfile)
        {
            ImportXMLfile(XMLfile);

            if (!GetExistingCharacteristics(GlobalVar.popMember))
            {
                //      Console.WriteLine("input error");
                return (false);
            }

            for (int i = 0; i < GlobalVar.samples; i++)
            {
                GlobalVar.sampleDiff[i] = 2 * (Math.Abs(GlobalVar.targetWav[i]));
                GlobalVar.totalDiff = GlobalVar.totalDiff + GlobalVar.sampleDiff[i];  // worst is mirror
            }

            for (int fx = 0; fx < scoreFrames; fx++)
            {
                GlobalVar.potentialDiff[fx] = 0;
                int startX = fx * (GlobalVar.samples / scoreFrames);
                int endX = startX + (GlobalVar.samples / scoreFrames);
                for (int sx = startX; sx < endX; sx++)
                {
                    GlobalVar.potentialDiff[fx] = GlobalVar.potentialDiff[fx] + (2 * Math.Abs(GlobalVar.targetWav[sx]));
                }
            }

            if (!BuildScoreFile())
            {
                return (false);
            }



            if (!RenderScoreToWav())
            {
                return (false);
            }

            // at this point I have both wav files, and the rest of the process should be identical


            GlobalVar.myScore = (GlobalVar.totalDiff - AlternateScore(0, GlobalVar.samples));
            //            Console.WriteLine("myScore: " + GlobalVar.myScore + " gs:" + GlobalVar.samples + " td:" + GlobalVar.totalDiff
            //                + " pop-"  + GlobalVar.popMember.ToString() + " gen-" + GlobalVar.myGeneration.ToString());

            return (true);
        }

        static bool BuildScoreFile()
        {
            // GetExistingCharacteristics should already have turned XML into RTCmix events
            // need to sort all events based on time (any event can have any time)
            // New routine to write header and all events into tracks

            string scoreFile = Convert.ToString(GlobalVar.popMember) + ".sco";
            string wavFile = Convert.ToString(GlobalVar.popMember) + ".wav";

            if (File.Exists(scoreFile))
            {
                File.Delete(scoreFile);
            }

            StreamWriter scoreText = new StreamWriter(scoreFile);
            scoreText.WriteLine("set_option(\"audio = off\", \"play = off\", \"fast_update = on\",  \"clobber = on\")");
            scoreText.WriteLine("rtsetparams(44100, 1)");
            scoreText.WriteLine("rtoutput(\"" + wavFile + "\")");
            scoreText.WriteLine("reset(44100)");
            scoreText.WriteLine("load(\"WAVETABLE\")");
            scoreText.WriteLine("wavet = maketable(\"wave\", 5000, \"sine\")");
            scoreText.WriteLine("note_env = maketable(\"line\", 5000, 0,0, 1,1, 2,1, 3,1, 4,0)");

            bool MoreEvents = true;
            int eventX = 0;
            double tempStart = 0.0;
            double tempLength = 0.0;
            double tempOffset = 0.0;
            double tempDur = 0.0;
            double tempFreq = 0.0;
            double tempPan = 0.0;
            double tempAmp = 0.0;

            //loop through events
            while (MoreEvents)
            {
                // check for conditions such as 0 dur or 0 freq

                //                tempStart = (eventX * GlobalVar.endTime) / GlobalVar.eventsThisRun;
                tempLength = (Convert.ToDouble((GlobalVar.samples / samplesSecond)));
                tempStart = (eventX * tempLength) / GlobalVar.eventsThisRun;
                //    tempStart = eventX / GlobalVar.eventsThisRun;
                tempOffset = (GlobalVar.CMIXstart[eventX]) / 4096.0;

                //          Console.WriteLine("times : " + Convert.ToString(tempLength) + " " +
                //               Convert.ToString(tempStart) + " " + Convert.ToString(tempOffset));

                tempStart = tempStart + tempOffset;


                tempDur = GlobalVar.CMIXdur[eventX] * 1.0;
                tempFreq = GlobalVar.freqLookup[GlobalVar.CMIXfreq[eventX]];
                tempAmp = (GlobalVar.CMIXamp[eventX] * 1024) / 256; // dsm too coarse?
                tempPan = 0;

                if ((tempAmp > 0) && (tempDur > 0))
                {
                    scoreText.WriteLine("WAVETABLE("
                        + Convert.ToString(tempStart) + "," // 0.00 to end time (1 bytes)
                        + Convert.ToString(tempDur / 4096.00) + "," // 0.00 to 0.25 (1 byte)
                        + Convert.ToString(tempAmp) + "," // 0 to ... (2 bytes)
                        + Convert.ToString(tempFreq) + "," // 0 to 20,500 (2 bytes)
                        + Convert.ToString(tempPan) + ",wavet)"); // 0.00 (mono)
                }

                eventX++;
                if (eventX == GlobalVar.eventsThisRun) MoreEvents = false;
            }
            scoreText.Close();

            return (true);
        }

        static bool RenderScoreToWav()
        {
            Process scoreProcess = new Process();
            String scoreFile = Convert.ToString(GlobalVar.popMember) + ".sco";
            String logFile = Convert.ToString(GlobalVar.popMember) + ".log";

            string shellFile = Convert.ToString(GlobalVar.popMember) + ".sh";

            if (File.Exists(shellFile))
            {
                File.Delete(shellFile);
            }

            StreamWriter shellText = new StreamWriter(shellFile);
            shellText.WriteLine("./RTcmix/bin/CMIX -D plug:null -Q < " + scoreFile + " > " + logFile);
            //            shellText.WriteLine("./RTcmix/bin/CMIX -Q -D plug:null < " + scoreFile);
            shellText.Close();

            scoreProcess = new Process();

            scoreProcess.StartInfo.CreateNoWindow = true;
            scoreProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            scoreProcess.StartInfo.UseShellExecute = true;

            scoreProcess.StartInfo.FileName = "/bin/bash";
            scoreProcess.StartInfo.Arguments = shellFile;

            scoreProcess.Start();
            scoreProcess.WaitForExit();
            scoreProcess.Dispose();

            GlobalVar.calcWav = openWav(Convert.ToString(GlobalVar.popMember) + ".wav");

            if (GlobalVar.wavErr)
            {
                return (false);
            }

            return (true);
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
                GlobalVar.frameScore[fx] = GlobalVar.potentialDiff[fx] - AlternateScore(startX, endX);
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

                int nonZero = 0;
                for (int i = 0; i < GlobalVar.featureCount; i++)
                {
                    GlobalVar.features[i] = buildChars[i];
                    if (GlobalVar.features[i] > 255)
                        GlobalVar.features[i] = 255;
                    if (GlobalVar.features[i] < 0)
                        GlobalVar.features[i] = 0;
                    if (GlobalVar.features[i] > 0)
                        nonZero++;
                }
                if (nonZero < 1)
                {
                    int newIntValue = 0;
                    for (int i = 0; i < GlobalVar.featureCount; i++)
                    {
                        newIntValue = GlobalVar.random.Next(0, 255);
                        GlobalVar.features[i] = newIntValue;
                    }

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
            int CMIXSize = bytesPerEvent;

            for (int i = 0; i < GlobalVar.eventsThisRun; i++)
            {
                GlobalVar.CMIXstart[i] = GlobalVar.features[(i * CMIXSize)];
                GlobalVar.CMIXdur[i] = GlobalVar.features[1 + (i * CMIXSize)];
                GlobalVar.CMIXamp[i] = GlobalVar.features[2 + (i * CMIXSize)];
                GlobalVar.CMIXfreq[i] = GlobalVar.features[3 + (i * CMIXSize)];
                //        GlobalVar.CMIXpan[i] = GlobalVar.features[4 + (i * CMIXSize)];
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
            int genSamples = wavSize / 2;

            //           Console.WriteLine("samples: " + GlobalVar.samples);

            // here can set frame size etc.

            //      GlobalVar.eventsThisRun = 960;
            //     GlobalVar.featureCount = (8 * GlobalVar.eventsThisRun);

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