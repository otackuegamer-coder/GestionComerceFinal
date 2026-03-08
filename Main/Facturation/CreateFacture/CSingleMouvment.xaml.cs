using GestionComerce;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GestionComerce.Main.Facturation.CreateFacture
{
    /// <summary>
    /// Interaction logic for CSingleMouvment.xaml
    /// </summary>
    public partial class CSingleMouvment : UserControl
    {
        public WMouvments ms;
        OperationArticle oa;
        Article article;
        CSingleOperation singleOp;
        private decimal expeditionQuantity = 0;
        private decimal expeditionTotalQuantity = 0;
        private decimal articlePrice = 0;
        private decimal articleTVA = 0;
        private bool isOperationSelected = false;
        private bool isExpeditionMode = false;

        // ADD THESE PROPERTIES
        public int ArticleIDValue
        {
            get
            {
                if (oa != null) return oa.ArticleID;
                if (article != null) return article.ArticleID;
                return 0;
            }
        }

        public string ArticleNameValue
        {
            get
            {
                if (!string.IsNullOrEmpty(ArticleName.Text)) return ArticleName.Text;
                if (article != null) return article.ArticleName;
                return "Unknown Article";
            }
        }

        public CSingleMouvment(WMouvments ms, OperationArticle oa)
        {
            InitializeComponent();
            this.ms = ms;
            this.oa = oa;
            this.singleOp = ms?.wso;

            // Check if operation is selected (in CMainFa view, not selection view)
            isOperationSelected = singleOp?.mainfa != null && singleOp.mainfa.SelectedOperations.Any(o => o.OperationID == oa.OperationID);

            // Check if oa is null
            if (oa == null)
            {
                MessageBox.Show("OperationArticle is null", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ArticleQuantity.Text = oa.QteArticle.ToString();

            // Load article information and initialize UI - NOW LOADS FROM DATABASE INCLUDING DELETED ARTICLES
            InitializeAsync();
        }

        // NEW METHOD: Initialize async - Load article from database and setup UI
        private async void InitializeAsync()
        {
            try
            {
                // Load ALL articles from database (including deleted ones)
                Article tempArticle = new Article();
                List<Article> allArticles = await tempArticle.GetAllArticlesAsync();

                // Find the article matching this OperationArticle
                Article foundArticle = allArticles.FirstOrDefault(a => a.ArticleID == oa.ArticleID);

                if (foundArticle != null)
                {
                    article = foundArticle;
                    ArticleName.Text = article.ArticleName;
                    articlePrice = article.PrixVente;
                    articleTVA = article.tva;

                    // Add indicator if article is deleted
                    if (article.Etat == false)
                    {
                        ArticleName.Text += " (Supprimé)";
                    }

                    if (oa.Reversed == true)
                    {
                        ArticleName.Text += " (Reversed)";
                    }
                }
                else
                {
                    ArticleName.Text = "Article not found (ID: " + oa.ArticleID + ")";
                }

                // NOW setup the UI after article is loaded
                InitializeUIControls();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading article: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ArticleName.Text = "Error loading article (ID: " + oa.ArticleID + ")";
            }
        }

        // Initialize UI controls after article is loaded
        private void InitializeUIControls()
        {
            // Get invoice type and CMainFa reference
            string invoiceType = null;
            CMainFa mainfa = null;

            if (singleOp?.mainfa != null)
            {
                mainfa = singleOp.mainfa;
                invoiceType = singleOp.mainfa.InvoiceType;
            }
            else if (ms?.wso?.sc?.main != null)
            {
                mainfa = ms.wso.sc.main;
                invoiceType = ms.wso.sc.main.InvoiceType;
            }

            isExpeditionMode = invoiceType == "Expedition";

            // Show quantity editor for all invoice types when operation is selected
            if (isOperationSelected)
            {
                NbrArticleExpide.Visibility = Visibility.Visible;
                NbrArticleExpide.IsEnabled = true;
                TxtPrice.Visibility = Visibility.Visible;
                TxtPrice.IsEnabled = true;
                TxtTVA.Visibility = Visibility.Visible;
                TxtTVA.IsEnabled = true;

                // Show expedition total only in expedition mode
                if (isExpeditionMode)
                {
                    TxtExpeditionTotal.Visibility = Visibility.Visible;
                    TxtExpeditionTotal.IsEnabled = true;
                }
                else
                {
                    TxtExpeditionTotal.Visibility = Visibility.Collapsed;
                }

                // Try to get saved quantity from InvoiceArticles
                var savedData = GetSavedDataFromInvoiceArticles(mainfa);

                // Set quantity
                NbrArticleExpide.Text = savedData.Quantity.ToString();
                expeditionQuantity = savedData.Quantity;

                // Set expedition total (only in expedition mode)
                if (isExpeditionMode)
                {
                    decimal defaultExpeditionTotal = savedData.ExpeditionTotal > 0 ? savedData.ExpeditionTotal : oa.QteArticle;
                    TxtExpeditionTotal.Text = defaultExpeditionTotal.ToString();
                    expeditionTotalQuantity = defaultExpeditionTotal;
                }

                // Set price
                TxtPrice.Text = savedData.Price.ToString("F2");
                articlePrice = savedData.Price;

                // Set TVA
                TxtTVA.Text = savedData.TVA.ToString("F2");
                articleTVA = savedData.TVA;

                // Add event handlers
                NbrArticleExpide.TextChanged += NbrArticleExpide_TextChanged;
                NbrArticleExpide.PreviewTextInput += NbrArticleExpide_PreviewTextInput;
                NbrArticleExpide.LostFocus += NbrArticleExpide_LostFocus;

                TxtExpeditionTotal.TextChanged += TxtExpeditionTotal_TextChanged;
                TxtExpeditionTotal.PreviewTextInput += TxtExpeditionTotal_PreviewTextInput;
                TxtExpeditionTotal.LostFocus += TxtExpeditionTotal_LostFocus;

                TxtPrice.TextChanged += TxtPrice_TextChanged;
                TxtPrice.PreviewTextInput += TxtPrice_PreviewTextInput;
                TxtPrice.LostFocus += TxtPrice_LostFocus;

                TxtTVA.TextChanged += TxtTVA_TextChanged;
                TxtTVA.PreviewTextInput += TxtTVA_PreviewTextInput;
                TxtTVA.LostFocus += TxtTVA_LostFocus;
            }
            else
            {
                NbrArticleExpide.Visibility = Visibility.Collapsed;
                NbrArticleExpide.IsEnabled = false;
                TxtExpeditionTotal.Visibility = Visibility.Collapsed;
                TxtExpeditionTotal.IsEnabled = false;
                TxtPrice.Visibility = Visibility.Collapsed;
                TxtPrice.IsEnabled = false;
                TxtTVA.Visibility = Visibility.Collapsed;
                TxtTVA.IsEnabled = false;
            }
        }

        // Get saved data from InvoiceArticles
        private (decimal Quantity, decimal ExpeditionTotal, decimal Price, decimal TVA) GetSavedDataFromInvoiceArticles(CMainFa mainfa)
        {
            if (mainfa?.InvoiceArticles == null || article == null || oa == null)
                return (oa?.QteArticle ?? 0, oa?.QteArticle ?? 0, articlePrice, articleTVA);

            var invoiceArticle = mainfa.InvoiceArticles.FirstOrDefault(ia =>
                ia.OperationID == oa.OperationID &&
                ia.ArticleID == oa.ArticleID);

            if (invoiceArticle != null)
            {
                decimal expTotal = invoiceArticle.ExpeditionTotal > 0 ? invoiceArticle.ExpeditionTotal : oa.QteArticle;
                return (invoiceArticle.Quantite, expTotal, invoiceArticle.Prix, invoiceArticle.TVA);
            }

            return (oa.QteArticle, oa.QteArticle, articlePrice, articleTVA);
        }

        // Prevent non-numeric input
        private void NbrArticleExpide_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0) && e.Text != "." && e.Text != ",";
        }

        private void TxtExpeditionTotal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0) && e.Text != "." && e.Text != ",";
        }

        private void TxtPrice_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0) && e.Text != "." && e.Text != ",";
        }

        private void TxtTVA_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0) && e.Text != "." && e.Text != ",";
        }

        private void NbrArticleExpide_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (oa == null)
                return;

            TextBox textBox = sender as TextBox;
            if (textBox == null || string.IsNullOrWhiteSpace(textBox.Text))
                return;

            string text = textBox.Text.Replace(',', '.');

            if (decimal.TryParse(text, out decimal enteredValue))
            {
                if (isExpeditionMode && enteredValue > expeditionTotalQuantity)
                {
                    MessageBox.Show(
                        $"La quantité ne peut pas dépasser la quantité totale expédiée ({expeditionTotalQuantity})",
                        "Limite dépassée",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    textBox.Text = expeditionTotalQuantity.ToString();
                    textBox.CaretIndex = textBox.Text.Length;
                    expeditionQuantity = expeditionTotalQuantity;
                }
                else
                {
                    expeditionQuantity = enteredValue;

                    // Auto-update for all invoice types when operation is selected
                    if (isOperationSelected)
                    {
                        UpdateInvoiceArticle();
                    }
                }
            }
            else
            {
                textBox.Text = "0";
                textBox.CaretIndex = textBox.Text.Length;
                expeditionQuantity = 0;

                if (isOperationSelected)
                {
                    UpdateInvoiceArticle();
                }
            }
        }

        private void NbrArticleExpide_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isOperationSelected)
            {
                UpdateInvoiceArticle();
            }
        }

        private void TxtExpeditionTotal_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (oa == null)
                return;

            TextBox textBox = sender as TextBox;
            if (textBox == null || string.IsNullOrWhiteSpace(textBox.Text))
                return;

            string text = textBox.Text.Replace(',', '.');

            if (decimal.TryParse(text, out decimal enteredValue))
            {
                if (enteredValue < 0)
                {
                    textBox.Text = "0";
                    textBox.CaretIndex = textBox.Text.Length;
                    expeditionTotalQuantity = 0;
                }
                else
                {
                    expeditionTotalQuantity = enteredValue;

                    // Adjust current quantity if it exceeds new total
                    if (expeditionQuantity > enteredValue)
                    {
                        expeditionQuantity = enteredValue;
                        NbrArticleExpide.Text = enteredValue.ToString();
                    }

                    if (isOperationSelected)
                    {
                        UpdateInvoiceArticle();
                    }
                }
            }
            else
            {
                textBox.Text = "0";
                textBox.CaretIndex = textBox.Text.Length;
                expeditionTotalQuantity = 0;

                if (isOperationSelected)
                {
                    UpdateInvoiceArticle();
                }
            }
        }

        private void TxtExpeditionTotal_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isOperationSelected)
            {
                UpdateInvoiceArticle();
            }
        }

        private void TxtPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null || string.IsNullOrWhiteSpace(textBox.Text))
                return;

            string text = textBox.Text.Replace(',', '.');

            if (decimal.TryParse(text, out decimal enteredValue))
            {
                if (enteredValue < 0)
                {
                    textBox.Text = "0";
                    textBox.CaretIndex = textBox.Text.Length;
                    articlePrice = 0;
                }
                else
                {
                    articlePrice = enteredValue;

                    if (isOperationSelected)
                    {
                        UpdateInvoiceArticle();
                    }
                }
            }
            else
            {
                textBox.Text = "0";
                textBox.CaretIndex = textBox.Text.Length;
                articlePrice = 0;

                if (isOperationSelected)
                {
                    UpdateInvoiceArticle();
                }
            }
        }

        private void TxtPrice_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isOperationSelected)
            {
                UpdateInvoiceArticle();
            }
        }

        private void TxtTVA_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null || string.IsNullOrWhiteSpace(textBox.Text))
                return;

            string text = textBox.Text.Replace(',', '.');

            if (decimal.TryParse(text, out decimal enteredValue))
            {
                if (enteredValue < 0)
                {
                    textBox.Text = "0";
                    textBox.CaretIndex = textBox.Text.Length;
                    articleTVA = 0;
                }
                else if (enteredValue > 100)
                {
                    textBox.Text = "100";
                    textBox.CaretIndex = textBox.Text.Length;
                    articleTVA = 100;

                    MessageBox.Show(
                        "La TVA ne peut pas dépasser 100%",
                        "Limite dépassée",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
                else
                {
                    articleTVA = enteredValue;

                    if (isOperationSelected)
                    {
                        UpdateInvoiceArticle();
                    }
                }
            }
            else
            {
                textBox.Text = "0";
                textBox.CaretIndex = textBox.Text.Length;
                articleTVA = 0;

                if (isOperationSelected)
                {
                    UpdateInvoiceArticle();
                }
            }
        }

        private void TxtTVA_LostFocus(object sender, RoutedEventArgs e)
        {
            if (isOperationSelected)
            {
                UpdateInvoiceArticle();
            }
        }

        public void UpdateInvoiceArticle()
        {
            if (oa == null || article == null || !isOperationSelected)
                return;

            CMainFa mainfa = null;
            if (singleOp?.mainfa != null)
            {
                mainfa = singleOp.mainfa;
            }
            else if (ms?.wso?.sc?.main != null)
            {
                mainfa = ms.wso.sc.main;
            }

            if (mainfa == null)
                return;

            // Get values from textboxes
            decimal quantityToUse = expeditionQuantity;
            decimal expeditionTotalToUse = expeditionTotalQuantity;
            decimal priceToUse = articlePrice;
            decimal tvaToUse = articleTVA;

            if (NbrArticleExpide.Visibility == Visibility.Visible)
            {
                if (decimal.TryParse(NbrArticleExpide.Text.Replace(',', '.'), out decimal expQuantity))
                {
                    quantityToUse = expQuantity;
                    expeditionQuantity = expQuantity;
                }
            }

            if (TxtExpeditionTotal.Visibility == Visibility.Visible)
            {
                if (decimal.TryParse(TxtExpeditionTotal.Text.Replace(',', '.'), out decimal expTotal))
                {
                    expeditionTotalToUse = expTotal;
                    expeditionTotalQuantity = expTotal;
                }
            }

            if (TxtPrice.Visibility == Visibility.Visible)
            {
                if (decimal.TryParse(TxtPrice.Text.Replace(',', '.'), out decimal price))
                {
                    priceToUse = price;
                    articlePrice = price;
                }
            }

            if (TxtTVA.Visibility == Visibility.Visible)
            {
                if (decimal.TryParse(TxtTVA.Text.Replace(',', '.'), out decimal tva))
                {
                    tvaToUse = tva;
                    articleTVA = tva;
                }
            }

            // Update or add article - each operation article is separate
            mainfa.UpdateOrAddArticleSeparate(oa.OperationID, oa.ArticleID, article.ArticleName,
                quantityToUse, priceToUse, tvaToUse, oa.QteArticle, expeditionTotalToUse);
        }
    }
}