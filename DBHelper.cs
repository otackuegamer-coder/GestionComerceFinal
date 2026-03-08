using System;
using System.Data.SqlClient;

namespace GestionComerce
{
    public class DBHelper
    {
        // Use the same connection string as DatabaseSetup
        private static string connectionString = @"Server=localhost\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;";

        public static string ConnectionString
        {
            get { return connectionString; }
            set { connectionString = value; }
        }

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public static bool TestConnection()
        {
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Connection test failed: {ex.Message}");
                return false;
            }
        }

        // Get connection string details for display/debugging
        public static string GetConnectionInfo()
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConnectionString);
                return $"Server: {builder.DataSource}, Database: {builder.InitialCatalog}";
            }
            catch
            {
                return "Connection string not configured";
            }
        }

        // Method to update connection string programmatically if needed
        public static void SetConnectionString(string server, string database, bool integratedSecurity = true, string username = null, string password = null)
        {
            if (integratedSecurity)
            {
                connectionString = $"Server={server};Database={database};Trusted_Connection=True;";
            }
            else
            {
                connectionString = $"Server={server};Database={database};User Id={username};Password={password};";
            }
        }
    }
}