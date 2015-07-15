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
            public static int[,] features = new int[65536,3];
            public static int featureCount = 0;
            public static int launchGeneration = 0;
            public static int myUniqueID = 0;
            public static int bestScore = 0;
            public static int appliedMut = 5;
            public static int p1s = 0;
            public static int p2s = 0;
            public static int xoType = 0;

        }

        static void Main(string[] args)
        {

            // initialize local variables

            int oldScore = 0;
            int myPopNumber = 0;
            int runLoop = 4;
            int topScore = 0;
            string jobType = null;
            string XMLfile = "";

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


            // have a valid job and know my population, let's  process


            // perform one time step for one entity, for each iteration requested


            while (runLoop > 0)
            {

        //        GlobalVar.featureCount = 0;

                myPopNumber = GetMemberToProcess(ref myConnection);

                // who am i? (find unoccupied)

                // find all features for this population

                if (GlobalVar.myGeneration.Equals(0))
                {
                    PopulateEmptyMember(ref myConnection, myPopNumber);
                }
                else
                {
                    GetExistingCharacteristics(ref myConnection, myPopNumber, 0);
                }

                // get my characteristics 

                // if not 1st generation, should do GA/Swarm/etc processing here

                int rxType = GlobalVar.random.Next(0, (100));
                GlobalVar.appliedMut = 5;
                if (rxType < 10)
                    GlobalVar.appliedMut = 30;

                int roType = GlobalVar.random.Next(0, (100));
                GlobalVar.xoType = 0;
                if (roType < 45)
                    GlobalVar.xoType = 1;
                if (roType > 90)
                    GlobalVar.xoType = 2;

               topScore = GetTopScore(ref myConnection);
               oldScore = GlobalVar.myScore;

               if ((GlobalVar.myGeneration > 1) )
                {
                    NextGenerationValues(ref myConnection, myPopNumber); // updating gx file here
                }


                // write out xml

                XMLfile = ExportXMLfile(myPopNumber);

                // call the user processing job in the series...

                // could turn off database while running user

                Process userProcess = new Process();

                userProcess.StartInfo.CreateNoWindow = true;
                userProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                userProcess.StartInfo.FileName = "as_wave.exe";
                userProcess.StartInfo.Arguments = XMLfile;
                userProcess.Start();
                userProcess.WaitForExit();
                userProcess.Dispose();


    // get updated XML (with score)
                try
                {
                    ImportXMLfile(XMLfile);  // now have score
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
                updateCommand.Dispose();  // score is updated, should update gx here


                char[] buildChars;
                buildChars = new char[65536];

                for (int i = 0; i < GlobalVar.featureCount; i++)
                    buildChars[i] = (char)GlobalVar.features[i, 0];

                string bs = new string(buildChars);
                bs = bs.Substring(0, GlobalVar.featureCount);

                string fn = "";
                fn = "gx" + Convert.ToString(myPopNumber);
                File.WriteAllText(fn, bs);

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
                " values (@parmJob, @parmAvg, @parmTop, @parmPossible, 0, 0) ", myConnection);

            insertCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
            insertCommand.Parameters.AddWithValue("@parmAvg", avgGeneration);
            insertCommand.Parameters.AddWithValue("@parmTop", GlobalVar.bestScore); 
     //       insertCommand.Parameters.AddWithValue("@parmPossible", GlobalVar.featureCount * 8);
            insertCommand.Parameters.AddWithValue("@parmPossible", 
                26214400);


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



            while (!foundMe)
            {

                // query X lowest generation members, pick one at random

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
            string fn = "";

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
                    GlobalVar.featureCount = myReader.GetInt32("feature_count");

                    //featureType = myReader.GetString("feature_type");
                    featureType = "int"; 
                    if (featureType.Equals("int"))
                    {
                        featureIntMin = myReader.GetInt32("feature_num_min"); 
                        featureIntMax = myReader.GetInt32("feature_num_max");
                    }
                }
                myReader.Close();


                for (int i = 0; i < GlobalVar.featureCount; i++)
                {

                    newIntValue = GlobalVar.random.Next(featureIntMin, featureIntMax);

                    buildChars[i] = (char)newIntValue;
                    GlobalVar.features[i, 0] = newIntValue;

                }

                string bs = new string(buildChars);
                bs = bs.Substring(0, GlobalVar.featureCount);

                fn = "gx" + Convert.ToString(popMember);
                File.WriteAllText(fn, bs);

                // this should be the right length

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
            string fn = "";

            try
            {

                MySqlDataReader myReader = null;

                MySqlCommand myCommand = new MySqlCommand("select feature_count, feature_type, feature_num_min, feature_num_max " +
                    "from population_features " +
                    "where job_name = @parmMyJob and population_index = @parmMyPop", myConnection);
                myCommand.Parameters.AddWithValue("@parmMyJob", GlobalVar.jobName);
                myCommand.Parameters.AddWithValue("@parmMyPop", GlobalVar.popIndex);
                myReader = myCommand.ExecuteReader();
                while (myReader.Read())
                {
                    GlobalVar.featureCount = myReader.GetInt32("feature_count");

                }
                myReader.Close();

                fn = "gx" + Convert.ToString(popMember);

                featureString = File.ReadAllText(fn);

                buildChars = featureString.ToCharArray();
 
                Array.Resize(ref buildChars, GlobalVar.featureCount); 

                for (int i = 0; i < GlobalVar.featureCount; i++)
                {
                    GlobalVar.features[i, whichArray] = buildChars[i];

                }
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

      //      char[] buildChars;
      //      buildChars = new char[65536];
      //      string fn = "";

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
                GlobalVar.p1s = parentIndex;
            }
            else
            {
                GlobalVar.p1s = popMember;
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
                GlobalVar.p2s = parentIndex;
                GetExistingCharacteristics(ref myConnection, parentIndex, 2);
            }
            else
            {
                GlobalVar.p2s = popMember;
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

                        if (GlobalVar.random.Next(0, (1000)) < GlobalVar.appliedMut)
                            //if (GlobalVar.random.Next(0, (1000 * GlobalVar.featureCount)) < GlobalVar.appliedMut)
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


            xoverPoint = GlobalVar.random.Next(1, GlobalVar.featureCount);

            changeCtr = 0;

            // no crossover version

            updateCommand.Parameters.Clear();
            updateCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
            updateCommand.Parameters.AddWithValue("@parmPop", GlobalVar.popIndex);
            updateCommand.Parameters.AddWithValue("@parmMember", popMember);
            updateCommand.Parameters.AddWithValue("@parmFeature", 0);
            updateCommand.Parameters.AddWithValue("@parmIndex", 0);
            updateCommand.Parameters.AddWithValue("@parmValue", 0);            

      //      GlobalVar.totalChanges = 0;
            for (int i = 0; i < GlobalVar.featureCount; i++)
            {
                parentIndex = 1;

                if ((i > xoverPoint) && (GlobalVar.xoType.Equals(0))) // crossover
                    parentIndex = 2;
                if ((i < xoverPoint) && (GlobalVar.xoType.Equals(1))) // reverse crossover
                    parentIndex = 2;
                if ((GlobalVar.random.Next(0, 100) < 50) && (GlobalVar.xoType.Equals(2)))  // - no crossover
                    parentIndex = 2;

       //         buildChars[i] = (char)GlobalVar.features[i, parentIndex];
     //           if (!GlobalVar.features[i, parentIndex].Equals(GlobalVar.features[i, 0]))
     //               GlobalVar.totalChanges++;
            //    GlobalVar.features[0, parentIndex] = GlobalVar.features[i, parentIndex]; // this has to be wrong
                GlobalVar.features[i, 0] = GlobalVar.features[i, parentIndex]; // replacement for above
            }

      //      string bs = new string(buildChars);
      //      bs = bs.Substring(0, GlobalVar.featureCount);

      //      fn = "gx" + Convert.ToString(popMember);
      //      File.WriteAllText(fn, bs);

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

            xml.WriteElementString("Best", GlobalVar.bestScore.ToString());
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

            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element: // The node is an element.
                        elementString = reader.Name;
                        break;
                    case XmlNodeType.Text: //Display the text in each element.

                        if (elementString.Equals("Score"))
                        {
                            GlobalVar.myScore = Convert.ToInt32(reader.Value);
                        }
                        break;
                    case XmlNodeType.EndElement: //Display the end of the element.
                        break;
                }

            }
            reader.Close();
        }


    }
}
