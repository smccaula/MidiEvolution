using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;

namespace test_mysql
{
    class test_mysql
    {
        static void Main(string[] args)
        {
            string connectionString;
            MySqlConnection connection;

            connectionString = "SERVER=rdc04.uits.iu.edu" + ";" + "Port=3059" +  ";" + "DATABASE=agent" +
        ";" + "UID=smccaula" + ";" + "PASSWORD=mysqlX666" + ";";

            connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();
            }
            catch
            {
                Console.WriteLine("boo!");
            }



            string query = "DELETE FROM busy_table";


                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.ExecuteNonQuery();


                MySqlCommand scoreCommand = new MySqlCommand("SELECT * FROM job_table " +
                    "where job_group = @jobParam", connection);
                scoreCommand.Parameters.AddWithValue("@jobParam", "all");
       //         MySqlCommand scoreCommand = new MySqlCommand("SELECT * FROM job_table", connection);

                try
                {
                    MySqlDataReader myReader = scoreCommand.ExecuteReader();


                    //             myReader = scoreCommand.ExecuteReader();
                    while (myReader.Read())
                    {
                  //      Console.WriteLine("row");
                    Console.WriteLine(myReader.GetString("job_name"));
                    
                        //Console.WriteLine(Convert.ToInt32(myReader["job_name"]));
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }


                connection.Close();
                System.Threading.Thread.Sleep(5000);
        }
    }
}
