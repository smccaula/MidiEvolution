using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.ComponentModel;
using MySql.Data.MySqlClient;

namespace as_get
{
    class ts_get
    {

        public static class GlobalVar
        {
            public static int popCount = 500;
            public static int popIndex = 0;
            public static string jobName = "test";
            public static int myGeneration = 0;
            public static int myScore = 0;
            public static Random random = new Random();
            public static int[,] features = new int[65536,3];
            public static int featureCount = 4096;
            public static int launchGeneration = 0;
            public static int myUniqueID = 0;
            public static int bestScore = 0;
            public static int getSeconds = 0;
            public static int putSeconds = 0;
            public static int getCtr = 0;
            public static int putCtr = 0;
            public static int mutPer10000 = 5;
            public static long calcCount = featureCount*1;
        }

        static void Main(string[] args)
        {

            // initialize local variables
            
            int myPopNumber = 0;
            int runLoop = 4;
            string jobType = null;
            string XMLfile = "";
            int tempValue = 0;
            int tempMult = 0;

   //         Stopwatch stopWatch = new Stopwatch();
   //         TimeSpan ts = stopWatch.Elapsed;
   //         stopWatch.Start();

            // get input parameters for job name and iteration count

            if (args.Length > 0) // first arg should be mandatory - job name
            {
                GlobalVar.jobName = args[0];
            }

            if (args.Length > 1)
            {
                runLoop = Convert.ToInt32(args[1]);
                runLoop = (runLoop / 2) + (GlobalVar.random.Next(1, runLoop));
            }

            if (args.Length > 2)
            {
                GlobalVar.myUniqueID = Convert.ToInt32(args[2]);
            }

            if (args.Length > 3)
            {
                GlobalVar.launchGeneration = Convert.ToInt32(args[3]);
            }

            if (args.Length > 4)
            {
                GlobalVar.calcCount = GlobalVar.featureCount * Convert.ToInt32(args[4]);
        //        Console.WriteLine("calc = " + GlobalVar.calcCount.ToString());
            }

      //      GlobalVar.launchGeneration = 17;

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
                Console.WriteLine("no database access - cancelling");
                return;
            }

            // check job_table that: job exists, users is authorized, save job type

            jobType = TestJobValidity(ref myConnection);

            if (String.IsNullOrEmpty(jobType))
            {
                Console.WriteLine("job not found - cancelling");
                return;
            }



            while (runLoop > 0)
            {

           //     GlobalVar.featureCount = 0;

                myPopNumber = GetMemberToProcess(ref myConnection);

                if (GlobalVar.myGeneration.Equals(0))
                {
               //    stopWatch.Restart();
                    PopulateEmptyMember(ref myConnection, myPopNumber);
               //    stopWatch.Stop();
               //     ts = stopWatch.Elapsed;
               //     GlobalVar.getSeconds = GlobalVar.getSeconds + ts.Milliseconds;
               //     GlobalVar.getCtr++;
                }
                else
                {
              //      stopWatch.Restart();
                    GetExistingCharacteristics(ref myConnection, myPopNumber,0);
              //      stopWatch.Stop();
              //      ts = stopWatch.Elapsed;
              //      GlobalVar.getSeconds = GlobalVar.getSeconds + ts.Milliseconds;
              //      GlobalVar.getCtr++;

                }

           //    stopWatch.Restart();


                for (long cx = 0; cx < (GlobalVar.calcCount); cx++)
                {
                    tempValue = tempValue * tempMult;
                }

               if ((GlobalVar.myGeneration > 0) )
               {

                   NextGenerationValues(ref myConnection, myPopNumber);
               }

         //       stopWatch.Stop();
         //      ts = stopWatch.Elapsed;
         //      GlobalVar.putSeconds = GlobalVar.putSeconds + ts.Milliseconds;
         //      GlobalVar.putCtr++;


                // write out xml

       //         XMLfile = ExportXMLfile(myPopNumber);

                // call the user processing job in the series...

       //         Process userProcess = new Process();

         //       userProcess.StartInfo.CreateNoWindow = true;
         //       userProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
         //       userProcess.StartInfo.FileName = "as_user.exe";
         //       userProcess.StartInfo.Arguments = XMLfile;
         //       userProcess.Start();

         //       userProcess = Process.Start("as_user.exe");  // user process will update the score

         //       userProcess.WaitForExit();
         //       userProcess.Dispose();


         //      for (int tz = 0; tz < GlobalVar.featureCount; tz++)
          //     {

          //     }


    // get updated XML (with score)
                try
                {
          //          ImportXMLfile(XMLfile);
                
            
            }
                catch
                {
                    Console.Write(".X5." + XMLfile + "."); // busy flag!!!!!!
                    return;
                }

                // update my data and unflag me (should go in final program)





                MySqlCommand updateCommand = new MySqlCommand("update member_header " +
                    " set member_generation = @parmGeneration, member_score = @parmScore " +
                    " where job_name = @parmJob and population_index = @parmPop and member_index = @parmMember ", myConnection);

                GlobalVar.myGeneration++;
                updateCommand.Parameters.AddWithValue("@parmGeneration", GlobalVar.myGeneration);
                updateCommand.Parameters.AddWithValue("@parmScore", GlobalVar.myScore);
                updateCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
                updateCommand.Parameters.AddWithValue("@parmPop", GlobalVar.popIndex);
                updateCommand.Parameters.AddWithValue("@parmMember", myPopNumber);

                updateCommand.ExecuteNonQuery();
                updateCommand.Dispose();

                try
                {
             //       File.Delete(XMLfile);
                }
                catch
                {
                    Console.Write(".X6." + XMLfile + ".");
                }

                // am I still looping?

                MySqlCommand unflagCommand = new MySqlCommand("delete from busy_table " +
    " where job_name = @parmJob and population_index = @parmPop and member_index = @parmMember ", myConnection);

                unflagCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
                unflagCommand.Parameters.AddWithValue("@parmPop", GlobalVar.popIndex);
                unflagCommand.Parameters.AddWithValue("@parmMember", myPopNumber);
                
                unflagCommand.ExecuteNonQuery();
                unflagCommand.Dispose();

                runLoop--;

            }
            InsertBestScore(ref myConnection);
        }

