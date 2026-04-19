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

        /// <summary>
        /// Adds any columns that exist in code but are missing from the database.
        /// Safe to call on every startup — each ALTER only runs if the column is absent.
        /// </summary>
        public static void EnsureColumns()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(APP_CONNECTION))
                {
                    conn.Open();
                    string[] migrations = new[]
                    {
                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Employes') AND name = 'SalaireBase')
                          ALTER TABLE Employes ADD SalaireBase DECIMAL(18,2) NOT NULL DEFAULT 0",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'IsTransformed')
                          ALTER TABLE Invoice ADD IsTransformed BIT NOT NULL DEFAULT 0",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'TransformedToId')
                          ALTER TABLE Invoice ADD TransformedToId INT NULL",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'TransformedFromId')
                          ALTER TABLE Invoice ADD TransformedFromId INT NULL",

                        // Invoice columns that may be missing from the original table
                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'IsReversed')
                          ALTER TABLE Invoice ADD IsReversed BIT NOT NULL DEFAULT 0",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'EtatFacture')
                          ALTER TABLE Invoice ADD EtatFacture INT NOT NULL DEFAULT 0",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'IsDeleted')
                          ALTER TABLE Invoice ADD IsDeleted BIT NOT NULL DEFAULT 0",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'Remise')
                          ALTER TABLE Invoice ADD Remise DECIMAL(18,2) NOT NULL DEFAULT 0",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'TotalAfterRemise')
                          ALTER TABLE Invoice ADD TotalAfterRemise DECIMAL(18,2) NOT NULL DEFAULT 0",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'TVARate')
                          ALTER TABLE Invoice ADD TVARate DECIMAL(18,2) NOT NULL DEFAULT 0",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'TotalHT')
                          ALTER TABLE Invoice ADD TotalHT DECIMAL(18,2) NOT NULL DEFAULT 0",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'TotalTVA')
                          ALTER TABLE Invoice ADD TotalTVA DECIMAL(18,2) NOT NULL DEFAULT 0",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'TotalTTC')
                          ALTER TABLE Invoice ADD TotalTTC DECIMAL(18,2) NOT NULL DEFAULT 0",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'CreditMontant')
                          ALTER TABLE Invoice ADD CreditMontant DECIMAL(18,2) NULL",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'CreditClientName')
                          ALTER TABLE Invoice ADD CreditClientName NVARCHAR(255) NULL",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'Currency')
                          ALTER TABLE Invoice ADD Currency NVARCHAR(10) NULL",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'InvoiceIndex')
                          ALTER TABLE Invoice ADD InvoiceIndex NVARCHAR(50) NULL",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'CreatedBy')
                          ALTER TABLE Invoice ADD CreatedBy INT NULL",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'ModifiedBy')
                          ALTER TABLE Invoice ADD ModifiedBy INT NULL",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'ModifiedDate')
                          ALTER TABLE Invoice ADD ModifiedDate DATETIME NULL",

                        // InvoiceArticle.Remise — added via migration, may be missing
                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('InvoiceArticle') AND name = 'Remise')
                          ALTER TABLE InvoiceArticle ADD Remise DECIMAL(18,2) NOT NULL DEFAULT 0",

                        // Invoice.LogoPath — widen so long paths don't truncate
                        @"IF EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Invoice') AND name = 'LogoPath'
                              AND max_length < 1000)
                          ALTER TABLE Invoice ALTER COLUMN LogoPath NVARCHAR(MAX) NULL",

                        // Article.Date — make nullable (TryUpdateExtendedColumns sends null when no date is set)
                        @"IF EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Article') AND name = 'Date'
                              AND is_nullable = 0)
                          ALTER TABLE Article ALTER COLUMN Date DATETIME NULL",

                        // Article columns that are hardcoded in the API's main UPDATE but may be missing
                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Article') AND name = 'IsUnlimitedStock')
                          ALTER TABLE Article ADD IsUnlimitedStock BIT NOT NULL DEFAULT 0",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Article') AND name = 'DateExpiration')
                          ALTER TABLE Article ADD DateExpiration DATETIME NULL",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Article') AND name = 'NumeroLot')
                          ALTER TABLE Article ADD NumeroLot NVARCHAR(100) NULL",

                        @"IF NOT EXISTS (
                            SELECT 1 FROM sys.columns
                            WHERE object_id = OBJECT_ID('Article') AND name = 'FournisseurID')
                          ALTER TABLE Article ADD FournisseurID INT NULL",

                        // Article.FournisseurID — make nullable so null supplier doesn't violate NOT NULL.
                        // The DROP + re-add of FK is needed on SQL Server if a FK constraint exists on this column.
                        @"BEGIN TRY
                            IF EXISTS (
                                SELECT 1 FROM sys.columns
                                WHERE object_id = OBJECT_ID('Article') AND name = 'FournisseurID'
                                  AND is_nullable = 0)
                            BEGIN
                                DECLARE @fkName NVARCHAR(256);
                                SELECT @fkName = fk.name
                                FROM sys.foreign_keys fk
                                JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                                JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
                                WHERE fkc.parent_object_id = OBJECT_ID('Article') AND c.name = 'FournisseurID';

                                IF @fkName IS NOT NULL
                                    EXEC('ALTER TABLE Article DROP CONSTRAINT [' + @fkName + ']');

                                ALTER TABLE Article ALTER COLUMN FournisseurID INT NULL;
                            END
                          END TRY
                          BEGIN CATCH
                          END CATCH"
                    };

                    foreach (string sql in migrations)
                    {
                        try
                        {
                            using (SqlCommand cmd = new SqlCommand(sql, conn))
                                cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[EnsureColumns] Migration failed: {ex.Message}\nSQL: {sql.Trim().Substring(0, Math.Min(120, sql.Trim().Length))}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EnsureColumns] Connection failed: {ex.Message}");
            }
        }
    }
}