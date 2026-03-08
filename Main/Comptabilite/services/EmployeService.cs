using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Superete.Main.Comptabilite.Models;

namespace Superete.Main.Comptabilite.Services
{
    public class EmployeService
    {
        private readonly string connectionString;

        public EmployeService(string connectionString)
        {
            this.connectionString = "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;";
        }

        // CREATE
        public int Create(EmployeModel employe)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    INSERT INTO Employes (
                        NomComplet, CIN, CNSS, DateNaissance, Telephone, 
                        Email, Adresse, Poste, DateEmbauche, Actif, SalaireBase,
                        CreePar, DateCreation
                    ) VALUES (
                        @NomComplet, @CIN, @CNSS, @DateNaissance, @Telephone,
                        @Email, @Adresse, @Poste, @DateEmbauche, @Actif, @SalaireBase,
                        @CreePar, GETDATE()
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    AddParameters(cmd, employe);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        // READ ALL
        public List<EmployeModel> GetAll()
        {
            List<EmployeModel> employes = new List<EmployeModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT EmployeID, NomComplet, CIN, CNSS, DateNaissance, 
                           Telephone, Email, Adresse, Poste, DateEmbauche, 
                           Actif, SalaireBase, CreePar, DateCreation, ModifiePar, DateModification
                    FROM Employes
                    ORDER BY NomComplet";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        employes.Add(MapFromReader(reader));
                }
            }
            return employes;
        }

        // READ ACTIVE ONLY
        public List<EmployeModel> GetActive()
        {
            List<EmployeModel> employes = new List<EmployeModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT EmployeID, NomComplet, CIN, CNSS, DateNaissance, 
                           Telephone, Email, Adresse, Poste, DateEmbauche, 
                           Actif, SalaireBase, CreePar, DateCreation, ModifiePar, DateModification
                    FROM Employes
                    WHERE Actif = 1
                    ORDER BY NomComplet";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        employes.Add(MapFromReader(reader));
                }
            }
            return employes;
        }

        // READ BY ID
        public EmployeModel GetById(int employeId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT EmployeID, NomComplet, CIN, CNSS, DateNaissance, 
                           Telephone, Email, Adresse, Poste, DateEmbauche, 
                           Actif, SalaireBase, CreePar, DateCreation, ModifiePar, DateModification
                    FROM Employes
                    WHERE EmployeID = @EmployeID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeID", employeId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) return MapFromReader(reader);
                    }
                }
            }
            return null;
        }

        // UPDATE
        public void Update(EmployeModel employe)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    UPDATE Employes SET
                        NomComplet = @NomComplet,
                        CIN = @CIN,
                        CNSS = @CNSS,
                        DateNaissance = @DateNaissance,
                        Telephone = @Telephone,
                        Email = @Email,
                        Adresse = @Adresse,
                        Poste = @Poste,
                        DateEmbauche = @DateEmbauche,
                        Actif = @Actif,
                        SalaireBase = @SalaireBase,
                        ModifiePar = @ModifiePar,
                        DateModification = GETDATE()
                    WHERE EmployeID = @EmployeID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeID", employe.EmployeID);
                    AddParameters(cmd, employe);
                    cmd.Parameters.AddWithValue("@ModifiePar", employe.ModifiePar ?? Environment.UserName);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // DELETE (Soft delete)
        public void Delete(int employeId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    UPDATE Employes 
                    SET Actif = 0, ModifiePar = @ModifiePar, DateModification = GETDATE()
                    WHERE EmployeID = @EmployeID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeID", employeId);
                    cmd.Parameters.AddWithValue("@ModifiePar", Environment.UserName);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // SEARCH
        public List<EmployeModel> Search(string searchTerm)
        {
            List<EmployeModel> employes = new List<EmployeModel>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT EmployeID, NomComplet, CIN, CNSS, DateNaissance, 
                           Telephone, Email, Adresse, Poste, DateEmbauche, 
                           Actif, SalaireBase, CreePar, DateCreation, ModifiePar, DateModification
                    FROM Employes
                    WHERE (NomComplet LIKE @SearchTerm 
                        OR CIN LIKE @SearchTerm 
                        OR CNSS LIKE @SearchTerm
                        OR Telephone LIKE @SearchTerm)
                        AND Actif = 1
                    ORDER BY NomComplet";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm + "%");
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            employes.Add(MapFromReader(reader));
                    }
                }
            }
            return employes;
        }

        private void AddParameters(SqlCommand cmd, EmployeModel employe)
        {
            cmd.Parameters.AddWithValue("@NomComplet", employe.NomComplet);
            cmd.Parameters.AddWithValue("@CIN", employe.CIN ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CNSS", employe.CNSS ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DateNaissance", employe.DateNaissance ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Telephone", employe.Telephone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", employe.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Adresse", employe.Adresse ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Poste", employe.Poste ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@DateEmbauche", employe.DateEmbauche ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Actif", employe.Actif);
            cmd.Parameters.AddWithValue("@SalaireBase", employe.SalaireBase);
            cmd.Parameters.AddWithValue("@CreePar", employe.CreePar ?? Environment.UserName);
        }

        private EmployeModel MapFromReader(SqlDataReader reader)
        {
            return new EmployeModel
            {
                EmployeID = reader.GetInt32(reader.GetOrdinal("EmployeID")),
                NomComplet = reader.GetString(reader.GetOrdinal("NomComplet")),
                CIN = reader.IsDBNull(reader.GetOrdinal("CIN")) ? null : reader.GetString(reader.GetOrdinal("CIN")),
                CNSS = reader.IsDBNull(reader.GetOrdinal("CNSS")) ? null : reader.GetString(reader.GetOrdinal("CNSS")),
                DateNaissance = reader.IsDBNull(reader.GetOrdinal("DateNaissance")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateNaissance")),
                Telephone = reader.IsDBNull(reader.GetOrdinal("Telephone")) ? null : reader.GetString(reader.GetOrdinal("Telephone")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
                Adresse = reader.IsDBNull(reader.GetOrdinal("Adresse")) ? null : reader.GetString(reader.GetOrdinal("Adresse")),
                Poste = reader.IsDBNull(reader.GetOrdinal("Poste")) ? null : reader.GetString(reader.GetOrdinal("Poste")),
                DateEmbauche = reader.IsDBNull(reader.GetOrdinal("DateEmbauche")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateEmbauche")),
                Actif = reader.GetBoolean(reader.GetOrdinal("Actif")),
                SalaireBase = reader.IsDBNull(reader.GetOrdinal("SalaireBase")) ? 0m : reader.GetDecimal(reader.GetOrdinal("SalaireBase")),
                CreePar = reader.IsDBNull(reader.GetOrdinal("CreePar")) ? null : reader.GetString(reader.GetOrdinal("CreePar")),
                DateCreation = reader.IsDBNull(reader.GetOrdinal("DateCreation")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateCreation")),
                ModifiePar = reader.IsDBNull(reader.GetOrdinal("ModifiePar")) ? null : reader.GetString(reader.GetOrdinal("ModifiePar")),
                DateModification = reader.IsDBNull(reader.GetOrdinal("DateModification")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateModification"))
            };
        }
    }
}