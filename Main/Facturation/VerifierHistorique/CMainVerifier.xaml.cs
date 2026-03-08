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
using System.Windows.Controls.Primitives;
using GestionComerce;
using GestionComerce.Main.Facturation;
using System.Windows.Media.Imaging;

namespace GestionComerce.Main.Facturation.VerifierHistorique
{
    public partial class CMainVerifier : UserControl
    {
        private User user;
        private MainWindow main;
        private CheckHistory selectedCheck;
        private List<CheckHistory> allChecks;
        private byte[] selectedImageBytes; // Store image bytes
        private List<InvoiceDisplay> allInvoices; // Store all invoices for searching

        // Invoice display class
        public class InvoiceDisplay
        {
            public int InvoiceID { get; set; }
            public string DisplayText { get; set; }
            public string InvoiceNumber { get; set; }
            public string ClientName { get; set; }
        }

        public CMainVerifier(User u, MainWindow mainWindow)
        {
            InitializeComponent();
            this.user = u;
            this.main = mainWindow;

            // Initialize the list to avoid null reference
            allChecks = new List<CheckHistory>();
            allInvoices = new List<InvoiceDisplay>();

            // Set default date
            dpCheckDate.SelectedDate = DateTime.Now;

            // Load data after everything is initialized
            this.Loaded += CMainVerifier_Loaded;
        }

        private async void CMainVerifier_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                // Load all checks
                CheckHistory checkHistory = new CheckHistory();
                allChecks = await checkHistory.GetAllChecksAsync();

                if (dgChecks != null)
                {
                    dgChecks.ItemsSource = allChecks;
                    UpdateResultsCount(allChecks?.Count ?? 0);
                }

