using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestionComerce;
using GestionComerce.Models;

namespace Superete.Main.Comptabilite.Views
{
    public partial class PlanComptableView : UserControl
    {
        private User currentUser;
        private List<PlanComptable> allComptes;

        public PlanComptableView(User u)
        {
            InitializeComponent();
            currentUser = u;
            LoadComptes();
        }

        private void LoadComptes()
        {
            try
            {
                allComptes = new List<PlanComptable>();

                using (SqlConnection conn = DBHelper.GetConnection())
                {
                    conn.Open();
                    string query = @"
                        SELECT CodeCompte, Libelle, Classe, TypeCompte, SensNormal, EstActif, DateCreation
                        FROM PlanComptable
                        ORDER BY CodeCompte";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                allComptes.Add(new PlanComptable
                                {
                                    CodeCompte = reader.GetString(0),
                                    Libelle = reader.GetString(1),
                                    Classe = reader.GetInt32(2),
                                    TypeCompte = reader.GetString(3),
                                    SensNormal = reader.GetString(4),
                                    EstActif = reader.GetBoolean(5),
                                    DateCreation = reader.GetDateTime(6)
                                });
                            }
                        }
                    }
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement: {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (allComptes == null) return;

            var filtered = allComptes.AsEnumerable();

            // Classe filter
            if (ClasseFilter.SelectedItem is ComboBoxItem selectedClasse)
            {
                int classe = Convert.ToInt32(selectedClasse.Tag);
                if (classe > 0)
                    filtered = filtered.Where(c => c.Classe == classe);
            }

            // Type filter
            if (TypeFilter.SelectedItem is ComboBoxItem selectedType && selectedType.Content.ToString() != "Tous")
            {
                string type = selectedType.Content.ToString();
                filtered = filtered.Where(c => c.TypeCompte == type);
            }

            // Statut filter
            if (StatutFilter.SelectedItem is ComboBoxItem selectedStatut && selectedStatut.Content.ToString() != "Tous")
            {
                bool estActif = Convert.ToBoolean(selectedStatut.Tag);
                filtered = filtered.Where(c => c.EstActif == estActif);
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                string search = SearchBox.Text.ToLower();
                filtered = filtered.Where(c =>
                    c.CodeCompte.ToLower().Contains(search) ||
                    c.Libelle.ToLower().Contains(search));
            }

            var filteredList = filtered.ToList();
            PlanComptableGrid.ItemsSource = filteredList;
            TxtTotalComptes.Text = filteredList.Count.ToString();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnNouveau_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new CompteDialog(null);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        // Check if account already exists
                        using (SqlConnection conn = DBHelper.GetConnection())
                        {
                            conn.Open();

                            string checkQuery = "SELECT COUNT(*) FROM PlanComptable WHERE CodeCompte = @CodeCompte";
                            using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                            {
                                checkCmd.Parameters.AddWithValue("@CodeCompte", dialog.CompteData.CodeCompte);
                                int count = (int)checkCmd.ExecuteScalar();

                                if (count > 0)
                                {
                                    MessageBox.Show($"⚠️ Le compte {dialog.CompteData.CodeCompte} existe déjà!",
                                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }

                            string query = @"
                        INSERT INTO PlanComptable (CodeCompte, Libelle, Classe, TypeCompte, SensNormal, EstActif, DateCreation)
                        VALUES (@CodeCompte, @Libelle, @Classe, @TypeCompte, @SensNormal, @EstActif, GETDATE())";

                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@CodeCompte", dialog.CompteData.CodeCompte);
                                cmd.Parameters.AddWithValue("@Libelle", dialog.CompteData.Libelle);
                                cmd.Parameters.AddWithValue("@Classe", dialog.CompteData.Classe);
                                cmd.Parameters.AddWithValue("@TypeCompte", dialog.CompteData.TypeCompte);
                                cmd.Parameters.AddWithValue("@SensNormal", dialog.CompteData.SensNormal);
                                cmd.Parameters.AddWithValue("@EstActif", dialog.CompteData.EstActif);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        MessageBox.Show("✅ Compte ajouté avec succès!", "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadComptes();
                    }
                    catch (SqlException sqlEx)
                    {
                        MessageBox.Show($"❌ Erreur SQL: {sqlEx.Message}", "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"❌ Erreur: {ex.Message}", "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erreur lors de l'ouverture du dialogue: {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                try
                {
                    string codeCompte = btn.Tag.ToString();
                    var compte = allComptes.FirstOrDefault(c => c.CodeCompte == codeCompte);

                    if (compte != null)
                    {
                        var dialog = new CompteDialog(compte);
                        if (dialog.ShowDialog() == true)
                        {
                            try
                            {
                                using (SqlConnection conn = DBHelper.GetConnection())
                                {
                                    conn.Open();
                                    string query = @"
                                UPDATE PlanComptable 
                                SET Libelle = @Libelle,
                                    Classe = @Classe,
                                    TypeCompte = @TypeCompte,
                                    SensNormal = @SensNormal,
                                    EstActif = @EstActif
                                WHERE CodeCompte = @CodeCompte";

                                    using (SqlCommand cmd = new SqlCommand(query, conn))
                                    {
                                        cmd.Parameters.AddWithValue("@CodeCompte", dialog.CompteData.CodeCompte);
                                        cmd.Parameters.AddWithValue("@Libelle", dialog.CompteData.Libelle);
                                        cmd.Parameters.AddWithValue("@Classe", dialog.CompteData.Classe);
                                        cmd.Parameters.AddWithValue("@TypeCompte", dialog.CompteData.TypeCompte);
                                        cmd.Parameters.AddWithValue("@SensNormal", dialog.CompteData.SensNormal);
                                        cmd.Parameters.AddWithValue("@EstActif", dialog.CompteData.EstActif);

                                        int rowsAffected = cmd.ExecuteNonQuery();

                                        if (rowsAffected == 0)
                                        {
                                            MessageBox.Show("⚠️ Aucune modification effectuée", "Information",
                                                MessageBoxButton.OK, MessageBoxImage.Information);
                                            return;
                                        }
                                    }
                                }

                                MessageBox.Show("✅ Compte modifié avec succès!", "Succès",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadComptes();
                            }
                            catch (SqlException sqlEx)
                            {
                                MessageBox.Show($"❌ Erreur SQL: {sqlEx.Message}", "Erreur",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"❌ Erreur: {ex.Message}", "Erreur",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("⚠️ Compte introuvable", "Erreur",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Erreur: {ex.Message}", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string codeCompte = btn.Tag.ToString();
                var result = MessageBox.Show(
                    $"Êtes-vous sûr de vouloir supprimer le compte {codeCompte}?\n\nCette action est irréversible!",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = DBHelper.GetConnection())
                        {
                            conn.Open();

                            // Check if account is used
                            string checkQuery = "SELECT COUNT(*) FROM EcrituresComptables WHERE CodeCompte = @CodeCompte";
                            using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                            {
                                checkCmd.Parameters.AddWithValue("@CodeCompte", codeCompte);
                                int count = (int)checkCmd.ExecuteScalar();

                                if (count > 0)
                                {
                                    MessageBox.Show("Ce compte ne peut pas être supprimé car il est utilisé dans des écritures comptables.",
                                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    return;
                                }
                            }

                            string query = "DELETE FROM PlanComptable WHERE CodeCompte = @CodeCompte";
                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@CodeCompte", codeCompte);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        MessageBox.Show("Compte supprimé avec succès!", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadComptes();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Fichier Excel|*.xlsx",
                    FileName = $"PlanComptable_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var dataToExport = PlanComptableGrid.ItemsSource as List<PlanComptable>;
                    if (dataToExport == null || !dataToExport.Any())
                    {
                        MessageBox.Show("Aucune donnée à exporter.", "Avertissement",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    using (var workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add("Plan Comptable");

                        // Headers
                        ws.Cell(1, 1).Value = "Code Compte";
                        ws.Cell(1, 2).Value = "Libellé";
                        ws.Cell(1, 3).Value = "Classe";
                        ws.Cell(1, 4).Value = "Type Compte";
                        ws.Cell(1, 5).Value = "Sens Normal";
                        ws.Cell(1, 6).Value = "Statut";
                        ws.Cell(1, 7).Value = "Date Création";

                        // Data
                        for (int i = 0; i < dataToExport.Count; i++)
                        {
                            var c = dataToExport[i];
                            int row = i + 2;
                            ws.Cell(row, 1).Value = c.CodeCompte;
                            ws.Cell(row, 2).Value = c.Libelle;
                            ws.Cell(row, 3).Value = c.Classe;
                            ws.Cell(row, 4).Value = c.TypeCompte;
                            ws.Cell(row, 5).Value = c.SensNormal;
                            ws.Cell(row, 6).Value = c.EstActif ? "Actif" : "Inactif";
                            ws.Cell(row, 7).Value = c.DateCreation.ToString("dd/MM/yyyy");
                        }

                        ws.Columns().AdjustToContents();
                        workbook.SaveAs(saveDialog.FileName);
                    }

                    MessageBox.Show("Export réussi!", "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'export: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

    // Dialog for Add/Edit Account
   