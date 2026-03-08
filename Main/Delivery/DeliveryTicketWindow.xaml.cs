using Superete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace GestionComerce.Main.Delivery
{
    public partial class DeliveryTicketWindow : Window
    {
        private Livraison livraison;
        private List<Operation> operations;
        private string livreurName;
        private string heureCrenneau;
        // When supplied, these override the API fetch — reflects in-memory edits from the details window
        private List<ArticleDisplay> prebuiltArticles;

        public DeliveryTicketWindow(
            Livraison livraison,
            List<Operation> operations,
            string livreurName,
            string heureCrenneau,
            List<ArticleDisplay> prebuiltArticles = null)
        {
            InitializeComponent();
            this.livraison = livraison;
            this.operations = operations;
            this.livreurName = livreurName;
            this.heureCrenneau = heureCrenneau;
            this.prebuiltArticles = prebuiltArticles;

            Loaded += DeliveryTicketWindow_Loaded;
        }

        private async void DeliveryTicketWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await PopulateTicketDataAsync();
        }

        private async System.Threading.Tasks.Task PopulateTicketDataAsync()
        {
            try
            {
                // ========== LOAD COMPANY INFO FROM FACTURE ==========
                Facture facture = new Facture();
                facture = await facture.GetFactureAsync();

                if (facture != null)
                {
                    TxtCompanyName.Text = facture.Name ?? "VOTRE ENTREPRISE";
                    TxtCompanyAddress.Text = facture.Adresse ?? "Adresse de l'entreprise";
                    TxtCompanyPhone.Text = $"Tél: {facture.Telephone ?? "+212 XXX XXX XXX"}";

                    // Optional: Display ICE/VAT if needed
                    // TxtCompanyAddress.Text = $"{facture.Adresse}\nICE: {facture.ICE} | TVA: {facture.VAT}";
                }
                else
                {
                    TxtCompanyName.Text = "VOTRE ENTREPRISE";
                    TxtCompanyAddress.Text = "123 Rue Principale, Tangier";
                    TxtCompanyPhone.Text = "Tél: +212 XXX XXX XXX";
                }

                // ========== GENERATE UNIQUE BON NUMBER ==========
                // Format: LIV-YYYYMMDD-ID (e.g., LIV-20260102-00005)
                string bonNumber = $"LIV-{DateTime.Now:yyyyMMdd}-{livraison.LivraisonID:D5}";
                TxtBonNumber.Text = bonNumber;

                // Delivery info
                TxtDate.Text = livraison.DateLivraisonPrevue?.ToString("dd/MM/yyyy") ?? DateTime.Now.ToString("dd/MM/yyyy");
                TxtLivreur.Text = livreurName ?? "Non assigné";
                TxtHeure.Text = heureCrenneau ?? "À définir";

                // Client info
                TxtClientName.Text = livraison.ClientNom ?? "";
                TxtClientPhone.Text = $"Tél: {livraison.ClientTelephone ?? ""}";
                TxtClientAddress.Text = livraison.AdresseLivraison ?? "";
                TxtClientCity.Text = $"{livraison.Ville ?? ""}, {livraison.CodePostal ?? ""}";

                // Load articles
                await LoadArticlesAsync();

                // Totals
                TxtTotalCommande.Text = $"{livraison.TotalCommande:N2} DH";
                TxtFraisLivraison.Text = $"{livraison.FraisLivraison:N2} DH";
                decimal total = livraison.TotalCommande + livraison.FraisLivraison;
                TxtTotal.Text = $"{total:N2} DH";

                // Payment & Zone
                TxtModePaiement.Text = livraison.ModePaiement ?? "Espèces";
                TxtZone.Text = livraison.ZoneLivraison ?? "";

                // Notes
                if (!string.IsNullOrWhiteSpace(livraison.Notes))
                {
                    NotesSection.Visibility = Visibility.Visible;
                    TxtNotes.Text = livraison.Notes;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des données: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task LoadArticlesAsync()
        {
            try
            {
                ArticlesPanel.Children.Clear();

                // ── Use prebuilt (edited) articles if supplied ─────────────────
                if (prebuiltArticles != null && prebuiltArticles.Count > 0)
                {
                    int index = 1;
                    foreach (var art in prebuiltArticles)
                    {
                        AddArticleRow(index, art.ArticleName, art.Quantity, art.UnitPrice);
                        index++;
                    }
                    return;
                }

                // ── Otherwise fetch from API (normal path) ─────────────────────
                var allOpArticles = await OperationArticle.GetAllOperationArticlesAsync();

                var operationIds = operations.Select(o => o.OperationID).ToList();

                // Fetch article catalogue ONCE to get unit prices
                Article articleObj = new Article();
                var allArticles = await articleObj.GetArticlesAsync();

                var relevantArticles = allOpArticles
                    .Where(oa => operationIds.Contains(oa.OperationID) && !oa.Reversed)
                    .GroupBy(oa => new { oa.ArticleID, oa.ArticleName })
                    .Select(g =>
                    {
                        var catalogue = allArticles.FirstOrDefault(a => a.ArticleID == g.Key.ArticleID);
                        return new
                        {
                            ArticleID = g.Key.ArticleID,
                            ArticleName = g.Key.ArticleName,
                            TotalQuantity = g.Sum(x => x.QteArticle),
                            UnitPrice = catalogue != null ? catalogue.PrixVente : 0m
                        };
                    })
                    .ToList();

                if (relevantArticles.Count == 0)
                {
                    ArticlesPanel.Children.Add(new TextBlock
                    {
                        Text = "Aucun article trouvé",
                        FontFamily = new FontFamily("Courier New"),
                        FontSize = 13,
                        FontStyle = FontStyles.Italic,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 10, 0, 10)
                    });
                    return;
                }

                int idx = 1;
                foreach (var artGroup in relevantArticles)
                {
                    string name = !string.IsNullOrWhiteSpace(artGroup.ArticleName)
                        ? artGroup.ArticleName
                        : $"Article #{artGroup.ArticleID}";

                    AddArticleRow(idx, name, artGroup.TotalQuantity, artGroup.UnitPrice);
                    idx++;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des articles: {ex.Message}\n\nDétails: {ex.StackTrace}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);

                ArticlesPanel.Children.Add(new TextBlock
                {
                    Text = $"Erreur: {ex.Message}",
                    FontFamily = new FontFamily("Courier New"),
                    FontSize = 13,
                    Foreground = System.Windows.Media.Brushes.Red,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 10, 0, 10)
                });
            }
        }

        /// <summary>Builds and appends one article row to ArticlesPanel.</summary>
        private void AddArticleRow(int index, string articleName, decimal quantity, decimal unitPrice)
        {
            decimal lineTotal = unitPrice * quantity;

            Grid articleGrid = new Grid { Margin = new Thickness(0, 0, 0, 5) };
            articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
            articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            articleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

            TextBlock txtIndex = new TextBlock
            {
                Text = $"{index}.",
                FontFamily = new FontFamily("Courier New"),
                FontSize = 13,
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(txtIndex, 0);

            TextBlock txtName = new TextBlock
            {
                Text = articleName,
                FontFamily = new FontFamily("Courier New"),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(txtName, 1);

            TextBlock txtQty = new TextBlock
            {
                Text = $"x{quantity}",
                FontFamily = new FontFamily("Courier New"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(txtQty, 2);

            TextBlock txtPrice = new TextBlock
            {
                Text = $"{lineTotal:N2} DH",
                FontFamily = new FontFamily("Courier New"),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(txtPrice, 3);

            articleGrid.Children.Add(txtIndex);
            articleGrid.Children.Add(txtName);
            articleGrid.Children.Add(txtQty);
            articleGrid.Children.Add(txtPrice);

            ArticlesPanel.Children.Add(articleGrid);
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Get the ticket content
                    printDialog.PrintVisual(TicketBorder, "Bon de Livraison");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'impression: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSavePDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = $"BonLivraison_{livraison.LivraisonID}_{DateTime.Now:yyyyMMdd}.pdf",
                    DefaultExt = ".pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // Note: For PDF export, you'll need to install a NuGet package like:
                    // - PdfSharp or iTextSharp or similar
                    // For now, we'll show a message
                    MessageBox.Show("Pour exporter en PDF, veuillez installer un package NuGet comme PdfSharp ou iTextSharp.\n\nVous pouvez utiliser 'Imprimer' et choisir 'Microsoft Print to PDF' comme imprimante.",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}