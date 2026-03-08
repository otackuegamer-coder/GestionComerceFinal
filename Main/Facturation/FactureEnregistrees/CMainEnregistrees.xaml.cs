using GestionComerce.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GestionComerce.Main.Facturation.FacturesEnregistrees
{
    public partial class CMainEnregistrees : UserControl
    {
        private User user;
        private MainWindow main;
        private FactureEnregistree selectedInvoice;
        private List<SavedInvoiceDisplay> allInvoices;

        private byte[] _pendingImageBytes = null;

        // ── display model — now includes ArticleCount ─────────────────────────
        public class SavedInvoiceDisplay
        {
            public int SavedInvoiceID { get; set; }
            public string InvoiceImagePath { get; set; }
            public byte[] InvoiceImage { get; set; }
            public int FournisseurID { get; set; }
            public decimal TotalAmount { get; set; }
            public DateTime InvoiceDate { get; set; }
            public string Description { get; set; }
            public string InvoiceReference { get; set; }
            public string Notes { get; set; }

            // ── article count ─────────────────────────────────────────────────
            public int ArticleCount { get; set; }

            /// <summary>Shown in the badge column — e.g. "3" or "—"</summary>
            public string ArticleCountLabel =>
                ArticleCount == 0 ? "—" : ArticleCount.ToString();
        }

        private class FournisseurDisplay
        {
            public int FournisseurID { get; set; }
            public string DisplayText { get; set; }
        }

        public CMainEnregistrees(User u, MainWindow mainWindow)
        {
            InitializeComponent();
            this.user = u;
            this.main = mainWindow;
            allInvoices = new List<SavedInvoiceDisplay>();
            dpInvoiceDate.SelectedDate = DateTime.Now;
            this.Loaded += CMainEnregistrees_Loaded;
        }

        private async void CMainEnregistrees_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        // ── DATA LOADING ──────────────────────────────────────────────────────

        private async Task LoadData()
        {
            try
            {
                await LoadFournisseurs();

                var invoices = await FactureEnregistree.GetAllAsync();

                // Build display objects first (without counts)
                var displays = invoices.Select(i => new SavedInvoiceDisplay
                {
                    SavedInvoiceID = i.SavedInvoiceID,
                    InvoiceImagePath = i.ImageFileName,
                    InvoiceImage = i.InvoiceImage,
                    FournisseurID = i.FournisseurID,
                    TotalAmount = i.TotalAmount,
                    InvoiceDate = i.InvoiceDate,
                    Description = i.Description,
                    InvoiceReference = i.InvoiceReference,
                    Notes = i.Notes,
                    ArticleCount = 0  // will be filled below
                }).ToList();

                // Load article counts in parallel for all invoices
                await LoadArticleCountsAsync(displays);

                allInvoices = displays;

                if (dgInvoices != null)
                {
                    dgInvoices.ItemsSource = allInvoices;
                    UpdateResultsCount(allInvoices.Count);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des données: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Loads the article count for each invoice concurrently and writes
        /// it back into the display object.
        /// </summary>
        private async Task LoadArticleCountsAsync(List<SavedInvoiceDisplay> displays)
        {
            var tasks = displays.Select(async d =>
            {
                try
                {
                    var arts = await FactureEnregistreeArticle.GetBySavedInvoiceIdAsync(d.SavedInvoiceID);
                    d.ArticleCount = arts.Count;
                }
                catch
                {
                    d.ArticleCount = 0;
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task LoadFournisseurs()
        {
            try
            {
                var fournisseur = new Fournisseur();
                var fournisseurs = await fournisseur.GetFournisseursAsync();

                var list = fournisseurs.Select(f => new FournisseurDisplay
                {
                    FournisseurID = f.FournisseurID,
                    DisplayText = f.Nom
                }).ToList();

                cmbSupplier.ItemsSource = list;

                var filterList = new List<FournisseurDisplay>
                    { new FournisseurDisplay { FournisseurID = 0, DisplayText = "Tous les fournisseurs" } };
                filterList.AddRange(list);
                cmbFilterSupplier.ItemsSource = filterList;
                cmbFilterSupplier.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des fournisseurs: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── IMAGE BROWSE ──────────────────────────────────────────────────────

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.pdf|Tous les fichiers|*.*",
                Title = "Sélectionner l'image de la facture"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                var info = new FileInfo(dlg.FileName);
                if (info.Length > 10 * 1024 * 1024)
                {
                    MessageBox.Show(
                        $"L'image sélectionnée est trop grande ({info.Length / 1024 / 1024} MB).\n" +
                        "Veuillez sélectionner une image de moins de 10 MB.",
                        "Image trop grande", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _pendingImageBytes = File.ReadAllBytes(dlg.FileName);
                txtImagePath.Text = dlg.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la lecture de l'image: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── ADD ───────────────────────────────────────────────────────────────

        private async void AddInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm()) return;

            if (_pendingImageBytes == null || _pendingImageBytes.Length == 0)
            {
                MessageBox.Show(
                    "Aucune image chargée.\nVeuillez cliquer sur « Parcourir » et sélectionner l'image.",
                    "Image manquante", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetFormBusy(true);
            try
            {
                var newInvoice = new FactureEnregistree
                {
                    InvoiceImage = _pendingImageBytes,
                    ImageFileName = Path.GetFileName(txtImagePath.Text),
                    FournisseurID = (int)cmbSupplier.SelectedValue,
                    TotalAmount = decimal.Parse(txtAmount.Text.Trim()),
                    InvoiceDate = dpInvoiceDate.SelectedDate ?? DateTime.Now,
                    Description = txtDescription.Text.Trim(),
                    InvoiceReference = txtReference.Text.Trim(),
                    Notes = txtNotes.Text.Trim()
                };

                bool ok = await newInvoice.InsertAsync();
                if (ok)
                {
                    MessageBox.Show("Facture enregistrée avec succès!",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearForm();
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetFormBusy(false);
            }
        }

        // ── UPDATE ────────────────────────────────────────────────────────────

        private async void UpdateInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (selectedInvoice == null)
            {
                MessageBox.Show("Veuillez sélectionner une facture à modifier.",
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!ValidateForm()) return;

            SetFormBusy(true);
            try
            {
                byte[] imageToSave = (_pendingImageBytes != null && _pendingImageBytes.Length > 0)
                    ? _pendingImageBytes
                    : selectedInvoice.InvoiceImage;

                string fileNameToSave = (_pendingImageBytes != null && _pendingImageBytes.Length > 0)
                    ? Path.GetFileName(txtImagePath.Text)
                    : selectedInvoice.InvoiceImagePath;

                var invoice = new FactureEnregistree
                {
                    SavedInvoiceID = selectedInvoice.SavedInvoiceID,
                    InvoiceImage = imageToSave,
                    ImageFileName = fileNameToSave,
                    FournisseurID = (int)cmbSupplier.SelectedValue,
                    TotalAmount = decimal.Parse(txtAmount.Text.Trim()),
                    InvoiceDate = dpInvoiceDate.SelectedDate ?? DateTime.Now,
                    Description = txtDescription.Text.Trim(),
                    InvoiceReference = txtReference.Text.Trim(),
                    Notes = txtNotes.Text.Trim()
                };

                bool ok = await invoice.UpdateAsync();
                if (ok)
                {
                    MessageBox.Show("Facture modifiée avec succès!",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearForm();
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la modification: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetFormBusy(false);
            }
        }

        // ── DELETE ────────────────────────────────────────────────────────────

        private async void DeleteInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (selectedInvoice == null)
            {
                MessageBox.Show("Veuillez sélectionner une facture à supprimer.",
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Êtes-vous sûr de vouloir supprimer cette facture?",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes) return;

            SetFormBusy(true);
            try
            {
                var invoice = new FactureEnregistree { SavedInvoiceID = selectedInvoice.SavedInvoiceID };
                bool ok = await invoice.DeleteAsync();
                if (ok)
                {
                    MessageBox.Show("Facture supprimée avec succès!",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearForm();
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la suppression: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetFormBusy(false);
            }
        }

        // ── MANAGE ARTICLES (form button — edit mode only) ────────────────────

        private void ManageArticles_Click(object sender, RoutedEventArgs e)
        {
            if (selectedInvoice == null) return;
            OpenArticlesWindow(selectedInvoice.SavedInvoiceID, selectedInvoice.InvoiceReference);
        }

        // ── VIEW ARTICLES (grid row button) ───────────────────────────────────

        private void ViewArticles_Click(object sender, RoutedEventArgs e)
        {
            if (!(((FrameworkElement)sender).DataContext is SavedInvoiceDisplay d)) return;
            OpenArticlesWindow(d.SavedInvoiceID, d.InvoiceReference);
        }

        /// <summary>Opens WInvoiceArticles and refreshes the count badge when it closes.</summary>
        private void OpenArticlesWindow(int savedInvoiceId, string reference)
        {
            var win = new WInvoiceArticles(savedInvoiceId, reference ?? $"#{savedInvoiceId}")
            {
                Owner = Window.GetWindow(this)
            };

            // When user adds/deletes inside the window, update the badge immediately
            win.ArticlesChanged += async (_, __) =>
            {
                var row = allInvoices.FirstOrDefault(i => i.SavedInvoiceID == savedInvoiceId);
                if (row == null) return;
                try
                {
                    var arts = await FactureEnregistreeArticle.GetBySavedInvoiceIdAsync(savedInvoiceId);
                    row.ArticleCount = arts.Count;
                    // Force DataGrid to refresh the badge cell
                    dgInvoices.Items.Refresh();
                }
                catch { /* best-effort */ }
            };

            win.ShowDialog();

            // Full refresh after window closes to capture any remaining changes
            _ = LoadData();
        }

        // ── SEARCH & FILTER ───────────────────────────────────────────────────

        private async void RefreshData_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            await LoadData();
        }

        private void ResetForm_Click(object sender, RoutedEventArgs e) => ClearForm();

        private async void Search_Click(object sender, RoutedEventArgs e) => await PerformSearch();
        private async void Search_TextChanged(object sender, TextChangedEventArgs e) => await PerformSearch();

        private Task PerformSearch()
        {
            try
            {
                string q = txtSearch?.Text?.Trim().ToLower() ?? "";
                var source = string.IsNullOrEmpty(q)
                    ? allInvoices
                    : allInvoices.Where(i =>
                        (i.InvoiceReference?.ToLower().Contains(q) == true) ||
                        (i.Description?.ToLower().Contains(q) == true) ||
                        i.FournisseurID.ToString().Contains(q)
                    ).ToList();

                dgInvoices.ItemsSource = source;
                UpdateResultsCount(source.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la recherche: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return Task.CompletedTask;
        }

        private void FilterSupplier_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded || allInvoices == null || cmbFilterSupplier.SelectedValue == null) return;
            try
            {
                int fid = (int)cmbFilterSupplier.SelectedValue;
                var source = fid == 0 ? allInvoices : allInvoices.Where(i => i.FournisseurID == fid).ToList();
                dgInvoices.ItemsSource = source;
                UpdateResultsCount(source.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du filtrage: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── GRID SELECTION ────────────────────────────────────────────────────

        private async void dgInvoices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgInvoices.SelectedItem is SavedInvoiceDisplay selected)
            {
                selectedInvoice = await FactureEnregistree.GetByIdAsync(selected.SavedInvoiceID)
                                  ?? new FactureEnregistree { SavedInvoiceID = selected.SavedInvoiceID };
                PopulateForm(selected);
                UpdateButtonVisibility();
            }
        }

        private void dgInvoices_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Double-click opens the articles window for the selected row
            if (dgInvoices.SelectedItem is SavedInvoiceDisplay d)
                OpenArticlesWindow(d.SavedInvoiceID, d.InvoiceReference);
        }

        private void ViewImage_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is SavedInvoiceDisplay d)
                ViewInvoiceImage(d.InvoiceImage, d.InvoiceImagePath);
        }

        private void PreviewImage_Click(object sender, RoutedEventArgs e)
        {
            if (_pendingImageBytes != null && _pendingImageBytes.Length > 0)
                ViewInvoiceImage(_pendingImageBytes, txtImagePath.Text);
            else if (selectedInvoice?.InvoiceImage != null)
                ViewInvoiceImage(selectedInvoice.InvoiceImage, selectedInvoice.ImageFileName);
            else
                MessageBox.Show("Veuillez sélectionner une image d'abord.",
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // ── IMAGE VIEWER ──────────────────────────────────────────────────────

        private void ViewInvoiceImage(byte[] imageBytes, string imagePath)
        {
            try
            {
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    string ext = Path.GetExtension(Path.GetFileName(imagePath ?? ""));
                    if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                    string tempPath = Path.Combine(Path.GetTempPath(), $"invoice_{Guid.NewGuid()}{ext}");
                    File.WriteAllBytes(tempPath, imageBytes);
                    Process.Start(new ProcessStartInfo { FileName = tempPath, UseShellExecute = true });
                    return;
                }

                MessageBox.Show(
                    "Aucune image trouvée dans la base de données pour cette facture.\n\n" +
                    "Sélectionnez cette facture, cliquez sur « Parcourir » pour choisir\n" +
                    "l'image et cliquez sur « Modifier » pour la sauvegarder.",
                    "Image non disponible", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de l'image: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── FORM HELPERS ──────────────────────────────────────────────────────

        private void PopulateForm(SavedInvoiceDisplay invoice)
        {
            if (invoice == null) return;
            txtReference.Text = invoice.InvoiceReference;
            cmbSupplier.SelectedValue = invoice.FournisseurID;
            txtAmount.Text = invoice.TotalAmount.ToString("F2");
            dpInvoiceDate.SelectedDate = invoice.InvoiceDate;
            txtImagePath.Text = invoice.InvoiceImagePath ?? "";
            _pendingImageBytes = null;
            txtDescription.Text = invoice.Description;
            txtNotes.Text = invoice.Notes;
        }

        private void ClearForm()
        {
            selectedInvoice = null;
            _pendingImageBytes = null;
            txtReference.Clear();
            cmbSupplier.SelectedIndex = -1;
            txtAmount.Clear();
            dpInvoiceDate.SelectedDate = DateTime.Now;
            txtImagePath.Clear();
            txtImagePath.Tag = null;
            txtDescription.Clear();
            txtNotes.Clear();
            if (dgInvoices != null) dgInvoices.SelectedItem = null;
            UpdateButtonVisibility();
        }

        private void UpdateResultsCount(int count)
        {
            if (txtResultsCount != null)
                txtResultsCount.Text = count == 0 ? "Aucune facture trouvée" :
                    count == 1 ? "1 facture trouvée" : $"{count} factures trouvées";
        }

        private void UpdateButtonVisibility()
        {
            bool editing = selectedInvoice != null;
            btnAdd.Visibility = editing ? Visibility.Collapsed : Visibility.Visible;
            btnUpdate.Visibility = editing ? Visibility.Visible : Visibility.Collapsed;
            btnDelete.Visibility = editing ? Visibility.Visible : Visibility.Collapsed;
            // Show "Gérer les Articles" only when an invoice is selected
            btnManageArticles.Visibility = editing ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetFormBusy(bool busy)
        {
            btnAdd.IsEnabled = !busy;
            btnUpdate.IsEnabled = !busy;
            btnDelete.IsEnabled = !busy;
            this.Cursor = busy
                ? System.Windows.Input.Cursors.Wait
                : System.Windows.Input.Cursors.Arrow;
        }

        private bool ValidateForm()
        {
            if (_pendingImageBytes == null || _pendingImageBytes.Length == 0)
            {
                if (selectedInvoice == null)
                {
                    MessageBox.Show(
                        "Veuillez sélectionner une image de la facture via « Parcourir ».",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            if (cmbSupplier.SelectedValue == null)
            {
                MessageBox.Show("Veuillez sélectionner un fournisseur.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbSupplier.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtAmount.Text) ||
                !decimal.TryParse(txtAmount.Text, out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Veuillez entrer un montant valide (nombre positif).",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAmount.Focus();
                return false;
            }

            if (dpInvoiceDate.SelectedDate == null)
            {
                MessageBox.Show("Veuillez sélectionner une date.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                dpInvoiceDate.Focus();
                return false;
            }

            return true;
        }
    }
}