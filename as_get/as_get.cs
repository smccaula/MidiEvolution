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
        const int scoreFrames = 8;

        public static class GlobalVar
        {
            public static int popCount = 0;
            public static int popIndex = 0;
            public static string jobName = "test";
            public static int myGeneration = 0;
            public static long myScore = 0;
            public static Random random = new Random();
            public static int[,] features = new int[150000,11];
            public static int featureCount = 0;
            public static int launchGeneration = 0;
            public static int myUniqueID = 0;
            public static long bestScore = -999999999;
            public static int MutPer100Members = 100;
            public static int normalMut = 50;
            public static int alternateMut = 300;
            public static int xoverType = 0; 
            public static int parentDist = 20;
            public static bool immuneFlag = false;
            public static int worstFrame = -1;
            public static long[,] frameScore = new long[11,scoreFrames];
        }

        static void Main(string[] args)
        {

            // initialize local variables

            int myPopNumber = 0;
            int runLoop = 1;
            long topScore = 0;
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
                ";" + "UID=agentx" + ";" + "PASSWORD=mysqlX666" + ";" + "pooling=false" + ";" +
                "default command timeout=5" + ";";

            myConnection = new MySqlConnection(connectionString);

            System.Threading.Thread.Sleep(0); // give the server a break
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

            jobType = "GA";
            if (GlobalVar.launchGeneration < 1) 
                jobType = TestJobValidity(ref myConnection);

            if (String.IsNullOrEmpty(jobType))
            {
                Console.WriteLine("job not found - cancelling");
                return;
            }

   //         Console.WriteLine("GetJobParameters"); // not doing anything
   //         GetJobParameters(ref myConnection);

            // okay, have a valid job.  does it have multiple populations?  which one is active?

            if (!FindActivePopIndex(ref myConnection)) // only does one population for now
            {
                Console.WriteLine("job contains no population(s) - cancelling");
                return;
            }

            GlobalVar.featureCount = GetFeatureCount(ref myConnection);
            if (GlobalVar.featureCount < 1) 
            {
                Console.WriteLine("job contains no feature(s) - cancelling");
                return;
            }

            // have a valid job and know my population, let's  process

            // perform one time step for one entity, for each iteration requested


            while (runLoop > 0)
            {
                //        GlobalVar.featureCount = 0;

                    try
                    {
                        myPopNumber = GetMemberToProcess(ref myConnection);
                    }
                    catch
                    {
                        return; 
                    }
 

                if (myPopNumber < 1)
                    return;

                if (!GlobalVar.myGeneration.Equals(0))
                {
                    // don't use as parent while score is not connected to new data
                    MySqlCommand preupdateCommand = new MySqlCommand("update member_header " +
                        " set member_score = @parmScore " +
                        " where job_name = @parmJob and population_index = @parmPop and member_index = @parmMember ", myConnection);

                    GlobalVar.myGeneration++;
                    preupdateCommand.Parameters.AddWithValue("@parmScore", -1);
                    preupdateCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
                    preupdateCommand.Parameters.AddWithValue("@parmPop", GlobalVar.popIndex);
                    preupdateCommand.Parameters.AddWithValue("@parmMember", myPopNumber);

                    preupdateCommand.ExecuteNonQuery();
                    preupdateCommand.Dispose();  // score is updated, should update gx here
                }

                if (GlobalVar.myGeneration.Equals(0))
                {
                    PopulateEmptyMember(ref myConnection, myPopNumber);
                }
                else
                {
                    GetExistingScores(myPopNumber, 0);
                }
 

         //           Console.WriteLine("GetTopScore");
                    topScore = GetTopScore(ref myConnection);

                    GlobalVar.MutPer100Members = GlobalVar.random.Next(GlobalVar.normalMut, GlobalVar.alternateMut);
                    GlobalVar.parentDist = GlobalVar.random.Next(0, 50) + 1;

                    if (!GlobalVar.myGeneration.Equals(0))
                    {
                        NextGenerationValues(myPopNumber);
                    }

                    try
                    {
                        myConnection.Close();
                        myConnection.Dispose();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        Console.WriteLine("database error - cancelling");
                        return;
                    }

                if (!GlobalVar.myGeneration.Equals(0))
                    {
                        char[] buildChars;
                        buildChars = new char[350000];

                        for (int i = 0; i < GlobalVar.featureCount; i++)
                            buildChars[i] = (char)GlobalVar.features[i, 0];

                        string bs = new string(buildChars);
                        bs = bs.Substring(0, GlobalVar.featureCount);

                        string fn = "";
                        fn = "mx" + Convert.ToString(myPopNumber);
                        File.WriteAllText(fn, bs);
                    }

                // write out xml
               bool outputXML = false;
               int XMLOut = 0;
               while (!outputXML)
               {
                   XMLOut++;
                   try
                   {
                       outputXML = true;
                       XMLfile = ExportXMLfile(myPopNumber);
                   }
                   catch
                   {
                       System.Threading.Thread.Sleep(10);
                       outputXML = false;
                   }
                   if (XMLfile.Equals("error"))
                       outputXML = false;
                   if (XMLOut > 5)
                       return;
               }

                // call the user processing job in the series...

                Process userProcess = new Process();

                userProcess.StartInfo.CreateNoWindow = true;
                userProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                userProcess.StartInfo.FileName = "as_midi.exe";
//                userProcess.StartInfo.FileName = "as_user.exe";  // dsm
                userProcess.StartInfo.Arguments = XMLfile;
                userProcess.Start();
                userProcess.WaitForExit();
                userProcess.Dispose();

    // get updated XML (with score)
                bool openXML = false;
                int XMLTry = 0;
                while (!openXML)
                {
                    XMLTry++;
                    try
                    {
                        openXML = true;
                        ImportXMLfile(XMLfile);
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(10);
                        openXML = false;
                    }
                    if (XMLTry > 5)
                        return;
                }

                try
                {
                    File.Delete(XMLfile);
                }
                catch
                {
                    //        Console.Write(".X6." + XMLfile + ".");
                }

                // update my data and unflag me (should go in final program)

                myConnection = new MySqlConnection(connectionString);
                try
                {
                    myConnection.Open();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Console.WriteLine("database error - cancelling");
                    return;
                }

                MySqlCommand updateCommand = new MySqlCommand("update member_header " +
                    " set member_generation = @parmGeneration, member_score = @parmScore, worst_frame = @parmWorst " +
                    " where job_name = @parmJob and population_index = @parmPop and member_index = @parmMember ", myConnection);

                GlobalVar.myGeneration++;
                updateCommand.Parameters.AddWithValue("@parmGeneration", GlobalVar.myGeneration);
                updateCommand.Parameters.AddWithValue("@parmScore", GlobalVar.myScore);
                updateCommand.Parameters.AddWithValue("@parmWorst", GlobalVar.worstFrame);
                updateCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
                updateCommand.Parameters.AddWithValue("@parmPop", GlobalVar.popIndex);
                updateCommand.Parameters.AddWithValue("@parmMember", myPopNumber);

                updateCommand.ExecuteNonQuery();
                updateCommand.Dispose();  // score is updated, should update gx here

                // am I still looping?

     //           Console.WriteLine("unflag");

                MySqlCommand unflagCommand = new MySqlCommand("delete from busy_table " +
    " where job_name = @parmJob and population_index = @parmPop and member_index = @parmMember ", myConnection);

                unflagCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
                unflagCommand.Parameters.AddWithValue("@parmPop", GlobalVar.popIndex);
                unflagCommand.Parameters.AddWithValue("@parmMember", myPopNumber);
                
                unflagCommand.ExecuteNonQuery();
                unflagCommand.Dispose();

                runLoop--;
    //            Console.WriteLine("loop");

            }
            myConnection.Close();
            myConnection.Dispose();
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


        static long GetTopScore(ref MySqlConnection myConnection)
        {

            long foundScore = 0;
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
                    foundScore = myReader.GetInt64("member_score");
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
//            insertCommand.Parameters.AddWithValue("@parmPossible", 515886062); //blue
//            insertCommand.Parameters.AddWithValue("@parmPossible", 820278363); //sine
            insertCommand.Parameters.AddWithValue("@parmPossible", 5939741796); //short
            //    insertCommand.Parameters.AddWithValue("@parmPossible", (GlobalVar.featureCount*8));
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

            int randomPopNumber = -1; 
            bool foundMe = false;
            bool gotResults = true;
            bool isBusy = false;
            int youngLimit = GlobalVar.popCount / 10;

            MySqlCommand getCommand = new MySqlCommand("select member_generation, member_score, worst_frame from member_header " +
                " where job_name = @parmJob and population_index = @parmPop and member_index = @parmMember", myConnection);


            MySqlCommand setBusyCommand = new MySqlCommand("insert into busy_table " + 
                "(job_name, population_index, member_index, process_id, process_gen) " +
                " values (@parmJob, @parmPop, @parmMember, @parmID, @parmGen) ", myConnection);


            MySqlCommand askBusyCommand = new MySqlCommand("select member_index from busy_table " +
                " where job_name = @parmJob and population_index = @parmPop and member_index = @parmMember", myConnection);

            MySqlCommand insertCommand = new MySqlCommand("insert into member_header (job_name, population_index, member_index, member_generation, member_score) " +
    " values (@parmJob, @parmPop, @parmMember, 0, -999999999) ", myConnection);

            MySqlCommand youngestCommand = new MySqlCommand("select member_index from member_header where job_name = @parmJob order by member_generation limit @parmYoung"
            , myConnection);


            MySqlDataReader myReader = null;
//            MySqlDataReader memberReader = null;


            while (!foundMe)
            {

                // query X lowest generation members, pick one at random

                randomPopNumber = GlobalVar.random.Next(1, GlobalVar.popCount);
           //     Console.WriteLine("random : " + randomPopNumber.ToString());

       //         if (GlobalVar.launchGeneration > (GlobalVar.popCount/2))
       //         {
       //             youngestCommand.Parameters.Clear();
       //             youngestCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
       //             youngestCommand.Parameters.AddWithValue("@parmYoung", youngLimit);
       //             try
       //             {
       //                 memberReader = youngestCommand.ExecuteReader();

       //                 gotResults = memberReader.HasRows;
       //                 int getPopNumber = GlobalVar.random.Next(1, youngLimit);
       //                 
       //                          if (gotResults)
       //                          {
       //                              bool moreRows = true;
       //                              for (int getX = 0; getX < getPopNumber; getX++)
       //                              {
       //                                  if (moreRows)
       //                                  {
       //                                      moreRows = memberReader.Read();
       //                                      randomPopNumber = memberReader.GetInt32("member_index");
       //                                  }
       //                              }
       //                          }
       //                 memberReader.Close();
       //             }
       //             catch (Exception e)
       //             {
       //                 Console.WriteLine(e.ToString());
       //             }

       //         }

       //         if (GlobalVar.random.Next(0,100) > 75)
       //             randomPopNumber = GlobalVar.random.Next(0, GlobalVar.popCount);

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
                        GlobalVar.myScore = myReader.GetInt64("member_score");
                        GlobalVar.worstFrame = -1;
                        if (!(myReader["worst_frame"] == DBNull.Value))
                            GlobalVar.worstFrame = myReader.GetInt32("worst_frame");                            
                    }
                    myReader.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    return (-1);
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
                        return (-1);
                    }

                }

                if (!foundMe) // exists - find out if busy
                {
                    isBusy = false;
                    try
                    {
                        myReader = askBusyCommand.ExecuteReader();
                        isBusy = myReader.HasRows;
                        myReader.Close();
                    }
                    catch
                    {
                        return (-1);
                    }


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
                            foundMe = false; // another process got it
                      //      Console.Write(".B" + randomPopNumber.ToString() + ".");
                            return (-1);
                        }
                    }
                }

            }

            // set a flag/ctr in the database that a job is running (to monitor max concurrent jobs)

            return randomPopNumber;
        }

        static void GetJobParameters(ref MySqlConnection myConnection)
        {

//            MySqlCommand myCommand = new MySqlCommand("select * from run_data where job_name = @jobParam "
//                + " and job_index = @ndxParam", myConnection);
//            myCommand.Parameters.AddWithValue("@jobParam", GlobalVar.jobName);
//            myCommand.Parameters.AddWithValue("@ndxParam", GlobalVar.myUniqueID);
       //     Console.WriteLine("find " + GlobalVar.jobName + " : " + GlobalVar.myUniqueID.ToString());
//            try
//            {
//                MySqlDataReader myReader = myCommand.ExecuteReader();
//
//                while (myReader.Read())
//                {
//                    GlobalVar.normalMut = myReader.GetInt32("mrate1");
//                    GlobalVar.alternateMut = myReader.GetInt32("mrate2");
//                    GlobalVar.xoverType = myReader.GetInt32("crossover_type");
//                    GlobalVar.parentDist = myReader.GetInt32("parent_neighborhood");
//                    GlobalVar.segmentPct = myReader.GetInt32("segment_pct");
                    //           Console.WriteLine("mut rates from db: " + GlobalVar.normalMut.ToString()
         //               + " " + GlobalVar.alternateMut.ToString());
 //               }
//                myReader.Close();
//            }
//            catch (Exception e)
//            {
//                Console.WriteLine(e.ToString());
//            }
//            if (GlobalVar.xoverType > 2)
//                GlobalVar.xoverType = GlobalVar.random.Next(0, 2);

        }

        static void PopulateEmptyMember(ref MySqlConnection myConnection, int popMember)
        {
            string featureType = "";
            int newIntValue = 0;
            int featureIntMin = 0;
            int featureIntMax = 0;
            char[] buildChars;
            buildChars = new char[350000];
            string fn = "";

            GlobalVar.myScore = 0;

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
                    GlobalVar.features[i, 0] = newIntValue;
//                    GlobalVar.features[i, 0] = 0; // dsm experiment start with zero
                }

                for (int i = 0; i < GlobalVar.featureCount; i++)
                    buildChars[i] = (char)GlobalVar.features[i, 0];

                string bs = new string(buildChars);
                bs = bs.Substring(0, GlobalVar.featureCount);

                fn = "mx" + Convert.ToString(popMember);
                File.WriteAllText(fn, bs);

                // this should be the right length

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static int GetFeatureCount(ref MySqlConnection myConnection)
        {
            int fCount = 0;

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
                    fCount = myReader.GetInt32("feature_count");
                }
                myReader.Close();
            }
            catch (Exception e)
            {
                fCount = 0;
            }

            return fCount;
        }

        static void GetExistingCharacteristics(int popMember, int parent)
        {
            char[] buildChars;
            buildChars = new char[350000];
            string featureString = "";
            string fn = "";

            try
            {
                fn = "mx" + Convert.ToString(popMember);
                if (File.Exists(fn))
                {
                    featureString = File.ReadAllText(fn);
                    buildChars = featureString.ToCharArray();
                }

                Array.Resize(ref buildChars, GlobalVar.featureCount); 

                for (int i = 0; i < GlobalVar.featureCount; i++)
                {
                    GlobalVar.features[i, parent] = buildChars[i];

                    if (GlobalVar.features[i, parent] > 255)
                        GlobalVar.features[i, parent] = 255;
                    if (GlobalVar.features[i, parent] < 0)
                        GlobalVar.features[i, parent] = 0;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static void GetExistingScores(int popMember, int parent)
        {
            string fn = "sx" + Convert.ToString(popMember);
            int frameCounter = GlobalVar.featureCount / 4;
            try
            {
                BinaryReader scoreFile = new BinaryReader(File.OpenRead(fn));

                for (int fx = 0; fx < scoreFrames; fx++)
                {
                    GlobalVar.frameScore[parent, fx] = scoreFile.ReadInt32();
                }
                scoreFile.Close();
            }
            catch
            {
                for (int fx = 0; fx < scoreFrames; fx++)
                {
                    GlobalVar.frameScore[parent, fx] = -1;
                }
            }

            GetExistingCharacteristics(popMember, parent);
        }

        static void NextGenerationValues(int popMember)
        {
            int[] mutateArray = new int[8] { 1, 2, 4, 8, 16, 32, 64, 128 };
            int mutatePosition = 0;
            int mutateValue = 0;
            int parentIndex = 0;
    //        long parentScore = -1;
            int nextNeighbor = 0;
    //        int xoverPoint = 0;
    //        int xoverDir = 0;
            int xP1 = 0;
            int xP2 = 0;
            int parentX = 1;

            int neighborhood = GlobalVar.random.Next(1,(GlobalVar.parentDist+1));

 //           GlobalVar.xoverType = GlobalVar.random.Next(0, 300);

            GlobalVar.immuneFlag = true;

      //      MySqlDataReader myReader = null;

            // get parent 1 candidates 

//            MySqlCommand getCommand = new MySqlCommand("select member_index, member_score from member_header " +
//    " where job_name = @parmJob and population_index = @parmPop and member_index in (@p1,@p2,@p3,@p4,@p5) " +
//    " order by member_score desc limit 1", myConnection);

//            getCommand.Parameters.AddWithValue("@parmJob", GlobalVar.jobName);
//            getCommand.Parameters.AddWithValue("@parmPop", GlobalVar.popIndex);

            nextNeighbor = popMember;

            if ((nextNeighbor - neighborhood) < 1)
                nextNeighbor = GlobalVar.popCount;
            nextNeighbor = nextNeighbor - neighborhood;

//            getCommand.Parameters.AddWithValue("@p1", nextNeighbor);
            GetExistingScores(nextNeighbor, parentX);
            parentX++;

            if ((nextNeighbor - neighborhood) < 1)
                nextNeighbor = GlobalVar.popCount;
            nextNeighbor = nextNeighbor - neighborhood;

//            getCommand.Parameters.AddWithValue("@p2", nextNeighbor);
            GetExistingScores(nextNeighbor, parentX);
            parentX++;

            if ((nextNeighbor - neighborhood) < 1)
                nextNeighbor = GlobalVar.popCount;
            nextNeighbor = nextNeighbor - neighborhood;

//            getCommand.Parameters.AddWithValue("@p3", nextNeighbor);
            GetExistingScores(nextNeighbor, parentX);
            parentX++;

            if ((nextNeighbor - neighborhood) < 1)
                nextNeighbor = GlobalVar.popCount;
            nextNeighbor = nextNeighbor - neighborhood;

//            getCommand.Parameters.AddWithValue("@p4", nextNeighbor);
            GetExistingScores(nextNeighbor, parentX);
            parentX++;

            if ((nextNeighbor - neighborhood) < 1)
                nextNeighbor = GlobalVar.popCount;
            nextNeighbor = nextNeighbor - neighborhood;

//            getCommand.Parameters.AddWithValue("@p5", nextNeighbor);
            GetExistingScores(nextNeighbor, parentX);
            parentX++;

            nextNeighbor = popMember;
            nextNeighbor = nextNeighbor + neighborhood;
            if (nextNeighbor >= GlobalVar.popCount)
                nextNeighbor = (nextNeighbor - GlobalVar.popCount)+1;
//            getCommand.Parameters.AddWithValue("@p1", nextNeighbor);
            GetExistingScores(nextNeighbor, parentX);
            parentX++;

            nextNeighbor = nextNeighbor + neighborhood;
            if (nextNeighbor >= GlobalVar.popCount)
                nextNeighbor = (nextNeighbor - GlobalVar.popCount)+1;
//            getCommand.Parameters.AddWithValue("@p2", nextNeighbor);
            GetExistingScores(nextNeighbor, parentX);
            parentX++;

            nextNeighbor = nextNeighbor + neighborhood;
            if (nextNeighbor >= GlobalVar.popCount)
                nextNeighbor = (nextNeighbor - GlobalVar.popCount)+1;
//            getCommand.Parameters.AddWithValue("@p3", nextNeighbor);
            GetExistingScores(nextNeighbor, parentX);
            parentX++;

            nextNeighbor = nextNeighbor + neighborhood;
            if (nextNeighbor >= GlobalVar.popCount)
                nextNeighbor = (nextNeighbor - GlobalVar.popCount)+1;
//            getCommand.Parameters.AddWithValue("@p4", nextNeighbor);
            GetExistingScores(nextNeighbor, parentX);
            parentX++;

            nextNeighbor = nextNeighbor + neighborhood;
            if (nextNeighbor >= GlobalVar.popCount)
                nextNeighbor = (nextNeighbor - GlobalVar.popCount)+1;
//            getCommand.Parameters.AddWithValue("@p5", nextNeighbor);
            GetExistingScores(nextNeighbor, parentX);

            long bestScore = 0; 
            int fIndex = 0;
            for (int i = 0; i < scoreFrames; i++)
            {
                // get parents based on feature/frame

                xP1 = 0;
                bestScore = GlobalVar.frameScore[xP1, i];
                for (int px = 1; px < 6; px++)
                {
                    if ((GlobalVar.frameScore[px, i] > GlobalVar.frameScore[xP1, i]) && (GlobalVar.frameScore[px, i] > 0))
                    {
                        xP1 = px;
                        bestScore = GlobalVar.frameScore[xP1, i];
                    }
                }

                xP2 = 0;
                bestScore = GlobalVar.frameScore[xP2, i];
                for (int px = 6; px < 11; px++)
                {
                    if ((GlobalVar.frameScore[px, i] > GlobalVar.frameScore[xP2, i]) && (GlobalVar.frameScore[px, i] > 0))
                    {
                        xP2 = px;
                        bestScore = GlobalVar.frameScore[xP2, i];
                    }
                }


//                Console.WriteLine(i.ToString() + " " + parentIndex.ToString() + " " +
//                    GlobalVar.frameScore[parentIndex, i].ToString());
                for (int fx = 0; fx < (GlobalVar.featureCount / scoreFrames); fx++)
                {
                    parentIndex = xP1;
                    if ((GlobalVar.random.Next(0, 100) < 50)) // - random no crossover
                        parentIndex = xP2;
                    GlobalVar.features[fIndex, 0] = GlobalVar.features[fIndex, parentIndex];
                    if ((GlobalVar.frameScore[parentIndex, i] < 0))
                    {
                        GlobalVar.features[fIndex, 0] = GlobalVar.random.Next(0, 255);
                    }
                    fIndex++;
                }
            }

            // mutate here
            // move mutation to be after crossover (don't want to mutate all potential parents)
            for (int i = 0; i < GlobalVar.featureCount; i++)
            {
                    if (GlobalVar.random.Next(0, (GlobalVar.featureCount * 100)) < GlobalVar.MutPer100Members)
                    {
                        mutatePosition = GlobalVar.random.Next(0, 8);
                        mutateValue = mutateArray[mutatePosition];
                        if (GlobalVar.random.Next(0, 100) < 50)
                            mutateValue = -1 * mutateValue;
                        GlobalVar.features[i, 0] = GlobalVar.features[i, 0] + mutateValue;
                        if (GlobalVar.features[i, 0] < 0)
                            GlobalVar.features[i, 0] = GlobalVar.features[i, 0] - (2 * mutateValue);
                        if (GlobalVar.features[i, 0] > 255)
                            GlobalVar.features[i, 0] = GlobalVar.features[i, 0] - (2 * mutateValue);
                    }                
            }
        }

        static string ExportXMLfile(int popMember)
        {

            string filename = "";

            try
            {
                filename = GlobalVar.jobName + GlobalVar.popIndex.ToString() + popMember.ToString() + ".xml";

                try
                {
                    File.Delete(filename);
                }
                catch
                {
                    //        Console.Write(".X6." + XMLfile + ".");
                }

                XmlTextWriter xml = null;

                xml = new XmlTextWriter(filename, null);

                if (!File.Exists(filename))
                {
                    return "error";
                }

                xml.Formatting = Formatting.Indented;
                xml.IndentChar = '\t';
                xml.Indentation = 1;

                xml.WriteStartDocument();
                System.Threading.Thread.Sleep(0);

                xml.WriteStartElement("Features");
                xml.WriteWhitespace("\n");

                xml.WriteElementString("Pop", popMember.ToString());
                xml.WriteWhitespace("\n  "); 
                
                xml.WriteElementString("Generation", GlobalVar.myGeneration.ToString());
                xml.WriteWhitespace("\n  ");

                xml.WriteElementString("Score", GlobalVar.myScore.ToString());
                xml.WriteWhitespace("\n  ");

                xml.WriteElementString("Best", GlobalVar.bestScore.ToString());
                xml.WriteWhitespace("\n  ");

//                for (int i = 0; i < GlobalVar.featureCount; i++)
//                {
//                    xml.WriteElementString("Index", i.ToString());
//                    xml.WriteElementString("Value", GlobalVar.features[i, 0].ToString());
//                    xml.WriteWhitespace("\n  ");
//                }

                xml.WriteEndElement();
                xml.WriteWhitespace("\n");

                xml.WriteEndDocument();
                System.Threading.Thread.Sleep(0);

                //Write the XML to file and close the writer.
                xml.Flush();
                System.Threading.Thread.Sleep(0);
                xml.Close();
                System.Threading.Thread.Sleep(0);
            }
            catch
            {
                filename = "error";
            }


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
                            GlobalVar.myScore = Convert.ToInt64(reader.Value);
                        }
                        GlobalVar.worstFrame = -1;
                        if (elementString.Equals("worstndx"))
                        {
                            GlobalVar.worstFrame = Convert.ToInt32(reader.Value);
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
