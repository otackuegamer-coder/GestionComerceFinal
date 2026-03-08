using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Superete; // For Facture class
using GestionComerce;

namespace GestionComerce.Main.Delivery
{
    // Helper class — implements INotifyPropertyChanged so Total auto-updates in the grid
    public class ArticleDisplay : INotifyPropertyChanged
    {
        private string _articleName;
        private int _quantity;
        private decimal _unitPrice;

        public string ArticleName
        {
            get => _articleName;
            set { _articleName = value; OnPropertyChanged(nameof(ArticleName)); }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(Total));
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                _unitPrice = value;
                OnPropertyChanged(nameof(UnitPrice));
                OnPropertyChanged(nameof(Total));
            }
        }

        // Computed — never set directly
        public decimal Total => UnitPrice * Quantity;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class LivraisonDetailsWindow : Window
    {
        private MainWindow main;
        private User u;
        private Livraison livraison;
        private Operation operation;

        // Keeps the editable article list in memory (bound to DgArticles)
        private ObservableCollection<ArticleDisplay> articleList = new ObservableCollection<ArticleDisplay>();

        // Event pour notifier que la livraison a été mise à jour
        public event EventHandler LivraisonUpdated;

        public LivraisonDetailsWindow(MainWindow main, User u, Livraison livraison)
        {
            InitializeComponent();
            this.main = main;
            this.u = u;
            this.livraison = livraison;

            Loaded += LivraisonDetailsWindow_Loaded;
        }

        private async void LivraisonDetailsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadLivraisonDetailsAsync();
        }

        // Charger les détails de la livraison
        private async Task LoadLivraisonDetailsAsync()
        {
            try
            {
                // Recharger les données depuis la base
                Livraison fresh = new Livraison();
                livraison = await fresh.GetLivraisonByIDAsync(livraison.LivraisonID);

                if (livraison == null)
                {
                    MessageBox.Show("Livraison introuvable.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                    return;
                }

                // Header
                TxtHeaderTitle.Text = $"📦 Détails de la Livraison #{livraison.LivraisonID}";
                TxtHeaderSubtitle.Text = $"Créée le {livraison.DateCreation:dd/MM/yyyy à HH:mm}";

                // Statut Badge
                UpdateStatutBadge(livraison.Statut);

                // Charger les infos de l'opération
                await LoadOperationInfoAsync();

                // Client Info
                TxtClientNom.Text = livraison.ClientNom ?? "-";
                TxtClientTelephone.Text = livraison.ClientTelephone ?? "-";
                TxtClientID.Text = livraison.ClientID?.ToString() ?? "-";

                // Adresse
                TxtAdresse.Text = livraison.AdresseLivraison ?? "-";
                TxtVille.Text = livraison.Ville ?? "-";
                TxtCodePostal.Text = livraison.CodePostal ?? "-";
                TxtZone.Text = livraison.ZoneLivraison ?? "-";

                // Détails Livraison
                TxtDatePrevue.Text = livraison.DateLivraisonPrevue?.ToString("dd/MM/yyyy HH:mm") ?? "-";
                TxtDateEffective.Text = livraison.DateLivraisonEffective?.ToString("dd/MM/yyyy HH:mm") ?? "-";
                TxtLivreur.Text = livraison.LivreurNom ?? "Non assigné";
                TxtModePaiement.Text = livraison.ModePaiement ?? "-";
                TxtNotes.Text = string.IsNullOrWhiteSpace(livraison.Notes) ? "Aucune note" : livraison.Notes;

                // Financier
                TxtTotalCommande.Text = $"{livraison.TotalCommande:N2} DH";
                TxtFraisLivraison.Text = $"{livraison.FraisLivraison:N2} DH";
                decimal total = livraison.TotalCommande + livraison.FraisLivraison;
                TxtTotal.Text = $"{total:N2} DH";

                // Sélectionner le statut actuel dans le ComboBox
                SelectStatutInComboBox(livraison.Statut);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des détails: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Charger les informations de l'opération et ses articles
        private async Task LoadOperationInfoAsync()
        {
            try
            {
                Operation op = new Operation { OperationID = livraison.OperationID };
                var operations = await op.GetOperationsAsync();
                operation = operations.FirstOrDefault(o => o.OperationID == livraison.OperationID);

                if (operation != null)
                {
                    TxtOperationID.Text = operation.OperationID.ToString();
                    TxtOperationType.Text = operation.OperationType ?? "Vente";
                    TxtOperationDate.Text = operation.DateOperation.ToString("dd/MM/yyyy HH:mm");

                    // Charger les articles de l'opération
                    await LoadOperationArticlesAsync();
                }
                else
                {
                    TxtOperationID.Text = livraison.OperationID.ToString();
                    TxtOperationType.Text = "-";
                    TxtOperationDate.Text = "-";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement de l'opération: {ex.Message}");
            }
        }

        // Charger les articles de l'opération
        private async Task LoadOperationArticlesAsync()
        {
            try
            {
                // Use the static method (same as DeliveryTicketWindow) which reliably
                // returns all operation articles without requiring a pre-set OperationID.
                var allOpArticles = await OperationArticle.GetAllOperationArticlesAsync();

                // Filter to this operation only, exclude reversed lines
                var operationArticles = allOpArticles
                    .Where(oa => oa.OperationID == livraison.OperationID && !oa.Reversed)
                    .ToList();

                // Fetch the full article catalogue ONCE (not inside the loop)
                Article articleObj = new Article();
                var allArticles = await articleObj.GetArticlesAsync();

                List<ArticleDisplay> displayList = new List<ArticleDisplay>();

                foreach (var opArt in operationArticles)
                {
                    // Prefer the name already carried by OperationArticle (API join),
                    // fall back to the catalogue lookup.
                    string name = !string.IsNullOrWhiteSpace(opArt.ArticleName)
                        ? opArt.ArticleName
                        : null;

                    decimal unitPrice = 0;

                    var foundArticle = allArticles.FirstOrDefault(a => a.ArticleID == opArt.ArticleID);
                    if (foundArticle != null)
                    {
                        if (name == null) name = foundArticle.ArticleName;
                        unitPrice = foundArticle.PrixVente;
                    }

                    if (name == null) name = $"Article #{opArt.ArticleID}";

                    displayList.Add(new ArticleDisplay
                    {
                        ArticleName = name,
                        Quantity = opArt.QteArticle,
                        UnitPrice = unitPrice
                    });
                }

                articleList = new ObservableCollection<ArticleDisplay>(displayList);
                DgArticles.ItemsSource = articleList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement des articles: {ex.Message}");
            }
        }

        // Mettre à jour le badge de statut
        private void UpdateStatutBadge(string statut)
        {
            switch (statut)
            {
                case "en_attente":
                    StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                    TxtStatut.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#92400E"));
                    TxtStatut.Text = "EN ATTENTE";
                    break;
                case "confirmee":
                    StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE"));
                    TxtStatut.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E40AF"));
                    TxtStatut.Text = "CONFIRMÉE";
                    break;
                case "en_preparation":
                    StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E7FF"));
                    TxtStatut.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4338CA"));
                    TxtStatut.Text = "EN PRÉPARATION";
                    break;
                case "en_cours":
                    StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DBEAFE"));
                    TxtStatut.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D4ED8"));
                    TxtStatut.Text = "EN COURS";
                    break;
                case "livree":
                    StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1FAE5"));
                    TxtStatut.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#065F46"));
                    TxtStatut.Text = "LIVRÉE";
                    break;
                case "annulee":
                    StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEE2E2"));
                    TxtStatut.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#991B1B"));
                    TxtStatut.Text = "ANNULÉE";
                    break;
                default:
                    StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9"));
                    TxtStatut.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B"));
                    TxtStatut.Text = "INCONNU";
                    break;
            }
        }

        // Sélectionner le statut dans le ComboBox
        private void SelectStatutInComboBox(string statut)
        {
            foreach (ComboBoxItem item in CmbNouveauStatut.Items)
            {
                if (item.Tag.ToString() == statut)
                {
                    CmbNouveauStatut.SelectedItem = item;
                    break;
                }
            }
        }

        // Bouton Mettre à jour le Statut
        private async void BtnUpdateStatut_Click(object sender, RoutedEventArgs e)
        {
            if (CmbNouveauStatut.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un nouveau statut.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItem = (ComboBoxItem)CmbNouveauStatut.SelectedItem;
            string nouveauStatut = selectedItem.Tag.ToString();

            if (nouveauStatut == livraison.Statut)
            {
                MessageBox.Show("Le statut sélectionné est déjà le statut actuel.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                BtnUpdateStatut.IsEnabled = false;
                BtnUpdateStatut.Content = "⏳ Mise à jour...";

                string commentaire = TxtCommentaire.Text.Trim();
                int result = await livraison.UpdateStatutAsync(nouveauStatut, commentaire);

                if (result > 0)
                {
                    MessageBox.Show("✅ Statut mis à jour avec succès!", "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Recharger les détails
                    await LoadLivraisonDetailsAsync();

                    // Vider le commentaire
                    TxtCommentaire.Clear();

                    // Déclencher l'événement
                    LivraisonUpdated?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    MessageBox.Show("Erreur lors de la mise à jour du statut.", "Erreur",
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
                BtnUpdateStatut.IsEnabled = true;
                BtnUpdateStatut.Content = "✅ Mettre à jour";
            }
        }

        // Bouton Appeler Client
        private void BtnCallClient_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(livraison.ClientTelephone))
            {
                try
                {
                    // Ouvrir l'application téléphone avec le numéro
                    Process.Start($"tel:{livraison.ClientTelephone}");
                }
                catch
                {
                    // Si ça ne marche pas, copier dans le presse-papier
                    Clipboard.SetText(livraison.ClientTelephone);
                    MessageBox.Show($"📞 Numéro copié dans le presse-papier:\n{livraison.ClientTelephone}",
                        "Téléphone", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Aucun numéro de téléphone disponible.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Bouton Voir Itinéraire
        private void BtnViewRoute_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(livraison.AdresseLivraison))
            {
                try
                {
                    // Construire l'URL Google Maps
                    string address = $"{livraison.AdresseLivraison}, {livraison.Ville}, {livraison.CodePostal}";
                    string encodedAddress = Uri.EscapeDataString(address);
                    string mapsUrl = $"https://www.google.com/maps/search/?api=1&query={encodedAddress}";

                    // Ouvrir dans le navigateur
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = mapsUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'ouverture de Google Maps: {ex.Message}",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Aucune adresse disponible.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Bouton Fermer
        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // ── Article editing ─────────────────────────────────────────────────────

        /// <summary>
        /// Called after the user commits a cell edit.
        /// Parses numeric input for Quantity and UnitPrice, then Total auto-updates
        /// via INotifyPropertyChanged.
        /// </summary>
        private void DgArticles_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;

            var article = e.Row.Item as ArticleDisplay;
            if (article == null) return;

            var textBox = e.EditingElement as TextBox;
            if (textBox == null) return;

            string header = (e.Column as DataGridTemplateColumn)?.Header?.ToString() ?? "";

            // We match by column index since Header is a resource string
            int colIndex = e.Column.DisplayIndex;

            // Column 1 = Quantity, Column 2 = UnitPrice
            if (colIndex == 1)
            {
                if (int.TryParse(textBox.Text, out int qty) && qty >= 0)
                    article.Quantity = qty;
                else
                    textBox.Text = article.Quantity.ToString(); // revert bad input
            }
            else if (colIndex == 2)
            {
                string cleaned = textBox.Text.Replace("DH", "").Replace(",", ".").Trim();
                if (decimal.TryParse(cleaned,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal price) && price >= 0)
                    article.UnitPrice = price;
                else
                    textBox.Text = article.UnitPrice.ToString("N2"); // revert bad input
            }
        }

        /// <summary>
        /// Commits any pending edit in the grid, then shows a confirmation.
        /// The in-memory articleList is the single source of truth — no DB write needed here.
        /// </summary>
        private void BtnSaveArticleEdits_Click(object sender, RoutedEventArgs e)
        {
            // Commit any cell that is still open
            DgArticles.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true);

            MessageBox.Show(
                $"✅ {articleList.Count} article(s) mis à jour en mémoire.\n" +
                "Les modifications seront reflétées dans le bon de livraison.",
                "Modifications enregistrées",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Bouton Voir Bon de Livraison
        private async void BtnViewTicket_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Commit any open cell edit before opening the ticket
                DgArticles.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true);

                string heureCrenneau = ExtractTimeFromNotes(livraison.Notes);

                // Try to get the linked operation for context; it's optional
                List<Operation> operationsList = new List<Operation>();
                if (livraison.OperationID > 0)
                {
                    var allOps = await new Operation().GetOperationsAsync();
                    var op = allOps.FirstOrDefault(o => o.OperationID == livraison.OperationID);
                    if (op != null) operationsList.Add(op);
                }

                // Pass the in-memory edited article list so the ticket reflects any edits
                DeliveryTicketWindow ticketWindow = new DeliveryTicketWindow(
                    livraison,
                    operationsList,
                    livraison.LivreurNom ?? "Non assigné",
                    heureCrenneau,
                    articleList.ToList()   // <-- edited articles override API fetch
                );
                ticketWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture du bon de livraison: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // Bouton Sauvegarder comme Facture
        private async void BtnSaveAsInvoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                BtnSaveAsInvoice.IsEnabled = false;
                BtnSaveAsInvoice.Content = "⏳ Sauvegarde...";

                // Load company info
                Superete.Facture facture = new Superete.Facture();
                facture = await facture.GetFactureAsync();

                // Load client info
                Client client = null;
                if (livraison.ClientID.HasValue)
                {
                    Client clientObj = new Client();
                    var clients = await clientObj.GetClientsAsync();
                    client = clients.FirstOrDefault(c => c.ClientID == livraison.ClientID.Value);
                }

                // Create invoice repository
                var invoiceRepo = new GestionComerce.Main.Facturation.InvoiceRepository("");

                // Generate unique invoice number
                string invoiceNumber = await GenerateUniqueInvoiceNumber(invoiceRepo);

                // Create invoice object
                var invoice = new GestionComerce.Invoice
                {
                    InvoiceNumber = invoiceNumber,
                    InvoiceDate = DateTime.Now,
                    InvoiceType = "Bon de Livraison",
                    InvoiceIndex = livraison.LivraisonID.ToString(),

                    // Company info
                    UserName = facture?.Name ?? "Votre Entreprise",
                    UserICE = facture?.ICE ?? "",
                    UserVAT = facture?.VAT ?? "",
                    UserPhone = facture?.Telephone ?? "",
                    UserAddress = facture?.Adresse ?? "",
                    UserEtatJuridique = facture?.EtatJuridic ?? "",
                    UserIdSociete = facture?.CompanyId ?? "",
                    UserSiegeEntreprise = facture?.SiegeEntreprise ?? "",

                    // Client info
                    ClientName = livraison.ClientNom ?? "",
                    ClientICE = client?.ICE ?? "",
                    ClientVAT = client?.EtatJuridique ?? "",
                    ClientPhone = livraison.ClientTelephone ?? "",
                    ClientAddress = $"{livraison.AdresseLivraison}, {livraison.Ville} {livraison.CodePostal}",

                    // Financial info
                    Currency = "DH",
                    TVARate = 0,
                    TotalHT = livraison.TotalCommande,
                    TotalTVA = 0,
                    TotalTTC = livraison.TotalCommande + livraison.FraisLivraison,
                    Remise = 0,
                    TotalAfterRemise = livraison.TotalCommande + livraison.FraisLivraison,

                    EtatFacture = 1,
                    IsReversed = false,
                    Description = $"Bon de livraison #{livraison.LivraisonID} - {livraison.Notes}",
                    LogoPath = facture?.LogoPath ?? "",
                    CreatedBy = u?.UserID
                };

                // Load articles from operation
                await LoadArticlesForInvoice(invoice);

                // Save invoice
                int invoiceId = await invoiceRepo.CreateInvoiceAsync(invoice);

                if (invoiceId > 0)
                {
                    MessageBox.Show($"✅ Bon de livraison sauvegardé comme facture!\n\nNuméro de facture: {invoiceNumber}",
                        "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Erreur lors de la sauvegarde de la facture.", "Erreur",
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
                BtnSaveAsInvoice.IsEnabled = true;
                BtnSaveAsInvoice.Content = "💾 Sauvegarder comme Facture";
            }
        }

        // Generate unique invoice number
        private async Task<string> GenerateUniqueInvoiceNumber(GestionComerce.Main.Facturation.InvoiceRepository repo)
        {
            string prefix = $"BL-{DateTime.Now:yyyyMMdd}";
            int counter = 1;
            string invoiceNumber;

            do
            {
                invoiceNumber = $"{prefix}-{counter:D3}";
                counter++;
            }
            while (await repo.InvoiceNumberExistsAsync(invoiceNumber));

            return invoiceNumber;
        }

        // Load articles for invoice
        private async Task LoadArticlesForInvoice(GestionComerce.Invoice invoice)
        {
            try
            {
                OperationArticle opArticle = new OperationArticle { OperationID = livraison.OperationID };
                var allOpArticles = await opArticle.GetOperationArticlesAsync();

                // Get articles for this operation
                var operationArticles = allOpArticles
                    .Where(oa => oa.OperationID == livraison.OperationID && !oa.Reversed)
                    .ToList();

                // Load article details
                Article articleObj = new Article();
                var allArticles = await articleObj.GetArticlesAsync();

                foreach (var opArt in operationArticles)
                {
                    var article = allArticles.FirstOrDefault(a => a.ArticleID == opArt.ArticleID);
                    if (article != null)
                    {
                        invoice.Articles.Add(new GestionComerce.Invoice.InvoiceArticle
                        {
                            OperationID = opArt.OperationID,
                            ArticleID = article.ArticleID,
                            ArticleName = article.marque ?? article.ArticleName ?? "Article",
                            PrixUnitaire = article.PrixVente,
                            Quantite = opArt.QteArticle,
                            TVA = 0,
                            IsReversed = false
                        });
                    }
                }

                // Calculate totals
                invoice.CalculateTotals();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors du chargement des articles: {ex.Message}");
            }
        }
    }
}