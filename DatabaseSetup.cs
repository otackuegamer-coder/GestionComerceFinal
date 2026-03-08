using System;
using System.Data.SqlClient;
using System.IO;
using System.Windows;

namespace Superete
{
    public static class DatabaseSetup
    {
        private const string DATABASE_NAME = "GESTIONCOMERCE";
        private const string MASTER_CONNECTION ="Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;";
        private const string APP_CONNECTION = "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;";

        /// <summary>
        /// Checks if database exists and creates it if not
        /// </summary>
        public static bool EnsureDatabaseExists()
        {
            try
            {
                // Check if SQL Server is accessible
                if (!IsSqlServerAvailable())
                {
                    MessageBox.Show(
                        "SQL Server Express is not installed or not running.\n\n" +
                        "Please install SQL Server Express 2022 from:\n" +
                        "https://go.microsoft.com/fwlink/?linkid=866658\n\n" +
                        "After installation, restart this application.",
                        "SQL Server Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return false;
                }

                // Check if database exists
                if (!DatabaseExists())
                {
                    // Try to restore from backup
                    if (RestoreDatabaseFromBackup())
                    {
                        MessageBox.Show("Database setup completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        return true;
                    }
                    else
                    {
                        // If backup restore fails, create empty database
                        if (CreateEmptyDatabase())
                        {
                            MessageBox.Show("Database created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            return true;
                        }
                        else
                        {
                            MessageBox.Show("Failed to setup database. Please contact support.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }
                }

                return true; // Database already exists
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database setup error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static bool IsSqlServerAvailable()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(MASTER_CONNECTION))
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool DatabaseExists()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(MASTER_CONNECTION))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand($"SELECT database_id FROM sys.databases WHERE Name = '{DATABASE_NAME}'", conn))
                    {
                        object result = cmd.ExecuteScalar();
                        return result != null;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool RestoreDatabaseFromBackup()
        {
            try
            {
                // Look for backup file in multiple locations
                string backupPath = FindBackupFile();

                if (string.IsNullOrEmpty(backupPath) || !File.Exists(backupPath))
                {
                    return false; // No backup file found
                }

                using (SqlConnection conn = new SqlConnection(MASTER_CONNECTION))
                {
                    conn.Open();

                    // Kill any existing connections
                    string killConnections = $@"
                        ALTER DATABASE [{DATABASE_NAME}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    ";

                    try
                    {
                        using (SqlCommand cmd = new SqlCommand(killConnections, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { } // Database might not exist yet

                    // Get default data and log paths
                    string dataPath = GetDefaultDataPath(conn);
                    string logPath = GetDefaultLogPath(conn);

                    // Restore database
                    string restoreQuery = $@"
                        RESTORE DATABASE [{DATABASE_NAME}]
                        FROM DISK = '{backupPath}'
                        WITH 
                            MOVE '{DATABASE_NAME}' TO '{dataPath}\{DATABASE_NAME}.mdf',
                            MOVE '{DATABASE_NAME}_log' TO '{logPath}\{DATABASE_NAME}_log.ldf',
                            REPLACE
                    ";

                    using (SqlCommand cmd = new SqlCommand(restoreQuery, conn))
                    {
                        cmd.CommandTimeout = 300; // 5 minutes
                        cmd.ExecuteNonQuery();
                    }

                    // Set database to multi-user mode
                    using (SqlCommand cmd = new SqlCommand($"ALTER DATABASE [{DATABASE_NAME}] SET MULTI_USER", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Restore failed: {ex.Message}");
                return false;
            }
        }

        private static bool CreateEmptyDatabase()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(MASTER_CONNECTION))
                {
                    conn.Open();
                    string createDb = $"CREATE DATABASE [{DATABASE_NAME}]";
                    using (SqlCommand cmd = new SqlCommand(createDb, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                // TODO: Add your table creation scripts here
                // CreateTables();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Create database failed: {ex.Message}");
                return false;
            }
        }

        private static string FindBackupFile()
        {
            // Check multiple possible locations
            string[] possiblePaths = new string[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "GESTIONCOMERCE.bak"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GESTIONCOMERCE.bak"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Superete", "Database", "GESTIONCOMERCE.bak")
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        private static string GetDefaultDataPath(SqlConnection conn)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand("SELECT SERVERPROPERTY('InstanceDefaultDataPath')", conn))
                {
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        return result.ToString().TrimEnd('\\');
                }
            }
            catch { }

            return @"C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA";
        }

        private static string GetDefaultLogPath(SqlConnection conn)
        {
            try
            {
                using (SqlCommand cmd = new SqlCommand("SELECT SERVERPROPERTY('InstanceDefaultLogPath')", conn))
                {
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        return result.ToString().TrimEnd('\\');
                }
            }
            catch { }

            return @"C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA";
        }
    }
}