using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Xml;

namespace as_put
{
    class as_put
    {

        public static class GlobalVar
        {
            public static int popCount = 0;
            public static int popIndex = 0;
            public static string jobName = "test";
            public static int myGeneration = 0;
            public static Random random = new Random();
            public static int[] features = new int[5000];
            public static int featureCount = 0;
        }

        static void Main(string[] args)
        {


            // initialize local variables

            Random random = new Random();
            int myPopNumber = 0;
            int runLoop = 100;
            string jobType = null;

            // get input parameters for job name and iteration count

            if (args.Length > 0) // first arg should be mandatory - job name
            {
                GlobalVar.jobName = args[0];
            }

            if (args.Length > 1)
            {
                runLoop = Convert.ToInt16(args[1]);
            }


            // establish SQL connection in main procedure, it will be passed to other procedures

            SqlConnection myConnection = new SqlConnection("user id=mssql;" +
                                                   "password=AgentX#666;server=129.79.49.130;" +
                                                   "Trusted_Connection=no;" +
                                                   "database=agent; " +
                                                   "connection timeout=30");

            try
            {
                myConnection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }



                GlobalVar.featureCount = 0;


                // read xml

                ImportXMLfile(myPopNumber);

                // update my data and unflag me 

                SqlCommand unflagCommand = new SqlCommand("update agent.dbo.member_header set member_busy = 0, " +
                    " member_generation = @parmGeneration " +
                    " where job_name = @parmJob and population_index = @parmPop and member_index = @parmMember ", myConnection);
                SqlParameter unflagGeneration = new SqlParameter("@parmGeneration", SqlDbType.BigInt);
                GlobalVar.myGeneration++;
                unflagGeneration.Value = GlobalVar.myGeneration;
                unflagCommand.Parameters.Add(unflagGeneration);
                SqlParameter unflagJob = new SqlParameter("@parmJob", SqlDbType.VarChar, 16);
                unflagJob.Value = GlobalVar.jobName;
                unflagCommand.Parameters.Add(unflagJob);
                SqlParameter unflagPop = new SqlParameter("@parmPop", SqlDbType.BigInt);
                unflagPop.Value = GlobalVar.popIndex;
                unflagCommand.Parameters.Add(unflagPop);
                SqlParameter unflagMember = new SqlParameter("@parmMember", SqlDbType.BigInt);
                unflagMember.Value = myPopNumber;
                unflagCommand.Parameters.Add(unflagMember);
                unflagCommand.ExecuteNonQuery();
                unflagCommand.Dispose();



            System.Threading.Thread.Sleep(5000);

        }

        static void ImportXMLfile(int popMember)
        {
            string filename = "";

            filename = GlobalVar.jobName + GlobalVar.popIndex.ToString() + popMember.ToString();

            XmlTextWriter xml = null;

            xml = new XmlTextWriter(filename, null);


            xml.WriteStartDocument();
            xml.WriteStartElement("Features");
            xml.WriteWhitespace("\n");

            for (int i = 0; i < GlobalVar.featureCount; i++)
            {
                xml.WriteElementString("Index", i.ToString());
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
