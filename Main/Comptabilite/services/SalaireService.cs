using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Superete.Main.Comptabilite.Models;

namespace Superete.Main.Comptabilite.Services
{
    public class SalaireService
    {
        private readonly string connectionString;

        public SalaireService(string connectionString)
        {
            this.connectionString = "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;";
        }

        // CREATE
        public int Create(SalaireModel salaire)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    INSERT INTO Salaires (
                        EmployeID, NomComplet, CIN, CNSS,
                        Mois, Annee, DatePaiement,
                        SalaireBase, TauxHoraire, HeuresNormales,
                        HeuresSupp25, HeuresSupp50, HeuresSupp100, MontantHeuresSupp,
                        PrimeAnciennete, PrimeRendement, PrimeResponsabilite,
                        IndemniteTransport, IndemniteLogement, AutresPrimes,
                        CotisationCNSS, CotisationAMO, CotisationCIMR, MontantIR,
                        CotisationPatronaleCNSS, CotisationPatronaleAMO,
                        AvanceSurSalaire, PretPersonnel, Penalites, AutresRetenues,
                        Statut, Remarques, CreePar, DateCreation
                    ) VALUES (
                        @EmployeID, @NomComplet, @CIN, @CNSS,
                        @Mois, @Annee, @DatePaiement,
                        @SalaireBase, @TauxHoraire, @HeuresNormales,
                        @HeuresSupp25, @HeuresSupp50, @HeuresSupp100, @MontantHeuresSupp,
                        @PrimeAnciennete, @PrimeRendement, @PrimeResponsabilite,
                        @IndemniteTransport, @IndemniteLogement, @AutresPrimes,
                        @CotisationCNSS, @CotisationAMO, @CotisationCIMR, @MontantIR,
                        @CotisationPatronaleCNSS, @CotisationPatronaleAMO,
                        @AvanceSurSalaire, @PretPersonnel, @Penalites, @AutresRetenues,
                        @Statut, @Remarques, @CreePar, GETDATE()
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    AddSalaireParameters(cmd, salaire);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }

