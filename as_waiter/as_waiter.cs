using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MySql.Data.MySqlClient;


namespace as_waiter
{
    class as_waiter
    {
        static void Main(string[] args)
        {
            int elapsedSeconds = 0;
            bool keepGoing = true;
            string commandFile = "as_run.txt";
            string line;
            string scriptName = "";
            string resourceName = "dsm_ic";
            string scriptType = "bat";
            string fn = "";


            if (args.Length > 0) // first arg should be mandatory - job name
            {
                resourceName = args[0];
            }

            if (args.Length > 1) // first arg should be mandatory - job name
            {
                scriptType = args[1];
            }

            // establish SQL connection in main procedure, it will be passed to other procedures

            string connectionString;
            MySqlConnection myConnection;

            connectionString = "SERVER=rdc04.uits.iu.edu" + ";" + "Port=3059" + ";" + "DATABASE=agent" +
        ";" + "UID=agentx" + ";" + "PASSWORD=************" + ";";

            myConnection = new MySqlConnection(connectionString);

            try
            {
                myConnection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            while (keepGoing)
            {

                Console.WriteLine("ticks : " + elapsedSeconds.ToString());
                scriptName = "";

            MySqlCommand waitCommand = new MySqlCommand("SELECT wait_job FROM wait_jobs " +
                "where wait_resource = @waitParam", myConnection);
            waitCommand.Parameters.AddWithValue("@waitParam", resourceName);

            try
            {
                MySqlDataReader myReader = waitCommand.ExecuteReader();

                while (myReader.Read())
                {
                    scriptName = myReader.GetString("wait_job");
                }
                myReader.Close();

                MySqlCommand delCommand = new MySqlCommand("DELETE FROM wait_jobs " +
    "where wait_resource = @waitParam", myConnection);
                delCommand.Parameters.AddWithValue("@waitParam", resourceName);
                delCommand.ExecuteNonQuery();
                delCommand.Dispose();

                if (scriptType.Equals("bat") && (!String.IsNullOrEmpty(scriptName)))
                {
                    fn = "ax_job.bat";
                    StreamWriter sw = new StreamWriter(fn);
                    sw.WriteLine(scriptName);
                    sw.Close();
                    scriptName = fn;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }


                    // look at parameters to show console on screen
                    if (!String.IsNullOrEmpty(scriptName))
                    {
                        Console.WriteLine("running - " + scriptName);
                        System.Diagnostics.Process proc = new System.Diagnostics.Process();
                        proc.StartInfo.FileName = scriptName;
                        proc.StartInfo.RedirectStandardError = true;
                        proc.StartInfo.RedirectStandardOutput = true;
                        proc.StartInfo.UseShellExecute = false;
                        proc.Start();
                        proc.WaitForExit();
                        Console.WriteLine("free");
                    }

                System.Threading.Thread.Sleep(5000);
                elapsedSeconds = elapsedSeconds + 5;
                if (elapsedSeconds >= (86400))
                {
                    keepGoing = false;
                }

            }

        }
    }
}
