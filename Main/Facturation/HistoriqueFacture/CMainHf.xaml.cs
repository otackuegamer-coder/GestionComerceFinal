using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using GestionComerce.Main.Facturation;
using GestionComerce.Main.Facturation.CreateFacture;
using GestionComerce;
using System.Configuration;

namespace GestionComerce.Main.Facturation.HistoriqueFacture
{
    /// <summary>
    /// Interaction logic for CMainHf.xaml
    /// </summary>
    public partial class CMainHf : UserControl
    {
        private InvoiceRepository _invoiceRepository;
        private List<Invoice> _allInvoices;
        private List<Invoice> _filteredInvoices;
        public MainWindow main;
        private User u;

        public CMainHf(User u, MainWindow main)
        {
            InitializeComponent();
            this.main = main;
            this.u = u;

            // Initialize repository with your connection string
            string connectionString = "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;"; // Replace with your actual connection string
            _invoiceRepository = new InvoiceRepository(connectionString);

            // Add converters as resources
            this.Resources.Add("StateBackgroundConverter", new StateBackgroundConverter());
            this.Resources.Add("StateForegroundConverter", new StateForegroundConverter());
            this.Resources.Add("StateTextConverter", new StateTextConverter());

            // Load invoices
            Loaded += async (s, e) => await LoadInvoicesAsync();
        }

        public async Task LoadInvoicesAsync()
        {
            try
            {
                // Show loading indicator (optional)
                dgInvoices.IsEnabled = false;
                dgInvoices.Opacity = 0.6;

                // Load all invoices from database
                _allInvoices = await _invoiceRepository.GetAllInvoicesAsync(includeDeleted: false);

                // Sort by date descending (most recent first)
                _allInvoices = _allInvoices.OrderByDescending(i => i.InvoiceDate).ToList();

                // Apply any active filters
                ApplyFilters();

                // Update UI
                dgInvoices.IsEnabled = true;
                dgInvoices.Opacity = 1.0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors du chargement des factures: {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                dgInvoices.IsEnabled = true;
                dgInvoices.Opacity = 1.0;
            }
        }

        private void ApplyFilters()
        {
            if (_allInvoices == null)
            {
                _filteredInvoices = new List<Invoice>();
                UpdateDataGrid();
                return;
            }

            _filteredInvoices = new List<Invoice>(_allInvoices);

            // Apply search filter
            string searchText = txtSearch?.Text?.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                _filteredInvoices = _filteredInvoices.Where(i =>
                    (i.InvoiceNumber?.ToLower().Contains(searchText) ?? false) ||
                    (i.ClientName?.ToLower().Contains(searchText) ?? false) ||
                    (i.ClientICE?.ToLower().Contains(searchText) ?? false)
                ).ToList();
            }

            // Apply date filters
            if (dpStartDate?.SelectedDate.HasValue == true)
            {
                DateTime startDate = dpStartDate.SelectedDate.Value.Date;
                _filteredInvoices = _filteredInvoices.Where(i => i.InvoiceDate.Date >= startDate).ToList();
            }

            if (dpEndDate?.SelectedDate.HasValue == true)
            {
                DateTime endDate = dpEndDate.SelectedDate.Value.Date;
                _filteredInvoices = _filteredInvoices.Where(i => i.InvoiceDate.Date <= endDate).ToList();
            }

            UpdateDataGrid();
        }

        private void UpdateDataGrid()
        {
            dgInvoices.ItemsSource = null;
            dgInvoices.ItemsSource = _filteredInvoices;

            // Update count
            int count = _filteredInvoices?.Count ?? 0;
            txtInvoiceCount.Text = count == 0 ? "Aucune facture" :
                                   count == 1 ? "1 facture" :
                                   $"{count} factures";
        }

        #region Event Handlers

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadInvoicesAsync();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void btnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            dpStartDate.SelectedDate = null;
            dpEndDate.SelectedDate = null;
            ApplyFilters();
        }

