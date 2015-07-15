using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace as_net
{
    class as_net
    {
        public static class GlobalVar
        {
            public static int[] features = new int[200000];
            public static int featureCount = 0;
            public static int testCount = 0;
            public static int myGeneration = 0;
            public static int myScore = 0;
            public static int runningScore = 0;
            public static Random random = new Random();
            public static string[] indices = new string[10];
            public static double[,] deltaValues = new double[10,15000];
            public static int[,] dateCheck = new int[10,15000];
            public static double[,] indexStats = new double[10, 2];
            public static double[] NNInput = new double[50];
            public static double[] NNOutput = new double[15];
            public static double[,] NNHidden = new double[4, 1000];
            public static double[,] NNWeight1 = new double[50, 200];
            public static double[,] NNWeight2 = new double[200, 250];
            public static double[,] NNWeight3 = new double[250, 150];
            public static double[,] NNWeight4 = new double[150, 15];
            public static int testNDX = 3;  // hard coded 
            public static int[] lastNDX = new int[10];
            public static int[] penaltyValue = new int[15];
            public static bool printFlag = false;
            public static string cfmxName = "";
        }

        static void Main(string[] args)
        {
            GlobalVar.featureCount = 199499;
            InitStrings();
            string XMLfile = "solution.xml";
            // get input data first

            if (args.Length > 0)
            {
                XMLfile = args[0];
                GlobalVar.cfmxName = XMLfile;
                GlobalVar.cfmxName = GlobalVar.cfmxName.Remove(GlobalVar.cfmxName.Length - 4) + ".cxm";
            }

            if (String.IsNullOrEmpty(XMLfile))
            {
                return;
            }

            try
            {
                ImportXMLfile(XMLfile);
            }
            catch
            {
                return;
            }

            if (GlobalVar.featureCount != GlobalVar.testCount)
            {
                Console.WriteLine("feature mismatch: " + GlobalVar.featureCount.ToString() + " " + GlobalVar.testCount.ToString());

              return;
            }

            GetInputData("NN_input.csv");

            CalculateWeights();


            GlobalVar.printFlag = false;
            if (GlobalVar.random.Next(0, 1500) < 3)
            {
                GlobalVar.printFlag = true;
            }

            DailyIterations();

            GlobalVar.myScore = GlobalVar.runningScore;
            
            //     ProcessAndScore();

            ExportXMLfile(XMLfile);

        }

        static void InitStrings()
        {
            GlobalVar.indices[0] = "DJI";
            GlobalVar.indices[1] = "GSPC";
            GlobalVar.indices[2] = "IXIC";
            GlobalVar.indices[3] = "FTSE";
            GlobalVar.indices[4] = "DAX";
            GlobalVar.indices[5] = "CAC";
            GlobalVar.indices[6] = "HSI";
            GlobalVar.indices[7] = "N225";
            GlobalVar.indices[8] = "SSE";
            GlobalVar.indices[9] = "AUX";
            GlobalVar.indexStats[0, 0] = 0.0566;
            GlobalVar.indexStats[0, 1] = 1.1019;
            GlobalVar.indexStats[1, 0] = 0.0645;
            GlobalVar.indexStats[1, 1] = 1.2263;
            GlobalVar.indexStats[2, 0] = 0.0862;
            GlobalVar.indexStats[2, 1] = 1.3215;
            GlobalVar.indexStats[3, 0] = 0.0389;
            GlobalVar.indexStats[3, 1] = 1.1294;
            GlobalVar.indexStats[4, 0] = 0.0651;
            GlobalVar.indexStats[4, 1] = 1.4326;
            GlobalVar.indexStats[5, 0] = 0.0335;
            GlobalVar.indexStats[5, 1] = 1.4786;
            GlobalVar.indexStats[6, 0] = 0.0495;
            GlobalVar.indexStats[6, 1] = 1.4355;
            GlobalVar.indexStats[7, 0] = 0.0607;
            GlobalVar.indexStats[7, 1] = 1.4791;
            GlobalVar.indexStats[8, 0] = 0.0220;
            GlobalVar.indexStats[8, 1] = 1.3836;
            GlobalVar.indexStats[9, 0] = 0.0313;
            GlobalVar.indexStats[9, 1] = 1.1716;
            GlobalVar.penaltyValue[0] = 0;
            GlobalVar.penaltyValue[1] = 35;
            GlobalVar.penaltyValue[2] = 60;
            GlobalVar.penaltyValue[3] = 80;
            GlobalVar.penaltyValue[4] = 95;
            GlobalVar.penaltyValue[5] = 105;
            GlobalVar.penaltyValue[6] = 114;
            GlobalVar.penaltyValue[7] = 122;
            GlobalVar.penaltyValue[8] = 129;
            GlobalVar.penaltyValue[9] = 135;
            GlobalVar.penaltyValue[10] = 140;
            GlobalVar.penaltyValue[11] = 144;
            GlobalVar.penaltyValue[12] = 147;
            GlobalVar.penaltyValue[13] = 149;
            GlobalVar.penaltyValue[14] = 150;
        }

        static void GetInputData(string CVSfile)
        {
            using (var sr = File.OpenText(CVSfile))
            {
                string line;
                string NDXName;
                int DateValue;
                int NDXndx;
                int Datendx;
                double deltaValue;

                line = sr.ReadLine(); //header row

                while ((line = sr.ReadLine()) != null)
                {
                    var fields = line.Split(',');
                    NDXName = fields[0].Trim().ToString();
                    NDXndx = 0;
                    for (int tx = 0; tx < GlobalVar.indices.Length; tx++)
                    {
                 //       Console.WriteLine(tx.ToString() + " " + GlobalVar.indices[tx] + " " + NDXName);
                        if (GlobalVar.indices[tx].Equals(NDXName))
                        {
                            NDXndx = tx;
                            tx = GlobalVar.indices.Length;
                        }
                    }
                    DateValue = Convert.ToInt32(fields[1].Trim());
                    Datendx = Convert.ToInt16(fields[2].Trim());
                    deltaValue = Convert.ToDouble(fields[4].Trim());
               //     System.Threading.Thread.Sleep(0);
                    GlobalVar.deltaValues[NDXndx, Datendx] = deltaValue;
                    GlobalVar.dateCheck[NDXndx, Datendx] = DateValue;
                    GlobalVar.dateCheck[NDXndx, Datendx + 1] = 999999;
                }
            }
        }

        static void CalculateWeights()
        {
            int fromNDX, toNDX;
            int featureNDX = 0;

            // first level 50 -> 200
            for (fromNDX = 0; fromNDX < 50; fromNDX++)
            {
                for (toNDX = 0; toNDX < 200; toNDX++)
                {
                    GlobalVar.NNWeight1[fromNDX, toNDX] = GlobalVar.features[featureNDX];
                    featureNDX++;
                    GlobalVar.NNWeight1[fromNDX, toNDX] = GlobalVar.NNWeight1[fromNDX, toNDX] + (256.0 * GlobalVar.features[featureNDX]);
                    featureNDX++;
                    GlobalVar.NNWeight1[fromNDX, toNDX] = GlobalVar.NNWeight1[fromNDX, toNDX] / (256.0*255.0);
                    GlobalVar.NNWeight1[fromNDX, toNDX] = GlobalVar.NNWeight1[fromNDX, toNDX] - 0.5;
                    GlobalVar.NNWeight1[fromNDX, toNDX] = GlobalVar.NNWeight1[fromNDX, toNDX] * 2;
                }
            }

            // second level 200 -> 250
            for (fromNDX = 0; fromNDX < 200; fromNDX++)
            {
                for (toNDX = 0; toNDX < 250; toNDX++)
                {
                    GlobalVar.NNWeight2[fromNDX, toNDX] = GlobalVar.features[featureNDX];
                    featureNDX++;
                    GlobalVar.NNWeight2[fromNDX, toNDX] = GlobalVar.NNWeight2[fromNDX, toNDX] + (256.0 * GlobalVar.features[featureNDX]);
                    featureNDX++;
                    GlobalVar.NNWeight2[fromNDX, toNDX] = GlobalVar.NNWeight2[fromNDX, toNDX] / (256.0 * 255.0);
                    GlobalVar.NNWeight2[fromNDX, toNDX] = GlobalVar.NNWeight2[fromNDX, toNDX] - 0.5;
                    GlobalVar.NNWeight2[fromNDX, toNDX] = GlobalVar.NNWeight2[fromNDX, toNDX] * 2;

                }
            }

            // third level 250 -> 150
            for (fromNDX = 0; fromNDX < 250; fromNDX++)
            {
                for (toNDX = 0; toNDX < 150; toNDX++)
                {
                    GlobalVar.NNWeight3[fromNDX, toNDX] = GlobalVar.features[featureNDX];
                    featureNDX++;
                    GlobalVar.NNWeight3[fromNDX, toNDX] = GlobalVar.NNWeight3[fromNDX, toNDX] + (256.0 * GlobalVar.features[featureNDX]);
                    featureNDX++;
                    GlobalVar.NNWeight3[fromNDX, toNDX] = GlobalVar.NNWeight3[fromNDX, toNDX] / (256.0 * 255.0);
                    GlobalVar.NNWeight3[fromNDX, toNDX] = GlobalVar.NNWeight3[fromNDX, toNDX] - 0.5;
                    GlobalVar.NNWeight3[fromNDX, toNDX] = GlobalVar.NNWeight3[fromNDX, toNDX] * 2;
                }
            }

            // fourth level 150 -> 15
            for (fromNDX = 0; fromNDX < 150; fromNDX++)
            {
                for (toNDX = 0; toNDX < 15; toNDX++)
                {
                    GlobalVar.NNWeight4[fromNDX, toNDX] = GlobalVar.features[featureNDX];
                    featureNDX++;
                    GlobalVar.NNWeight4[fromNDX, toNDX] = GlobalVar.NNWeight4[fromNDX, toNDX] + (256.0 * GlobalVar.features[featureNDX]);
                    featureNDX++;
                    GlobalVar.NNWeight4[fromNDX, toNDX] = GlobalVar.NNWeight4[fromNDX, toNDX] / (256.0 * 255.0);
                    GlobalVar.NNWeight4[fromNDX, toNDX] = GlobalVar.NNWeight4[fromNDX, toNDX] - 0.5;
                    GlobalVar.NNWeight4[fromNDX, toNDX] = GlobalVar.NNWeight4[fromNDX, toNDX] * 2;
                }
            }

        }


        static void DailyIterations()
        {
            int matchDate = 0;
            int dailyNDX = 10;
            int dateCompare = 0;
            bool gotDate = false;
            int inputNDX = 0;
            bool moreDates = true;
            int fromNDX, toNDX;
            double bestScore = 0.0;
            int bestNDX = 0;
            double todaysActual = 0.0;
            int todaysBand = 0;
            int extraPenalty = 0;

            TextWriter cxm = new StreamWriter(GlobalVar.cfmxName, false);

            for (int xx = 0; xx < 10; xx++)
                GlobalVar.lastNDX[xx] = 1;
            matchDate = GlobalVar.dateCheck[GlobalVar.testNDX, dailyNDX];

            while (moreDates)
            {
                // get first text date for test index

                // find matching date for all indices
                for (int xx = 0; xx < 10; xx++)
                {
                    gotDate = false;
                    while (!gotDate)
                    {
                        dateCompare = GlobalVar.dateCheck[xx, GlobalVar.lastNDX[xx]].CompareTo(matchDate);

                        if (dateCompare < 0)
                        {
                            GlobalVar.lastNDX[xx]++;
                        }
                        if (dateCompare.Equals(0))
                        {
                            gotDate = true;
                        }
                        if (dateCompare > 0)
                        {
                            GlobalVar.lastNDX[xx]--;
                            gotDate = true;
                        }
                    }
                }

                todaysActual = 100 * GlobalVar.deltaValues[GlobalVar.testNDX, GlobalVar.lastNDX[GlobalVar.testNDX]];
                // which band?

                todaysBand = Convert.ToInt16((todaysActual - GlobalVar.indexStats[GlobalVar.testNDX, 0]) 
                    / ( 0.5 * GlobalVar.indexStats[GlobalVar.testNDX, 1]));

                if (todaysBand < -7)
                    todaysBand = -7;
                if (todaysBand > 7)
                    todaysBand = 7;

        //        Console.WriteLine("a: " + todaysActual.ToString() + " " + todaysBand.ToString() + " " +
        //            GlobalVar.indexStats[GlobalVar.testNDX, 0]);

                // have starting positions for all indices (need to shift down by 1)
                inputNDX = 0;
                for (int xx = 0; xx < 10; xx++)
                {
                    if ((xx < 6) || (xx > 8)) // can use same day for asian, euro markets if looking at na index
                        GlobalVar.lastNDX[xx]--; // to previous day
                    for (int xy = 0; xy < 5; xy++)
                    {
                   //     Console.WriteLine(xx.ToString() + " " + xy.ToString() + " " + inputNDX.ToString() + " " +
                     //      GlobalVar.deltaValues[xx, GlobalVar.lastNDX[xx] - xy].ToString());
                        GlobalVar.NNInput[inputNDX] = GlobalVar.deltaValues[xx, GlobalVar.lastNDX[xx] - xy];
                        inputNDX++;
                    }
                }
                // inputs are loaded
                // here we'll calculate all the nodes

                int hiddenLevel = 0;
                // first level 50 -> 200
                for (toNDX = 0; toNDX < 200; toNDX++)
                {
                    GlobalVar.NNHidden[hiddenLevel,toNDX] = 0;
                    for (fromNDX = 0; fromNDX < 50; fromNDX++)
                    {
                        GlobalVar.NNHidden[hiddenLevel, toNDX] = GlobalVar.NNHidden[hiddenLevel,toNDX] + 
                            GlobalVar.NNInput[fromNDX] * GlobalVar.NNWeight1[fromNDX, toNDX];
                    }
                }

                // second level 200 -> 250
                hiddenLevel++;
                for (toNDX = 0; toNDX < 250; toNDX++)
                {
                    GlobalVar.NNHidden[hiddenLevel, toNDX] = 0;
                    for (fromNDX = 0; fromNDX < 200; fromNDX++)
                    {
                        GlobalVar.NNHidden[hiddenLevel, toNDX] = GlobalVar.NNHidden[hiddenLevel, toNDX] +
                            GlobalVar.NNHidden[hiddenLevel-1, fromNDX] * GlobalVar.NNWeight2[fromNDX, toNDX];
                    }
                }

                // third level 250 -> 150
                hiddenLevel++;
                for (toNDX = 0; toNDX < 150; toNDX++)
                {
                    GlobalVar.NNHidden[hiddenLevel, toNDX] = 0;
                    for (fromNDX = 0; fromNDX < 250; fromNDX++)
                    {
                        GlobalVar.NNHidden[hiddenLevel, toNDX] = GlobalVar.NNHidden[hiddenLevel, toNDX] +
                            GlobalVar.NNHidden[hiddenLevel-1, fromNDX] * GlobalVar.NNWeight3[fromNDX, toNDX];
                    }
                }

                // fourth level 150 -> 15
                hiddenLevel++;
                for (toNDX = 0; toNDX < 15; toNDX++)
                {
                    GlobalVar.NNOutput[toNDX] = 0;
                    for (fromNDX = 0; fromNDX < 150; fromNDX++)
                    {
                        GlobalVar.NNOutput[toNDX] = GlobalVar.NNOutput[toNDX] +
                            GlobalVar.NNHidden[hiddenLevel - 1, fromNDX] * GlobalVar.NNWeight4[fromNDX, toNDX];
                    }
                }
                bestScore = -99999999;
                bestNDX = -1;
                for (toNDX = 0; toNDX < 15; toNDX++)
                {
                    if (GlobalVar.NNOutput[toNDX] > bestScore)
                    {
                        bestScore = GlobalVar.NNOutput[toNDX];
                        bestNDX = toNDX;
                    }
                }

                bestNDX = bestNDX - 7;


                extraPenalty = Math.Abs(todaysBand) + 1;

                GlobalVar.runningScore = GlobalVar.runningScore + 1000 - 
                    (GlobalVar.penaltyValue[Math.Abs(todaysBand - bestNDX)] * extraPenalty);


                // dsm print confusion matrix
         //       if (GlobalVar.printFlag)
         //       {
                    cxm.WriteLine(dailyNDX.ToString() + "," + todaysBand.ToString() + "," + bestNDX.ToString());
              //      Console.WriteLine("results, " + dailyNDX.ToString() + "," + todaysBand.ToString() + "," + bestNDX.ToString());
         //       }

           //     if ((todaysBand > 2) ||(todaysBand < -2))
           //         Console.WriteLine("results, " + dailyNDX.ToString() + "," + todaysBand.ToString() + "," + bestNDX.ToString());

             //   Console.WriteLine(dailyNDX.ToString() + " actual " + todaysActual.ToString() + " " + todaysBand.ToString() + 
             //       " best guess = " + bestNDX.ToString() + " " + bestScore.ToString() + " " + GlobalVar.runningScore.ToString());


                dailyNDX++;
                matchDate = GlobalVar.dateCheck[GlobalVar.testNDX, dailyNDX];
                if (matchDate.Equals(999999))
                    moreDates = false;
            }

        //    System.Threading.Thread.Sleep(200000);

                // compare calculation to reality
            // what is reality?  that day's return adjusted by average and 1/2 std dev
                
            // at final date - have total score - write results (different routine?)

            cxm.Close();   
        }

        static void ImportXMLfile(string XMLfile)
        {

            int featureNDX = 0;
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
                        if (elementString.Equals("Index"))
                        {
                            featureNDX = Convert.ToInt32(reader.Value);
                            if (featureNDX > GlobalVar.testCount)
                                GlobalVar.testCount = featureNDX;
                        }
                        if (elementString.Equals("Value"))
                        {
                            GlobalVar.features[featureNDX] = Convert.ToInt32(reader.Value);
                        }
                        if (elementString.Equals("Generation"))
                        {
                            GlobalVar.myGeneration = Convert.ToInt32(reader.Value);
                        }
                        break;
                    case XmlNodeType.EndElement: //Display the end of the element.
                        break;
                }

            }
            reader.Close();
        }


        static void ExportXMLfile(string XMLfile)
        {
            string filename = XMLfile;

            try
            {
                File.Delete(XMLfile);
            }
            catch
            {

            }

            XmlTextWriter xml = null;

            xml = new XmlTextWriter(filename, null);


            xml.WriteStartDocument();
            xml.WriteStartElement("Features");
            xml.WriteWhitespace("\n");
            xml.WriteElementString("Score", GlobalVar.myScore.ToString());
            xml.WriteWhitespace("\n  ");

            xml.WriteEndElement();
            xml.WriteWhitespace("\n");

            xml.WriteEndDocument();

            //Write the XML to file and close the writer.
            xml.Flush();
            xml.Close();
        }
    }
}