        static string TestJobValidity(ref MySqlConnection myConnection)
        {
            // now is only testing for existence of job, will eventually test user authorization, etc.

            string jobType = null;

            MySqlCommand myCommand = new MySqlCommand("select job_type from job_table where job_name = @Param1", myConnection);
            myCommand.Parameters.AddWithValue("@Param1", GlobalVar.jobName);

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


        static void InsertBestScore(ref MySqlConnection myConnection)
        {

            int avgGeneration = 0;
            MySqlDataReader myReader = null;

            MySqlCommand genCommand = new MySqlCommand("SELECT AVG (member_generation) as 'generation' FROM member_header where job_name = @genParm", myConnection);

            genCommand.Parameters.AddWithValue("@genParm", GlobalVar.jobName);

            try
            {
                myReader = genCommand.ExecuteReader();
                while (myReader.Read())
                {
                    avgGeneration = myReader.GetInt32("generation");
                }
                myReader.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            MySqlCommand deleteCommand = new MySqlCommand("DELETE FROM best_scores where job_name = @deleteParm", myConnection);
            deleteCommand.Parameters.AddWithValue("@deleteParm", GlobalVar.jobName);

            try
            {
                deleteCommand.ExecuteNonQuery();
                deleteCommand.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            MySqlCommand insertCommand = new MySqlCommand("insert into best_scores " +
                "(job_name, avg_generation, top_score, possible_score, in_ms, out_ms) " +
                " values (@parmJob, @parmAvg, @parmTop, @parmPossible, @parmInMS, @parmOutMS) ", myConnection);

            insertCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
            insertCommand.Parameters.AddWithValue("@parmAvg", avgGeneration);
            insertCommand.Parameters.AddWithValue("@parmTop", GlobalVar.bestScore);
            insertCommand.Parameters.AddWithValue("@parmPossible", GlobalVar.featureCount * 8);

            if (GlobalVar.getCtr < 1)
                GlobalVar.getCtr = 1;
            insertCommand.Parameters.AddWithValue("@parmInMS", GlobalVar.getSeconds / GlobalVar.getCtr);

            if (GlobalVar.putCtr < 1)
                GlobalVar.putCtr = 1;
            insertCommand.Parameters.AddWithValue("@parmOutMS", GlobalVar.putSeconds / GlobalVar.putCtr);

            GlobalVar.getSeconds = 0;
            GlobalVar.getCtr = 0;
            GlobalVar.putSeconds = 0;
            GlobalVar.putCtr = 0;

            try
            {
                insertCommand.ExecuteNonQuery();
                insertCommand.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }



        static int GetMemberToProcess(ref MySqlConnection myConnection)
        {
            // find (or create) a non-busy population member, return its index

            int randomPopNumber = 0; 
            bool foundMe = false;
            bool gotResults = true;
            bool isBusy = false;

            MySqlCommand getCommand = new MySqlCommand("select member_generation, member_score from member_header " +
                " where job_name = @parmJob and population_index = @parmPop and member_index = @parmMember", myConnection);


            MySqlCommand setBusyCommand = new MySqlCommand("insert into busy_table " + 
                "(job_name, population_index, member_index, process_id, process_gen) " +
                " values (@parmJob, @parmPop, @parmMember, @parmID, @parmGen) ", myConnection);


            MySqlCommand askBusyCommand = new MySqlCommand("select member_index from busy_table " +
                " where job_name = @parmJob and population_index = @parmPop and member_index = @parmMember", myConnection);

            MySqlCommand insertCommand = new MySqlCommand("insert into member_header (job_name, population_index, member_index, member_generation, member_score) " +
    " values (@parmJob, @parmPop, @parmMember, 0, 0) ", myConnection);


            MySqlDataReader myReader = null;



            while (!foundMe)
            {
                randomPopNumber = GlobalVar.random.Next(1, GlobalVar.popCount);

                getCommand.Parameters.Clear();
                getCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
                getCommand.Parameters.AddWithValue("@parmPop", GlobalVar.popIndex);
                getCommand.Parameters.AddWithValue("@parmMember", randomPopNumber);

                insertCommand.Parameters.Clear();
                insertCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
                insertCommand.Parameters.AddWithValue("@parmPop", GlobalVar.popIndex);
                insertCommand.Parameters.AddWithValue("@parmMember", randomPopNumber);

                setBusyCommand.Parameters.Clear();
                setBusyCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
                setBusyCommand.Parameters.AddWithValue("@parmPop", GlobalVar.popIndex);
                setBusyCommand.Parameters.AddWithValue("@parmMember", randomPopNumber);
                setBusyCommand.Parameters.AddWithValue("@parmID", GlobalVar.myUniqueID);
                setBusyCommand.Parameters.AddWithValue("@parmGen", GlobalVar.launchGeneration);

                askBusyCommand.Parameters.Clear();
                askBusyCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
                askBusyCommand.Parameters.AddWithValue("@parmPop", GlobalVar.popIndex);
                askBusyCommand.Parameters.AddWithValue("@parmMember", randomPopNumber);

                try
                {
                    myReader = getCommand.ExecuteReader();
                    gotResults = myReader.HasRows;
                    if (gotResults)
                    {
                        myReader.Read();
                        GlobalVar.myGeneration = myReader.GetInt32("member_generation"); 
                        GlobalVar.myScore = myReader.GetInt32("member_score");
                    }
                    myReader.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                if (!gotResults)
                {
                    // new population member, create/insert etc.
                    try
                    {
                        insertCommand.ExecuteNonQuery();
                        insertCommand.Dispose();
                        setBusyCommand.ExecuteNonQuery();
                        setBusyCommand.Dispose();
                        foundMe = true;
                        GlobalVar.myGeneration = 0;
                    }
                    catch 
                    {
                        foundMe = false;
                    }

                }

                if (!foundMe) // exists - find out if busy
                {
                    isBusy = false;
                    myReader = askBusyCommand.ExecuteReader();
                    isBusy = myReader.HasRows;
                    myReader.Close();

                    // if it is busy, check the timestamp, it may be frozen (launch will do this)

                    if (!isBusy)
                    {

                        // flag me as occupied

                        try
                        {

                            setBusyCommand.ExecuteNonQuery();
                            setBusyCommand.Dispose();
                            foundMe = true;

                        }
                        catch 
                        {
                        }
 

                    }
                }

            }



            return randomPopNumber;
        }

        static void PopulateEmptyMember(ref MySqlConnection myConnection, int popMember)
        {
            string featureType = "";
            int newIntValue = 0;
            int featureIntMin = 0;
            int featureIntMax = 0;
            char[] buildChars;
            buildChars = new char[65536];

            try
            {


//                MySqlCommand insertCommand = new MySqlCommand("insert into member_detail (job_name, population_index, member_index, feature_index, value_index, char_value, num_value) " +
// " values (@parmNewJob, @parmNewPop, @parmNewMember, @parmNewFeature, @parmNewValue, null, @parmNewInteger) ", myConnection);
                MySqlCommand insertCommand = new MySqlCommand("insert into member_detail (job_name, population_index, member_index, feature_index, value_index, char_value, num_value) " +
 " values (@parmNewJob, @parmNewPop, @parmNewMember, @parmNewFeature, @parmNewValue, @parmNewChar, null) ", myConnection);

                for (int i = 0; i < GlobalVar.featureCount; i++)
                                    {
      //              newIntValue = GlobalVar.random.Next(featureIntMin, featureIntMax);

                    //buildChars[i] = (char)newIntValue;
                    buildChars[i] = (char)0;
                    //        insertCommand.ExecuteNonQuery();
                    GlobalVar.features[i,0] = newIntValue;
                }

                string bs = new string(buildChars);
                bs = bs.Substring(0, GlobalVar.featureCount);

                insertCommand.Parameters.Clear();
                insertCommand.Parameters.AddWithValue("@parmNewJob", GlobalVar.jobName);
                insertCommand.Parameters.AddWithValue("@parmNewPop", GlobalVar.popIndex);
                insertCommand.Parameters.AddWithValue("@parmNewMember", popMember);
                insertCommand.Parameters.AddWithValue("@parmNewFeature", 0);
                insertCommand.Parameters.AddWithValue("@parmNewValue", 0);
                insertCommand.Parameters.AddWithValue("@parmNewChar", bs);
                insertCommand.ExecuteNonQuery();
                insertCommand.Dispose();
            }
            catch (Exception e)
            {
          //      Console.WriteLine(e.ToString());
            }
        }

        static void GetExistingCharacteristics(ref MySqlConnection myConnection, int popMember, int whichArray)
        {

            GlobalVar.featureCount = 0;
            char[] buildChars;
            buildChars = new char[65536];
            string featureString = "";

            try
            {
                MySqlDataReader myReader = null;

                MySqlCommand getCommand = new MySqlCommand("select value_index, char_value from member_detail " +
 " where job_name = @parmGetJob and population_index = @parmGetPop and member_index = @parmGetMember and feature_index = @parmGetFeature ", myConnection);
                getCommand.Parameters.AddWithValue("@parmGetJob", GlobalVar.jobName);
                getCommand.Parameters.AddWithValue("@parmGetPop", GlobalVar.popIndex);
                getCommand.Parameters.AddWithValue("@parmGetMember", popMember);
                getCommand.Parameters.AddWithValue("@parmGetFeature", 0);

                myReader = getCommand.ExecuteReader();
                 while (myReader.Read())
                {
                    featureString = myReader.GetString("char_value");
                }
                GlobalVar.featureCount = featureString.Length;
                buildChars = featureString.ToCharArray();
                for (int i = 0; i < GlobalVar.featureCount; i++)
                {
                    GlobalVar.features[i, whichArray] = buildChars[i];

                }
                myReader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void NextGenerationValues(ref MySqlConnection myConnection, int popMember)
        {

            char[] buildChars;
            buildChars = new char[65536];

            MySqlCommand updateCommand = new MySqlCommand("update member_detail set char_value = @parmValue " +
    " where job_name = @parmJob and population_index = @parmPop and member_index = @parmMember and feature_index = @parmFeature and value_index = @parmIndex", myConnection);



            for (int i = 0; i < GlobalVar.featureCount; i++)
            {
                buildChars[i] = (char)GlobalVar.features[i, 0];
            }

            string bs = new string(buildChars);
            bs = bs.Substring(0, GlobalVar.featureCount);

            updateCommand.Parameters.Clear();
            updateCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
            updateCommand.Parameters.AddWithValue("@parmPop", GlobalVar.popIndex);
            updateCommand.Parameters.AddWithValue("@parmMember", popMember);
            updateCommand.Parameters.AddWithValue("@parmFeature", 0);
            updateCommand.Parameters.AddWithValue("@parmIndex", 0);
            updateCommand.Parameters.AddWithValue("@parmValue", bs);

            updateCommand.ExecuteNonQuery();


        }






    }
}
