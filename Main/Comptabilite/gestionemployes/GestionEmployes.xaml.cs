using Superete.Main.Comptabilite.gestionemployes;
using Superete.Main.Comptabilite.Models;
using Superete.Main.Comptabilite.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Superete.Main.Comptabilite
{
    public partial class GestionEmployes : UserControl
    {
        private const string CONNECTION_STRING = "Server=localhost;Database=SupereteDB;Integrated Security=true;";

        private readonly EmployeService employeService;
        private List<EmployeModel> employes;
        private int? currentEmployeId = null;
        private bool isEditMode = false;

        public GestionEmployes()
        {
            try
            {
                InitializeComponent();
                employeService = new EmployeService(CONNECTION_STRING);
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'initialisation: {ex.Message}", "Erreur",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            try
            {
                employes = employeService.GetAll();
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
            var filtered = employes.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(txtRecherche?.Text))
            {
                string search = txtRecherche.Text.ToLower();
                filtered = filtered.Where(e =>
                    e.NomComplet.ToLower().Contains(search) ||
                    (e.CIN != null && e.CIN.ToLower().Contains(search)) ||
                    (e.CNSS != null && e.CNSS.ToLower().Contains(search))
                );
            }

            if (dgEmployes != null)
                dgEmployes.ItemsSource = filtered.ToList();
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnNouvelEmploye_Click(object sender, RoutedEventArgs e)
        {
            ShowForm(false);
        }

        // ── "Payer le Salaire" button in each DataGrid row ──────────────────────
        private void BtnPayerSalaire_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int employeId = Convert.ToInt32(btn.Tag);
                try
                {
                    var employe = employeService.GetById(employeId);
                    if (employe == null)
                    {
                        MessageBox.Show("Employé introuvable.", "Erreur",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var window = new PayerSalaireWindow(employe)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    window.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur: {ex.Message}", "Erreur",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int employeId = Convert.ToInt32(btn.Tag);
                LoadEmployeForEdit(employeId);
            }
        }

        private void BtnDesactiver_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                var result = MessageBox.Show("Êtes-vous sûr de vouloir désactiver cet employé?",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    int employeId = Convert.ToInt32(btn.Tag);
                    try
                    {
                        employeService.Delete(employeId);
                        MessageBox.Show("Employé désactivé avec succès!", "Succès",
                                      MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur: {ex.Message}", "Erreur",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnRetour_Click(object sender, RoutedEventArgs e)
        {
            if (ListViewPanel != null) ListViewPanel.Visibility = Visibility.Visible;
            if (FormPanel != null) FormPanel.Visibility = Visibility.Collapsed;
            ClearForm();
        }

        private void BtnSauvegarder_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateForm())
            {
                if (isEditMode)
                    UpdateEmploye();
                else
                    InsertEmploye();
            }
        }

        private void ShowForm(bool editMode)
        {
            isEditMode = editMode;

            if (txtFormTitle != null)
                txtFormTitle.Text = editMode ? "Modifier l'Employé" : "Nouvel Employé";

            if (btnSauvegarder != null)
                btnSauvegarder.Content = editMode ? "Mettre à Jour" : "Enregistrer";

            if (ListViewPanel != null) ListViewPanel.Visibility = Visibility.Collapsed;
            if (FormPanel != null) FormPanel.Visibility = Visibility.Visible;

            if (!editMode)
                ClearForm();
        }

        private void ClearForm()
        {
            currentEmployeId = null;
            if (txtNomComplet != null) txtNomComplet.Clear();
            if (txtCIN != null) txtCIN.Clear();
            if (txtCNSS != null) txtCNSS.Clear();
            if (txtTelephone != null) txtTelephone.Clear();
            if (txtEmail != null) txtEmail.Clear();
            if (txtPoste != null) txtPoste.Clear();
            if (txtAdresse != null) txtAdresse.Clear();
            if (txtSalaireBase != null) txtSalaireBase.Text = "0";
            if (dpDateNaissance != null) dpDateNaissance.SelectedDate = null;
            if (dpDateEmbauche != null) dpDateEmbauche.SelectedDate = null;
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtNomComplet?.Text))
            {
                MessageBox.Show("Le nom complet est obligatoire.", "Validation",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private decimal ParseSalaireBase()
        {
            if (decimal.TryParse(txtSalaireBase?.Text?.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal v))
                return v;
            return 0m;
        }

        private void InsertEmploye()
        {
            try
            {
                var employe = new EmployeModel
                {
                    NomComplet = txtNomComplet.Text,
                    CIN = txtCIN.Text,
                    CNSS = txtCNSS.Text,
                    Telephone = txtTelephone.Text,
                    Email = txtEmail.Text,
                    Poste = txtPoste.Text,
                    Adresse = txtAdresse.Text,
                    SalaireBase = ParseSalaireBase(),
                    DateNaissance = dpDateNaissance.SelectedDate,
                    DateEmbauche = dpDateEmbauche.SelectedDate,
                    Actif = true,
                    CreePar = Environment.UserName
                };

                employeService.Create(employe);
                MessageBox.Show("Employé créé avec succès!", "Succès",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                LoadData();
                BtnRetour_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateEmploye()
        {
            if (!currentEmployeId.HasValue) return;

            try
            {
                var employe = new EmployeModel
                {
                    EmployeID = currentEmployeId.Value,
                    NomComplet = txtNomComplet.Text,
                    CIN = txtCIN.Text,
                    CNSS = txtCNSS.Text,
                    Telephone = txtTelephone.Text,
                    Email = txtEmail.Text,
                    Poste = txtPoste.Text,
                    Adresse = txtAdresse.Text,
                    SalaireBase = ParseSalaireBase(),
                    DateNaissance = dpDateNaissance.SelectedDate,
                    DateEmbauche = dpDateEmbauche.SelectedDate,
                    Actif = true,
                    ModifiePar = Environment.UserName
                };

                employeService.Update(employe);
                MessageBox.Show("Employé mis à jour avec succès!", "Succès",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                LoadData();
                BtnRetour_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEmployeForEdit(int employeId)
        {
            try
            {
                var employe = employeService.GetById(employeId);
                if (employe != null)
                {
                    currentEmployeId = employeId;
                    ShowForm(true);

                    txtNomComplet.Text = employe.NomComplet;
                    txtCIN.Text = employe.CIN;
                    txtCNSS.Text = employe.CNSS;
                    txtTelephone.Text = employe.Telephone;
                    txtEmail.Text = employe.Email;
                    txtPoste.Text = employe.Poste;
                    txtAdresse.Text = employe.Adresse;
                    txtSalaireBase.Text = employe.SalaireBase.ToString("N2");
                    dpDateNaissance.SelectedDate = employe.DateNaissance;
                    dpDateEmbauche.SelectedDate = employe.DateEmbauche;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnHistorique_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int employeId = Convert.ToInt32(btn.Tag);
                try
                {
                    var employe = employeService.GetById(employeId);
                    if (employe == null)
                    {
                        MessageBox.Show("Employé introuvable.", "Erreur",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var window = new HistoriquePaiementWindow(employe)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    window.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur: {ex.Message}", "Erreur",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}