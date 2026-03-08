using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Superete;

namespace GestionComerce.Main.Inventory
{
    public partial class WDevisPreview : Window
    {
        private List<Article> selectedArticles;
        private List<Famille> allFamilles;
        private List<Fournisseur> allFournisseurs;
        private DevisConfiguration config;
        private Facture companyInfo;
        private const int ARTICLES_PER_PAGE = 20;

        public WDevisPreview(List<Article> articles, List<Famille> familles,
            List<Fournisseur> fournisseurs, DevisConfiguration configuration)
        {
            InitializeComponent();
            this.selectedArticles = articles;
            this.allFamilles = familles;
            this.allFournisseurs = fournisseurs;
            this.config = configuration;

            ArticleCountText.Text = $"({articles.Count} article{(articles.Count > 1 ? "s" : "")})";

            LoadCompanyInfo();
        }

        private async void LoadCompanyInfo()
        {
            try
            {
                Facture f = new Facture();
                companyInfo = await f.GetFactureAsync();
                GenerateDevis();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des informations: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateDevis()
        {
            DevisContent.Children.Clear();

            // Calculate number of pages needed
            int totalPages = (int)Math.Ceiling((double)selectedArticles.Count / ARTICLES_PER_PAGE);

            for (int pageNum = 1; pageNum <= totalPages; pageNum++)
            {
                // Add page break between pages
                if (pageNum > 1)
                {
                    Border pageBreak = new Border
                    {
                        Height = 30,
                        Background = new SolidColorBrush(Color.FromRgb(243, 244, 246)),
                        Margin = new Thickness(0, 10, 0, 10)
                    };
                    DevisContent.Children.Add(pageBreak);
                }

                StackPanel page = CreatePage(pageNum, totalPages);
                DevisContent.Children.Add(page);
            }
        }

        private StackPanel CreatePage(int pageNum, int totalPages)
        {
            StackPanel page = new StackPanel();

            // Header Section
            Grid header = CreateHeader(pageNum, totalPages);
            page.Children.Add(header);

            // Separator
            page.Children.Add(new Border
            {
                Height = 2,
                Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Margin = new Thickness(0, 20, 0, 20)
            });

            // Client Info (only on first page)
            if (pageNum == 1 && config.ShowClientSection &&
                !string.IsNullOrWhiteSpace(config.ClientName))
            {
                Grid clientInfo = CreateClientSection();
                page.Children.Add(clientInfo);
                page.Children.Add(new Border { Height = 20 });
            }

            // Articles Table
            Grid articlesTable = CreateArticlesTable(pageNum);
            page.Children.Add(articlesTable);

            // Totals (only on last page)
            if (pageNum == totalPages)
            {
                page.Children.Add(new Border { Height = 20 });
                Grid totals = CreateTotalsSection();
                page.Children.Add(totals);

                // Notes and Payment Terms
                if (config.ShowNotes || config.ShowPaymentTerms)
                {
                    page.Children.Add(new Border { Height = 20 });
                    StackPanel footer = CreateFooterSection();
                    page.Children.Add(footer);
                }
            }

            // Page number at bottom
            TextBlock pageNumber = new TextBlock
            {
                Text = $"Page {pageNum} / {totalPages}",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };
            page.Children.Add(pageNumber);

            return page;
        }

        private Grid CreateHeader(int pageNum, int totalPages)
        {
            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left side - Company Info
            StackPanel leftPanel = new StackPanel();

            // Logo
            if (config.ShowLogo && !string.IsNullOrWhiteSpace(companyInfo?.LogoPath) &&
                File.Exists(companyInfo.LogoPath))
            {
                try
                {
                    Image logo = new Image
                    {
                        Source = new BitmapImage(new Uri(companyInfo.LogoPath, UriKind.Absolute)),
                        Width = 120,
                        Height = 60,
                        Stretch = Stretch.Uniform,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(0, 0, 0, 12)
                    };
                    leftPanel.Children.Add(logo);
                }
                catch { }
            }

            if (config.ShowCompanyName && !string.IsNullOrWhiteSpace(companyInfo?.Name))
            {
                leftPanel.Children.Add(CreateText(companyInfo.Name, 18, true));
            }

            if (config.ShowICE && !string.IsNullOrWhiteSpace(companyInfo?.ICE))
            {
                leftPanel.Children.Add(CreateText($"ICE: {companyInfo.ICE}", 11));
            }

            if (config.ShowVAT && !string.IsNullOrWhiteSpace(companyInfo?.VAT))
            {
                leftPanel.Children.Add(CreateText($"TVA: {companyInfo.VAT}", 11));
            }

            if (config.ShowCompanyId && !string.IsNullOrWhiteSpace(companyInfo?.CompanyId))
            {
                leftPanel.Children.Add(CreateText($"ID: {companyInfo.CompanyId}", 11));
            }

            if (config.ShowEtatJuridic && !string.IsNullOrWhiteSpace(companyInfo?.EtatJuridic))
            {
                leftPanel.Children.Add(CreateText($"Forme juridique: {companyInfo.EtatJuridic}", 11));
            }

            if (config.ShowSiege && !string.IsNullOrWhiteSpace(companyInfo?.SiegeEntreprise))
            {
                leftPanel.Children.Add(CreateText($"Siège: {companyInfo.SiegeEntreprise}", 11));
            }

            if (config.ShowTelephone && !string.IsNullOrWhiteSpace(companyInfo?.Telephone))
            {
                leftPanel.Children.Add(CreateText($"Tél: {companyInfo.Telephone}", 11));
            }

            if (config.ShowAdresse && !string.IsNullOrWhiteSpace(companyInfo?.Adresse))
            {
                leftPanel.Children.Add(CreateText($"Adresse: {companyInfo.Adresse}", 11));
            }

            Grid.SetColumn(leftPanel, 0);
            header.Children.Add(leftPanel);

            // Right side - Devis Info (only on first page)
            if (pageNum == 1)
            {
                StackPanel rightPanel = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                TextBlock devisTitle = new TextBlock
                {
                    Text = "DEVIS",
                    FontSize = 28,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                    Margin = new Thickness(0, 0, 0, 12)
                };
                rightPanel.Children.Add(devisTitle);

                if (config.ShowDevisNumber)
                {
                    string devisNumber = $"DEV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                    rightPanel.Children.Add(CreateText($"N°: {devisNumber}", 12, true));
                }

                if (config.ShowDevisDate)
                {
                    rightPanel.Children.Add(CreateText($"Date: {DateTime.Now:dd/MM/yyyy}", 12));
                }

                if (config.ShowValidity)
                {
                    DateTime validUntil = DateTime.Now.AddDays(config.ValidityDays);
                    rightPanel.Children.Add(CreateText($"Valable jusqu'au: {validUntil:dd/MM/yyyy}", 12));
                }

                Grid.SetColumn(rightPanel, 1);
                header.Children.Add(rightPanel);
            }

            return header;
        }

        private Grid CreateClientSection()
        {
            Grid clientGrid = new Grid
            {
                Background = new SolidColorBrush(Color.FromRgb(239, 246, 255))
            };

            Border border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16)
            };

            StackPanel clientPanel = new StackPanel();

            TextBlock title = new TextBlock
            {
                Text = "CLIENT",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            clientPanel.Children.Add(title);

            if (!string.IsNullOrWhiteSpace(config.ClientName))
            {
                clientPanel.Children.Add(CreateText(config.ClientName, 12, true));
            }

            if (!string.IsNullOrWhiteSpace(config.ClientICE))
            {
                clientPanel.Children.Add(CreateText($"ICE: {config.ClientICE}", 11));
            }

            if (!string.IsNullOrWhiteSpace(config.ClientAddress))
            {
                clientPanel.Children.Add(CreateText($"Adresse: {config.ClientAddress}", 11));
            }

            border.Child = clientPanel;
            clientGrid.Children.Add(border);

            return clientGrid;
        }

        private Grid CreateArticlesTable(int pageNum)
        {
            int startIdx = (pageNum - 1) * ARTICLES_PER_PAGE;
            int endIdx = Math.Min(startIdx + ARTICLES_PER_PAGE, selectedArticles.Count);

            Grid table = new Grid();

            // Define columns based on configuration
            List<string> columnHeaders = new List<string>();
            List<GridLength> columnWidths = new List<GridLength>();

            if (config.ShowCode) { columnHeaders.Add("Code"); columnWidths.Add(GridLength.Auto); }
            if (config.ShowArticleName) { columnHeaders.Add("Article"); columnWidths.Add(new GridLength(2, GridUnitType.Star)); }
            if (config.ShowFamille) { columnHeaders.Add("Famille"); columnWidths.Add(GridLength.Auto); }
            if (config.ShowFournisseur) { columnHeaders.Add("Fournisseur"); columnWidths.Add(GridLength.Auto); }
            if (config.ShowMarque) { columnHeaders.Add("Marque"); columnWidths.Add(GridLength.Auto); }
            if (config.ShowLot) { columnHeaders.Add("N° Lot"); columnWidths.Add(GridLength.Auto); }
            if (config.ShowBonLivraison) { columnHeaders.Add("Bon Liv."); columnWidths.Add(GridLength.Auto); }
            if (config.ShowExpiration) { columnHeaders.Add("Expiration"); columnWidths.Add(GridLength.Auto); }
            if (config.ShowQuantity) { columnHeaders.Add("Qté"); columnWidths.Add(GridLength.Auto); }
            if (config.ShowUnitPrice) { columnHeaders.Add("P.U. HT"); columnWidths.Add(GridLength.Auto); }
            if (config.ShowTVA) { columnHeaders.Add("TVA %"); columnWidths.Add(GridLength.Auto); }
            if (config.ShowTotalPrice) { columnHeaders.Add("Total HT"); columnWidths.Add(GridLength.Auto); }

            // Create columns for table
            foreach (var width in columnWidths)
            {
                table.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
            }

            // Header row
            table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Border headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                Padding = new Thickness(8, 10, 8, 10)
            };

            Grid headerGrid = new Grid();
            // Create NEW column definitions for headerGrid
            foreach (var width in columnWidths)
            {
                headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
            }

            for (int i = 0; i < columnHeaders.Count; i++)
            {
                TextBlock headerText = new TextBlock
                {
                    Text = columnHeaders[i],
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    Padding = new Thickness(4, 0, 4, 0)
                };
                Grid.SetColumn(headerText, i);
                headerGrid.Children.Add(headerText);
            }

            headerBorder.Child = headerGrid;
            Grid.SetRow(headerBorder, 0);
            Grid.SetColumnSpan(headerBorder, columnHeaders.Count);
            table.Children.Add(headerBorder);

            // Data rows
            for (int idx = startIdx; idx < endIdx; idx++)
            {
                Article article = selectedArticles[idx];
                int rowIndex = idx - startIdx + 1;
                table.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Border rowBorder = new Border
                {
                    Background = rowIndex % 2 == 0 ?
                        new SolidColorBrush(Color.FromRgb(248, 250, 252)) : Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(8, 8, 8, 8)
                };

                Grid rowGrid = new Grid();
                // Create NEW column definitions for each rowGrid
                foreach (var width in columnWidths)
                {
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
                }

                int colIdx = 0;
                if (config.ShowCode)
                {
                    AddCellText(rowGrid, article.Code.ToString(), colIdx++);
                }
                if (config.ShowArticleName)
                {
                    AddCellText(rowGrid, article.ArticleName ?? "", colIdx++);
                }
                if (config.ShowFamille)
                {
                    AddCellText(rowGrid, GetFamilleName(article.FamillyID), colIdx++);
                }
                if (config.ShowFournisseur)
                {
                    AddCellText(rowGrid, GetFournisseurName(article.FournisseurID), colIdx++);
                }
                if (config.ShowMarque)
                {
                    AddCellText(rowGrid, article.marque ?? "", colIdx++);
                }
                if (config.ShowLot)
                {
                    AddCellText(rowGrid, article.numeroLot ?? "", colIdx++);
                }
                if (config.ShowBonLivraison)
                {
                    AddCellText(rowGrid, article.bonlivraison ?? "", colIdx++);
                }
                if (config.ShowExpiration)
                {
                    string expDate = article.DateExpiration.HasValue ?
                        article.DateExpiration.Value.ToString("dd/MM/yyyy") : "-";
                    AddCellText(rowGrid, expDate, colIdx++);
                }
                if (config.ShowQuantity)
                {
                    AddCellText(rowGrid, article.Quantite.ToString(), colIdx++);
                }
                if (config.ShowUnitPrice)
                {
                    AddCellText(rowGrid, $"{article.PrixVente:F2} DH", colIdx++);
                }
                if (config.ShowTVA)
                {
                    AddCellText(rowGrid, $"{article.tva}%", colIdx++);
                }
                if (config.ShowTotalPrice)
                {
                    decimal total = article.PrixVente * article.Quantite;
                    AddCellText(rowGrid, $"{total:F2} DH", colIdx++, true);
                }

                rowBorder.Child = rowGrid;
                Grid.SetRow(rowBorder, rowIndex);
                Grid.SetColumnSpan(rowBorder, columnHeaders.Count);
                table.Children.Add(rowBorder);
            }

            return table;
        }

