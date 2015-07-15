using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;
using System.Xml;
using System.Collections;

namespace as_compare
{
    class as_compare
    {
        const double samplesSecond = 192000.0;
        const int maxSamples = 5898240; // 30 seconds max
 
        public static class GlobalVar
        {
            public static int samples = 0;
            public static long[] leftmono = new long[maxSamples];
            public static long[] targetWave = new long[maxSamples];
            public static long[] diffWave = new long[maxSamples];
            public static long myScore = 0;
            public static int soundPos = 0;
        }

        static void Main(string[] args)
        {
            openWav("target.wav");
            for (int i = 0; i < (GlobalVar.samples-0); i++)
            {
                GlobalVar.targetWave[i] = GlobalVar.leftmono[i];
            }
            openWav("compare.wav");
            for (int i = 0; i < (GlobalVar.samples-0); i++)
            {
                GlobalVar.diffWave[i] = GlobalVar.leftmono[i+0];
            }
            AlternateScore();
        }

        static void AlternateScore()
        {
            long runningScore = 0;
            long tempScore = 0;
            long potentialDiff = 0;
            int calcPct = 0;
            int calcDiff = 0;

            System.IO.StreamWriter file = new System.IO.StreamWriter("diff.txt");

            int offSet = 128;

            for (int i = 1; i < (GlobalVar.samples-256); i++)
            {
                calcDiff = Math.Abs(Convert.ToInt32(GlobalVar.targetWave[i] - GlobalVar.diffWave[i]));
                calcPct = 0;
                if (!GlobalVar.targetWave[i].Equals(0))
                {
                    calcPct = Math.Abs(Convert.ToInt32((10000*calcDiff) / GlobalVar.targetWave[i]));
                } 
                file.WriteLine(i.ToString() + "," + GlobalVar.targetWave[i].ToString() + "," 
                    + GlobalVar.diffWave[i].ToString() + "," + calcDiff.ToString() + "," + calcPct.ToString());
                tempScore = Math.Abs(GlobalVar.targetWave[i] - GlobalVar.diffWave[i]);
                potentialDiff = potentialDiff + (2 * Math.Abs(GlobalVar.targetWave[i]));  // worst is mirror
                runningScore = runningScore + (tempScore);
            }

            GlobalVar.myScore = Convert.ToInt64((potentialDiff - runningScore));
            calcPct = Convert.ToInt16((100 * GlobalVar.myScore) / potentialDiff);

            Console.WriteLine(" score " + GlobalVar.myScore.ToString() + " of " + potentialDiff.ToString() + 
                " " + calcPct.ToString());
            file.Close();
        }

        static int bytesToInteger(byte firstByte, byte secondByte, byte thirdByte)
        {
            // convert two bytes to one short (little endian)
            int s = (Convert.ToInt16(thirdByte) * (256*256)) + (Convert.ToInt16(secondByte) * 256) + Convert.ToInt16(firstByte);
            // convert to range from -1 to (just below) 1

            if (s > (8 * 1024 * 1024))
            {
                s = s - (16 * 1024 * 1024);
            }

            return s;
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
            GlobalVar.samples = (wavSize) / 3;     // more accurate, get actual chunk size

            GlobalVar.leftmono = new long[GlobalVar.samples];

            // Write to double array/s:
            int i = 0;

            while (i < (GlobalVar.samples))
            {
                GlobalVar.leftmono[i] = bytesToInteger(wav[pos], wav[pos + 1], wav[pos+2]);
                pos += 3;
                i++;
            }
        }

    }
}
