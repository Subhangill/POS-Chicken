using Microsoft.Data.SqlClient;
using POS.Models;

namespace POS.Data.Service
{
    public class UserLog
    {
        private readonly string _connectionString;

        public UserLog( IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new Exception("Error Coming");
        }
        public void SaveHistory(string form, string action, string detail)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    string query = @"INSERT INTO UserLog (Time, UserId, Form, Action, Detail) 
                                     VALUES (@Time, @UserId, @Form, @Action, @Detail)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        // Add parameters (prevents SQL injection)\
                        cmd.Parameters.AddWithValue("@Time", AppDate.Now);
                        cmd.Parameters.AddWithValue("@UserId", 1);
                        cmd.Parameters.AddWithValue("@Form", form ?? "");
                        cmd.Parameters.AddWithValue("@Action", action ?? "");
                        cmd.Parameters.AddWithValue("@Detail", detail ?? "");
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving history: " + ex.Message);
            }
        }
    }
}
