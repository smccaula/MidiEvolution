using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace as_solution
{
    class as_solution
    {

        public static class GlobalVar
        {
            public static int[] features = new int[350000];
            public static int featureCount = 1024;
            public static Random random = new Random();
        }

        static void Main(string[] args)
        {
            int newIntValue = 0;

            if (args.Length > 0) // first arg should be mandatory - job name
            {
                GlobalVar.featureCount = Convert.ToInt32(args[0]);
            }

            for (int i = 0; i < GlobalVar.featureCount; i++)
            {
                newIntValue = GlobalVar.random.Next(0, 255);
          //      newIntValue = 255;  //maxone
                GlobalVar.features[i] = newIntValue;
            }

            string XMLfile = "solution.xml";

            ExportXMLfile(XMLfile);
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
                Console.Write(".X7." + XMLfile + ".");
            }

            XmlTextWriter xml = null;

            xml = new XmlTextWriter(filename, null);


            xml.WriteStartDocument();
            xml.WriteStartElement("Features");
            xml.WriteWhitespace("\n");

            for (int i = 0; i < GlobalVar.featureCount; i++)
            {
                xml.WriteElementString("Index", i.ToString());
                xml.WriteWhitespace("\n  ");
                xml.WriteElementString("Value", GlobalVar.features[i].ToString());
                xml.WriteWhitespace("\n  ");
            }

            xml.WriteEndElement();
            xml.WriteWhitespace("\n");

            xml.WriteEndDocument();

            //Write the XML to file and close the writer.
            xml.Flush();
            xml.Close();
        }


    }
}
