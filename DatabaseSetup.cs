using System;
using System.Data.SqlClient;
using System.IO;
using System.Windows;

namespace Superete
{
    public static class DatabaseSetup
    {
        private const string DATABASE_NAME = "GESTIONCOMERCEP";

        // Detected at first call — null until IsSqlServerAvailable() succeeds
        private static string _detectedServer = null;

        private static string MasterConnection =>
            $"Server={_detectedServer ?? "localhost\\SQLEXPRESS"};Database=master;Trusted_Connection=True;Connection Timeout=5;";

        private static string AppConnection =>
            $"Server={_detectedServer ?? "localhost\\SQLEXPRESS"};Database={DATABASE_NAME};Trusted_Connection=True;Connection Timeout=5;";

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

                // Check if database exists — create if missing
                if (!DatabaseExists())
                {
                    if (!RestoreDatabaseFromBackup() && !CreateEmptyDatabase())
                    {
                        MessageBox.Show("Failed to setup database. Please contact support.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }

                // Check if tables exist — run init_db.sql if the DB is empty (sqlcmd may have failed)
                if (!TablesExist())
                    RunInitScript();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database setup error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static bool IsSqlServerAvailable()
        {
            // Try common SQL Server instance names in order
            string[] candidates =
            {
                "localhost\\SQLEXPRESS",
                "localhost",
                "(local)\\SQLEXPRESS",
                "(local)",
                ".\\SQLEXPRESS",
                ".",
            };

            foreach (var server in candidates)
            {
                try
                {
                    var cs = $"Server={server};Database=master;Trusted_Connection=True;Connection Timeout=3;";
                    using (var conn = new SqlConnection(cs))
                    {
                        conn.Open();
                        _detectedServer = server;
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        private static bool DatabaseExists()
        {
            try
            {
                using (var conn = new SqlConnection(MasterConnection))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        $"SELECT database_id FROM sys.databases WHERE Name = '{DATABASE_NAME}'", conn))
                        return cmd.ExecuteScalar() != null;
                }
            }
            catch { return false; }
        }

        private static bool TablesExist()
        {
            try
            {
                using (var conn = new SqlConnection(AppConnection))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(1) FROM sys.tables WHERE type='U'", conn))
                        return (int)cmd.ExecuteScalar() > 0;
                }
            }
            catch { return false; }
        }

        // All permissions = 1, used on dev machine where init_db.sql still has the placeholder.
        // Count must match the 64 columns in the Role INSERT in init_db.sql.
        private const string AllPermissions =
            "1,1,1,1,1,1," + // CreateClient..ViewClient
            "1,1,1,1,1,1," + // CreateFournisseur..ViewFournisseur
            "1,1,1,1,"      + // ReverseOperation..ViewMouvment
            "1,"            + // ViewProjectManagment
            "1,1,1,1,1,"   + // ViewSettings..AddUsers
            "1,1,1,"        + // ViewRoles..DeleteRoles
            "1,1,1,1,"      + // ViewFamilly..AddFamilly
            "1,1,1,1,"      + // AddArticle..ViewArticle
            "1,1,1,1,"      + // Repport,Ticket,SolderFournisseur,SolderClient
            "1,1,1,"        + // ViewFactureSettings,ModifyFactureSettings,ViewFacture
            "1,1,1,1,"      + // ViewPaymentMethod..DeletePaymentMethod
            "1,1,1,1,"      + // ViewApropos..ViewShutDown
            "1,1,1,1,"      + // ViewClientsPage..ViewVente
            "1,1,1,1,"      + // CashClient,ViewCreditClient,ViewCreditFournisseur,CashFournisseur
            "1,1,1,1,1,"   + // AccessFacturation..FactureEnregistrees
            "1,1,1";          // AccessLivraison,CreationLivraison,GestionLivreur

        // Runs init_db.sql (deployed alongside the exe by the installer).
        // Splits by GO so SqlCommand can execute each batch independently.
        private static void RunInitScript()
        {
            try
            {
                var scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "init_db.sql");
                if (!File.Exists(scriptPath)) return;

                var script = File.ReadAllText(scriptPath, System.Text.Encoding.UTF8);
                // Replace placeholder with all-1s when running on dev machine
                // (installed clients get the processed version from the installer)
                script = script.Replace("{{ROLE_PERMISSION_VALUES}}", AllPermissions);

                // Split batches on GO lines (case-insensitive, any surrounding whitespace)
                var batches = System.Text.RegularExpressions.Regex.Split(
                    script, @"^\s*GO\s*$",
                    System.Text.RegularExpressions.RegexOptions.Multiline |
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                // Use master connection so USE statements in the script work
                using (var conn = new SqlConnection(MasterConnection))
                {
                    conn.Open();
                    foreach (var batch in batches)
                    {
                        var sql = batch.Trim();
                        if (string.IsNullOrWhiteSpace(sql)) continue;
                        try
                        {
                            using (var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 })
                                cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[RunInitScript] batch failed: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RunInitScript] error: {ex.Message}");
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

                using (SqlConnection conn = new SqlConnection(MasterConnection))
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
                using (SqlConnection conn = new SqlConnection(MasterConnection))
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
                using (SqlConnection conn = new SqlConnection(AppConnection))
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

                        // Livraison table — add columns that may be missing from older schema versions
                        @"IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Livraison') AND name='Etat')
                          ALTER TABLE Livraison ADD Etat BIT NULL DEFAULT 1",

                        @"IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Livraison') AND name='DateCreation')
                          ALTER TABLE Livraison ADD DateCreation DATETIME NULL DEFAULT GETDATE()",

                        @"IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Livraison') AND name='DateCommande')
                          ALTER TABLE Livraison ADD DateCommande DATETIME NULL DEFAULT GETDATE()",

                        @"IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Livraison') AND name='PaiementStatut')
                          ALTER TABLE Livraison ADD PaiementStatut NVARCHAR(50) NULL DEFAULT 'non_paye'",

                        @"IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('Livraison') AND name='TotalCommande')
                          ALTER TABLE Livraison ADD TotalCommande DECIMAL(10,2) NOT NULL DEFAULT 0",

                        @"UPDATE Livraison SET Etat=1 WHERE Etat IS NULL",

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