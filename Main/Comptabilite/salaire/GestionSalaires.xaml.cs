using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Superete.Main.Comptabilite.Models;
using Superete.Main.Comptabilite.Services;

namespace Superete.Main.Comptabilite
{
    public partial class GestionSalaires : UserControl
    {
        // UPDATE THIS CONNECTION STRING
        private const string CONNECTION_STRING = "Server=localhost;Database=SupereteDB;Integrated Security=true;";

        private readonly EmployeService employeService;
        private readonly SalaireService salaireService;

        private ObservableCollection<SalaireModel> salaires;
        private List<EmployeModel> employes;
        private int? currentSalaireId = null;
        private bool isEditMode = false;

        public GestionSalaires()
        {
            try
            {
                InitializeComponent();

                // Initialize services
                employeService = new EmployeService(CONNECTION_STRING);
                salaireService = new SalaireService(CONNECTION_STRING);

                InitializeData();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'initialisation: {ex.Message}\n\nStack: {ex.StackTrace}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Initialization

        private void InitializeData()
        {
            try
            {
                // Initialize months
                cmbMois.Items.Add(new ComboBoxItem { Content = "Tous", Tag = 0 });

                for (int i = 1; i <= 12; i++)
                {
                    cmbMois.Items.Add(new ComboBoxItem { Content = GetMonthName(i), Tag = i });
                    cmbFormMois.Items.Add(new ComboBoxItem { Content = GetMonthName(i), Tag = i });
                }

                // Initialize years
                cmbAnnee.Items.Add(new ComboBoxItem { Content = "Tous", Tag = 0 });

                int currentYear = DateTime.Now.Year;
                for (int i = currentYear - 2; i <= currentYear + 1; i++)
                {
                    cmbAnnee.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i });
                    cmbFormAnnee.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i });
                }

                // Initialize status
                cmbStatut.Items.Add(new ComboBoxItem { Content = "Tous", Tag = "" });
                cmbStatut.Items.Add(new ComboBoxItem { Content = "En Attente", Tag = "En Attente" });
                cmbStatut.Items.Add(new ComboBoxItem { Content = "Validé", Tag = "Validé" });
                cmbStatut.Items.Add(new ComboBoxItem { Content = "Payé", Tag = "Payé" });

                // Set defaults
                cmbMois.SelectedIndex = 0;
                cmbAnnee.SelectedIndex = 3;
                cmbStatut.SelectedIndex = 0;
                cmbFormMois.SelectedIndex = DateTime.Now.Month - 1;
                cmbFormAnnee.SelectedIndex = 2;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'initialisation des données: {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetMonthName(int month)
        {
            string[] months = { "", "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
                              "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre" };
            return month > 0 && month <= 12 ? months[month] : "";
        }

        #endregion

        #region Data Loading

        private void LoadData()
        {
            LoadEmployes();
            LoadSalaires();
        }

