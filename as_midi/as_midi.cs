using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Xml;
using System.Collections;

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
            public static int framesThisRun = 120000;
            public static int featureCount = (4 * framesThisRun);
            public static int[] features = new int[350000];
            public static string arg0 = "";
            public static long[] leftmono = new long[maxSamples];
            public static int samples = 0;

            public static double[] runningWave = new double[maxSamples];
            public static long[] calcWave = new long[maxSamples];
            public static long[] diffWave = new long[maxSamples];

            public static int[] levelOffset = new int[framesThisRun];
            public static int[] levelFrequency = new int[framesThisRun];
            public static double[] levelFD = new double[framesThisRun];
            public static int[] levelAmplitude = new int[framesThisRun];
            public static int[] levelDirection = new int[framesThisRun];
            public static int[] frameFirstSample = new int[framesThisRun];
            public static int[] frameLastSample = new int[framesThisRun];
            public static long[] frameScore = new long[framesThisRun];
            public static bool[] frameActive = new bool[framesThisRun];
            public static double[] freqLookup = new double[256 * 16];
            public static long[] sampleDiff = new long[maxSamples];
            public static int[,] frameSamples = new int[framesThisRun, 410];
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

            string XMLfile = "test0995.xml";
            openWav("target.wav");
            int nextApply = -1;
            bool someLeft = true;
            Random random = new Random();

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
                //    Console.WriteLine("input error");
                ExportXMLfile(XMLfile);
                return;
            }

            for (int frameX = 0; frameX < GlobalVar.framesThisRun; frameX++)
            {
                GlobalVar.frameActive[frameX] = false;
            }

            for (int i = 0; i < GlobalVar.samples; i++)
            {
                GlobalVar.calcWave[i] = 0;
            }

            for (int i = 0; i < GlobalVar.samples; i++)
            {
                GlobalVar.sampleDiff[i] = (2 * Math.Abs(GlobalVar.leftmono[i]));
                GlobalVar.potentialDiff = GlobalVar.potentialDiff + GlobalVar.sampleDiff[i];  // worst is mirror
            }

            someLeft = true;
            int loopCTR = 0;
            int consecutiveEmpty = 0;
            while (someLeft)
            {
                nextApply = -1;
                if (!GlobalVar.noMore[(GlobalVar.startFrame / (GlobalVar.framesThisRun / 32))])
                {
                    nextApply = GetWeights();
                }

                if (GlobalVar.noMore[(GlobalVar.startFrame / (GlobalVar.framesThisRun / 32))])
                {
                    GlobalVar.startFrame = GlobalVar.startFrame + (GlobalVar.framesThisRun / 32);
                    int endFrame = GlobalVar.startFrame + (GlobalVar.framesThisRun / 16);
                    if ((GlobalVar.framesThisRun - endFrame) < (GlobalVar.framesThisRun / 16))
                    {
                        GlobalVar.startFrame = 0;
                    }
                }

                if (nextApply > -1)
                    consecutiveEmpty = 0;
                if (nextApply < 0)
                {
                    consecutiveEmpty++;
                    GlobalVar.noMore[(GlobalVar.startFrame / (GlobalVar.framesThisRun / 32))] = true;
                }
                if (consecutiveEmpty > 32)
                    someLeft = false;
                loopCTR++;
            }

            someLeft = true;
            while (someLeft)
            {
                nextApply = GetWeights();

                if (nextApply > -1)
                    consecutiveEmpty = 0;
                if (nextApply < 0)
                {
                    consecutiveEmpty++;
                }
                if (consecutiveEmpty > 32)
                    someLeft = false;
            }


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




        static int GetWeights()
        {
            int weightNDX = -1;
            long weightValue = 0;
            long oldScore = 0;
            long newScore = 0;
            long[] newCalc = new long[maxSamples];
            bool broken = false;
            int sNDX = 0;
            int startFrame = 0;
            int endFrame = GlobalVar.framesThisRun;
            Random random = new Random();

            startFrame = GlobalVar.startFrame;
            GlobalVar.startFrame = GlobalVar.startFrame + (GlobalVar.framesThisRun / 32);
            endFrame = startFrame + (GlobalVar.framesThisRun / 16);
            if ((GlobalVar.framesThisRun - endFrame) < (GlobalVar.framesThisRun / 16))
            {
                endFrame = GlobalVar.framesThisRun;
                GlobalVar.startFrame = 0;
            }

            for (int frameX = startFrame; frameX < endFrame; frameX++)
            {
                if (!GlobalVar.frameActive[frameX])
                {
                    oldScore = 0;
                    newScore = 0;
                    broken = false;
                    for (int wx = GlobalVar.frameFirstSample[frameX]; wx < GlobalVar.frameLastSample[frameX]; wx++)
                    {
                        oldScore = oldScore + (Math.Abs(GlobalVar.leftmono[wx] - GlobalVar.calcWave[wx]));

                        sNDX = wx - GlobalVar.frameFirstSample[frameX];
                        try
                        {
                            newCalc[wx] = GlobalVar.calcWave[wx] + GlobalVar.frameSamples[frameX, sNDX];
                        }
                        catch
                        {
                            broken = true;
                            if (GlobalVar.calcWave[wx] > -1)
                                newCalc[wx] = ((128 * 256) - 1);

                            if (GlobalVar.calcWave[wx] < 0)
                                newCalc[wx] = -(128 * 256) + 1;
                        }
                        newScore = newScore + (Math.Abs(GlobalVar.leftmono[wx] - newCalc[wx]));
                    }
                    GlobalVar.frameScore[frameX] = oldScore - newScore;
                    if ((oldScore - newScore) < GlobalVar.worstScore)
                    {
                        GlobalVar.worstScore = (oldScore - newScore);
                        GlobalVar.worstNDX = frameX;
                    }
                    if (((oldScore - newScore) > weightValue) && (!broken))
                    {
                        weightNDX = frameX;
                        weightValue = oldScore - newScore;
                    }
                }
            }

            if (weightNDX > -1)
            {
                for (int wx = GlobalVar.frameFirstSample[weightNDX]; wx < GlobalVar.frameLastSample[weightNDX]; wx++)
                {
                    sNDX = wx - GlobalVar.frameFirstSample[weightNDX];
                    try
                    {
                        GlobalVar.calcWave[wx] = GlobalVar.calcWave[wx] + GlobalVar.frameSamples[weightNDX, sNDX];
                    }
                    catch
                    {
                        broken = true;
                        if (GlobalVar.calcWave[wx] > -1)
                            GlobalVar.calcWave[wx] = ((128 * 256) - 1);

                        if (GlobalVar.calcWave[wx] < 0)
                            GlobalVar.calcWave[wx] = -(128 * 256) + 1;
                    }
                }

                GlobalVar.frameActive[weightNDX] = true;
                GlobalVar.activeFeatures++;
                GlobalVar.lastLength = GlobalVar.frameLastSample[weightNDX] - GlobalVar.frameFirstSample[weightNDX];
            }

            return (weightNDX);
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
                    for (int frameX = 0; frameX < GlobalVar.framesThisRun; frameX++)
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

            for (int fx = 0; fx < GlobalVar.framesThisRun; fx++)
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

                    xml.WriteElementString("frames", GlobalVar.framesThisRun.ToString());
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
            int waveSize = 4;

            for (int i = 0; i < GlobalVar.framesThisRun; i++)
            {

                GlobalVar.levelOffset[i] = 0;

                GlobalVar.levelFrequency[i] = GlobalVar.features[(i * waveSize)] + (256 * GlobalVar.features[1 + (i * waveSize)]);

                if (GlobalVar.levelFrequency[i] >= (256 * 128))
                {
                    GlobalVar.levelOffset[i] = GlobalVar.levelOffset[i] + 1;
                    GlobalVar.levelFrequency[i] = GlobalVar.levelFrequency[i] - (256 * 128);
                }
                if (GlobalVar.levelFrequency[i] >= (256 * 64))
                {
                    GlobalVar.levelOffset[i] = GlobalVar.levelOffset[i] + 2;
                    GlobalVar.levelFrequency[i] = GlobalVar.levelFrequency[i] - (256 * 64);
                }
                if (GlobalVar.levelFrequency[i] >= (256 * 32))
                {
                    GlobalVar.levelOffset[i] = GlobalVar.levelOffset[i] + 4;
                    GlobalVar.levelFrequency[i] = GlobalVar.levelFrequency[i] - (256 * 32);
                }
                if (GlobalVar.levelFrequency[i] >= (256 * 16))
                {
                    GlobalVar.levelOffset[i] = GlobalVar.levelOffset[i] + 8;
                    GlobalVar.levelFrequency[i] = GlobalVar.levelFrequency[i] - (256 * 16);
                }

                GlobalVar.levelAmplitude[i] = GlobalVar.features[2 + (i * waveSize)] + (256 * GlobalVar.features[3 + (i * waveSize)]);
                GlobalVar.levelDirection[i] = -1;
                if (GlobalVar.levelAmplitude[i] >= (256 * 128))
                {
                    GlobalVar.levelDirection[i] = 1;
                    GlobalVar.levelAmplitude[i] = GlobalVar.levelAmplitude[i] - (256 * 128);
                }


                if (GlobalVar.levelAmplitude[i] >= (256 * 64))
                {
                    GlobalVar.levelOffset[i] = GlobalVar.levelOffset[i] + 16;
                    GlobalVar.levelAmplitude[i] = GlobalVar.levelAmplitude[i] - (256 * 64);
                }

                GlobalVar.levelFD[i] = GlobalVar.freqLookup[GlobalVar.levelFrequency[i]];

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

            GlobalVar.framesThisRun = Convert.ToInt16(GlobalVar.samples / sineInterval) + 1;
            GlobalVar.featureCount = (4 * GlobalVar.framesThisRun);
            //    GlobalVar.extraBits = Convert.ToInt16(GlobalVar.samples - (sineInterval * GlobalVar.framesThisRun));
            //           Console.WriteLine("samples - " + GlobalVar.samples.ToString() + " frames - " + GlobalVar.framesThisRun.ToString() 
            //               + " features - " + GlobalVar.featureCount.ToString() 
            //              + " interval - " + sineInterval.ToString());
            //            System.Threading.Thread.Sleep(25000);

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