                // Load invoices for combobox
                await LoadInvoices();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des données: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadInvoices()
        {
            try
            {
                // Load real invoices from database
                InvoiceRepository invoiceRepo = new InvoiceRepository("");

                var invoices = await invoiceRepo.GetAllInvoicesAsync();

                allInvoices = invoices.Select(i => new InvoiceDisplay
                {
                    InvoiceID = i.InvoiceID,
                    InvoiceNumber = i.InvoiceNumber ?? "N/A",
                    ClientName = i.ClientName ?? "N/A",
                    DisplayText = $"Facture #{i.InvoiceNumber ?? "N/A"} - {i.ClientName ?? "N/A"} - {i.TotalTTC:N2} MAD"
                }).ToList();

                cmbInvoice.ItemsSource = allInvoices;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des factures: {ex.Message}\n\nStack: {ex.StackTrace}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchInvoice_Click(object sender, RoutedEventArgs e)
        {
            // Create and show the search dialog
            var searchDialog = new InvoiceSearchDialog(allInvoices);
            searchDialog.Owner = Window.GetWindow(this);

            if (searchDialog.ShowDialog() == true && searchDialog.SelectedInvoice != null)
            {
                // Set the selected invoice in the combobox
                cmbInvoice.SelectedValue = searchDialog.SelectedInvoice.InvoiceID;
            }
        }

        private void UpdateResultsCount(int count)
        {
            if (txtResultsCount != null)
            {
                txtResultsCount.Text = count == 0 ? "Aucun chèque trouvé" :
                    count == 1 ? "1 chèque trouvé" :
                    $"{count} chèques trouvés";
            }
        }

        private void UpdateButtonVisibility()
        {
            if (selectedCheck != null)
            {
                // Hide Add button, show Update and Delete buttons
                btnAdd.Visibility = Visibility.Collapsed;
                btnUpdate.Visibility = Visibility.Visible;
                btnDelete.Visibility = Visibility.Visible;
            }
            else
            {
                // Show Add button, hide Update and Delete buttons
                btnAdd.Visibility = Visibility.Visible;
                btnUpdate.Visibility = Visibility.Collapsed;
                btnDelete.Visibility = Visibility.Collapsed;
            }
        }

        private void BrowseImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Tous les fichiers|*.*",
                Title = "Sélectionner une image du chèque"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Read the image file as bytes
                    selectedImageBytes = File.ReadAllBytes(openFileDialog.FileName);
                    txtImagePath.Text = Path.GetFileName(openFileDialog.FileName); // Store only filename

                    MessageBox.Show("Image chargée avec succès!", "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors du chargement de l'image: {ex.Message}",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void AddCheck_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            try
            {
                CheckHistory newCheck = new CheckHistory
                {
                    CheckReference = txtCheckReference.Text.Trim(),
                    CheckImage = selectedImageBytes, // Save binary data
                    CheckImagePath = txtImagePath.Text.Trim(), // Save filename for reference
                    InvoiceID = (int)cmbInvoice.SelectedValue,
                    CheckAmount = string.IsNullOrEmpty(txtCheckAmount.Text) ? (decimal?)null :
                        decimal.Parse(txtCheckAmount.Text.Trim()),
                    CheckDate = dpCheckDate.SelectedDate ?? DateTime.Now,
                    BankName = txtBankName.Text.Trim(),
                    CheckStatus = (cmbStatus.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "En Attente",
                    Notes = txtNotes.Text.Trim()
                };

                int result = await newCheck.InsertCheckAsync();
                if (result > 0)
                {
                    MessageBox.Show("Chèque ajouté avec succès!", "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearForm();
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ajout: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UpdateCheck_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCheck == null)
            {
                MessageBox.Show("Veuillez sélectionner un chèque à modifier.",
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateForm())
                return;

            try
            {
                selectedCheck.CheckReference = txtCheckReference.Text.Trim();

                // Update image only if new one was selected
                if (selectedImageBytes != null)
                {
                    selectedCheck.CheckImage = selectedImageBytes;
                    selectedCheck.CheckImagePath = txtImagePath.Text.Trim();
                }

                selectedCheck.InvoiceID = (int)cmbInvoice.SelectedValue;
                selectedCheck.CheckAmount = string.IsNullOrEmpty(txtCheckAmount.Text) ? (decimal?)null :
                    decimal.Parse(txtCheckAmount.Text.Trim());
                selectedCheck.CheckDate = dpCheckDate.SelectedDate ?? DateTime.Now;
                selectedCheck.BankName = txtBankName.Text.Trim();
                selectedCheck.CheckStatus = (cmbStatus.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "En Attente";
                selectedCheck.Notes = txtNotes.Text.Trim();

                int result = await selectedCheck.UpdateCheckAsync();
                if (result > 0)
                {
                    MessageBox.Show("Chèque modifié avec succès!", "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearForm();
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la modification: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteCheck_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCheck == null)
            {
                MessageBox.Show("Veuillez sélectionner un chèque à supprimer.",
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer le chèque '{selectedCheck.CheckReference}'?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    int deleteResult = await selectedCheck.DeleteCheckAsync();
                    if (deleteResult > 0)
                    {
                        MessageBox.Show("Chèque supprimé avec succès!", "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearForm();
                        await LoadData();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la suppression: {ex.Message}",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RefreshData_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            _ = LoadData();
        }

        private void ResetForm_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            await PerformSearch();
        }

        private async void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            await PerformSearch();
        }

        private async Task PerformSearch()
        {
            try
            {
                if (allChecks == null)
                    allChecks = new List<CheckHistory>();

                string searchText = txtSearch.Text.Trim();

                if (string.IsNullOrEmpty(searchText))
                {
                    if (dgChecks != null)
                    {
                        dgChecks.ItemsSource = allChecks;
                        UpdateResultsCount(allChecks?.Count ?? 0);
                    }
                }
                else
                {
                    CheckHistory checkHistory = new CheckHistory();
                    var results = await checkHistory.SearchChecksByReferenceAsync(searchText);
                    if (dgChecks != null)
                    {
                        dgChecks.ItemsSource = results;
                        UpdateResultsCount(results?.Count ?? 0);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la recherche: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void FilterStatus_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Avoid running on initialization
            if (!this.IsLoaded || allChecks == null)
                return;

            try
            {
                var selectedItem = (cmbFilterStatus.SelectedItem as ComboBoxItem)?.Content.ToString();

                if (selectedItem == "Tous les statuts" || string.IsNullOrEmpty(selectedItem))
                {
                    if (dgChecks != null)
                    {
                        dgChecks.ItemsSource = allChecks;
                        UpdateResultsCount(allChecks?.Count ?? 0);
                    }
                }
                else
                {
                    CheckHistory checkHistory = new CheckHistory();
                    var results = await checkHistory.GetChecksByStatusAsync(selectedItem);
                    if (dgChecks != null)
                    {
                        dgChecks.ItemsSource = results;
                        UpdateResultsCount(results?.Count ?? 0);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du filtrage: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void dgChecks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgChecks.SelectedItem != null)
            {
                var selected = dgChecks.SelectedItem as CheckHistory;
                if (selected != null)
                {
                    // Fetch the full record (includes checkImageBase64 from the single-record endpoint)
                    CheckHistory checkHistory = new CheckHistory();
                    var full = await checkHistory.GetCheckByIDAsync(selected.CheckID);
                    selectedCheck = full ?? selected;
                    PopulateForm(selectedCheck);
                    UpdateButtonVisibility();
                }
            }
        }

        private void dgChecks_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (selectedCheck != null)
            {
                ViewCheckImage(selectedCheck.CheckImage, selectedCheck.CheckImagePath);
            }
        }

        private void ViewImage_Click(object sender, RoutedEventArgs e)
        {
            var check = ((FrameworkElement)sender).DataContext as CheckHistory;
            if (check != null)
            {
                ViewCheckImage(check.CheckImage, check.CheckImagePath);
            }
        }

        /// <summary>
        /// Shows the image for a check.
        /// Priority: 1) in-memory bytes, 2) absolute file path, 3) file name in common folders.
        /// </summary>
        /// <summary>
        /// Displays the check image stored in the database (received as Base64 from the API).
        /// Images are stored in the DB so they work on all devices with a hosted database.
        /// </summary>
        private void ViewCheckImage(byte[] imageBytes, string imagePath)
        {
            try
            {
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    string ext = !string.IsNullOrEmpty(imagePath)
                        ? Path.GetExtension(Path.GetFileName(imagePath))
                        : ".jpg";
                    if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                    string tempPath = Path.Combine(Path.GetTempPath(), $"check_{Guid.NewGuid()}{ext}");
                    File.WriteAllBytes(tempPath, imageBytes);
                    Process.Start(new ProcessStartInfo { FileName = tempPath, UseShellExecute = true });
                    return;
                }

                MessageBox.Show(
                    "Aucune image trouvée dans la base de données pour ce chèque.\n\n" +
                    "Cause possible : l'image a été enregistrée avant la mise à jour du système.\n" +
                    "Sélectionnez ce chèque, cliquez sur « Parcourir » pour choisir l'image et cliquez sur « Modifier » pour la sauvegarder.",
                    "Image non disponible", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de l'image: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateForm(CheckHistory check)
        {
            if (check == null) return;

            txtCheckReference.Text = check.CheckReference;
            txtImagePath.Text = check.CheckImagePath ?? "Image enregistrée dans la base de données";
            selectedImageBytes = check.CheckImage; // Load existing image bytes
            cmbInvoice.SelectedValue = check.InvoiceID;
            txtCheckAmount.Text = check.CheckAmount?.ToString("F2") ?? "";
            dpCheckDate.SelectedDate = check.CheckDate;
            txtBankName.Text = check.BankName;
            txtNotes.Text = check.Notes;

            // Set status
            foreach (ComboBoxItem item in cmbStatus.Items)
            {
                if (item.Content.ToString() == check.CheckStatus)
                {
                    cmbStatus.SelectedItem = item;
                    break;
                }
            }
        }

        private void ClearForm()
        {
            selectedCheck = null;
            selectedImageBytes = null; // Clear image bytes
            txtCheckReference.Clear();
            txtImagePath.Clear();
            cmbInvoice.SelectedIndex = -1;
            txtCheckAmount.Clear();
            dpCheckDate.SelectedDate = DateTime.Now;
            txtBankName.Clear();
            txtNotes.Clear();
            cmbStatus.SelectedIndex = 0;
            dgChecks.SelectedItem = null;
            UpdateButtonVisibility();
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(txtCheckReference.Text))
            {
                MessageBox.Show("Veuillez entrer la référence du chèque.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCheckReference.Focus();
                return false;
            }

            // Only require image for new checks
            if (selectedCheck == null && selectedImageBytes == null)
            {
                MessageBox.Show("Veuillez sélectionner une image du chèque.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cmbInvoice.SelectedValue == null)
            {
                MessageBox.Show("Veuillez sélectionner une facture.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbInvoice.Focus();
                return false;
            }

            if (!string.IsNullOrEmpty(txtCheckAmount.Text))
            {
                if (!decimal.TryParse(txtCheckAmount.Text, out decimal amount) || amount < 0)
                {
                    MessageBox.Show("Veuillez entrer un montant valide.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtCheckAmount.Focus();
                    return false;
                }
            }

            if (dpCheckDate.SelectedDate == null)
            {
                MessageBox.Show("Veuillez sélectionner une date.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                dpCheckDate.Focus();
                return false;
            }

            return true;
        }
    }

    // Invoice Search Dialog Window
    public class InvoiceSearchDialog : Window
    {
        private ComboBox cmbSearchType;
        private TextBox txtSearchInvoice;
        private DataGrid dgInvoiceResults;
        private List<CMainVerifier.InvoiceDisplay> allInvoices;
        private List<CMainVerifier.InvoiceDisplay> filteredInvoices;
        private TextBlock txtResultCount;

        public CMainVerifier.InvoiceDisplay SelectedInvoice { get; private set; }

        public InvoiceSearchDialog(List<CMainVerifier.InvoiceDisplay> invoices)
        {
            allInvoices = invoices;
            filteredInvoices = new List<CMainVerifier.InvoiceDisplay>(invoices);
            InitializeDialog();
        }

        private void InitializeDialog()
        {
            // Window properties
            Title = "Rechercher une Facture";
            Width = 900;
            Height = 650;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResize;
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F9FAFB"));

            // Main container with padding
            var mainBorder = new Border
            {
                Margin = new Thickness(25),
                Background = System.Windows.Media.Brushes.White,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(30)
            };

            // Add drop shadow effect
            mainBorder.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = System.Windows.Media.Colors.Black,
                Opacity = 0.1,
                BlurRadius = 20,
                ShadowDepth = 0
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(25) }); // Spacing
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Search
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) }); // Spacing
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Result count
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); // Spacing
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // DataGrid
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(25) }); // Spacing
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            // Header section
            var headerPanel = CreateHeaderPanel();
            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);

            // Search section
            var searchPanel = CreateSearchPanel();
            Grid.SetRow(searchPanel, 2);
            mainGrid.Children.Add(searchPanel);

            // Result count
            txtResultCount = new TextBlock
            {
                FontSize = 13,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6B7280")),
                Text = $"{filteredInvoices.Count} facture(s) trouvée(s)"
            };
            Grid.SetRow(txtResultCount, 4);
            mainGrid.Children.Add(txtResultCount);

            // DataGrid with border
            var dataGridBorder = new Border
            {
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E5E7EB")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Background = System.Windows.Media.Brushes.White
            };
            dgInvoiceResults = CreateDataGrid();
            dataGridBorder.Child = dgInvoiceResults;
            Grid.SetRow(dataGridBorder, 6);
            mainGrid.Children.Add(dataGridBorder);

            // Buttons
            var buttonPanel = CreateButtonPanel();
            Grid.SetRow(buttonPanel, 8);
            mainGrid.Children.Add(buttonPanel);

            mainBorder.Child = mainGrid;
            Content = mainBorder;

            // Load initial data
            dgInvoiceResults.ItemsSource = filteredInvoices;
        }

        private StackPanel CreateHeaderPanel()
        {
            var panel = new StackPanel();

            // Title
            var title = new TextBlock
            {
                Text = "🔍 Rechercher une Facture",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#111827")),
                Margin = new Thickness(0, 0, 0, 8)
            };
            panel.Children.Add(title);

            // Subtitle
            var subtitle = new TextBlock
            {
                Text = "Sélectionnez la facture associée au chèque",
                FontSize = 14,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6B7280"))
            };
            panel.Children.Add(subtitle);

            return panel;
        }

        private Grid CreateSearchPanel()
        {
            var searchGrid = new Grid();
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(15) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(15) });
            searchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });

            // Search type section
            var typePanel = new StackPanel();
            var typeLabel = new TextBlock
            {
                Text = "Type de Recherche",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#374151")),
                Margin = new Thickness(0, 0, 0, 6)
            };
            typePanel.Children.Add(typeLabel);

            cmbSearchType = CreateStyledComboBox();
            cmbSearchType.Items.Add(new ComboBoxItem { Content = "Numéro de Facture", IsSelected = true });
            cmbSearchType.Items.Add(new ComboBoxItem { Content = "Nom du Client" });
            typePanel.Children.Add(cmbSearchType);
            Grid.SetColumn(typePanel, 0);
            searchGrid.Children.Add(typePanel);

            // Search text section
            var searchPanel = new StackPanel();
            var searchLabel = new TextBlock
            {
                Text = "Rechercher",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#374151")),
                Margin = new Thickness(0, 0, 0, 6)
            };
            searchPanel.Children.Add(searchLabel);

            txtSearchInvoice = CreateStyledTextBox();
            txtSearchInvoice.TextChanged += SearchInvoice_TextChanged;
            searchPanel.Children.Add(txtSearchInvoice);
            Grid.SetColumn(searchPanel, 2);
            searchGrid.Children.Add(searchPanel);

            // Clear button section (aligned to bottom)
            var btnClear = CreateStyledButton("🔄 Réinitialiser", "#F3F4F6", "#374151", "#E5E7EB");
            btnClear.Click += ClearSearch_Click;
            btnClear.VerticalAlignment = VerticalAlignment.Bottom;
            btnClear.Margin = new Thickness(0, 0, 0, 0);
            Grid.SetColumn(btnClear, 4);
            searchGrid.Children.Add(btnClear);

            return searchGrid;
        }

        private ComboBox CreateStyledComboBox()
        {
            var comboBox = new ComboBox
            {
                Height = 40,
                FontSize = 14,
                Padding = new Thickness(12, 10, 12, 10),
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D1D5DB")),
                BorderThickness = new Thickness(1)
            };
            return comboBox;
        }

        private TextBox CreateStyledTextBox()
        {
            var textBox = new TextBox
            {
                Height = 40,
                FontSize = 14,
                Padding = new Thickness(12, 0, 12, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D1D5DB")),
                BorderThickness = new Thickness(1)
            };

            // Add rounded corners style
            var style = new Style(typeof(TextBox));
            var template = new ControlTemplate(typeof(TextBox));
            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "border";
            border.SetValue(Border.BackgroundProperty, new System.Windows.TemplateBindingExtension(TextBox.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new System.Windows.TemplateBindingExtension(TextBox.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new System.Windows.TemplateBindingExtension(TextBox.BorderThicknessProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            border.SetValue(Border.PaddingProperty, new System.Windows.TemplateBindingExtension(TextBox.PaddingProperty));

            var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer));
            scrollViewer.Name = "PART_ContentHost";
            scrollViewer.SetValue(ScrollViewer.FocusableProperty, false);
            scrollViewer.SetValue(ScrollViewer.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(scrollViewer);

            template.VisualTree = border;
            style.Setters.Add(new Setter(TextBox.TemplateProperty, template));
            textBox.Style = style;

            return textBox;
        }

        private Button CreateStyledButton(string content, string bgColor, string fgColor, string borderColor = null)
        {
            var button = new Button
            {
                Content = content,
                Height = 40,
                Padding = new Thickness(20, 10, 20, 10),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(bgColor)),
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(fgColor)),
                BorderThickness = borderColor != null ? new Thickness(1) : new Thickness(0),
                BorderBrush = borderColor != null ? new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(borderColor)) : null
            };

            // Add rounded corners style
            var style = new Style(typeof(Button));
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new System.Windows.TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            border.SetValue(Border.PaddingProperty, new System.Windows.TemplateBindingExtension(Button.PaddingProperty));
            border.SetValue(Border.BorderBrushProperty, new System.Windows.TemplateBindingExtension(Button.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new System.Windows.TemplateBindingExtension(Button.BorderThicknessProperty));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(contentPresenter);

            template.VisualTree = border;
            style.Setters.Add(new Setter(Button.TemplateProperty, template));
            button.Style = style;

            return button;
        }

        private DataGrid CreateDataGrid()
        {
            var dataGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserResizeRows = false,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.None,
                Background = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                RowHeight = 50,
                FontSize = 13,
                AlternatingRowBackground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F9FAFB"))
            };

            // Header style
            var headerStyle = new Style(typeof(DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty,
                new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F9FAFB"))));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty,
                new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#111827"))));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.SemiBold));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontSizeProperty, 13.0));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.PaddingProperty, new Thickness(12, 10, 12, 10)));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 0, 2)));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty,
                new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E5E7EB"))));
            dataGrid.ColumnHeaderStyle = headerStyle;

            // Row style
            var rowStyle = new Style(typeof(DataGridRow));
            rowStyle.Setters.Add(new Setter(DataGridRow.BackgroundProperty, System.Windows.Media.Brushes.White));
            var mouseOverTrigger = new Trigger { Property = DataGridRow.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(DataGridRow.BackgroundProperty,
                new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F3F4F6"))));
            rowStyle.Triggers.Add(mouseOverTrigger);

            var selectedTrigger = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(DataGridRow.BackgroundProperty,
                new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#DBEAFE"))));
            selectedTrigger.Setters.Add(new Setter(DataGridRow.ForegroundProperty,
                new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E40AF"))));
            rowStyle.Triggers.Add(selectedTrigger);
            dataGrid.RowStyle = rowStyle;

            // Cell style
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderThicknessProperty, new Thickness(0)));
            cellStyle.Setters.Add(new Setter(DataGridCell.PaddingProperty, new Thickness(12, 0, 12, 0)));
            cellStyle.Setters.Add(new Setter(DataGridCell.FocusVisualStyleProperty, null));
            var cellTemplate = new ControlTemplate(typeof(DataGridCell));
            var cellBorder = new FrameworkElementFactory(typeof(Border));
            cellBorder.SetValue(Border.BackgroundProperty, new System.Windows.TemplateBindingExtension(DataGridCell.BackgroundProperty));
            cellBorder.SetValue(Border.PaddingProperty, new System.Windows.TemplateBindingExtension(DataGridCell.PaddingProperty));
            var cellContent = new FrameworkElementFactory(typeof(ContentPresenter));
            cellContent.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            cellBorder.AppendChild(cellContent);
            cellTemplate.VisualTree = cellBorder;
            cellStyle.Setters.Add(new Setter(DataGridCell.TemplateProperty, cellTemplate));
            dataGrid.CellStyle = cellStyle;

            // Columns
            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "ID",
                Binding = new System.Windows.Data.Binding("InvoiceID"),
                Width = 70
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Numéro de Facture",
                Binding = new System.Windows.Data.Binding("InvoiceNumber"),
                Width = 160
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Nom du Client",
                Binding = new System.Windows.Data.Binding("ClientName"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                MinWidth = 180
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Détails Complets",
                Binding = new System.Windows.Data.Binding("DisplayText"),
                Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                MinWidth = 250
            });

            dataGrid.MouseDoubleClick += DataGrid_MouseDoubleClick;

            return dataGrid;
        }

        private StackPanel CreateButtonPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // Select button
            var btnSelect = CreateStyledButton("✓ Sélectionner", "#3B82F6", "#FFFFFF", null);
            btnSelect.Width = 150;
            btnSelect.Margin = new Thickness(0, 0, 10, 0);
            btnSelect.Click += SelectInvoice_Click;
            panel.Children.Add(btnSelect);

            // Cancel button
            var btnCancel = CreateStyledButton("✕ Annuler", "#F3F4F6", "#374151", "#D1D5DB");
            btnCancel.Width = 130;
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };
            panel.Children.Add(btnCancel);

            return panel;
        }

        private void SearchInvoice_TextChanged(object sender, TextChangedEventArgs e)
        {
            PerformSearch();
        }

        private void PerformSearch()
        {
            string searchText = txtSearchInvoice.Text.Trim().ToLower();
            var selectedSearchType = (cmbSearchType.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (string.IsNullOrEmpty(searchText))
            {
                filteredInvoices = new List<CMainVerifier.InvoiceDisplay>(allInvoices);
            }
            else
            {
                if (selectedSearchType == "Numéro de Facture")
                {
                    filteredInvoices = allInvoices
                        .Where(i => i.InvoiceNumber.ToLower().Contains(searchText))
                        .ToList();
                }
                else // Search by Client Name
                {
                    filteredInvoices = allInvoices
                        .Where(i => i.ClientName.ToLower().Contains(searchText))
                        .ToList();
                }
            }

            dgInvoiceResults.ItemsSource = null;
            dgInvoiceResults.ItemsSource = filteredInvoices;

            // Update result count
            txtResultCount.Text = filteredInvoices.Count == 0 ? "Aucune facture trouvée" :
                filteredInvoices.Count == 1 ? "1 facture trouvée" :
                $"{filteredInvoices.Count} factures trouvées";
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearchInvoice.Clear();
            cmbSearchType.SelectedIndex = 0;
            filteredInvoices = new List<CMainVerifier.InvoiceDisplay>(allInvoices);
            dgInvoiceResults.ItemsSource = null;
            dgInvoiceResults.ItemsSource = filteredInvoices;
            txtResultCount.Text = $"{filteredInvoices.Count} facture(s) trouvée(s)";
        }

        private void SelectInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (dgInvoiceResults.SelectedItem != null)
            {
                SelectedInvoice = dgInvoiceResults.SelectedItem as CMainVerifier.InvoiceDisplay;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner une facture.",
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgInvoiceResults.SelectedItem != null)
            {
                SelectedInvoice = dgInvoiceResults.SelectedItem as CMainVerifier.InvoiceDisplay;
                DialogResult = true;
                Close();
            }
        }
    }
}