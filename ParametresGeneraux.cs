using System;
using System.ComponentModel;
using System.Data.SqlClient;

namespace Superete
{
    // NO API ENDPOINT EXISTS for ParametresGeneraux in ZenixApi.
    // This class retains its original direct-SQL implementation until a
    // /api/parametres endpoint is added to the API.

    public class ParametresGeneraux : INotifyPropertyChanged
    {
        private int _id;
        private int _userId;
        private string _afficherClavier;
        private bool _masquerEtiquettesVides;
        private bool _supprimerArticlesQuantiteZero;
        private string _langue;
        private bool _imprimerFactureParDefaut;
        private bool _imprimerTicketParDefaut;
        private string _methodePaiementParDefaut;
        private string _vueParDefaut;
        private string _trierParDefaut;
        private string _tailleIcones;
        private DateTime _dateCreation;
        private DateTime _dateModification;

        public int Id
        {
            get { return _id; }
            set { _id = value; OnPropertyChanged("Id"); }
        }

        public int UserId
        {
            get { return _userId; }
            set { _userId = value; OnPropertyChanged("UserId"); }
        }

        public string AfficherClavier
        {
            get { return _afficherClavier; }
            set { _afficherClavier = value; OnPropertyChanged("AfficherClavier"); }
        }

        public bool MasquerEtiquettesVides
        {
            get { return _masquerEtiquettesVides; }
            set { _masquerEtiquettesVides = value; OnPropertyChanged("MasquerEtiquettesVides"); }
        }

        public bool SupprimerArticlesQuantiteZero
        {
            get { return _supprimerArticlesQuantiteZero; }
            set { _supprimerArticlesQuantiteZero = value; OnPropertyChanged("SupprimerArticlesQuantiteZero"); }
        }

        public string Langue
        {
            get { return _langue; }
            set { _langue = value; OnPropertyChanged("Langue"); }
        }

        public bool ImprimerFactureParDefaut
        {
            get { return _imprimerFactureParDefaut; }
            set { _imprimerFactureParDefaut = value; OnPropertyChanged("ImprimerFactureParDefaut"); }
        }

        public bool ImprimerTicketParDefaut
        {
            get { return _imprimerTicketParDefaut; }
            set { _imprimerTicketParDefaut = value; OnPropertyChanged("ImprimerTicketParDefaut"); }
        }

        public string MethodePaiementParDefaut
        {
            get { return _methodePaiementParDefaut; }
            set { _methodePaiementParDefaut = value; OnPropertyChanged("MethodePaiementParDefaut"); }
        }

        public string VueParDefaut
        {
            get { return _vueParDefaut; }
            set { _vueParDefaut = value; OnPropertyChanged("VueParDefaut"); }
        }

        public string TrierParDefaut
        {
            get { return _trierParDefaut; }
            set { _trierParDefaut = value; OnPropertyChanged("TrierParDefaut"); }
        }

        public string TailleIcones
        {
            get { return _tailleIcones; }
            set { _tailleIcones = value; OnPropertyChanged("TailleIcones"); }
        }

        public DateTime DateCreation
        {
            get { return _dateCreation; }
            set { _dateCreation = value; OnPropertyChanged("DateCreation"); }
        }

