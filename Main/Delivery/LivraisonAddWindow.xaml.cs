using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GestionComerce.Main.Delivery
{
    public partial class LivraisonAddWindow : Window
    {
        private MainWindow main;
        private User u;
        private List<Livreur> livreurs;
        private List<Operation> operations;
        private List<Operation> selectedOperations;
        private List<PaymentMethod> paymentMethods;

        // ── Autocomplete: existing inventory articles ──────────────────────────
        private List<Article> allArticles = new List<Article>();

        // ── Invoice feature fields ─────────────────────────────────────────────
        private List<InvoiceDisplayItem> allInvoices = new List<InvoiceDisplayItem>();
        private Invoice selectedInvoice = null;
        private List<Invoice.InvoiceArticle> selectedInvoiceArticles = new List<Invoice.InvoiceArticle>();

        /// <summary>
        /// Lightweight projection of Invoice used for DataGrid display,
        /// including a computed ArticleCount column.
        /// </summary>
        private class InvoiceDisplayItem
        {
            public int InvoiceID { get; set; }
            public string InvoiceNumber { get; set; }
            public DateTime InvoiceDate { get; set; }
            public string ClientName { get; set; }
            public string ClientPhone { get; set; }
            public string ClientAddress { get; set; }
            public decimal TotalTTC { get; set; }
            public decimal TotalAfterRemise { get; set; }
            public int ArticleCount { get; set; }
            // Keep a reference to the full Invoice for later use
            public Invoice Source { get; set; }
        }

        public event EventHandler LivraisonAdded;

        public LivraisonAddWindow(MainWindow main, User u)
        {
            InitializeComponent();
            this.main = main;
            this.u = u;
            this.selectedOperations = new List<Operation>();

            Loaded += LivraisonAddWindow_Loaded;
        }

        private async void LivraisonAddWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DateLivraisonPrevue.SelectedDate = DateTime.Now.AddDays(1);
            await LoadLivreursAsync();
            await LoadPaymentMethodsAsync();
            await LoadOperationsAsync();
            await LoadInvoicesAsync();
            await LoadArticlesForSuggestAsync();
        }

        private async Task LoadLivreursAsync()
        {
            try
            {
                Livreur livreur = new Livreur();
                livreurs = await livreur.GetLivreursDisponiblesAsync();
                CmbLivreur.ItemsSource = livreurs;

                if (livreurs.Count > 0)
                {
                    CmbLivreur.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des livreurs: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadPaymentMethodsAsync()
        {
            try
            {
                PaymentMethod paymentMethod = new PaymentMethod();
                paymentMethods = await paymentMethod.GetPaymentMethodsAsync();

                CmbModePaiement.ItemsSource = paymentMethods;
                CmbModePaiement.DisplayMemberPath = "PaymentMethodName";
                CmbModePaiement.SelectedValuePath = "PaymentMethodID";

                if (paymentMethods.Count > 0)
                {
                    var defaultPayment = paymentMethods.FirstOrDefault(p =>
                        p.PaymentMethodName.Equals("Espèces", StringComparison.OrdinalIgnoreCase) ||
                        p.PaymentMethodName.Equals("Cash", StringComparison.OrdinalIgnoreCase) ||
                        p.PaymentMethodName.Equals("À la livraison", StringComparison.OrdinalIgnoreCase));

                    if (defaultPayment != null)
                    {
                        CmbModePaiement.SelectedItem = defaultPayment;
                    }
                    else
                    {
                        CmbModePaiement.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des modes de paiement: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadOperationsAsync()
        {
            try
            {
                Operation operation = new Operation();
                var allOperations = await operation.GetOperationsAsync();

                operations = allOperations
                    .Where(op =>
                        op.Etat &&
                        !op.Reversed &&
                        op.OperationType.StartsWith("Vente", StringComparison.OrdinalIgnoreCase) &&
                        op.OperationType != "VenteLiv" &&
                        op.OperationType != "Livraison Groupée" &&
                        !IsOperationAlreadyInDelivery(op.OperationID))
                    .OrderByDescending(op => op.DateOperation)
                    .ToList();

                DgOperations.ItemsSource = null;
                DgOperations.ItemsSource = operations;

                if (operations.Count == 0)
                {
                    MessageBox.Show(
                        "Aucune opération de vente disponible pour créer une livraison.\n\n" +
                        "Assurez-vous d'avoir des ventes actives qui ne sont pas déjà en livraison.",
                        "Information",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des opérations: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsOperationAlreadyInDelivery(int operationId)
        {
            try
            {
                using (var connection = new System.Data.SqlClient.SqlConnection(
                    "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;"))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM Livraison WHERE OperationID = @OperationID AND Etat = 1";
                    using (var cmd = new System.Data.SqlClient.SqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@OperationID", operationId);
                        int count = (int)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private async void BtnRefreshOperations_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnRefreshOperations.IsEnabled = false;
                BtnRefreshOperations.Content = "⏳ Chargement...";

                selectedOperations.Clear();
                TxtSelectedCount.Text = "0";
                TxtTotalCommande.Text = "0.00";

                await LoadOperationsAsync();

                BtnRefreshOperations.Content = "✅ Actualisé";
                await Task.Delay(1000);
                BtnRefreshOperations.Content = "🔄 Actualiser";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'actualisation: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnRefreshOperations.Content = "🔄 Actualiser";
            }
            finally
            {
                BtnRefreshOperations.IsEnabled = true;
            }
        }

        private void ChkSelect_Click(object sender, RoutedEventArgs e)
        {
            UpdateSelectedOperations();
        }

        private void ChkSelectAll_Click(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            if (checkbox != null)
            {
                SetAllCheckboxes(checkbox.IsChecked == true);
            }
        }

        private void SetAllCheckboxes(bool isChecked)
        {
            foreach (var item in DgOperations.Items)
            {
                var row = DgOperations.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (row != null)
                {
                    var checkbox = FindVisualChild<CheckBox>(row);
                    if (checkbox != null)
                    {
                        checkbox.IsChecked = isChecked;
                    }
                }
            }
            UpdateSelectedOperations();
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        private void UpdateSelectedOperations()
        {
            selectedOperations.Clear();

            foreach (var item in DgOperations.Items)
            {
                var operation = item as Operation;
                if (operation == null) continue;

                var row = DgOperations.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (row != null)
                {
                    var checkbox = FindVisualChild<CheckBox>(row);
                    if (checkbox != null && checkbox.IsChecked == true)
                    {
                        selectedOperations.Add(operation);
                    }
                }
            }

            TxtSelectedCount.Text = selectedOperations.Count.ToString();
            CalculateTotalCommande();

            // ── Auto-fill client + total from selected operations ──────────────
            // Only auto-fill when no invoice is already selected (don't overwrite invoice data)
            if (selectedInvoice == null && selectedOperations.Count > 0)
            {
                AutoFillFromOperations();
            }
        }

        /// <summary>
        /// Fetches client info for the first selected operation that has a ClientID,
        /// and fills the client fields + TotalCommande.
        /// </summary>
        private async void AutoFillFromOperations()
        {
            try
            {
                // Total = sum of (PrixOperation - Remise) across selected operations
                decimal total = selectedOperations.Sum(op => op.PrixOperation - op.Remise);
                TxtTotalCommande.Text = total.ToString("N2");

                // Find the first operation with a client
                var opWithClient = selectedOperations.FirstOrDefault(op => op.ClientID.HasValue);
                if (opWithClient == null) return;

                // Load clients and find the matching one
                Client clientObj = new Client();
                var allClients = await clientObj.GetClientsAsync();
                var client = allClients.FirstOrDefault(c => c.ClientID == opWithClient.ClientID.Value);

                if (client != null)
                {
                    // Only fill if the field is currently empty (don't overwrite user edits)
                    if (string.IsNullOrWhiteSpace(TxtClientNom.Text))
                        TxtClientNom.Text = client.Nom ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(TxtClientTelephone.Text))
                        TxtClientTelephone.Text = client.Telephone ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(TxtAdresse.Text))
                        TxtAdresse.Text = client.Adresse ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutoFillFromOperations error: {ex.Message}");
            }
        }

        private void CalculateTotalCommande()
        {
            decimal total = 0;
            foreach (var op in selectedOperations)
            {
                total += op.PrixOperation - op.Remise;
            }
            TxtTotalCommande.Text = total.ToString("N2");
        }

        private void CmbZoneLivraison_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbZoneLivraison.SelectedItem != null)
            {
                ComboBoxItem selected = (ComboBoxItem)CmbZoneLivraison.SelectedItem;
                TxtFraisLivraison.Text = selected.Tag.ToString();
            }
        }

        private async void BtnCreer_Click(object sender, RoutedEventArgs e)
        {
            // ── Validation: need at least one source ────────────────────────
            bool hasInvoice    = selectedInvoice != null;
            bool hasOperations = selectedOperations.Count > 0;
            bool hasLibre      = libreArticles.Count > 0;

            if (!hasInvoice && !hasOperations && !hasLibre)
            {
                MessageBox.Show(
                    "Veuillez sélectionner au moins une opération, une facture, ou ajouter des articles libres.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtClientNom.Text))
            {
                MessageBox.Show("Le nom du client est obligatoire.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtClientNom.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtClientTelephone.Text))
            {
                MessageBox.Show("Le téléphone du client est obligatoire.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtClientTelephone.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtAdresse.Text))
            {
                MessageBox.Show("L'adresse de livraison est obligatoire.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtAdresse.Focus();
                return;
            }

            // Include libre articles in the total if no other source already sets it
            if (hasLibre && !hasOperations && !hasInvoice)
                TxtTotalCommande.Text = libreArticles.Sum(a => a.Total).ToString("N2");
            else if (hasLibre && decimal.TryParse(TxtTotalCommande.Text, out decimal existingTotal))
                TxtTotalCommande.Text = (existingTotal + libreArticles.Sum(a => a.Total)).ToString("N2");

            if (!decimal.TryParse(TxtTotalCommande.Text, out decimal totalCommande) || totalCommande <= 0)
            {
                MessageBox.Show("Le montant total de la commande est invalide.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DateLivraisonPrevue.SelectedDate.HasValue)
            {
                MessageBox.Show("La date de livraison prévue est obligatoire.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DateLivraisonPrevue.Focus();
                return;
            }

            try
            {
                BtnCreer.IsEnabled = false;
                BtnCreer.Content = "⏳ Création en cours...";

                // Update selected operations type to VenteLiv (only when operations were selected)
                if (hasOperations)
                    await UpdateOperationsTypeToVenteLivAsync(selectedOperations);

                // Create a VenteLiv operation for libre articles and record its ID
                int libreOperationId = 0;
                if (hasLibre)
                {
                    decimal libreTotal = libreArticles.Sum(a => a.Total);
                    Operation libreOp = new Operation
                    {
                        OperationType = "VenteLiv",
                        PrixOperation = libreTotal,
                        Remise        = 0,
                        UserID        = u.UserID
                    };
                    libreOperationId = await libreOp.InsertOperationAsync();
                }

                // OperationID: prefer selected operation, fall back to libre operation
                int operationId = hasOperations
                    ? selectedOperations.First().OperationID
                    : libreOperationId;

                // ClientID: from operations if available
                int? clientId = hasOperations
                    ? selectedOperations.FirstOrDefault(o => o.ClientID.HasValue)?.ClientID
                    : null;

                // Notes: describe the source
                var notesParts = new System.Text.StringBuilder();
                if (hasOperations)
                    notesParts.AppendLine($"Livraison groupée de {selectedOperations.Count} commande(s): {string.Join(", ", selectedOperations.Select(o => $"Op#{o.OperationID}"))}");
                if (hasInvoice)
                    notesParts.AppendLine($"Facture {selectedInvoice.InvoiceNumber}");
                if (hasLibre)
                {
                    notesParts.AppendLine("Articles libres:");
                    foreach (var al in libreArticles)
                        notesParts.AppendLine($"  - {al.Nom} x{al.Quantite} @ {al.PrixUnitaire:N2} DH");
                }
                if (!string.IsNullOrWhiteSpace(TxtNotes.Text))
                    notesParts.AppendLine(TxtNotes.Text.Trim());
                string notes = notesParts.ToString().Trim();

                // Create delivery
                Livraison livraison = new Livraison
                {
                    OperationID = operationId,
                    ClientID = clientId,
                    ClientNom = TxtClientNom.Text.Trim(),
                    ClientTelephone = TxtClientTelephone.Text.Trim(),
                    AdresseLivraison = TxtAdresse.Text.Trim(),
                    Ville = TxtVille.Text.Trim(),
                    CodePostal = TxtCodePostal.Text.Trim(),
                    ZoneLivraison = CmbZoneLivraison.SelectedItem != null
                        ? ((ComboBoxItem)CmbZoneLivraison.SelectedItem).Content.ToString()
                        : null,
                    FraisLivraison = decimal.Parse(TxtFraisLivraison.Text),
                    DateLivraisonPrevue = DateLivraisonPrevue.SelectedDate.Value,
                    LivreurID = CmbLivreur.SelectedValue != null
                        ? (int?)CmbLivreur.SelectedValue
                        : null,
                    LivreurNom = CmbLivreur.SelectedItem != null
                        ? ((Livreur)CmbLivreur.SelectedItem).NomComplet
                        : null,
                    Statut = CmbStatut.SelectedItem != null
                        ? ((ComboBoxItem)CmbStatut.SelectedItem).Tag.ToString()
                        : "en_attente",
                    Notes = notes,
                    TotalCommande = totalCommande,
                    ModePaiement = CmbModePaiement.SelectedItem != null
                        ? ((PaymentMethod)CmbModePaiement.SelectedItem).PaymentMethodName
                        : "Espèces",
                    PaiementStatut = "non_paye"
                };

                int livraisonID = await livraison.InsertLivraisonAsync();

                if (livraisonID > 0)
                {
                    string livreurNom = CmbLivreur.SelectedItem != null
                        ? ((Livreur)CmbLivreur.SelectedItem).NomComplet
                        : "Non assigné";

                    string heureLivraison = CmbHeureLivraison.SelectedItem != null
                        ? ((ComboBoxItem)CmbHeureLivraison.SelectedItem).Content.ToString()
                        : "";

                    // Build prebuilt article list for ticket display
                    List<ArticleDisplay> prebuiltArticleList = null;
                    bool needPrebuilt = (hasInvoice && selectedInvoiceArticles.Count > 0 && !hasOperations) || hasLibre;
                    if (needPrebuilt)
                    {
                        prebuiltArticleList = new List<ArticleDisplay>();
                        // Invoice articles (when no operation selected)
                        if (hasInvoice && selectedInvoiceArticles.Count > 0 && !hasOperations)
                        {
                            prebuiltArticleList.AddRange(selectedInvoiceArticles.Select(a => new ArticleDisplay
                            {
                                ArticleName = a.ArticleName ?? "",
                                Quantity    = (int)Math.Max(1, a.Quantite),
                                UnitPrice   = a.PrixUnitaire
                            }));
                        }
                        // Libre articles always added
                        foreach (var al in libreArticles)
                            prebuiltArticleList.Add(new ArticleDisplay
                            {
                                ArticleName = al.Nom,
                                Quantity    = al.Quantite,
                                UnitPrice   = al.PrixUnitaire
                            });
                    }

                    DeliveryTicketWindow ticketWindow = new DeliveryTicketWindow(
                        livraison,
                        hasOperations ? selectedOperations : new List<Operation>(),
                        livreurNom,
                        heureLivraison,
                        prebuiltArticleList
                    );
                    ticketWindow.ShowDialog();

                    LivraisonAdded?.Invoke(this, EventArgs.Empty);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Erreur lors de la création de la livraison.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnCreer.IsEnabled = true;
                BtnCreer.Content = "✅ Créer la Livraison";
            }
        }

        private async Task UpdateOperationsTypeToVenteLivAsync(List<Operation> operations)
        {
            try
            {
                using (var connection = new System.Data.SqlClient.SqlConnection(
                    "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;"))
                {
                    await connection.OpenAsync();

                    foreach (var op in operations)
                    {
                        string query = @"UPDATE Operation 
                                       SET OperationType = 'VenteLiv' 
                                       WHERE OperationID = @OperationID";

                        using (var cmd = new System.Data.SqlClient.SqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@OperationID", op.OperationID);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la mise à jour des types d'opérations: {ex.Message}");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  INVOICE SECTION
        // ════════════════════════════════════════════════════════════════════════

        private async Task LoadInvoicesAsync()
        {
            try
            {
                var repo = new GestionComerce.Main.Facturation.InvoiceRepository(
                    "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;");

                var invoices = await repo.GetAllInvoicesAsync(includeDeleted: false);

                allInvoices = invoices
                    .Where(inv => !inv.IsReversed && inv.EtatFacture != 0)
                    .Select(inv => new InvoiceDisplayItem
                    {
                        InvoiceID = inv.InvoiceID,
                        InvoiceNumber = inv.InvoiceNumber,
                        InvoiceDate = inv.InvoiceDate,
                        ClientName = inv.ClientName,
                        ClientPhone = inv.ClientPhone,
                        ClientAddress = inv.ClientAddress,
                        TotalTTC = inv.TotalTTC,
                        TotalAfterRemise = inv.TotalAfterRemise,
                        ArticleCount = inv.Articles?.Count ?? 0,
                        Source = inv
                    })
                    .OrderByDescending(inv => inv.InvoiceDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                // Non-blocking — invoice selection is optional
                System.Diagnostics.Debug.WriteLine($"LoadInvoicesAsync error: {ex.Message}");
            }
        }

        private void TxtInvoiceSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Live search: show results as user types (minimum 2 chars)
            string term = TxtInvoiceSearch.Text.Trim();
            if (term.Length >= 2)
                ShowInvoiceResults(term);
            else if (term.Length == 0)
                DgInvoices.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void BtnSearchInvoices_Click(object sender, RoutedEventArgs e)
        {
            string term = TxtInvoiceSearch.Text.Trim();
            ShowInvoiceResults(string.IsNullOrEmpty(term) ? null : term);
        }

        private void ShowInvoiceResults(string term)
        {
            IEnumerable<InvoiceDisplayItem> filtered = allInvoices;

            if (!string.IsNullOrEmpty(term))
            {
                string lower = term.ToLower();
                filtered = allInvoices.Where(inv =>
                    (inv.InvoiceNumber != null && inv.InvoiceNumber.ToLower().Contains(lower)) ||
                    (inv.ClientName != null && inv.ClientName.ToLower().Contains(lower)));
            }

            var results = filtered.ToList();
            DgInvoices.ItemsSource = results;
            DgInvoices.Visibility = results.Count > 0
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;
        }

        private async void DgInvoices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = DgInvoices.SelectedItem as InvoiceDisplayItem;
            if (item == null) return;

            // Collapse the results grid immediately after selection
            DgInvoices.Visibility = System.Windows.Visibility.Collapsed;

            try
            {
                var repo = new GestionComerce.Main.Facturation.InvoiceRepository(
                    "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;");

                var fullInvoice = await repo.GetInvoiceByIdAsync(item.InvoiceID);
                if (fullInvoice == null)
                {
                    MessageBox.Show("Impossible de charger les détails de la facture.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                selectedInvoice = fullInvoice;
                selectedInvoiceArticles = fullInvoice.Articles ?? new List<Invoice.InvoiceArticle>();

                // ── Resolve the total ──────────────────────────────────────────
                // TotalAfterRemise can be negative when a credit/remise exceeds TTC.
                // Always take the absolute value — a delivery amount is never negative.
                decimal resolvedTotal = Math.Abs(fullInvoice.TotalAfterRemise);

                if (resolvedTotal == 0 && selectedInvoiceArticles.Count > 0)
                {
                    decimal articlesSum = selectedInvoiceArticles.Sum(a => a.TotalTTC);
                    resolvedTotal = Math.Abs(articlesSum);
                }

                if (resolvedTotal == 0)
                    resolvedTotal = Math.Abs(fullInvoice.TotalTTC);

                // ── Show selected invoice banner ───────────────────────────────
                TxtSelectedInvoiceInfo.Text =
                    $"Facture {fullInvoice.InvoiceNumber}  —  {fullInvoice.ClientName}  —  {resolvedTotal:N2} DH";
                TxtSelectedInvoiceArticles.Text =
                    $"{selectedInvoiceArticles.Count} article(s) extrait(s)";

                SelectedInvoiceBanner.Visibility = System.Windows.Visibility.Visible;
                BtnClearInvoice.Visibility = System.Windows.Visibility.Visible;

                // ── Populate extracted articles grid ───────────────────────────
                DgExtractedArticles.ItemsSource = selectedInvoiceArticles.Count > 0
                    ? selectedInvoiceArticles
                    : null;
                ExtractedArticlesPanel.Visibility = selectedInvoiceArticles.Count > 0
                    ? System.Windows.Visibility.Visible
                    : System.Windows.Visibility.Collapsed;

                // ── Auto-fill client fields (editable) ────────────────────────
                if (!string.IsNullOrWhiteSpace(fullInvoice.ClientName))
                    TxtClientNom.Text = fullInvoice.ClientName;

                if (!string.IsNullOrWhiteSpace(fullInvoice.ClientPhone))
                    TxtClientTelephone.Text = fullInvoice.ClientPhone;

                if (!string.IsNullOrWhiteSpace(fullInvoice.ClientAddress))
                    TxtAdresse.Text = fullInvoice.ClientAddress;

                // ── Auto-fill TotalCommande ────────────────────────────────────
                // TxtTotalCommande is IsReadOnly — set via dispatcher to ensure the UI updates
                TxtTotalCommande.Dispatcher.Invoke(() =>
                {
                    TxtTotalCommande.IsReadOnly = false;
                    TxtTotalCommande.Text = resolvedTotal.ToString("N2");
                    TxtTotalCommande.IsReadOnly = true;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de la facture: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearInvoice_Click(object sender, RoutedEventArgs e)
        {
            selectedInvoice = null;
            selectedInvoiceArticles = new List<Invoice.InvoiceArticle>();

            TxtInvoiceSearch.Text = string.Empty;
            DgInvoices.ItemsSource = null;
            DgInvoices.Visibility = System.Windows.Visibility.Collapsed;
            DgExtractedArticles.ItemsSource = null;
            ExtractedArticlesPanel.Visibility = System.Windows.Visibility.Collapsed;
            SelectedInvoiceBanner.Visibility = System.Windows.Visibility.Collapsed;
            BtnClearInvoice.Visibility = System.Windows.Visibility.Collapsed;

            // Clear the auto-filled fields
            TxtClientNom.Text = string.Empty;
            TxtClientTelephone.Text = string.Empty;
            TxtAdresse.Text = string.Empty;
            TxtTotalCommande.Text = "0.00";
        }

        // ════════════════════════════════════════════════════════════════════════
        //  ARTICLES LIBRES
        // ════════════════════════════════════════════════════════════════════════

        private class ArticleLibre
        {
            public string Nom          { get; set; }
            public int    Quantite     { get; set; }
            public decimal PrixUnitaire { get; set; }
            public decimal Total        => Quantite * PrixUnitaire;
        }

        private readonly List<ArticleLibre> libreArticles = new List<ArticleLibre>();

        private void BtnAddLibre_Click(object sender, RoutedEventArgs e)
        {
            string nom = TxtLibreNom.Text.Trim();
            if (string.IsNullOrWhiteSpace(nom))
            {
                MessageBox.Show("Veuillez entrer une désignation.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtLibreNom.Focus();
                return;
            }

            if (!int.TryParse(TxtLibreQte.Text, out int qty) || qty <= 0)
            {
                MessageBox.Show("La quantité doit être un entier positif.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtLibreQte.Focus();
                return;
            }

            if (!decimal.TryParse(TxtLibrePrix.Text, out decimal prix) || prix < 0)
            {
                MessageBox.Show("Le prix doit être un nombre positif.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtLibrePrix.Focus();
                return;
            }

            var art = new ArticleLibre { Nom = nom, Quantite = qty, PrixUnitaire = prix };
            libreArticles.Add(art);
            RebuildLibrePanel();

            TxtLibreNom.Text  = string.Empty;
            TxtLibreQte.Text  = "1";
            TxtLibrePrix.Text = "0.00";
            TxtLibreNom.Focus();
        }

        private void RemoveLibreArticle(ArticleLibre art)
        {
            libreArticles.Remove(art);
            RebuildLibrePanel();
        }

        private void RebuildLibrePanel()
        {
            LibreArticlesPanel.Children.Clear();

            foreach (var art in libreArticles)
            {
                var captured = art;
                var row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(60) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(100) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(100) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(40) });

                AddLibreCell(row, art.Nom,                                    0, bold: true);
                AddLibreCell(row, art.Quantite.ToString(),                    1, center: true);
                AddLibreCell(row, $"{art.PrixUnitaire:N2} DH",               2, right: true);
                AddLibreCell(row, $"{art.Total:N2} DH",                      3, right: true, green: true);

                var del = new Button
                {
                    Content = "✕",
                    FontSize = 12,
                    Background = System.Windows.Media.Brushes.Transparent,
                    Foreground = System.Windows.Media.Brushes.Red,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                del.Click += (s, e) => RemoveLibreArticle(captured);
                System.Windows.Controls.Grid.SetColumn(del, 4);
                row.Children.Add(del);

                LibreArticlesPanel.Children.Add(row);
            }

            bool hasAny = libreArticles.Count > 0;
            LibreListContainer.Visibility = hasAny ? Visibility.Visible : Visibility.Collapsed;
            if (hasAny)
                TxtLibreTotal.Text = $"Total articles libres : {libreArticles.Sum(a => a.Total):N2} DH";

            // Recalculate overall total when no operation/invoice already sets it
            if (selectedOperations.Count == 0 && selectedInvoice == null && hasAny)
                TxtTotalCommande.Text = libreArticles.Sum(a => a.Total).ToString("N2");
        }

        private void AddLibreCell(Grid grid, string text, int col,
            bool bold = false, bool center = false, bool right = false, bool green = false)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = 12,
                Padding = new Thickness(8, 4, 8, 4),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                TextAlignment = center ? TextAlignment.Center : right ? TextAlignment.Right : TextAlignment.Left,
                Foreground = green
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 163, 74))
                    : System.Windows.Media.Brushes.Black
            };
            System.Windows.Controls.Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  AUTOCOMPLETE — TxtLibreNom
        // ════════════════════════════════════════════════════════════════════════

        private async Task LoadArticlesForSuggestAsync()
        {
            try
            {
                allArticles = await new Article().GetArticlesAsync() ?? new List<Article>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadArticlesForSuggest error: {ex.Message}");
            }
        }

        private void TxtLibreNom_TextChanged(object sender, TextChangedEventArgs e)
        {
            string term = TxtLibreNom.Text.Trim();
            if (term.Length < 2)
            {
                ArticleSuggestPopup.IsOpen = false;
                return;
            }

            string lower = term.ToLower();
            var matches = allArticles
                .Where(a => a.Etat && a.ArticleName != null && a.ArticleName.ToLower().Contains(lower))
                .Take(10)
                .ToList();

            if (matches.Count == 0)
            {
                ArticleSuggestPopup.IsOpen = false;
                return;
            }

            ArticleSuggestList.ItemsSource = matches;
            ArticleSuggestPopup.IsOpen = true;
        }

        private async void TxtLibreNom_LostFocus(object sender, RoutedEventArgs e)
        {
            // Delay lets SelectionChanged fire before the popup closes
            await Task.Delay(150);
            ArticleSuggestPopup.IsOpen = false;
        }

        private void ArticleSuggestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = ArticleSuggestList.SelectedItem as Article;
            if (selected == null) return;

            TxtLibreNom.Text = selected.ArticleName;
            TxtLibrePrix.Text = selected.PrixVente.ToString("N2");
            ArticleSuggestPopup.IsOpen = false;
            ArticleSuggestList.SelectedItem = null;
            TxtLibreQte.Focus();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  INPUT VALIDATION
        // ════════════════════════════════════════════════════════════════════════

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = e.Text.Length == 0 || !char.IsDigit(e.Text[0]);
        }

        private void DecimalOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length == 0) { e.Handled = true; return; }
            char c = e.Text[0];
            var tb = sender as TextBox;
            bool hasDot = tb != null && tb.Text.Contains('.');
            e.Handled = !(char.IsDigit(c) || (c == '.' && !hasDot));
        }

        // ════════════════════════════════════════════════════════════════════════

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Êtes-vous sûr de vouloir annuler? Les données non enregistrées seront perdues.",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }
    }
}