        private void AddCellText(Grid grid, string text, int column, bool isBold = false)
        {
            TextBlock cellText = new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                Padding = new Thickness(4, 0, 4, 0),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(cellText, column);
            grid.Children.Add(cellText);
        }

        private Grid CreateTotalsSection()
        {
            Grid totalsGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Width = 300
            };

            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            totalsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

            // Calculate totals
            decimal subtotal = 0;
            decimal totalTVA = 0;

            foreach (Article article in selectedArticles)
            {
                decimal articleTotal = article.PrixVente * article.Quantite;
                subtotal += articleTotal;
                totalTVA += (articleTotal * article.tva / 100);
            }

            decimal grandTotal = subtotal + totalTVA;

            int rowIdx = 0;

            if (config.ShowSubtotal)
            {
                totalsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                AddTotalRow(totalsGrid, "Sous-total HT:", $"{subtotal:F2} DH", rowIdx++, false);
            }

            if (config.ShowTVATotal)
            {
                totalsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                AddTotalRow(totalsGrid, "Total TVA:", $"{totalTVA:F2} DH", rowIdx++, false);
            }

            if (config.ShowGrandTotal)
            {
                totalsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                AddTotalRow(totalsGrid, "TOTAL TTC:", $"{grandTotal:F2} DH", rowIdx++, true);
            }

