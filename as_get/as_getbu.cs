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
    class as_get
    {

        public static class GlobalVar
        {
            public static int popCount = 0;
            public static int popIndex = 0;
            public static string jobName = "test";
            public static int myGeneration = 0;
            public static int myScore = 0;
            public static Random random = new Random();
            public static int[,] features = new int[5000,3];
            public static int featureCount = 0;
            public static int launchGeneration = 0;
            public static int myUniqueID = 0;
            public static int bestScore = 0;
            public static int getSeconds = 0;
            public static int putSeconds = 0;
            public static int getCtr = 0;
            public static int putCtr = 0;
            public static int mutPer1000 = 10;
        }

        static void Main(string[] args)
        {

            // initialize local variables
            
            int myPopNumber = 0;
            int runLoop = 4;
            int topScore = 0;
            string jobType = null;
      //      Process userProcess = null;
            string XMLfile = "";

            Stopwatch stopWatch = new Stopwatch();
            TimeSpan ts = stopWatch.Elapsed;
            stopWatch.Start();

            // get input parameters for job name and iteration count

            if (args.Length > 0) // first arg should be mandatory - job name
            {
                GlobalVar.jobName = args[0];
            }

            if (args.Length > 1)
            {
                runLoop = Convert.ToInt32(args[1]);
            }

            if (args.Length > 2)
            {
                GlobalVar.myUniqueID = Convert.ToInt32(args[2]);
            }

            if (args.Length > 3)
            {
                GlobalVar.launchGeneration = Convert.ToInt32(args[3]);
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


            // okay, have a valid job.  does it have multiple populations?  which one is active?

            if (!FindActivePopIndex(ref myConnection)) // only does one population for now
            {
                Console.WriteLine("job contains no population(s) - cancelling");
                return;
            }

            // should delete all the tables prior to starting the job (set a switch so it's not done twice)
            // add an as_prep step to set a job flag, delete from tables?
            // or add a version/run # to tables, so I can store multiple runs

            // have a valid job and know my population, let's  process



            // perform one time step for one entity, for each iteration requested



            while (runLoop > 0)
            {

                GlobalVar.featureCount = 0;

                myPopNumber = GetMemberToProcess(ref myConnection);

         //       Console.Write(".M." + myPopNumber.ToString());
         //       Console.Write(".G." + GlobalVar.myGeneration.ToString());

                // who am i? (find unoccupied)

                // find all features for this population

                if (GlobalVar.myGeneration.Equals(0))
                {
                   stopWatch.Restart();
                    PopulateEmptyMember(ref myConnection, myPopNumber);
                   stopWatch.Stop();
            //        ts = stopWatch.Elapsed;
            //        GlobalVar.getSeconds = GlobalVar.getSeconds + ts.Milliseconds + (1000 * ts.Seconds);
            //        GlobalVar.getCtr++;
                }
                else
                {
                    stopWatch.Restart();
                    GetExistingCharacteristics(ref myConnection, myPopNumber,0);
                    stopWatch.Stop();
                    ts = stopWatch.Elapsed;
//                    GlobalVar.getSeconds = GlobalVar.getSeconds + ts.Milliseconds + (1000 * ts.Seconds);
                    GlobalVar.getSeconds = GlobalVar.getSeconds + ts.Milliseconds;
                    GlobalVar.getCtr++;

                }

                // get my characteristics 

                // if not 1st generation, should do GA/Swarm/etc processing here

               topScore = GetTopScore(ref myConnection);

            //   Console.Write(".T." + topScore.ToString() + "-" + Convert.ToString(GlobalVar.myScore) + "-" + Convert.ToString(GlobalVar.featureCount * 255));
                // don't immunize top score if it's common, but not ultimate

               if ((GlobalVar.myGeneration < 2) || (GlobalVar.myScore >= topScore) || (GlobalVar.myScore >= (GlobalVar.featureCount * 8)))
               {
         //          Console.Write(".immune!." + myPopNumber.ToString());
                   NextGenerationValues(ref myConnection, myPopNumber);
               }
               if ((GlobalVar.myGeneration > 1) && (GlobalVar.myScore < topScore) && (GlobalVar.myScore < (GlobalVar.featureCount * 8)))
               {
              //     Console.Write(".G." + myPopNumber.ToString());
                   NextGenerationValues(ref myConnection, myPopNumber);
               }

                // write out xml

                XMLfile = ExportXMLfile(myPopNumber);

                // call the user processing job in the series...

                Process userProcess = new Process();

                userProcess.StartInfo.CreateNoWindow = true;
                userProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                userProcess.StartInfo.FileName = "as_user.exe";
                userProcess.StartInfo.Arguments = XMLfile;
                userProcess.Start();

            //    userProcess = Process.Start("as_user.exe");  // user process will update the score

                userProcess.WaitForExit();
                userProcess.Dispose();


    // get updated XML (with score)
                try
                {
                    ImportXMLfile(XMLfile);
                }
                catch
                {
                    Console.Write(".X5." + XMLfile + ".");
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
                    File.Delete(XMLfile);
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


        static int GetTopScore(ref MySqlConnection myConnection)
        {

            int foundScore = 0;
            int foundCount = 0;

            MySqlDataReader myReader = null;

            MySqlCommand scoreCommand = new MySqlCommand("SELECT member_score FROM member_header " +
                "where job_name = @jobParam order by member_score desc limit 1", myConnection);
            scoreCommand.Parameters.AddWithValue("@jobParam", GlobalVar.jobName);

            try
            {
                myReader = scoreCommand.ExecuteReader();
                while (myReader.Read())
                {
                    foundScore = myReader.GetInt32("member_score");
                }
                myReader.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            if ((foundScore > GlobalVar.bestScore))
            {
                GlobalVar.bestScore = foundScore;
                InsertBestScore(ref myConnection);
                if ((100 * GlobalVar.bestScore) > (99 * GlobalVar.featureCount * 8))
                    GlobalVar.mutPer1000 = 10;

            }

            MySqlCommand countCommand = new MySqlCommand ("SELECT member_index FROM member_header where member_score = @scoreParm", myConnection);
            countCommand.Parameters.AddWithValue("@scoreParm", foundScore);
            foundCount = 0;
            try
            {
                myReader = countCommand.ExecuteReader();
                while (myReader.Read())
                {
                    foundCount++;
                }
                myReader.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            if (foundCount > 1)
                foundScore++;


            return (foundScore);
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



        static bool FindActivePopIndex(ref MySqlConnection myConnection)
        {
            // not active at this time, first examples will have only one population

            bool foundPop = false;
            MySqlDataReader myReader = null;

            MySqlCommand myCommand = new MySqlCommand("select population_count, population_index from population_table where job_name = @Param1", myConnection);
            myCommand.Parameters.AddWithValue("@Param1", GlobalVar.jobName);

            try
            {
                myReader = myCommand.ExecuteReader();
                while (myReader.Read())
                {
                    GlobalVar.popCount = myReader.GetInt32("population_count");
                    GlobalVar.popIndex = myReader.GetInt32("population_index");
                    foundPop = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            myReader.Close();
            return foundPop;
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

            // instead, add a "busy" table.  It won't allow duplicates, so a process can never select a busy record
            // launcher can check for hung records in busy table by time stamp and free them


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
                   //     Console.Write(".I" + randomPopNumber.ToString() + ".");
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
                      //      Console.Write(".B" + randomPopNumber.ToString() + ".");
                        }
 

                    }
                }

            }

            // set a flag/ctr in the database that a job is running (to monitor max concurrent jobs)

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
                MySqlDataReader myReader = null;

                MySqlCommand myCommand = new MySqlCommand("select feature_count, feature_type, feature_num_min, feature_num_max " + 
                    "from population_features " + 
                    "where job_name = @parmMyJob and population_index = @parmMyPop", myConnection);
                myCommand.Parameters.AddWithValue("@parmMyJob",GlobalVar.jobName);
                myCommand.Parameters.AddWithValue("@parmMyPop",GlobalVar.popIndex);
                myReader = myCommand.ExecuteReader();
                while (myReader.Read())
                {
                    GlobalVar.featureCount =  myReader.GetInt32("feature_count");
                    //featureType = myReader.GetString("feature_type");
                    featureType = "int"; 
                    if (featureType.Equals("int"))
                    {
                        featureIntMin = myReader.GetInt32("feature_num_min"); 
                        featureIntMax = myReader.GetInt32("feature_num_max");
                    }
                }
                myReader.Close();


                MySqlCommand insertCommand = new MySqlCommand("insert into member_detail (job_name, population_index, member_index, feature_index, value_index, char_value, num_value) " +
 " values (@parmNewJob, @parmNewPop, @parmNewMember, @parmNewFeature, @parmNewValue, @parmNewChar, null) ", myConnection);

                for (int i = 0; i < GlobalVar.featureCount; i++)
                {

                    GlobalVar.features[i,0] = newIntValue;

                    newIntValue = GlobalVar.random.Next(featureIntMin, featureIntMax);

                    buildChars[i] = (char)newIntValue;
                    GlobalVar.features[i, 0] = newIntValue;

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
                Console.WriteLine(e.ToString());
            }
        }

        static void GetExistingCharacteristics(ref MySqlConnection myConnection, int popMember, int whichArray)
        {

            char[] buildChars;
            buildChars = new char[65536];
            string featureString = "";

            GlobalVar.featureCount = 0;

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
    //        int neighborhood = 10; // will be user parameter
            int[] mutateArray = new int[8] {1,2,4,8,16,32,64,128};
            int mutatePosition = 0;
            int mutateValue = 0;
            int parentIndex = 0;
            int parentScore = -1;
            int nextNeighbor = 0;
            int xoverPoint = 0;
            int changeCtr = 0;

            char[] buildChars;
            buildChars = new char[65536];

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            TimeSpan ts = stopWatch.Elapsed;

            MySqlDataReader myReader = null;

            // get parent 1 candidates 

            MySqlCommand getCommand = new MySqlCommand("select member_index, member_score from member_header " +
    " where job_name = @parmJob and population_index = @parmPop and member_index in (@p1,@p2,@p3,@p4,@p5) " +
    " order by member_score desc limit 1", myConnection);

            getCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
            getCommand.Parameters.AddWithValue("@parmPop", GlobalVar.popIndex);

            nextNeighbor = popMember;

            if (nextNeighbor == 0)
                nextNeighbor = GlobalVar.popCount;
            nextNeighbor--;

            getCommand.Parameters.AddWithValue("@p1", nextNeighbor);

            if (nextNeighbor == 0)
                nextNeighbor = GlobalVar.popCount;
            nextNeighbor--;

            getCommand.Parameters.AddWithValue("@p2", nextNeighbor);
            
            if (nextNeighbor == 0)
                nextNeighbor = GlobalVar.popCount;
            nextNeighbor--;

            getCommand.Parameters.AddWithValue("@p3", nextNeighbor);

            if (nextNeighbor == 0)
                nextNeighbor = GlobalVar.popCount;
            nextNeighbor--;

            getCommand.Parameters.AddWithValue("@p4", nextNeighbor);

            
            if (nextNeighbor == 0)
                nextNeighbor = GlobalVar.popCount;
            nextNeighbor--;
            getCommand.Parameters.AddWithValue("@p5", nextNeighbor);

            myReader = getCommand.ExecuteReader();
            if (myReader.HasRows)
            {
                myReader.Read();

                parentIndex = myReader.GetInt32("member_index"); 
                parentScore = myReader.GetInt32("member_score");
            }
            myReader.Close();

            getCommand.Parameters.Clear();
            getCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
            getCommand.Parameters.AddWithValue("@parmPop", GlobalVar.popIndex);

            if (GlobalVar.myScore <= parentScore)
                changeCtr++; // need mutations even in a tie to prevent convergence

            if (parentScore > GlobalVar.myScore)
            {
                GetExistingCharacteristics(ref myConnection, parentIndex, 1);
            }
            else
            {
                for (int i = 0; i < GlobalVar.featureCount; i++)
                {
                    GlobalVar.features[i, 1] = GlobalVar.features[i, 0];
                } 

            }

            nextNeighbor = popMember;
            nextNeighbor++;
            if (nextNeighbor == GlobalVar.popCount)
                nextNeighbor = 0;
            getCommand.Parameters.AddWithValue("@p1", nextNeighbor);
            nextNeighbor++;
            if (nextNeighbor == GlobalVar.popCount)
                nextNeighbor = 0;
            getCommand.Parameters.AddWithValue("@p2", nextNeighbor);
            nextNeighbor++;
            if (nextNeighbor == GlobalVar.popCount)
                nextNeighbor = 0;
            getCommand.Parameters.AddWithValue("@p3", nextNeighbor);
            nextNeighbor++;
            if (nextNeighbor == GlobalVar.popCount)
                nextNeighbor = 0;
            getCommand.Parameters.AddWithValue("@p4", nextNeighbor);
            nextNeighbor++;
            if (nextNeighbor == GlobalVar.popCount)
                nextNeighbor = 0;
            getCommand.Parameters.AddWithValue("@p5", nextNeighbor);

            myReader = getCommand.ExecuteReader();
            parentScore = -1;
            if (myReader.HasRows)
            {
                myReader.Read();
                parentIndex = myReader.GetInt32("member_index");
                parentScore = myReader.GetInt32("member_score");
            }
            myReader.Close();

            if (GlobalVar.myScore <= parentScore)
                changeCtr++; // need mutations even in a tie to prevent convergence

            if (parentScore > GlobalVar.myScore)
            {
                GetExistingCharacteristics(ref myConnection, parentIndex, 2);
            }
            else
            {
                for (int i = 0; i < GlobalVar.featureCount; i++)
                {
                    GlobalVar.features[i, 2] = GlobalVar.features[i, 0];
                }
            }


            if (changeCtr > 0)
            {
                for (int i = 0; i < GlobalVar.featureCount; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (GlobalVar.random.Next(0, 1000) < GlobalVar.mutPer1000)
                        {
                            mutatePosition = GlobalVar.random.Next(0, 8);
                            mutateValue = mutateArray[mutatePosition];
                            if (GlobalVar.random.Next(0, 100) < 50)
                                mutateValue = -1 * mutateValue;
                            GlobalVar.features[i, j] = GlobalVar.features[i, j] + mutateValue;
                            if (GlobalVar.features[i, j] < 0)
                                GlobalVar.features[i, j] = GlobalVar.features[i, j] - (2 * mutateValue);
                            if (GlobalVar.features[i, j] > 255)
                                GlobalVar.features[i, j] = GlobalVar.features[i, j] - (2 * mutateValue);
                        }
                    }
                }
            }


            MySqlCommand updateCommand = new MySqlCommand("update member_detail set char_value = @parmValue " +
" where job_name = @parmJob and population_index = @parmPop and member_index = @parmMember and feature_index = @parmFeature and value_index = @parmIndex", myConnection);


//            MySqlCommand updateCommand = new MySqlCommand("update member_detail set num_value = @parmValue " +
//    " where job_name = @parmJob and population_index = @parmPop and member_index = @parmMember and feature_index = @parmFeature and value_index = @parmIndex", myConnection);

      //      xoverPoint = GlobalVar.random.Next(1, GlobalVar.featureCount);
            changeCtr = 0;

            // no crossover version

            updateCommand.Parameters.Clear();
            updateCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
            updateCommand.Parameters.AddWithValue("@parmPop", GlobalVar.popIndex);
            updateCommand.Parameters.AddWithValue("@parmMember", popMember);
            updateCommand.Parameters.AddWithValue("@parmFeature", 0);
            updateCommand.Parameters.AddWithValue("@parmIndex", 0);
            updateCommand.Parameters.AddWithValue("@parmValue", 0);            
            stopWatch.Restart();

            for (int i = 0; i < GlobalVar.featureCount; i++)
            {
                parentIndex = 1;
                if (GlobalVar.random.Next(0, 100) < 50)
                    parentIndex = 2;

                buildChars[i] = (char)GlobalVar.features[i, parentIndex];

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

            stopWatch.Stop();
            ts = stopWatch.Elapsed;
            GlobalVar.putSeconds = GlobalVar.putSeconds + ts.Milliseconds;
//            GlobalVar.putSeconds = GlobalVar.putSeconds + ts.Milliseconds + (1000 * ts.Seconds);
            GlobalVar.putCtr++;

        }

        static string ExportXMLfile(int popMember)
        {
            string filename = "";

            filename = GlobalVar.jobName + GlobalVar.popIndex.ToString() + popMember.ToString() + ".xml";

            XmlTextWriter xml = null;

            xml = new XmlTextWriter(filename, null);

            xml.Formatting = Formatting.Indented;
            xml.IndentChar = '\t';
            xml.Indentation = 1;

            xml.WriteStartDocument();

            xml.WriteStartElement("Features");
            xml.WriteWhitespace("\n");

            xml.WriteElementString("Generation", GlobalVar.myGeneration.ToString());
            xml.WriteWhitespace("\n  ");

            xml.WriteElementString("Score", GlobalVar.myScore.ToString());
            xml.WriteWhitespace("\n  ");

            for (int i = 0; i < GlobalVar.featureCount; i++)
            {
                xml.WriteElementString("Index", i.ToString());
                xml.WriteElementString("Value", GlobalVar.features[i,0].ToString());
                xml.WriteWhitespace("\n  ");
            }

            xml.WriteEndElement();
            xml.WriteWhitespace("\n");

            xml.WriteEndDocument();

            //Write the XML to file and close the writer.
            xml.Flush();
            xml.Close();
            return filename;
        }


        static void ImportXMLfile(string XMLfile)
        {
            string elementString = "";

            System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(XMLfile);
      //      Console.Write(".reading." + XMLfile + ".");

            while (reader.Read())
            {
        //        Console.Write("." + reader.NodeType + ".");
        //        Console.Write("." + reader.Name + ".");
        //        Console.Write("." + reader.Value + ".");
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // The node is an element.
                        elementString = reader.Name;
                        break;
                    case XmlNodeType.Text: //Display the text in each element.

                        if (elementString.Equals("Score"))
                        {
          //                  Console.Write(".score." + XMLfile + ".");
                            GlobalVar.myScore = Convert.ToInt32(reader.Value);
            //                Console.Write(".convert." + XMLfile + ".");
                        }
                        break;
                    case XmlNodeType.EndElement: //Display the end of the element.
                        break;
                }

            }
        //    Console.Write(".closing." + XMLfile + ".");
            reader.Close();
        //    Console.Write(".closed." + XMLfile + ".");
        }


    }
}