        private async void dgInvoices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgInvoices.SelectedItem is Invoice selectedInvoice)
            {
                await ViewInvoiceAsync(selectedInvoice.InvoiceID);
            }
        }

        private async void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int invoiceId)
            {
                await ViewInvoiceAsync(invoiceId);
            }
        }

        private async void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int invoiceId)
            {
                await EditInvoiceAsync(invoiceId);
            }
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int invoiceId)
            {
                var result = MessageBox.Show(
                    "Êtes-vous sûr de vouloir supprimer cette facture?",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await DeleteInvoiceAsync(invoiceId);
                }
            }
        }

        #endregion

        #region Business Logic

        private async Task ViewInvoiceAsync(int invoiceId)
        {
            try
            {
                // Load invoice with articles from database
                Invoice invoice = await _invoiceRepository.GetInvoiceByIdAsync(invoiceId);

                if (invoice == null)
                {
                    MessageBox.Show(
                        "Facture introuvable",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // ========== CHECK IF IT'S A BON DE LIVRAISON ==========
                if (invoice.InvoiceType == "Bon de Livraison")
                {
                    await ShowDeliveryTicketAsync(invoice);
                    return;
                }

                // ========== REGULAR INVOICE - SHOW FACTURE PAGE ==========
                // Prepare invoice info dictionary for WFacturePage
                Dictionary<string, string> factureInfo = new Dictionary<string, string>()
        {
            { "NFacture", invoice.InvoiceNumber ?? "" },
            { "Date", invoice.InvoiceDate.ToString("dd/MM/yyyy") },
            { "Type", invoice.InvoiceType ?? "" },
            { "NomU", invoice.UserName ?? "" },
            { "ICEU", invoice.UserICE ?? "" },
            { "VATU", invoice.UserVAT ?? "" },
            { "TelephoneU", invoice.UserPhone ?? "" },
            { "EtatJuridiqueU", invoice.UserEtatJuridique ?? "" },
            { "IdSocieteU", invoice.UserIdSociete ?? "" },
            { "SiegeEntrepriseU", invoice.UserSiegeEntreprise ?? "" },
            { "AdressU", invoice.UserAddress ?? "" },
            { "NomC", invoice.ClientName ?? "" },
            { "ICEC", invoice.ClientICE ?? "" },
            { "VATC", invoice.ClientVAT ?? "" },
            { "TelephoneC", invoice.ClientPhone ?? "" },
            { "EtatJuridiqueC", invoice.ClientEtatJuridique ?? "" },
            { "IdSocieteC", invoice.ClientIdSociete ?? "" },
            { "SiegeEntrepriseC", invoice.ClientSiegeEntreprise ?? "" },
            { "AdressC", invoice.ClientAddress ?? "" },
            { "EtatFature", invoice.IsReversed ? "Annulée" : "Normale" },
            { "Device", invoice.Currency ?? "DH" },
            { "TVA", invoice.TVARate.ToString("0.00") },
            { "MontantTotal", invoice.TotalHT.ToString("0.00") + " DH" },
            { "MontantTVA", invoice.TotalTVA.ToString("0.00") + " DH" },
            { "MontantApresTVA", invoice.TotalTTC.ToString("0.00") + " DH" },
            { "MontantApresRemise", invoice.TotalAfterRemise.ToString("0.00") + " DH" },
            { "IndexDeFacture", invoice.InvoiceIndex ?? "" },
            { "Description", invoice.Description ?? "" },
            { "Logo", invoice.LogoPath ?? "" },
            { "Reversed", invoice.IsReversed ? "Oui" : "Non" },
            { "Remise", invoice.Remise.ToString("0.00") },
            { "CreditClientName", invoice.CreditClientName ?? "" },
            { "CreditMontant", invoice.CreditMontant.ToString("0.00") + " DH" }
        };

                // Convert Invoice.InvoiceArticle to InvoiceArticle for compatibility
                List<InvoiceArticle> articles = invoice.Articles.Select(a => new InvoiceArticle
                {
                    OperationID = a.OperationID ?? 0,
                    ArticleID = a.ArticleID,
                    ArticleName = a.ArticleName,
                    Prix = a.PrixUnitaire,
                    Quantite = a.Quantite,
                    TVA = a.TVA,
                    Reversed = a.IsReversed
                }).ToList();

                // Open WFacturePage to display the invoice
                WFacturePage wFacturePage = new WFacturePage(null, factureInfo, articles);
                wFacturePage.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de l'affichage de la facture: {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ========== NEW METHOD: Show Delivery Ticket for Bon de Livraison ==========
        private async Task ShowDeliveryTicketAsync(Invoice invoice)
        {
            try
            {
                // Extract LivraisonID from InvoiceIndex
                if (!int.TryParse(invoice.InvoiceIndex, out int livraisonId))
                {
                    MessageBox.Show(
                        "Impossible de trouver la livraison associée.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Load the Livraison
                Livraison livraisonObj = new Livraison();
                Livraison livraison = await livraisonObj.GetLivraisonByIDAsync(livraisonId);

                if (livraison == null)
                {
                    MessageBox.Show(
                        "Livraison introuvable.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Load the associated operation
                Operation opObj = new Operation { OperationID = livraison.OperationID };
                var operations = await opObj.GetOperationsAsync();
                var operation = operations.FirstOrDefault(o => o.OperationID == livraison.OperationID);

                if (operation == null)
                {
                    MessageBox.Show(
                        "Opération introuvable.",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Create operation list
                List<Operation> operationsList = new List<Operation> { operation };

                // Extract time from notes or use default
                string heureCrenneau = ExtractTimeFromNotes(livraison.Notes);

                // Open DeliveryTicketWindow
                GestionComerce.Main.Delivery.DeliveryTicketWindow ticketWindow =
                    new GestionComerce.Main.Delivery.DeliveryTicketWindow(
                        livraison,
                        operationsList,
                        livraison.LivreurNom ?? "Non assigné",
                        heureCrenneau
                    );
                ticketWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de l'affichage du bon de livraison: {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // Helper method to extract time from notes
        private string ExtractTimeFromNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return "À définir";

            // Try to find time pattern like "10:00 - 12:00"
            var timePatterns = new[] {
        "08:00 - 10:00", "10:00 - 12:00", "12:00 - 14:00",
        "14:00 - 16:00", "16:00 - 18:00", "18:00 - 20:00"
    };

            foreach (var pattern in timePatterns)
            {
                if (notes.Contains(pattern))
                    return pattern;
            }

            return "À définir";
        }

        // Add this method in the #region Business Logic section of CMainHf.cs
        // Place it right after the ViewInvoiceAsync method

        private async Task EditInvoiceAsync(int invoiceId)
        {
            try
            {
                // Load invoice with articles from database
                Invoice invoice = await _invoiceRepository.GetInvoiceByIdAsync(invoiceId);

                if (invoice == null)
                {
                    MessageBox.Show(
                        "Facture introuvable",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // ========== PREVENT EDITING BON DE LIVRAISON ==========
                if (invoice.InvoiceType == "Bon de Livraison")
                {
                    MessageBox.Show(
                        "Les bons de livraison ne peuvent pas être modifiés.\nVeuillez modifier la livraison directement depuis le module de gestion des livraisons.",
                        "Information",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Check if invoice is reversed (cancelled) - cannot edit cancelled invoices
                if (invoice.IsReversed)
                {
                    MessageBox.Show(
                        "Impossible de modifier une facture annulée",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Open edit window
                WEditInvoice editWindow = new WEditInvoice(invoice, _invoiceRepository, this);
                bool? result = editWindow.ShowDialog();

                // If edit was successful, reload the invoices
                if (result == true)
                {
                    await LoadInvoicesAsync();
                    MessageBox.Show(
                        "Facture modifiée avec succès",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de l'édition de la facture: {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task DeleteInvoiceAsync(int invoiceId)
        {
            try
            {
                bool success = await _invoiceRepository.DeleteInvoiceAsync(invoiceId);

                if (success)
                {
                    MessageBox.Show(
                        "Facture supprimée avec succès",
                        "Succès",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Reload invoices
                    await LoadInvoicesAsync();
                }
                else
                {
                    MessageBox.Show(
                        "Échec de la suppression de la facture",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors de la suppression: {ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        private void dgInvoices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }

    #region Value Converters

    // Converter for state background color
    public class StateBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isReversed)
            {
                return isReversed ?
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2")) :
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5"));
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter for state foreground color
    public class StateForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isReversed)
            {
                return isReversed ?
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626")) :
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter for state text
    public class StateTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isReversed)
            {
                return isReversed ? "Annulée" : "Normale";
            }
            return "Inconnue";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}