        private void LoadEmployes()
        {
            try
            {
                employes = employeService.GetActive();
                cmbEmploye.ItemsSource = employes;
                cmbEmploye.DisplayMemberPath = "NomComplet";
                cmbEmploye.SelectedValuePath = "EmployeID";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des employés: {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSalaires()
        {
            try
            {
                var allSalaires = salaireService.GetAll();
                salaires = new ObservableCollection<SalaireModel>(allSalaires);
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des salaires: {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            if (salaires == null) return;

            var filtered = salaires.AsEnumerable();

            // Month filter
            if (cmbMois?.SelectedItem is ComboBoxItem selectedMonth)
            {
                int month = (int)selectedMonth.Tag;
                if (month > 0)
                    filtered = filtered.Where(s => s.Mois == month);
            }

            // Year filter
            if (cmbAnnee?.SelectedItem is ComboBoxItem selectedYear)
            {
                int year = (int)selectedYear.Tag;
                if (year > 0)
                    filtered = filtered.Where(s => s.Annee == year);
            }

            // Status filter
            if (cmbStatut?.SelectedItem is ComboBoxItem selectedStatut)
            {
                string statut = selectedStatut.Tag?.ToString();
                if (!string.IsNullOrEmpty(statut))
                    filtered = filtered.Where(s => s.Statut == statut);
            }

            // Search filter
            if (txtRecherche != null && !string.IsNullOrWhiteSpace(txtRecherche.Text))
            {
                string search = txtRecherche.Text.ToLower();
                filtered = filtered.Where(s =>
                    s.NomComplet.ToLower().Contains(search) ||
                    (s.CIN != null && s.CIN.ToLower().Contains(search)) ||
                    (s.CNSS != null && s.CNSS.ToLower().Contains(search))
                );
            }

            var filteredList = filtered.ToList();
            if (dgSalaires != null)
                dgSalaires.ItemsSource = filteredList;

            // Calculate total
            decimal total = filteredList.Sum(s => s.SalaireNet);
            if (txtTotalSalaires != null)
                txtTotalSalaires.Text = $"{total:N2} DH";
        }

        #endregion

        #region Event Handlers

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (dgSalaires != null)
                ApplyFilters();
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DgSalaires_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle selection if needed
        }

        private void CmbEmploye_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbEmploye?.SelectedItem is EmployeModel employe)
            {
                if (txtCIN != null) txtCIN.Text = employe.CIN ?? "";
                if (txtCNSS != null) txtCNSS.Text = employe.CNSS ?? "";
            }
        }

        #endregion

        #region Button Clicks

        private void BtnNouveauSalaire_Click(object sender, RoutedEventArgs e)
        {
            ShowForm(false);
        }

        private void BtnExporter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files|*.csv",
                    FileName = $"Salaires_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToCSV(saveDialog.FileName);
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

        private void BtnVoir_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int salaireId = Convert.ToInt32(btn.Tag);
                ViewSalaryDetails(salaireId);
            }
        }


        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                int salaireId = Convert.ToInt32(btn.Tag);
                LoadSalaireForEdit(salaireId);
            }
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                var result = MessageBox.Show(
                    "Êtes-vous sûr de vouloir supprimer ce salaire?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    int salaireId = Convert.ToInt32(btn.Tag);
                    DeleteSalaire(salaireId);
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
                    UpdateSalaire();
                else
                    InsertSalaire();
            }
        }

        #endregion

        #region Form Operations

        private void ShowForm(bool editMode)
        {
            isEditMode = editMode;

            if (txtFormTitle != null)
                txtFormTitle.Text = editMode ? "Modifier le Bulletin de Salaire" : "Nouveau Bulletin de Salaire";

            if (btnSauvegarder != null)
                btnSauvegarder.Content = editMode ? "Mettre à Jour" : "Enregistrer le Salaire";

            if (ListViewPanel != null) ListViewPanel.Visibility = Visibility.Collapsed;
            if (FormPanel != null) FormPanel.Visibility = Visibility.Visible;

            if (!editMode)
            {
                ClearForm();
            }
        }

        private void ClearForm()
        {
            currentSalaireId = null;

            if (cmbEmploye != null) cmbEmploye.SelectedIndex = -1;
            if (txtCIN != null) txtCIN.Clear();
            if (txtCNSS != null) txtCNSS.Clear();
            if (cmbFormMois != null) cmbFormMois.SelectedIndex = DateTime.Now.Month - 1;
            if (cmbFormAnnee != null) cmbFormAnnee.SelectedIndex = 2;

            SafeSetText(txtSalaireBase, "");
            SafeSetText(txtTauxHoraire, "");
            SafeSetText(txtHeuresSupp25, "0");
            SafeSetText(txtHeuresSupp50, "0");
            SafeSetText(txtHeuresSupp100, "0");

            SafeSetText(txtPrimeAnciennete, "0");
            SafeSetText(txtPrimeRendement, "0");
            SafeSetText(txtPrimeResponsabilite, "0");
            SafeSetText(txtIndemniteTransport, "0");
            SafeSetText(txtIndemniteLogement, "0");
            SafeSetText(txtAutresPrimes, "0");

            SafeSetText(txtAvance, "0");
            SafeSetText(txtPret, "0");
            SafeSetText(txtCIMR, "0");
            SafeSetText(txtPenalites, "0");
            SafeSetText(txtAutresRetenues, "0");

            if (txtRemarques != null) txtRemarques.Clear();

            CalculateSalary(null, null);
        }

        private void SafeSetText(TextBox textBox, string value)
        {
            if (textBox != null)
                textBox.Text = value;
        }

        private string SafeGetText(TextBox textBox)
        {
            return textBox?.Text ?? "0";
        }

        private bool ValidateForm()
        {
            if (cmbEmploye?.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un employé.", "Validation",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            string salaireBaseText = SafeGetText(txtSalaireBase);
            if (string.IsNullOrWhiteSpace(salaireBaseText) ||
                !decimal.TryParse(salaireBaseText, out decimal salaireBase) ||
                salaireBase <= 0)
            {
                MessageBox.Show("Veuillez entrer un salaire de base valide.", "Validation",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        #endregion

        #region Salary Calculations

        private void CalculateSalary(object sender, TextChangedEventArgs e)
        {
            try
            {
                decimal salaireBase = ParseDecimal(SafeGetText(txtSalaireBase));
                decimal heuresSupp25 = ParseDecimal(SafeGetText(txtHeuresSupp25));
                decimal heuresSupp50 = ParseDecimal(SafeGetText(txtHeuresSupp50));
                decimal heuresSupp100 = ParseDecimal(SafeGetText(txtHeuresSupp100));

                decimal primeAnciennete = ParseDecimal(SafeGetText(txtPrimeAnciennete));
                decimal primeRendement = ParseDecimal(SafeGetText(txtPrimeRendement));
                decimal primeResponsabilite = ParseDecimal(SafeGetText(txtPrimeResponsabilite));
                decimal indemniteTransport = ParseDecimal(SafeGetText(txtIndemniteTransport));
                decimal indemniteLogement = ParseDecimal(SafeGetText(txtIndemniteLogement));
                decimal autresPrimes = ParseDecimal(SafeGetText(txtAutresPrimes));

                decimal avance = ParseDecimal(SafeGetText(txtAvance));
                decimal pret = ParseDecimal(SafeGetText(txtPret));
                decimal cimr = ParseDecimal(SafeGetText(txtCIMR));
                decimal penalites = ParseDecimal(SafeGetText(txtPenalites));
                decimal autresRetenues = ParseDecimal(SafeGetText(txtAutresRetenues));

                decimal tauxHoraire = salaireBase / 191;
                SafeSetText(txtTauxHoraire, $"{tauxHoraire:F2}");

                decimal montantHeuresSupp =
                    (heuresSupp25 * tauxHoraire * 1.25m) +
                    (heuresSupp50 * tauxHoraire * 1.50m) +
                    (heuresSupp100 * tauxHoraire * 2.00m);

                decimal salaireBrut = salaireBase + primeAnciennete + primeRendement +
                                    primeResponsabilite + indemniteTransport +
                                    indemniteLogement + autresPrimes + montantHeuresSupp;

                decimal plafondCNSS = 6000m;
                decimal salaireImposableCNSS = Math.Min(salaireBrut, plafondCNSS);

                decimal cotisationCNSS = salaireImposableCNSS * 0.0448m;
                decimal cotisationAMO = salaireBrut * 0.0226m;

                decimal deductionFraisPro = Math.Min(salaireBrut * 0.20m, 2500m);
                decimal salaireImposable = salaireBrut - cotisationCNSS - cotisationAMO - deductionFraisPro;
                decimal salaireAnnuel = salaireImposable * 12;

                decimal irAnnuel = CalculateIR(salaireAnnuel);
                decimal montantIR = irAnnuel / 12;

                decimal totalRetenues = cotisationCNSS + cotisationAMO + montantIR +
                                      cimr + avance + pret + penalites + autresRetenues;

                decimal salaireNet = salaireBrut - totalRetenues;

                // Update display with null checks
                if (txtCalculSalaireBrut != null)
                    txtCalculSalaireBrut.Text = $"{salaireBrut:N2} DH";
                if (txtCalculTotalRetenues != null)
                    txtCalculTotalRetenues.Text = $"{totalRetenues:N2} DH";
                if (txtCalculSalaireNet != null)
                    txtCalculSalaireNet.Text = $"{salaireNet:N2} DH";

                if (txtCalculCNSS != null)
                    txtCalculCNSS.Text = $"CNSS: {cotisationCNSS:N2} DH";
                if (txtCalculAMO != null)
                    txtCalculAMO.Text = $"AMO: {cotisationAMO:N2} DH";
                if (txtCalculIR != null)
                    txtCalculIR.Text = $"IR: {montantIR:N2} DH";
            }
            catch
            {
                // Ignore calculation errors during input
            }
        }

        private decimal CalculateIR(decimal salaireAnnuel)
        {
            decimal ir = 0;

            if (salaireAnnuel <= 30000)
                ir = 0;
            else if (salaireAnnuel <= 50000)
                ir = (salaireAnnuel - 30000) * 0.10m;
            else if (salaireAnnuel <= 60000)
                ir = 2000 + (salaireAnnuel - 50000) * 0.20m;
            else if (salaireAnnuel <= 80000)
                ir = 4000 + (salaireAnnuel - 60000) * 0.30m;
            else if (salaireAnnuel <= 180000)
                ir = 10000 + (salaireAnnuel - 80000) * 0.34m;
            else if (salaireAnnuel <= 250000)
                ir = 44000 + (salaireAnnuel - 180000) * 0.37m;
            else
                ir = 69900 + (salaireAnnuel - 250000) * 0.38m;

            return Math.Max(0, ir);
        }

        private decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            if (decimal.TryParse(value, out decimal result))
                return result;

            return 0;
        }

        #endregion

        #region Database Operations

        private void InsertSalaire()
        {
            try
            {
                var employe = (EmployeModel)cmbEmploye.SelectedItem;
                var mois = (ComboBoxItem)cmbFormMois.SelectedItem;
                var annee = (ComboBoxItem)cmbFormAnnee.SelectedItem;

                SalaireModel salaire = BuildSalaireModel();
                salaire.EmployeID = employe.EmployeID;
                salaire.NomComplet = employe.NomComplet;
                salaire.CIN = employe.CIN;
                salaire.CNSS = employe.CNSS;
                salaire.Mois = (int)mois.Tag;
                salaire.Annee = (int)annee.Tag;
                salaire.CreePar = Environment.UserName;

                int newSalaireId = salaireService.Create(salaire);

                // ⬇️⬇️⬇️ ADD ACCOUNTING HOOK HERE ⬇️⬇️⬇️
                try
                {
                    var comptaService = new GestionComerce.ComptabiliteService();
                    comptaService.EnregistrerSalaire(
                        newSalaireId,
                        salaire.EmployeID,
                        salaire.SalaireBrut,
                        salaire.CotisationCNSS,
                        salaire.CotisationPatronaleCNSS,
                        salaire.CotisationAMO,
                        salaire.MontantIR,
                        salaire.SalaireNet,
                        DateTime.Now,
                        new DateTime(salaire.Annee, salaire.Mois, 1)
                    );
                }
                catch (Exception exCompta)
                {
                    MessageBox.Show("Erreur comptabilité: " + exCompta.Message + "\n\n" + exCompta.StackTrace,
                        "Erreur Compta", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                // ⬆️⬆️⬆️ HOOK ENDS HERE ⬆️⬆️⬆️

                MessageBox.Show("Salaire enregistré avec succès!", "Succès",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                LoadSalaires();
                BtnRetour_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement: {ex.Message}\n\n{ex.StackTrace}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void UpdateSalaire()
        {
            if (!currentSalaireId.HasValue) return;

            try
            {
                var employe = (EmployeModel)cmbEmploye.SelectedItem;
                var mois = (ComboBoxItem)cmbFormMois.SelectedItem;
                var annee = (ComboBoxItem)cmbFormAnnee.SelectedItem;

                SalaireModel salaire = BuildSalaireModel();
                salaire.SalaireID = currentSalaireId.Value;
                salaire.EmployeID = employe.EmployeID;
                salaire.NomComplet = employe.NomComplet;
                salaire.CIN = employe.CIN;
                salaire.CNSS = employe.CNSS;
                salaire.Mois = (int)mois.Tag;
                salaire.Annee = (int)annee.Tag;
                salaire.ModifiePar = Environment.UserName;

                salaireService.Update(salaire);

                MessageBox.Show("Salaire mis à jour avec succès!", "Succès",
                              MessageBoxButton.OK, MessageBoxImage.Information);

                LoadSalaires();
                BtnRetour_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la mise à jour: {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private SalaireModel BuildSalaireModel()
        {
            decimal salaireBase = ParseDecimal(SafeGetText(txtSalaireBase));
            decimal tauxHoraire = salaireBase / 191;

            decimal heuresSupp25 = ParseDecimal(SafeGetText(txtHeuresSupp25));
            decimal heuresSupp50 = ParseDecimal(SafeGetText(txtHeuresSupp50));
            decimal heuresSupp100 = ParseDecimal(SafeGetText(txtHeuresSupp100));

            decimal montantHeuresSupp =
                (heuresSupp25 * tauxHoraire * 1.25m) +
                (heuresSupp50 * tauxHoraire * 1.50m) +
                (heuresSupp100 * tauxHoraire * 2.00m);

            decimal salaireBrut =
                salaireBase +
                ParseDecimal(SafeGetText(txtPrimeAnciennete)) +
                ParseDecimal(SafeGetText(txtPrimeRendement)) +
                ParseDecimal(SafeGetText(txtPrimeResponsabilite)) +
                ParseDecimal(SafeGetText(txtIndemniteTransport)) +
                ParseDecimal(SafeGetText(txtIndemniteLogement)) +
                ParseDecimal(SafeGetText(txtAutresPrimes)) +
                montantHeuresSupp;

            decimal plafondCNSS = 6000m;
            decimal salaireImposableCNSS = Math.Min(salaireBrut, plafondCNSS);

            decimal cotisationCNSS = salaireImposableCNSS * 0.0448m;
            decimal cotisationAMO = salaireBrut * 0.0226m;
            decimal cotisationPatronaleCNSS = salaireImposableCNSS * 0.0898m;
            decimal cotisationPatronaleAMO = salaireBrut * 0.0411m;

            decimal deductionFraisPro = Math.Min(salaireBrut * 0.20m, 2500m);
            decimal salaireImposable = salaireBrut - cotisationCNSS - cotisationAMO - deductionFraisPro;
            decimal montantIR = CalculateIR(salaireImposable * 12) / 12;

            // ⬇️⬇️⬇️ ADD THESE CALCULATIONS BEFORE THE RETURN ⬇️⬇️⬇️
            decimal totalRetenues = cotisationCNSS + cotisationAMO + montantIR +
                ParseDecimal(SafeGetText(txtCIMR)) +
                ParseDecimal(SafeGetText(txtAvance)) +
                ParseDecimal(SafeGetText(txtPret)) +
                ParseDecimal(SafeGetText(txtPenalites)) +
                ParseDecimal(SafeGetText(txtAutresRetenues));

            decimal salaireNet = salaireBrut - totalRetenues;
            // ⬆️⬆️⬆️ END OF ADDED CALCULATIONS ⬆️⬆️⬆️

            return new SalaireModel
            {
                SalaireBase = salaireBase,
                TauxHoraire = tauxHoraire,
                HeuresNormales = 191,
                HeuresSupp25 = heuresSupp25,
                HeuresSupp50 = heuresSupp50,
                HeuresSupp100 = heuresSupp100,
                MontantHeuresSupp = montantHeuresSupp,
                PrimeAnciennete = ParseDecimal(SafeGetText(txtPrimeAnciennete)),
                PrimeRendement = ParseDecimal(SafeGetText(txtPrimeRendement)),
                PrimeResponsabilite = ParseDecimal(SafeGetText(txtPrimeResponsabilite)),
                IndemniteTransport = ParseDecimal(SafeGetText(txtIndemniteTransport)),
                IndemniteLogement = ParseDecimal(SafeGetText(txtIndemniteLogement)),
                AutresPrimes = ParseDecimal(SafeGetText(txtAutresPrimes)),
                SalaireBrut = salaireBrut,  // ← NOW IT EXISTS
                CotisationCNSS = cotisationCNSS,
                CotisationAMO = cotisationAMO,
                CotisationCIMR = ParseDecimal(SafeGetText(txtCIMR)),
                MontantIR = montantIR,
                CotisationPatronaleCNSS = cotisationPatronaleCNSS,
                CotisationPatronaleAMO = cotisationPatronaleAMO,
                AvanceSurSalaire = ParseDecimal(SafeGetText(txtAvance)),
                PretPersonnel = ParseDecimal(SafeGetText(txtPret)),
                Penalites = ParseDecimal(SafeGetText(txtPenalites)),
                AutresRetenues = ParseDecimal(SafeGetText(txtAutresRetenues)),
                TotalRetenues = totalRetenues,  // ← NOW IT EXISTS
                SalaireNet = salaireNet,  // ← NOW IT EXISTS
                Statut = "Payé",
                DatePaiement = DateTime.Now,
                Remarques = txtRemarques?.Text
            };
        }

        private void DeleteSalaire(int salaireId)
        {
            try
            {
                salaireService.Delete(salaireId);
                MessageBox.Show("Salaire supprimé avec succès!", "Succès",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                LoadSalaires();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression: {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSalaireForEdit(int salaireId)
        {
            try
            {
                var salaire = salaireService.GetById(salaireId);
                if (salaire != null)
                {
                    currentSalaireId = salaireId;
                    ShowForm(true);

                    if (cmbEmploye != null) cmbEmploye.SelectedValue = salaire.EmployeID;
                    if (cmbFormMois != null) cmbFormMois.SelectedIndex = salaire.Mois - 1;
                    if (cmbFormAnnee != null)
                    {
                        cmbFormAnnee.SelectedItem = cmbFormAnnee.Items.Cast<ComboBoxItem>()
                            .FirstOrDefault(i => (int)i.Tag == salaire.Annee);
                    }

                    SafeSetText(txtSalaireBase, salaire.SalaireBase.ToString());
                    SafeSetText(txtHeuresSupp25, salaire.HeuresSupp25.ToString());
                    SafeSetText(txtHeuresSupp50, salaire.HeuresSupp50.ToString());
                    SafeSetText(txtHeuresSupp100, salaire.HeuresSupp100.ToString());

                    SafeSetText(txtPrimeAnciennete, salaire.PrimeAnciennete.ToString());
                    SafeSetText(txtPrimeRendement, salaire.PrimeRendement.ToString());
                    SafeSetText(txtPrimeResponsabilite, salaire.PrimeResponsabilite.ToString());
                    SafeSetText(txtIndemniteTransport, salaire.IndemniteTransport.ToString());
                    SafeSetText(txtIndemniteLogement, salaire.IndemniteLogement.ToString());
                    SafeSetText(txtAutresPrimes, salaire.AutresPrimes.ToString());

                    SafeSetText(txtAvance, salaire.AvanceSurSalaire.ToString());
                    SafeSetText(txtPret, salaire.PretPersonnel.ToString());
                    SafeSetText(txtCIMR, salaire.CotisationCIMR.ToString());
                    SafeSetText(txtPenalites, salaire.Penalites.ToString());
                    SafeSetText(txtAutresRetenues, salaire.AutresRetenues.ToString());

                    if (txtRemarques != null) txtRemarques.Text = salaire.Remarques;

                    CalculateSalary(null, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement: {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewSalaryDetails(int salaireId)
        {
            try
            {
                var salaire = salaireService.GetById(salaireId);
                if (salaire != null)
                {
                    var detailsWindow = new SalaireDetailsWindow(salaire);
                    detailsWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region CSV Export

        private void ExportToCSV(string filePath)
        {
            var data = dgSalaires?.ItemsSource as List<SalaireModel>;
            if (data == null || data.Count == 0)
            {
                MessageBox.Show("Aucune donnée à exporter", "Information",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            using (StreamWriter writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine("Employé,CIN,CNSS,Mois,Année,Salaire Base,Salaire Brut,CNSS,AMO,IR,Total Retenues,Salaire Net,Statut");

                foreach (var salaire in data)
                {
                    writer.WriteLine($"{salaire.NomComplet},{salaire.CIN},{salaire.CNSS}," +
                                   $"{salaire.Mois},{salaire.Annee},{salaire.SalaireBase:F2}," +
                                   $"{salaire.SalaireBrut:F2},{salaire.CotisationCNSS:F2}," +
                                   $"{salaire.CotisationAMO:F2},{salaire.MontantIR:F2}," +
                                   $"{salaire.TotalRetenues:F2},{salaire.SalaireNet:F2},{salaire.Statut}");
                }
            }
        }

        #endregion
    }
}