        // READ ALL
        public List<SalaireModel> GetAll()
        {
            List<SalaireModel> salaires = new List<SalaireModel>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT SalaireID, EmployeID, NomComplet, CIN, CNSS, 
                           Mois, Annee, DatePaiement,
                           SalaireBase, HeuresNormales, TauxHoraire,
                           PrimeAnciennete, PrimeRendement, PrimeResponsabilite,
                           IndemniteTransport, IndemniteLogement, AutresPrimes,
                           HeuresSupp25, HeuresSupp50, HeuresSupp100, MontantHeuresSupp,
                           SalaireBrut, CotisationCNSS, CotisationAMO, CotisationCIMR,
                           MontantIR, AvanceSurSalaire, PretPersonnel, Penalites, AutresRetenues,
                           TotalRetenues, SalaireNet,
                           CotisationPatronaleCNSS, CotisationPatronaleAMO,
                           Statut, Remarques, CreePar, DateCreation, ModifiePar, DateModification
                    FROM Salaires 
                    ORDER BY Annee DESC, Mois DESC, NomComplet";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        salaires.Add(MapFromReader(reader));
                    }
                }
            }

            return salaires;
        }

        // READ BY ID
        public SalaireModel GetById(int salaireId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT SalaireID, EmployeID, NomComplet, CIN, CNSS, 
                           Mois, Annee, DatePaiement,
                           SalaireBase, HeuresNormales, TauxHoraire,
                           PrimeAnciennete, PrimeRendement, PrimeResponsabilite,
                           IndemniteTransport, IndemniteLogement, AutresPrimes,
                           HeuresSupp25, HeuresSupp50, HeuresSupp100, MontantHeuresSupp,
                           SalaireBrut, CotisationCNSS, CotisationAMO, CotisationCIMR,
                           MontantIR, AvanceSurSalaire, PretPersonnel, Penalites, AutresRetenues,
                           TotalRetenues, SalaireNet,
                           CotisationPatronaleCNSS, CotisationPatronaleAMO,
                           Statut, Remarques, CreePar, DateCreation, ModifiePar, DateModification
                    FROM Salaires
                    WHERE SalaireID = @SalaireID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SalaireID", salaireId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapFromReader(reader);
                        }
                    }
                }
            }

            return null;
        }

        // READ BY EMPLOYEE
        public List<SalaireModel> GetByEmploye(int employeId)
        {
            List<SalaireModel> salaires = new List<SalaireModel>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT SalaireID, EmployeID, NomComplet, CIN, CNSS, 
                           Mois, Annee, DatePaiement,
                           SalaireBase, HeuresNormales, TauxHoraire,
                           PrimeAnciennete, PrimeRendement, PrimeResponsabilite,
                           IndemniteTransport, IndemniteLogement, AutresPrimes,
                           HeuresSupp25, HeuresSupp50, HeuresSupp100, MontantHeuresSupp,
                           SalaireBrut, CotisationCNSS, CotisationAMO, CotisationCIMR,
                           MontantIR, AvanceSurSalaire, PretPersonnel, Penalites, AutresRetenues,
                           TotalRetenues, SalaireNet,
                           CotisationPatronaleCNSS, CotisationPatronaleAMO,
                           Statut, Remarques, CreePar, DateCreation, ModifiePar, DateModification
                    FROM Salaires
                    WHERE EmployeID = @EmployeID
                    ORDER BY Annee DESC, Mois DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@EmployeID", employeId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            salaires.Add(MapFromReader(reader));
                        }
                    }
                }
            }

            return salaires;
        }

        // READ BY PERIOD
        public List<SalaireModel> GetByPeriod(int mois, int annee)
        {
            List<SalaireModel> salaires = new List<SalaireModel>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT SalaireID, EmployeID, NomComplet, CIN, CNSS, 
                           Mois, Annee, DatePaiement,
                           SalaireBase, HeuresNormales, TauxHoraire,
                           PrimeAnciennete, PrimeRendement, PrimeResponsabilite,
                           IndemniteTransport, IndemniteLogement, AutresPrimes,
                           HeuresSupp25, HeuresSupp50, HeuresSupp100, MontantHeuresSupp,
                           SalaireBrut, CotisationCNSS, CotisationAMO, CotisationCIMR,
                           MontantIR, AvanceSurSalaire, PretPersonnel, Penalites, AutresRetenues,
                           TotalRetenues, SalaireNet,
                           CotisationPatronaleCNSS, CotisationPatronaleAMO,
                           Statut, Remarques, CreePar, DateCreation, ModifiePar, DateModification
                    FROM Salaires
                    WHERE Mois = @Mois AND Annee = @Annee
                    ORDER BY NomComplet";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Mois", mois);
                    cmd.Parameters.AddWithValue("@Annee", annee);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            salaires.Add(MapFromReader(reader));
                        }
                    }
                }
            }

            return salaires;
        }

        // UPDATE
        public void Update(SalaireModel salaire)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    UPDATE Salaires SET
                        EmployeID = @EmployeID,
                        NomComplet = @NomComplet,
                        CIN = @CIN,
                        CNSS = @CNSS,
                        Mois = @Mois,
                        Annee = @Annee,
                        DatePaiement = @DatePaiement,
                        SalaireBase = @SalaireBase,
                        TauxHoraire = @TauxHoraire,
                        HeuresNormales = @HeuresNormales,
                        HeuresSupp25 = @HeuresSupp25,
                        HeuresSupp50 = @HeuresSupp50,
                        HeuresSupp100 = @HeuresSupp100,
                        MontantHeuresSupp = @MontantHeuresSupp,
                        PrimeAnciennete = @PrimeAnciennete,
                        PrimeRendement = @PrimeRendement,
                        PrimeResponsabilite = @PrimeResponsabilite,
                        IndemniteTransport = @IndemniteTransport,
                        IndemniteLogement = @IndemniteLogement,
                        AutresPrimes = @AutresPrimes,
                        CotisationCNSS = @CotisationCNSS,
                        CotisationAMO = @CotisationAMO,
                        CotisationCIMR = @CotisationCIMR,
                        MontantIR = @MontantIR,
                        CotisationPatronaleCNSS = @CotisationPatronaleCNSS,
                        CotisationPatronaleAMO = @CotisationPatronaleAMO,
                        AvanceSurSalaire = @AvanceSurSalaire,
                        PretPersonnel = @PretPersonnel,
                        Penalites = @Penalites,
                        AutresRetenues = @AutresRetenues,
                        Statut = @Statut,
                        Remarques = @Remarques,
                        ModifiePar = @ModifiePar,
                        DateModification = GETDATE()
                    WHERE SalaireID = @SalaireID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SalaireID", salaire.SalaireID);
                    cmd.Parameters.AddWithValue("@ModifiePar", salaire.ModifiePar ?? Environment.UserName);
                    AddSalaireParameters(cmd, salaire);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // DELETE
        public void Delete(int salaireId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Salaires WHERE SalaireID = @SalaireID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SalaireID", salaireId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // UPDATE STATUS
        public void UpdateStatus(int salaireId, string statut)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    UPDATE Salaires 
                    SET Statut = @Statut, 
                        DatePaiement = CASE WHEN @Statut = 'Payé' THEN GETDATE() ELSE DatePaiement END,
                        ModifiePar = @ModifiePar,
                        DateModification = GETDATE()
                    WHERE SalaireID = @SalaireID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SalaireID", salaireId);
                    cmd.Parameters.AddWithValue("@Statut", statut);
                    cmd.Parameters.AddWithValue("@ModifiePar", Environment.UserName);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Helper method to add parameters
        private void AddSalaireParameters(SqlCommand cmd, SalaireModel salaire)
        {
            cmd.Parameters.AddWithValue("@EmployeID", salaire.EmployeID);
            cmd.Parameters.AddWithValue("@NomComplet", salaire.NomComplet);
            cmd.Parameters.AddWithValue("@CIN", salaire.CIN ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CNSS", salaire.CNSS ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Mois", salaire.Mois);
            cmd.Parameters.AddWithValue("@Annee", salaire.Annee);
            cmd.Parameters.AddWithValue("@DatePaiement", salaire.DatePaiement ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@SalaireBase", salaire.SalaireBase);
            cmd.Parameters.AddWithValue("@TauxHoraire", salaire.TauxHoraire);
            cmd.Parameters.AddWithValue("@HeuresNormales", salaire.HeuresNormales);
            cmd.Parameters.AddWithValue("@HeuresSupp25", salaire.HeuresSupp25);
            cmd.Parameters.AddWithValue("@HeuresSupp50", salaire.HeuresSupp50);
            cmd.Parameters.AddWithValue("@HeuresSupp100", salaire.HeuresSupp100);
            cmd.Parameters.AddWithValue("@MontantHeuresSupp", salaire.MontantHeuresSupp);
            cmd.Parameters.AddWithValue("@PrimeAnciennete", salaire.PrimeAnciennete);
            cmd.Parameters.AddWithValue("@PrimeRendement", salaire.PrimeRendement);
            cmd.Parameters.AddWithValue("@PrimeResponsabilite", salaire.PrimeResponsabilite);
            cmd.Parameters.AddWithValue("@IndemniteTransport", salaire.IndemniteTransport);
            cmd.Parameters.AddWithValue("@IndemniteLogement", salaire.IndemniteLogement);
            cmd.Parameters.AddWithValue("@AutresPrimes", salaire.AutresPrimes);
            cmd.Parameters.AddWithValue("@CotisationCNSS", salaire.CotisationCNSS);
            cmd.Parameters.AddWithValue("@CotisationAMO", salaire.CotisationAMO);
            cmd.Parameters.AddWithValue("@CotisationCIMR", salaire.CotisationCIMR);
            cmd.Parameters.AddWithValue("@MontantIR", salaire.MontantIR);
            cmd.Parameters.AddWithValue("@CotisationPatronaleCNSS", salaire.CotisationPatronaleCNSS);
            cmd.Parameters.AddWithValue("@CotisationPatronaleAMO", salaire.CotisationPatronaleAMO);
            cmd.Parameters.AddWithValue("@AvanceSurSalaire", salaire.AvanceSurSalaire);
            cmd.Parameters.AddWithValue("@PretPersonnel", salaire.PretPersonnel);
            cmd.Parameters.AddWithValue("@Penalites", salaire.Penalites);
            cmd.Parameters.AddWithValue("@AutresRetenues", salaire.AutresRetenues);
            cmd.Parameters.AddWithValue("@Statut", salaire.Statut ?? "En Attente");
            cmd.Parameters.AddWithValue("@Remarques", salaire.Remarques ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CreePar", salaire.CreePar ?? Environment.UserName);
        }

        // Helper method to map reader to model
        private SalaireModel MapFromReader(SqlDataReader reader)
        {
            return new SalaireModel
            {
                SalaireID = reader.GetInt32(reader.GetOrdinal("SalaireID")),
                EmployeID = reader.GetInt32(reader.GetOrdinal("EmployeID")),
                NomComplet = reader.GetString(reader.GetOrdinal("NomComplet")),
                CIN = reader.IsDBNull(reader.GetOrdinal("CIN")) ? null : reader.GetString(reader.GetOrdinal("CIN")),
                CNSS = reader.IsDBNull(reader.GetOrdinal("CNSS")) ? null : reader.GetString(reader.GetOrdinal("CNSS")),
                Mois = reader.GetInt32(reader.GetOrdinal("Mois")),
                Annee = reader.GetInt32(reader.GetOrdinal("Annee")),
                DatePaiement = reader.IsDBNull(reader.GetOrdinal("DatePaiement")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DatePaiement")),
                SalaireBase = reader.GetDecimal(reader.GetOrdinal("SalaireBase")),
                HeuresNormales = reader.GetDecimal(reader.GetOrdinal("HeuresNormales")),
                TauxHoraire = reader.GetDecimal(reader.GetOrdinal("TauxHoraire")),
                PrimeAnciennete = reader.GetDecimal(reader.GetOrdinal("PrimeAnciennete")),
                PrimeRendement = reader.GetDecimal(reader.GetOrdinal("PrimeRendement")),
                PrimeResponsabilite = reader.GetDecimal(reader.GetOrdinal("PrimeResponsabilite")),
                IndemniteTransport = reader.GetDecimal(reader.GetOrdinal("IndemniteTransport")),
                IndemniteLogement = reader.GetDecimal(reader.GetOrdinal("IndemniteLogement")),
                AutresPrimes = reader.GetDecimal(reader.GetOrdinal("AutresPrimes")),
                HeuresSupp25 = reader.GetDecimal(reader.GetOrdinal("HeuresSupp25")),
                HeuresSupp50 = reader.GetDecimal(reader.GetOrdinal("HeuresSupp50")),
                HeuresSupp100 = reader.GetDecimal(reader.GetOrdinal("HeuresSupp100")),
                MontantHeuresSupp = reader.GetDecimal(reader.GetOrdinal("MontantHeuresSupp")),
                SalaireBrut = reader.GetDecimal(reader.GetOrdinal("SalaireBrut")),
                CotisationCNSS = reader.GetDecimal(reader.GetOrdinal("CotisationCNSS")),
                CotisationAMO = reader.GetDecimal(reader.GetOrdinal("CotisationAMO")),
                CotisationCIMR = reader.GetDecimal(reader.GetOrdinal("CotisationCIMR")),
                MontantIR = reader.GetDecimal(reader.GetOrdinal("MontantIR")),
                AvanceSurSalaire = reader.GetDecimal(reader.GetOrdinal("AvanceSurSalaire")),
                PretPersonnel = reader.GetDecimal(reader.GetOrdinal("PretPersonnel")),
                Penalites = reader.GetDecimal(reader.GetOrdinal("Penalites")),
                AutresRetenues = reader.GetDecimal(reader.GetOrdinal("AutresRetenues")),
                TotalRetenues = reader.GetDecimal(reader.GetOrdinal("TotalRetenues")),
                SalaireNet = reader.GetDecimal(reader.GetOrdinal("SalaireNet")),
                CotisationPatronaleCNSS = reader.GetDecimal(reader.GetOrdinal("CotisationPatronaleCNSS")),
                CotisationPatronaleAMO = reader.GetDecimal(reader.GetOrdinal("CotisationPatronaleAMO")),
                Statut = reader.GetString(reader.GetOrdinal("Statut")),
                Remarques = reader.IsDBNull(reader.GetOrdinal("Remarques")) ? null : reader.GetString(reader.GetOrdinal("Remarques")),
                CreePar = reader.IsDBNull(reader.GetOrdinal("CreePar")) ? null : reader.GetString(reader.GetOrdinal("CreePar")),
                DateCreation = reader.IsDBNull(reader.GetOrdinal("DateCreation")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateCreation")),
                ModifiePar = reader.IsDBNull(reader.GetOrdinal("ModifiePar")) ? null : reader.GetString(reader.GetOrdinal("ModifiePar")),
                DateModification = reader.IsDBNull(reader.GetOrdinal("DateModification")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DateModification"))
            };
        }
    }
}