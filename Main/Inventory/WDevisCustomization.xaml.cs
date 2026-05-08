using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GestionComerce.Main.Inventory
{
    public partial class WDevisCustomization : Window
    {
        private List<Article> selectedArticles;
        private List<Famille> allFamilles;
        private List<Fournisseur> allFournisseurs;
        // TextBoxes for per-article quantity input: ArticleID → TextBox
        private Dictionary<int, TextBox> quantityBoxes = new Dictionary<int, TextBox>();

        public WDevisCustomization(List<Article> articles, List<Famille> familles, List<Fournisseur> fournisseurs)
        {
            InitializeComponent();
            this.selectedArticles = articles;
            this.allFamilles = familles;
            this.allFournisseurs = fournisseurs;
            BuildQuantityTable();
        }

        private void BuildQuantityTable()
        {
            quantityBoxes.Clear();
            ArticleQuantityPanel.Children.Clear();

            // Header row
            Grid header = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            AddHeaderCell(header, "Article", 0);
            AddHeaderCell(header, "Stock", 1);
            AddHeaderCell(header, "Qté Devis", 2);
            AddHeaderCell(header, "Prix Unit.", 3);
            ArticleQuantityPanel.Children.Add(header);

            // One row per article
            foreach (var article in selectedArticles)
            {
                Grid row = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

                AddCell(row, article.ArticleName ?? article.Code.ToString(), 0);
                AddCell(row, article.Quantite.ToString("0.##"), 1, "#64748B");

                TextBox qtyBox = new TextBox
                {
                    Text = "1",
                    Height = 30,
                    Padding = new Thickness(6, 4, 6, 4),
                    FontSize = 13,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(4, 0, 4, 0)
                };
                Grid.SetColumn(qtyBox, 2);
                row.Children.Add(qtyBox);
                quantityBoxes[article.ArticleID] = qtyBox;

                AddCell(row, $"{article.PrixVente:N2} DH", 3);
                ArticleQuantityPanel.Children.Add(row);
            }
        }

        private void AddHeaderCell(Grid grid, string text, int col)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Padding = new Thickness(6, 4, 6, 4)
            };
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        private void AddCell(Grid grid, string text, int col, string hexColor = null)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                FontSize = 12,
                Padding = new Thickness(6, 4, 6, 4),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = hexColor != null
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor))
                    : Brushes.Black
            };
            Grid.SetColumn(tb, col);
            grid.Children.Add(tb);
        }

        private void ClientSection_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (ClientFieldsPanel != null)
            {
                ClientFieldsPanel.IsEnabled = ShowClientSectionCheckBox.IsChecked == true;
            }
        }

        private void NotesSection_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (NotesTextBox != null)
            {
                NotesTextBox.IsEnabled = ShowNotesCheckBox.IsChecked == true;
            }
        }

        private void PaymentTerms_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (PaymentTermsTextBox != null)
            {
                PaymentTermsTextBox.IsEnabled = ShowPaymentTermsCheckBox.IsChecked == true;
            }
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            // Validate validity days
            if (!int.TryParse(ValidityDaysTextBox.Text, out int validityDays) || validityDays <= 0)
            {
                MessageBox.Show("Veuillez entrer un nombre valide de jours pour la validité.",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create configuration object
            DevisConfiguration config = new DevisConfiguration
            {
                // Company info
                ShowLogo = ShowLogoCheckBox.IsChecked == true,
                ShowCompanyName = ShowCompanyNameCheckBox.IsChecked == true,
                ShowICE = ShowICECheckBox.IsChecked == true,
                ShowVAT = ShowVATCheckBox.IsChecked == true,
                ShowCompanyId = ShowCompanyIdCheckBox.IsChecked == true,
                ShowEtatJuridic = ShowEtatJuridicCheckBox.IsChecked == true,
                ShowSiege = ShowSiegeCheckBox.IsChecked == true,
                ShowTelephone = ShowTelephoneCheckBox.IsChecked == true,
                ShowAdresse = ShowAdresseCheckBox.IsChecked == true,

                // Article fields
                ShowCode = ShowCodeCheckBox.IsChecked == true,
                ShowArticleName = ShowArticleNameCheckBox.IsChecked == true,
                ShowQuantity = ShowQuantityCheckBox.IsChecked == true,
                ShowUnitPrice = ShowUnitPriceCheckBox.IsChecked == true,
                ShowTotalPrice = ShowTotalPriceCheckBox.IsChecked == true,
                ShowTVA = ShowTVACheckBox.IsChecked == true,
                ShowFamille = ShowFamilleCheckBox.IsChecked == true,
                ShowFournisseur = ShowFournisseurCheckBox.IsChecked == true,
                ShowMarque = ShowMarqueCheckBox.IsChecked == true,
                ShowLot = ShowLotCheckBox.IsChecked == true,
                ShowBonLivraison = ShowBonLivraisonCheckBox.IsChecked == true,
                ShowExpiration = ShowExpirationCheckBox.IsChecked == true,

                // Devis info
                ShowDevisNumber = ShowDevisNumberCheckBox.IsChecked == true,
                ShowDevisDate = ShowDevisDateCheckBox.IsChecked == true,
                ShowValidity = ShowValidityCheckBox.IsChecked == true,
                ValidityDays = validityDays,

                // Client info
                ShowClientSection = ShowClientSectionCheckBox.IsChecked == true,
                ClientName = ClientNameTextBox.Text,
                ClientICE = ClientICETextBox.Text,
                ClientAddress = ClientAddressTextBox.Text,

                // Totals
                ShowSubtotal = ShowSubtotalCheckBox.IsChecked == true,
                ShowTVATotal = ShowTVATotalCheckBox.IsChecked == true,
                ShowGrandTotal = ShowGrandTotalCheckBox.IsChecked == true,

                // Additional
                ShowNotes = ShowNotesCheckBox.IsChecked == true,
                Notes = NotesTextBox.Text,
                ShowPaymentTerms = ShowPaymentTermsCheckBox.IsChecked == true,
                PaymentTerms = PaymentTermsTextBox.Text
            };

            // Collect per-article quantities from the input table
            var quantities = new Dictionary<int, decimal>();
            foreach (var article in selectedArticles)
            {
                if (quantityBoxes.TryGetValue(article.ArticleID, out TextBox box) &&
                    decimal.TryParse(box.Text, out decimal qty) && qty > 0)
                    quantities[article.ArticleID] = qty;
                else
                    quantities[article.ArticleID] = 1;
            }
            config.ArticleQuantities = quantities;
            config.ClientName = ClientNameTextBox.Text;

            // Open preview window
            WDevisPreview previewWindow = new WDevisPreview(
                selectedArticles, allFamilles, allFournisseurs, config);

            bool? result = previewWindow.ShowDialog();

            // After preview closes, close this window and complete the flow
            this.DialogResult = true;
            this.Close();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }

    // Configuration class to hold all settings
    public class DevisConfiguration
    {
        // Company Information
        public bool ShowLogo { get; set; }
        public bool ShowCompanyName { get; set; }
        public bool ShowICE { get; set; }
        public bool ShowVAT { get; set; }
        public bool ShowCompanyId { get; set; }
        public bool ShowEtatJuridic { get; set; }
        public bool ShowSiege { get; set; }
        public bool ShowTelephone { get; set; }
        public bool ShowAdresse { get; set; }

        // Article Fields
        public bool ShowCode { get; set; }
        public bool ShowArticleName { get; set; }
        public bool ShowQuantity { get; set; }
        public bool ShowUnitPrice { get; set; }
        public bool ShowTotalPrice { get; set; }
        public bool ShowTVA { get; set; }
        public bool ShowFamille { get; set; }
        public bool ShowFournisseur { get; set; }
        public bool ShowMarque { get; set; }
        public bool ShowLot { get; set; }
        public bool ShowBonLivraison { get; set; }
        public bool ShowExpiration { get; set; }

        // Devis Information
        public bool ShowDevisNumber { get; set; }
        public bool ShowDevisDate { get; set; }
        public bool ShowValidity { get; set; }
        public int ValidityDays { get; set; }

        // Client Information
        public bool ShowClientSection { get; set; }
        public string ClientName { get; set; }
        public string ClientICE { get; set; }
        public string ClientAddress { get; set; }

        // Totals
        public bool ShowSubtotal { get; set; }
        public bool ShowTVATotal { get; set; }
        public bool ShowGrandTotal { get; set; }

        // Additional Options
        public bool ShowNotes { get; set; }
        public string Notes { get; set; }
        public bool ShowPaymentTerms { get; set; }
        public string PaymentTerms { get; set; }

        // Per-article quantities entered by user (ArticleID → quantity)
        public Dictionary<int, decimal> ArticleQuantities { get; set; } = new Dictionary<int, decimal>();
    }
}