        public DateTime DateModification
        {
            get { return _dateModification; }
            set { _dateModification = value; OnPropertyChanged("DateModification"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private static string _connectionString;

        public static void SetConnectionString(string connectionString)
        {
            _connectionString = "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;";
        }

        public static ParametresGeneraux ObtenirParametresParUserId(int userId, string connectionString)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM ParametresGeneraux WHERE UserId = @UserId";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new ParametresGeneraux
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                                    AfficherClavier = reader.GetString(reader.GetOrdinal("AfficherClavier")),
                                    MasquerEtiquettesVides = reader.GetBoolean(reader.GetOrdinal("MasquerEtiquettesVides")),
                                    SupprimerArticlesQuantiteZero = reader.GetBoolean(reader.GetOrdinal("SupprimerArticlesQuantiteZero")),
                                    Langue = reader.GetString(reader.GetOrdinal("Langue")),
                                    ImprimerFactureParDefaut = reader.GetBoolean(reader.GetOrdinal("ImprimerFactureParDefaut")),
                                    ImprimerTicketParDefaut = reader.GetBoolean(reader.GetOrdinal("ImprimerTicketParDefaut")),
                                    MethodePaiementParDefaut = reader.GetString(reader.GetOrdinal("MethodePaiementParDefaut")),
                                    VueParDefaut = reader.GetString(reader.GetOrdinal("VueParDefaut")),
                                    TrierParDefaut = reader.GetString(reader.GetOrdinal("TrierParDefaut")),
                                    TailleIcones = reader.GetString(reader.GetOrdinal("TailleIcones")),
                                    DateCreation = reader.GetDateTime(reader.GetOrdinal("DateCreation")),
                                    DateModification = reader.GetDateTime(reader.GetOrdinal("DateModification"))
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur lors de la récupération des paramètres : " + ex.Message, ex);
            }

            return null;
        }

        public static int CreerParametresParDefaut(int userId, string connectionString)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO ParametresGeneraux 
                        (UserId, AfficherClavier, MasquerEtiquettesVides, SupprimerArticlesQuantiteZero, 
                         Langue, ImprimerFactureParDefaut, ImprimerTicketParDefaut, MethodePaiementParDefaut,
                         VueParDefaut, TrierParDefaut, TailleIcones, DateCreation, DateModification)
                        VALUES (@UserId, @AfficherClavier, @MasquerEtiquettesVides, @SupprimerArticlesQuantiteZero,
                         @Langue, @ImprimerFactureParDefaut, @ImprimerTicketParDefaut, @MethodePaiementParDefaut,
                         @VueParDefaut, @TrierParDefaut, @TailleIcones, @DateCreation, @DateModification);
                        SELECT CAST(SCOPE_IDENTITY() as int)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@AfficherClavier", "Manuel");
                        cmd.Parameters.AddWithValue("@MasquerEtiquettesVides", false);
                        cmd.Parameters.AddWithValue("@SupprimerArticlesQuantiteZero", false);
                        cmd.Parameters.AddWithValue("@Langue", "Français");
                        cmd.Parameters.AddWithValue("@ImprimerFactureParDefaut", false);
                        cmd.Parameters.AddWithValue("@ImprimerTicketParDefaut", false);
                        cmd.Parameters.AddWithValue("@MethodePaiementParDefaut", "Espèces");
                        cmd.Parameters.AddWithValue("@VueParDefaut", "Cartes");
                        cmd.Parameters.AddWithValue("@TrierParDefaut", "Nom (A-Z)");
                        cmd.Parameters.AddWithValue("@TailleIcones", "Moyennes");
                        cmd.Parameters.AddWithValue("@DateCreation", DateTime.Now);
                        cmd.Parameters.AddWithValue("@DateModification", DateTime.Now);

                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur lors de la création des paramètres : " + ex.Message, ex);
            }
        }

        public bool MettreAJourParametres(string connectionString)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"UPDATE ParametresGeneraux 
                        SET AfficherClavier = @AfficherClavier,
                            MasquerEtiquettesVides = @MasquerEtiquettesVides,
                            SupprimerArticlesQuantiteZero = @SupprimerArticlesQuantiteZero,
                            Langue = @Langue,
                            ImprimerFactureParDefaut = @ImprimerFactureParDefaut,
                            ImprimerTicketParDefaut = @ImprimerTicketParDefaut,
                            MethodePaiementParDefaut = @MethodePaiementParDefaut,
                            VueParDefaut = @VueParDefaut,
                            TrierParDefaut = @TrierParDefaut,
                            TailleIcones = @TailleIcones,
                            DateModification = @DateModification
                        WHERE UserId = @UserId";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", this.UserId);
                        cmd.Parameters.AddWithValue("@AfficherClavier", this.AfficherClavier);
                        cmd.Parameters.AddWithValue("@MasquerEtiquettesVides", this.MasquerEtiquettesVides);
                        cmd.Parameters.AddWithValue("@SupprimerArticlesQuantiteZero", this.SupprimerArticlesQuantiteZero);
                        cmd.Parameters.AddWithValue("@Langue", this.Langue);
                        cmd.Parameters.AddWithValue("@ImprimerFactureParDefaut", this.ImprimerFactureParDefaut);
                        cmd.Parameters.AddWithValue("@ImprimerTicketParDefaut", this.ImprimerTicketParDefaut);
                        cmd.Parameters.AddWithValue("@MethodePaiementParDefaut", this.MethodePaiementParDefaut);
                        cmd.Parameters.AddWithValue("@VueParDefaut", this.VueParDefaut);
                        cmd.Parameters.AddWithValue("@TrierParDefaut", this.TrierParDefaut);
                        cmd.Parameters.AddWithValue("@TailleIcones", this.TailleIcones);
                        cmd.Parameters.AddWithValue("@DateModification", DateTime.Now);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur lors de la mise à jour des paramètres : " + ex.Message, ex);
            }
        }

        public static ParametresGeneraux ObtenirOuCreerParametres(int userId, string connectionString)
        {
            var parametres = ObtenirParametresParUserId(userId, connectionString);

            if (parametres == null)
            {
                CreerParametresParDefaut(userId, connectionString);
                parametres = ObtenirParametresParUserId(userId, connectionString);
            }

            return parametres;
        }
    }
}