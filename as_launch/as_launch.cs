using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Threading;
using MySql.Data.MySqlClient;

namespace as_launch
{
    class as_launch
    {

        public static class GlobalVar
        {
            public static int savedGeneration = 0;
            public static long savedScore = 0;
        }

        static void Main(string[] args)
        {

            string jobName = "stones";
            string jobType = "";
            int runSeconds = 60;
            int maxConcurrent = 4;
            bool keepGoing = true;
            bool keepWaiting = false;
            int myUniqueID = 0;
            int elapsedSeconds = 0;
            Random random = new Random();
            Process[] getProcess = new Process[512];
            int launchCtr = 0;
        //    int prevCtr = 0;
            int runLoop = 1;
       //     int deleteCounter = 0;
            int unFlag = 0;

            if (args.Length > 0) // first arg should be mandatory - job name
            {
                jobName = args[0];
            }

            if (args.Length > 1)
            {
                maxConcurrent = Convert.ToInt32(args[1]);
            }

            if (args.Length > 2)
            {
                runSeconds = Convert.ToInt32(args[2]);
            }

            if (args.Length > 3)
            {
                runLoop = Convert.ToInt32(args[3]);
            } 
            
            if (String.IsNullOrEmpty(jobName))
            {
                Console.WriteLine("missing job parameter - cancelling");
                return;
            }

            if (maxConcurrent < 1)
            {
                Console.WriteLine("missing or invalid concurrent jobs parameter - cancelling");
                return;
            }

            if (runSeconds < 1)
            {
                Console.WriteLine("missing or invalid run time parameter - cancelling");
                return;
            }

            // establish SQL connection in main procedure, it will be passed to other procedures

            string connectionString;
            MySqlConnection myConnection;

            connectionString = "SERVER=rdc04.uits.iu.edu" + ";" + "Port=3059" + ";" + "DATABASE=agent" +
        ";" + "UID=agentx" + ";" + "PASSWORD=mysqlX666" + ";";

            myConnection = new MySqlConnection(connectionString);

            try
            {
                myConnection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            jobType = TestJobValidity(ref myConnection, jobName);
//            myUniqueID = GetJobID(ref myConnection, jobName);
            myUniqueID = random.Next(1, 32000);

            Console.WriteLine("starting process ID : " + myUniqueID.ToString());

            if (String.IsNullOrEmpty(jobType))
            {
                Console.WriteLine("job not found - cancelling");
                return;
            }


            for (int i = 0; i < maxConcurrent; i++)
            {
                getProcess[i] = new Process();

                getProcess[i].StartInfo.CreateNoWindow = true; 
                getProcess[i].StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                getProcess[i].StartInfo.FileName = "as_get.exe";
                getProcess[i].StartInfo.Arguments = " " + jobName + " " + Convert.ToString(runLoop) + " " +
                    Convert.ToString(myUniqueID) + " " + Convert.ToString(launchCtr);
                getProcess[i].Start();

                System.Threading.Thread.Sleep(5); // offset them to get unique random seeds
            }
            launchCtr = maxConcurrent; 

            MySqlCommand unflagCommand = new MySqlCommand("delete from busy_table " +
                " where process_id = @parmID and process_gen < @parmGen ", myConnection);
            unflagCommand.Parameters.AddWithValue("@parmID", myUniqueID);
            unflagCommand.Parameters.AddWithValue("@parmGen", 0);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            TimeSpan ts = stopWatch.Elapsed;

            Console.WriteLine("Cycles, Seconds, Generation, TopScore, Goal, Pct");

            while (keepGoing)
            {

                ts = stopWatch.Elapsed;
                elapsedSeconds = ts.Seconds + (60 * ts.Minutes) + (60 * 60 * ts.Hours) + (60 * 60 * 24 * ts.Days);

                if (elapsedSeconds >= (runSeconds))
                {
                    Console.WriteLine("...time expired, shutting down...");
                    keepGoing = false;
                }
                for (int i = 0; i < maxConcurrent; i++)
                {
                    if (getProcess[i].HasExited) 
                    {
                        getProcess[i].Dispose();

                        if (keepGoing)
                        {
                            if (GetTopScore(ref myConnection, elapsedSeconds, jobName, launchCtr) > 0)
                            {
                           //     Console.WriteLine("...goal achieved, shutting down...");
                           //     keepGoing = false;
                            }
                            if (GetTopScore(ref myConnection, elapsedSeconds, jobName, launchCtr) < 0)
                            {
                                Console.WriteLine("...failure, shutting down...");
                                keepGoing = false;
                            }
                            getProcess[i] = new Process();

                            getProcess[i].StartInfo.CreateNoWindow = true;
                            getProcess[i].StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                            getProcess[i].StartInfo.FileName = "as_get.exe";
                            getProcess[i].StartInfo.Arguments = " " + jobName + " " + Convert.ToString(runLoop) + " " +
                                Convert.ToString(myUniqueID) + " " + Convert.ToString(launchCtr);
                            getProcess[i].Start();
                            launchCtr++;
                        }
                    }
                }
           //     if (!prevCtr.Equals(launchCtr))
           //     {
           //         prevCtr = launchCtr;
           //     }

                // delete dead 'busy' records
                 //   unFlag = (launchCtr - maxConcurrent);
                 //   unflagCommand.Parameters.Clear();
                 //   unflagCommand.Parameters.AddWithValue("@parmID", myUniqueID);
                 //   unflagCommand.Parameters.AddWithValue("@parmGen", unFlag);
                 //   unflagCommand.ExecuteNonQuery();
                 //   unflagCommand.Dispose();
            }

//            int finalGen = GetTopScore(ref myConnection, elapsedSeconds, jobName, launchCtr);
//
//            MySqlCommand updateGen = new MySqlCommand("update run_data set generation_count = @genParm " +
//" where job_name = @jobParm and job_index = @ndxParm ", myConnection);
//            updateGen.Parameters.AddWithValue("@jobParm", jobName);
//            updateGen.Parameters.AddWithValue("@genParm", finalGen);
//            updateGen.Parameters.AddWithValue("@ndxParm", myUniqueID);

//            updateGen.ExecuteNonQuery();

            // wait for launched processes to finish

            keepWaiting = true;

            while (keepWaiting)
            {
                keepWaiting = false;
                for (int i = 0; i < maxConcurrent; i++)
                {
                    try
                    {
                        if (!getProcess[i].HasExited)
                            keepWaiting = true;
                    }
                    catch (Exception e)
                    {
                       // Console.WriteLine(e.ToString());
                    }

                }
            }

//            unFlag = (launchCtr + 1);

//            unflagCommand.Parameters.Clear();
//            unflagCommand.Parameters.AddWithValue("@parmID", myUniqueID);
//            unflagCommand.Parameters.AddWithValue("@parmGen", unFlag);
//            unflagCommand.ExecuteNonQuery();
//            unflagCommand.Dispose();

            Console.WriteLine("done.");
        }

        static int GetTopScore(ref MySqlConnection myConnection, int elapsedSeconds, string jobName, int launchCtr)
        {

            long foundScore = 0;
            long bestScore = 0;
            int bestGeneration = 0;
            double pctDone = 0.0;

            MySqlCommand scoreCommand = new MySqlCommand("SELECT * FROM best_scores " +
                "where job_name = @jobParam order by top_score desc", myConnection);
            scoreCommand.Parameters.AddWithValue("@jobParam", jobName);

            try
            {
                MySqlDataReader myReader = scoreCommand.ExecuteReader();

                while (myReader.Read())
                {
                    foundScore =  myReader.GetInt64("top_score");
                    bestScore = myReader.GetInt64("possible_score");
                    bestGeneration = myReader.GetInt32("avg_generation");
                }
                myReader.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            if ((bestGeneration > GlobalVar.savedGeneration))
            //    if ((bestGeneration > GlobalVar.savedGeneration) || (foundScore > GlobalVar.savedScore))
                {
                pctDone = (Convert.ToDouble(1.0*foundScore) / (1.0*Convert.ToDouble(bestScore)));
                Console.WriteLine(launchCtr.ToString() + "," + elapsedSeconds.ToString() + "," + bestGeneration.ToString() + "," + foundScore.ToString() + "," + bestScore.ToString() 
                    + "," + pctDone.ToString("P") );
                GlobalVar.savedGeneration =  bestGeneration;
                GlobalVar.savedScore = foundScore;
            }

      //      if ((bestGeneration < 10) && (foundScore > (bestScore * 0.75)))
      //          bestGeneration = -1;

      //      if (bestGeneration > 1200)
      //          bestGeneration = -1;

            if (bestScore > foundScore)
                bestGeneration = 0;

            return (bestGeneration);
        }

        static string TestJobValidity(ref MySqlConnection myConnection, string jobName)
        {
            // now is only testing for existence of job, will eventually test user authorization, etc.

            string jobType = null;


            MySqlCommand myCommand = new MySqlCommand("select job_type from job_table where job_name = @Param1", myConnection);
            myCommand.Parameters.AddWithValue("@Param1", jobName);

            try
            {
                MySqlDataReader myReader = myCommand.ExecuteReader();

                while (myReader.Read())
                {
                    jobType = myReader.GetString("job_type");
                }
                myReader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return ("GA");
         //   return (jobType);

        }

        static int GetJobID(ref MySqlConnection myConnection, string jobName)
        {
            int jobID  = 0;

//            MySqlCommand myCommand = new MySqlCommand("select job_index from run_data where job_name = @Param1 "
//                + " and generation_count = 0", myConnection);
//            myCommand.Parameters.AddWithValue("@Param1", jobName);

//            try
//            {
//                MySqlDataReader myReader = myCommand.ExecuteReader();

//                while (myReader.Read())
//                {
//                    jobID = myReader.GetInt32("job_index");
//                }
//                myReader.Close();
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e.ToString());
//            }

            return (jobID);
        }

    }
}
