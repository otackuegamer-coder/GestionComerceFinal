using GestionComerce.Main.Facturation;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GestionComerce.Main.Facturation
{
    // ════════════════════════════════════════════════════════════════════════════
    // PENDING INVOICE INTENT
    // Pure data object — no API calls.
    // WConfirmTransaction reads this after ShowDialog() and commits it to the
    // database only when the purchase itself succeeds.
    // ════════════════════════════════════════════════════════════════════════════
    public class PendingInvoiceIntent
    {
        /// <summary>True = create a brand-new SavedInvoice; False = append to an existing one.</summary>
        public bool IsCreate { get; set; }

        // ── fields for CREATE mode ────────────────────────────────────────────
        public byte[] ImageBytes { get; set; }
        public string ImageFileName { get; set; }
        public decimal Amount { get; set; }
        public string Reference { get; set; }
        public string Description { get; set; }
        public int FournisseurId { get; set; }

        // ── field for EXISTING mode ───────────────────────────────────────────
        public int TargetSavedInvoiceId { get; set; }
        public string TargetDisplayLabel { get; set; }

        // ── articles to insert (shared by both modes) ─────────────────────────
        public List<FactureEnregistreeArticle> Articles { get; set; }
            = new List<FactureEnregistreeArticle>();

        /// <summary>Human-readable summary shown in WConfirmTransaction.</summary>
        public string Summary => IsCreate
            ? $"Nouvelle facture « {Reference} » — {Amount:N2} DH"
            : $"Ajout à la facture #{TargetSavedInvoiceId} — {TargetDisplayLabel}";
    }

    /// <summary>
    /// Window opened from WConfirmTransaction.
    /// DOES NOT write anything to the database.
    /// On Confirm it validates the form, packs everything into
    /// <see cref="PendingIntent"/> and closes. The caller commits later.
    /// </summary>
    public partial class WSavedInvoiceLinker : Window
    {
        // ── display wrapper for the existing-invoice ComboBox ──────────────────
        private class SavedInvoiceItem
        {
            public int SavedInvoiceID { get; set; }
            public string DisplayLabel { get; set; }
        }

        // ── state ───────────────────────────────────────────────────────────────
        private readonly List<FactureEnregistreeArticle> _operationArticles;
        private readonly int _fournisseurId;
        private readonly decimal _operationTotal;

        private byte[] _pendingImageBytes;
        private string _pendingImageFileName;

        /// <summary>
        /// Filled only when the user clicks Confirm and validation passes.
        /// Null means the user cancelled or the window was closed without confirming.
        /// </summary>
        public PendingInvoiceIntent PendingIntent { get; private set; }

        // ── ctor ────────────────────────────────────────────────────────────────
        public WSavedInvoiceLinker(
            List<FactureEnregistreeArticle> operationArticles,
            int fournisseurId,
            decimal operationTotal)
        {
            InitializeComponent();

            _operationArticles = operationArticles ?? new List<FactureEnregistreeArticle>();
            _fournisseurId = fournisseurId;
            _operationTotal = operationTotal;

            dgOperationArticles.ItemsSource = _operationArticles;
            txtNewAmount.Text = _operationTotal.ToString("F2");

            this.Loaded += async (_, __) => await LoadExistingInvoicesAsync();
        }

        // ════════════════════════════════════════════════════════════════════════
        // MODE RADIO
        // ════════════════════════════════════════════════════════════════════════

        private void ModeRadio_Changed(object sender, RoutedEventArgs e)
        {
            if (panelCreate == null) return;

            bool isCreate = rbCreate.IsChecked == true;
            panelCreate.Visibility = isCreate ? Visibility.Visible : Visibility.Collapsed;
            panelExisting.Visibility = isCreate ? Visibility.Collapsed : Visibility.Visible;
            dgExistingArticles.Visibility = isCreate ? Visibility.Collapsed : Visibility.Visible;
            lblExistingArticles.Visibility = isCreate ? Visibility.Collapsed : Visibility.Visible;
            lblStatus.Text = "";
        }

        // ════════════════════════════════════════════════════════════════════════
        // LOAD EXISTING INVOICES (read-only, just to populate the ComboBox)
        // ════════════════════════════════════════════════════════════════════════

        private async Task LoadExistingInvoicesAsync()
        {
            try
            {
                var all = await FactureEnregistree.GetAllAsync();
                var items = all.Select(i => new SavedInvoiceItem
                {
                    SavedInvoiceID = i.SavedInvoiceID,
                    DisplayLabel = $"#{i.SavedInvoiceID}  {i.InvoiceReference}  —  {i.TotalAmount:N2} DH  ({i.InvoiceDate:dd/MM/yyyy})"
                }).ToList();
                cmbExistingInvoices.ItemsSource = items;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Erreur chargement: {ex.Message}";
            }
        }

        private async void RefreshExisting_Click(object sender, RoutedEventArgs e)
            => await LoadExistingInvoicesAsync();

        // ════════════════════════════════════════════════════════════════════════
        // SHOW EXISTING INVOICE ARTICLES (preview only — no writes)
        // ════════════════════════════════════════════════════════════════════════

        private async void cmbExistingInvoices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(cmbExistingInvoices.SelectedItem is SavedInvoiceItem item)) return;

            lblStatus.Text = "Chargement des articles…";
            try
            {
                var existing = await FactureEnregistreeArticle.GetBySavedInvoiceIdAsync(item.SavedInvoiceID);
                dgExistingArticles.ItemsSource = existing;
                lblStatus.Text = existing.Count == 0
                    ? "Cette facture ne contient aucun article pour l'instant."
                    : $"{existing.Count} article(s) déjà dans cette facture.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Erreur: {ex.Message}";
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // IMAGE BROWSE
        // ════════════════════════════════════════════════════════════════════════

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.pdf|Tous les fichiers|*.*",
                Title = "Sélectionner l'image de la facture fournisseur"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var info = new FileInfo(dlg.FileName);
                if (info.Length > 10 * 1024 * 1024)
                {
                    MessageBox.Show($"Image trop grande ({info.Length / 1024 / 1024} MB). Max 10 MB.",
                        "Image trop grande", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _pendingImageBytes = File.ReadAllBytes(dlg.FileName);
                _pendingImageFileName = Path.GetFileName(dlg.FileName);
                txtImageName.Text = _pendingImageFileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lecture image: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // CONFIRM — VALIDATE ONLY, pack intent, close
        // ════════════════════════════════════════════════════════════════════════

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (rbCreate.IsChecked == true)
            {
                // ── validate CREATE fields ──────────────────────────────────────
                if (_pendingImageBytes == null || _pendingImageBytes.Length == 0)
                {
                    MessageBox.Show(
                        "Veuillez sélectionner une image pour la facture (bouton 📂 Image).",
                        "Image manquante", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(txtNewAmount.Text.Trim(), out decimal amount) || amount <= 0)
                {
                    MessageBox.Show("Montant invalide.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ── pack intent (no API call) ───────────────────────────────────
                PendingIntent = new PendingInvoiceIntent
                {
                    IsCreate = true,
                    ImageBytes = _pendingImageBytes,
                    ImageFileName = _pendingImageFileName ?? "facture.jpg",
                    Amount = amount,
                    Reference = txtNewRef.Text.Trim(),
                    Description = txtNewDesc.Text.Trim(),
                    FournisseurId = _fournisseurId,
                    Articles = _operationArticles
                };
            }
            else
            {
                // ── validate EXISTING selection ─────────────────────────────────
                if (!(cmbExistingInvoices.SelectedItem is SavedInvoiceItem item))
                {
                    MessageBox.Show("Veuillez sélectionner une facture existante.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ── pack intent (no API call) ───────────────────────────────────
                PendingIntent = new PendingInvoiceIntent
                {
                    IsCreate = false,
                    TargetSavedInvoiceId = item.SavedInvoiceID,
                    TargetDisplayLabel = item.DisplayLabel,
                    Articles = _operationArticles
                };
            }

            // All good — close window so caller can read PendingIntent
            this.Close();
        }

        // ════════════════════════════════════════════════════════════════════════
        // CANCEL
        // ════════════════════════════════════════════════════════════════════════

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            PendingIntent = null;   // explicit: nothing to commit
            this.Close();
        }

        // ════════════════════════════════════════════════════════════════════════
        // INPUT HELPERS
        // ════════════════════════════════════════════════════════════════════════

        private void DecimalOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = sender as TextBox;
            e.Handled = e.Text == "."
                ? tb.Text.Contains(".")
                : !e.Text.All(char.IsDigit);
        }
    }
}