using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace as_tests
{
    class as_tests
    {
        static void Main(string[] args)
        {
            int [] dailyBands = new int[76];
            int [] guessBands = new int[1283];

            for (int dayX = 0; dayX < 76; dayX++)
            {
                if (dayX < 3) dailyBands[dayX] = -3;
                if ((dayX > 2) && (dayX < 8)) dailyBands[dayX] = -2;
                if ((dayX > 7) && (dayX < 26)) dailyBands[dayX] = -1;
                if ((dayX > 25) && (dayX < 53)) dailyBands[dayX] = 0;
                if ((dayX > 52) && (dayX < 69)) dailyBands[dayX] = 1;
                if ((dayX > 68) && (dayX < 74)) dailyBands[dayX] = 2;
                if ((dayX > 73) ) dailyBands[dayX] = 3;
            }

            for (int guessX = 0; guessX < 1283; guessX++)
            {
                if (guessX < 3) guessBands[guessX] = -7;
                if ((guessX > 2) && (guessX < 13)) guessBands[guessX] = -6;
                if ((guessX > 12) && (guessX < 28)) guessBands[guessX] = -5;
                if ((guessX > 27) && (guessX < 57)) guessBands[guessX] = -4;
                if ((guessX > 56) && (guessX < 115)) guessBands[guessX] = -3;
                if ((guessX > 114) && (guessX < 224)) guessBands[guessX] = -2;
                if ((guessX > 223) && (guessX < 453)) guessBands[guessX] = -1;
                if ((guessX > 452) && (guessX < 800)) guessBands[guessX] = 0;
                if ((guessX > 799) && (guessX < 1054)) guessBands[guessX] = 1;
                if ((guessX > 1053) && (guessX < 1180)) guessBands[guessX] = 2;
                if ((guessX > 1179) && (guessX < 1238)) guessBands[guessX] = 3;
                if ((guessX > 1237) && (guessX < 1259)) guessBands[guessX] = 4;
                if ((guessX > 1258) && (guessX < 1272)) guessBands[guessX] = 5;
                if ((guessX > 1271) && (guessX < 1275)) guessBands[guessX] = 6;
                if ((guessX > 1274)) guessBands[guessX] = 7;
            }

            TextWriter cxm = new StreamWriter("testx.csv", false);
            Random random = new Random();
            int nextGuess = 0;
            int tempScore = 0;

            int runningScore = 0;
            for (int testx = 0; testx < 100000; testx++)
            {
                runningScore = 0;
            //    for (int dayX = 0; dayX < 76; dayX++)
                    for (int dayX = 0; dayX < 33; dayX++)
                    {
                        nextGuess = random.Next(0, 1283);

                        tempScore = Math.Abs(dailyBands[dayX] - guessBands[nextGuess]);
                        tempScore = tempScore * (Math.Abs(dailyBands[dayX]) + 1);
                        runningScore = runningScore + tempScore;

                }
                cxm.WriteLine(testx.ToString() + "," + runningScore.ToString());

                }

                cxm.Close();
        }
    }
}