            Border totalsBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Background = new SolidColorBrush(Color.FromRgb(239, 246, 255))
            };
            totalsBorder.Child = totalsGrid;

            Grid container = new Grid();
            container.Children.Add(totalsBorder);
            return container;
        }

        private void AddTotalRow(Grid grid, string label, string value, int row, bool isBold)
        {
            TextBlock labelText = new TextBlock
            {
                Text = label,
                FontSize = isBold ? 14 : 12,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.SemiBold,
                Margin = new Thickness(0, 4, 0, 4)
            };
            Grid.SetRow(labelText, row);
            Grid.SetColumn(labelText, 0);
            grid.Children.Add(labelText);

            TextBlock valueText = new TextBlock
            {
                Text = value,
                FontSize = isBold ? 14 : 12,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 4, 0, 4),
                Foreground = isBold ? new SolidColorBrush(Color.FromRgb(59, 130, 246)) : Brushes.Black
            };
            Grid.SetRow(valueText, row);
            Grid.SetColumn(valueText, 1);
            grid.Children.Add(valueText);
        }

        private StackPanel CreateFooterSection()
        {
            StackPanel footer = new StackPanel();

            if (config.ShowPaymentTerms && !string.IsNullOrWhiteSpace(config.PaymentTerms))
            {
                Border paymentBorder = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16),
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                    Margin = new Thickness(0, 0, 0, 12)
                };

                StackPanel paymentPanel = new StackPanel();
                paymentPanel.Children.Add(CreateText("Conditions de paiement:", 12, true));
                paymentPanel.Children.Add(CreateText(config.PaymentTerms, 11));

                paymentBorder.Child = paymentPanel;
                footer.Children.Add(paymentBorder);
            }

            if (config.ShowNotes && !string.IsNullOrWhiteSpace(config.Notes))
            {
                Border notesBorder = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16),
                    Background = new SolidColorBrush(Color.FromRgb(254, 252, 232))
                };

                StackPanel notesPanel = new StackPanel();
                notesPanel.Children.Add(CreateText("Notes / Remarques:", 12, true));
                notesPanel.Children.Add(CreateText(config.Notes, 11));

                notesBorder.Child = notesPanel;
                footer.Children.Add(notesBorder);
            }

            return footer;
        }

        private TextBlock CreateText(string text, double fontSize, bool isBold = false)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(0, 0, 0, 4),
                TextWrapping = TextWrapping.Wrap
            };
        }

        private string GetFamilleName(int familleId)
        {
            foreach (var famille in allFamilles)
            {
                if (famille.FamilleID == familleId)
                    return famille.FamilleName;
            }
            return "";
        }

        private string GetFournisseurName(int fournisseurId)
        {
            foreach (var fournisseur in allFournisseurs)
            {
                if (fournisseur.FournisseurID == fournisseurId)
                    return fournisseur.Nom;
            }
            return "";
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(PrintArea, "Devis");
                    MessageBox.Show("Impression lancée avec succès!", "Succès",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'impression: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePDF_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("La fonctionnalité d'export PDF nécessite une bibliothèque externe comme iTextSharp ou PdfSharp.\n\nVous pouvez utiliser 'Imprimer' et choisir 'Microsoft Print to PDF' comme imprimante.",
                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}