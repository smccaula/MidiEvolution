﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace as_user
{
    class as_user
    {
        public static class GlobalVar
        {
            public static int[] features = new int[65536];
            public static int[] solutions = new int[65536];

            public static int featureCount = 0;
            public static int testCount = 0;
            public static int myGeneration = 0;
            public static int myScore = 0;
            public static Random random = new Random();
        }

        static void Main(string[] args)
        {
            string XMLfile = "solution.xml";

            try
            {
                ImportSolutionfile(XMLfile);
            }
            catch
            {
                return;
            }

            if (args.Length > 0) 
            {
                XMLfile = args[0];
            }

            if (String.IsNullOrEmpty(XMLfile))
            {
      //          Console.Write(".X3." + XMLfile + ".");
                return;
            }

            try
            {
                ImportXMLfile(XMLfile);
            }
            catch
            {
      //          Console.Write(".X4." + XMLfile + ".");
                return;
            }

            if (GlobalVar.featureCount != GlobalVar.testCount)
            {
     //           Console.Write(".X1." + XMLfile + ".");
                return;
            }

            ProcessAndScore();

            ExportXMLfile(XMLfile);

        }

        static void ImportSolutionfile(string XMLfile)
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
                            if (featureNDX > GlobalVar.featureCount)
                                GlobalVar.featureCount = featureNDX;
                        }
                        if (elementString.Equals("Value"))
                        {
                            GlobalVar.solutions[featureNDX] = Convert.ToInt32(reader.Value);
                            GlobalVar.features[featureNDX] = 0;
                        }
                        break;
                    case XmlNodeType.EndElement: //Display the end of the element.
                        break;
                }

            }
            reader.Close();
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

        static void ProcessAndScore()
        {

            int bitDifference = 0;
            int bitPossible = 0;
            int bitValInd = 0;
            int bitValSol = 0;
            int bitVal = 0;
            int tempInd = 0;
            int tempSol = 0;

            for (int i = 0; i <= (GlobalVar.featureCount); i++)
            {
                bitVal = 128;
                tempInd = GlobalVar.features[i];
                tempSol = GlobalVar.solutions[i];
                for (int tx = 0; tx < 8; tx++)
                {
                    bitValInd = tempInd / bitVal;
                    bitValSol = tempSol / bitVal;
                    bitPossible++;
                    if (bitValInd != bitValSol)
                        bitDifference++;
                    tempInd = tempInd - (bitVal * bitValInd);
                    tempSol = tempSol - (bitVal * bitValSol);
                    bitVal = bitVal / 2;
                }



            }

            GlobalVar.myScore = bitPossible - bitDifference;
       //     System.Threading.Thread.Sleep(25);



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
        //        Console.Write(".X2." + XMLfile + ".");
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
