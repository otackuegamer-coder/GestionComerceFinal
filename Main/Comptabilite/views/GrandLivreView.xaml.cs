using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestionComerce;
using GestionComerce.Models;
using GestionComerce;

namespace Superete.Main.Comptabilite.Views
{
    public partial class GrandLivreView : UserControl
    {
        private User currentUser;
        private ComptabiliteService comptabiliteService;

        public GrandLivreView(User u)
        {
            InitializeComponent();
            currentUser = u;
            comptabiliteService = new ComptabiliteService();
            LoadComptes();
        }

        private void LoadComptes()
        {
            try
            {
                List<CompteItem> comptes = new List<CompteItem>();
                
                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                        SELECT CodeCompte, Libelle 
                        FROM PlanComptable 
                        WHERE EstActif = 1 
                        ORDER BY CodeCompte";
                    
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                comptes.Add(new CompteItem
                                {
                                    CodeCompte = reader.GetString(0),
                                    Libelle = reader.GetString(1),
                                    Display = string.Format("{0} - {1}", reader.GetString(0), reader.GetString(1))
                                });
                            }
                        }
                    }
                }
                
                CompteComboBox.ItemsSource = comptes;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur: {0}", ex.Message), "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAccount_Click(object sender, RoutedEventArgs args)
        {
            if (CompteComboBox.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un compte", "Attention", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                CompteItem selectedCompte = (CompteItem)CompteComboBox.SelectedItem;
                
                List<EcrituresComptables> ecritures = comptabiliteService.ObtenirGrandLivreParCompte(
                    selectedCompte.CodeCompte, null, null);
                
                if (ecritures.Count == 0)
                {
                    MessageBox.Show("Aucune écriture trouvée pour ce compte", "Information", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Show details
                TxtCompteCode.Text = selectedCompte.CodeCompte;
                TxtCompteLibelle.Text = selectedCompte.Libelle;
                
                decimal solde = ecritures.Sum(ec => ec.Debit - ec.Credit);
                TxtSolde.Text = string.Format("{0:N2} DH", Math.Abs(solde));
                
                if (solde >= 0)
                {
                    TxtSolde.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10B981"));
                }
                else
                {
                    TxtSolde.Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444"));
                }

                GrandLivreDataGrid.ItemsSource = ecritures;
                
                EmptyState.Visibility = Visibility.Collapsed;
                DetailsPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Erreur: {0}", ex.Message), "Erreur", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class CompteItem
    {
        public string CodeCompte { get; set; }
        public string Libelle { get; set; }
        public string Display { get; set; }
    }
}