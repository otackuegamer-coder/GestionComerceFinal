using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GestionComerce.Main.FournisseurPage
{
    public partial class CMainF : UserControl
    {
        private MainWindow _mainWindow;
        private User _currentUser;
        private List<Fournisseur> _allFournisseurs = new List<Fournisseur>();
        private List<Credit> _credits = new List<Credit>();

        public CMainF(User u, MainWindow mainWindow)
        {
            InitializeComponent();

            _mainWindow = mainWindow;
            _currentUser = u;

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (Role r in _mainWindow.lr)
            {
                if (_currentUser.RoleID == r.RoleID)
                {
                    if (r.ViewFournisseur == true)
                    {
                        LoadAllData();
                    }
                }
            }
        }

        public void ReloadSuppliers()
        {
            LoadAllData();
        }

        public async void LoadAllData()
        {
            try
            {
                // Reload from database to get fresh data (including Etat updates)
                var fournisseur = new Fournisseur();
                var freshSuppliers = await fournisseur.GetFournisseursAsync();

                // Update MainWindow list - clear and reload to ensure synchronization
                _mainWindow.lfo.Clear();
                foreach (var supplier in freshSuppliers)
                {
                    _mainWindow.lfo.Add(supplier);
                }

                // Use the MainWindow list as source of truth
                _allFournisseurs = _mainWindow.lfo;

                // Get credits from MainWindow
                _credits = _mainWindow.credits ?? new List<Credit>();

                System.Diagnostics.Debug.WriteLine($"Loaded suppliers: {_allFournisseurs.Count} (Active: {_allFournisseurs.Count(f => f.Etat)}), Credits: {_credits.Count}");

                RefreshSupplierDisplay();
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Error in LoadAllData: {ex}");
            }
        }

        public void RefreshSupplierDisplay()
        {
            try
            {
                SuppliersContainer.Children.Clear();

                if (_allFournisseurs == null || _allFournisseurs.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No suppliers to display");
                    return;
                }

                // Only show active suppliers (Etat = true)
                var activeSuppliers = _allFournisseurs.Where(f => f.Etat).OrderBy(f => f.Nom).ToList();
                System.Diagnostics.Debug.WriteLine($"Displaying active suppliers: {activeSuppliers.Count}");

                foreach (var supplier in activeSuppliers)
                {
                    var supplierRow = CreateSupplierRow(supplier);
                    SuppliersContainer.Children.Add(supplierRow);
                }

                // Update statistics after refresh
                UpdateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing supplier display: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Error in RefreshSupplierDisplay: {ex}");
            }
        }

        private UserControl CreateSupplierRow(Fournisseur supplier)
        {
            var supplierRow = new SingleRowSupplier(_mainWindow, _currentUser);
            supplierRow.DataContext = supplier;
            return supplierRow;
        }

        public void UpdateStatistics()
        {
            try
            {
                var activeSuppliers = _allFournisseurs?.Where(f => f.Etat).ToList() ?? new List<Fournisseur>();

                // Filter only SUPPLIER credits (where FournisseurID is not null and is active)
                var activeFournisseurIds = activeSuppliers.Select(f => f.FournisseurID).ToList();
                var supplierCredits = _credits?.Where(c => c.Etat &&
                                                          c.FournisseurID.HasValue &&
                                                          activeFournisseurIds.Contains(c.FournisseurID.Value))
                                               .ToList() ?? new List<Credit>();

                int totalSuppliers = activeSuppliers.Count;
                decimal totalCredit = supplierCredits.Sum(c => c.Total);
                decimal totalPaid = supplierCredits.Sum(c => c.Paye);
                decimal pending = supplierCredits.Sum(c => c.Difference);

                if (TotalSuppliersText != null)
                    TotalSuppliersText.Text = totalSuppliers.ToString();

                if (TotalCreditText != null)
                    TotalCreditText.Text = $"{totalCredit:N2} DH";

                if (PaidThisMonthText != null)
                    PaidThisMonthText.Text = $"{totalPaid:N2} DH";

                if (PendingText != null)
                    PendingText.Text = $"{pending:N2} DH";

                System.Diagnostics.Debug.WriteLine($"Statistics - Suppliers: {totalSuppliers}, Total: {totalCredit:N2}, Paid: {totalPaid:N2}, Pending: {pending:N2}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating statistics: {ex.Message}");
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_allFournisseurs == null) return;

                string query = (SearchTextBox?.Text ?? "").Trim().ToLowerInvariant();

                SuppliersContainer.Children.Clear();

                List<Fournisseur> filteredSuppliers;

                if (string.IsNullOrEmpty(query))
                {
                    // Show only active suppliers (Etat = true)
                    filteredSuppliers = _allFournisseurs.Where(f => f.Etat).OrderBy(f => f.Nom).ToList();
                }
                else
                {
                    // Search in multiple fields and only show active suppliers
                    filteredSuppliers = _allFournisseurs.Where(f => f.Etat &&
                        ((!string.IsNullOrEmpty(f.Nom) && f.Nom.ToLowerInvariant().Contains(query)) ||
                         (!string.IsNullOrEmpty(f.Code) && f.Code.ToLowerInvariant().Contains(query)) ||
                         (!string.IsNullOrEmpty(f.Telephone) && f.Telephone.ToLowerInvariant().Contains(query)) ||
                         (!string.IsNullOrEmpty(f.ICE) && f.ICE.ToLowerInvariant().Contains(query)) ||
                         (!string.IsNullOrEmpty(f.EtatJuridic) && f.EtatJuridic.ToLowerInvariant().Contains(query)) ||
                         (!string.IsNullOrEmpty(f.SiegeEntreprise) && f.SiegeEntreprise.ToLowerInvariant().Contains(query)) ||
                         (!string.IsNullOrEmpty(f.Adresse) && f.Adresse.ToLowerInvariant().Contains(query)) ||
                         f.FournisseurID.ToString().Contains(query))
                    ).OrderBy(f => f.Nom).ToList();
                }

                System.Diagnostics.Debug.WriteLine($"Search query: '{query}', Found: {filteredSuppliers.Count}");

                foreach (var supplier in filteredSuppliers)
                {
                    var supplierRow = CreateSupplierRow(supplier);
                    SuppliersContainer.Children.Add(supplierRow);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in search: {ex.Message}");
            }
        }

        private void AddNewSupplier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var wnd = new SupplierFormWindow(_mainWindow);
                wnd.SupplierSaved += (s, ev) => LoadAllData();
                bool? result = wnd.ShowDialog();

                if (result == true)
                {
                    LoadAllData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening supplier form: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.load_main(_currentUser);
        }
    }
}