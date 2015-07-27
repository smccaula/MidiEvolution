﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.IO;


namespace as_clear
{
    class as_clear
    {
        static void Main(string[] args)
        {
            string jobName = "test";
            int featureCount = 15360;
            int popCount = 100;
            Random random = new Random();
            int randomJobNumber = random.Next(1, 5000000);

            if (args.Length > 0) // first arg should be mandatory - job name
            {
                jobName = args[0];
            }

            if (args.Length > 1)
            {
                featureCount = Convert.ToInt32(args[1]);
            }

            if (args.Length > 2)
            {
                popCount = Convert.ToInt32(args[2]);
            }

            foreach (FileInfo f in new DirectoryInfo(Directory.GetCurrentDirectory()).GetFiles("mx*"))
            {
                f.Delete();
            }

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

            Console.WriteLine("delete busy table");
            MySqlCommand deleteCommand = new MySqlCommand("delete from busy_table " +
    " where job_name = @jobParm ", myConnection);
            deleteCommand.Parameters.AddWithValue("@jobParm", jobName);

            deleteCommand.ExecuteNonQuery();

     //       Console.WriteLine("delete member detail");
     //       deleteCommand.CommandText = "delete from member_detail where job_name = @jobParm ";
     //       deleteCommand.ExecuteNonQuery();

            Console.WriteLine("delete member headers");
            deleteCommand.CommandText = "delete from member_header where job_name = @jobParm ";
            deleteCommand.ExecuteNonQuery();

            Console.WriteLine("delete best scores");
            deleteCommand.CommandText = "delete from best_scores where job_name = @jobParm ";
            deleteCommand.ExecuteNonQuery();

            Console.WriteLine("delete bonus scores");
            deleteCommand.CommandText = "delete from bonus_score where job_name = @jobParm ";
            deleteCommand.ExecuteNonQuery();

//            Console.WriteLine("delete run data");
//            deleteCommand.CommandText = "delete from run_data where job_name = @jobParm ";
//            deleteCommand.CommandText = "delete from run_data";
//            deleteCommand.ExecuteNonQuery();

            Console.WriteLine("delete score detail");
            deleteCommand.CommandText = "delete from score_detail where job_name = @jobParm ";
//            deleteCommand.CommandText = "delete from score_detail";
            deleteCommand.ExecuteNonQuery();

            Console.WriteLine("update population features");
            MySqlCommand updateFeatures = new MySqlCommand("update population_features set feature_count = @featureParm " +
" where job_name = @jobParm ", myConnection);
            updateFeatures.Parameters.AddWithValue("@jobParm", jobName);
            updateFeatures.Parameters.AddWithValue("@featureParm", featureCount);

            updateFeatures.ExecuteNonQuery();

            Console.WriteLine("update population table");
            MySqlCommand updatePop = new MySqlCommand("update population_table set population_count = @popParm " +
" where job_name = @jobParm ", myConnection);
            updatePop.Parameters.AddWithValue("@jobParm", jobName);
            updatePop.Parameters.AddWithValue("@popParm", popCount);

            updatePop.ExecuteNonQuery();

//            Console.WriteLine("insert run data");
//            MySqlCommand insertJob = new MySqlCommand("insert into run_data (job_name, population_count, feature_count, parent_neighborhood, " +
//                "crossover_type, mutation_zones, mrate1, mrate2, generation_count, job_index, segment_pct) " +
//                " VALUES (@jobParm, @popParm, @featureParm, @parentParm, @crossParm, @zonesParm, @rate1Parm, @rate2Parm, 0, @ndxParm, 35)", myConnection);
//            insertJob.Parameters.AddWithValue("@jobParm", jobName);
//            insertJob.Parameters.AddWithValue("@popParm", popCount);
//            insertJob.Parameters.AddWithValue("@featureParm", featureCount);
   //         int parentCount = random.Next(1, 5);
//            int parentCount = 10; 
//            insertJob.Parameters.AddWithValue("@parentParm", parentCount);
//            int crossType = random.Next(0, 2);
//            int crossType = 1;
//            insertJob.Parameters.AddWithValue("@crossParm", crossType);
//            int mutZones = random.Next(1, 3);
//            int mutZones = 2;
//            insertJob.Parameters.AddWithValue("@zonesParm", mutZones);
//            int rate1 = random.Next(75, 125);
//            insertJob.Parameters.AddWithValue("@rate1Parm", rate1);
//            int rate2 = random.Next(175, 225);
//            if (mutZones < 2)
//                rate2 = rate1;
//            insertJob.Parameters.AddWithValue("@rate2Parm", rate2);
//            insertJob.Parameters.AddWithValue("@ndxParm", randomJobNumber);
//            insertJob.ExecuteNonQuery();

            Console.WriteLine("call as_solution");

            Process userProcess = new Process();

            userProcess.StartInfo.CreateNoWindow = true;
            userProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            userProcess.StartInfo.FileName = "as_solution.exe";
            userProcess.StartInfo.Arguments = featureCount.ToString();
            userProcess.Start();

            userProcess.WaitForExit();
            userProcess.Dispose();

        }
    }
}
