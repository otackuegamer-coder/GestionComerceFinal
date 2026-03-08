using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GestionComerce.Main.Facturation.CreateFacture
{
    public partial class WEditArticle : Window
    {
        private readonly CMainFa       mainFa;
        private readonly InvoiceArticle article;
        private readonly CSingleArticle articleControl;
        private readonly bool          isExpeditionMode;

        /// <summary>
        /// The original name when the window opened.
        /// Used to detect whether the user has changed it.
        /// </summary>
        private readonly string _originalName;

        /// <summary>
        /// Whether the article was a stock article (ArticleID >= 0 and OperationID == 0)
        /// when the window opened. If the user changes the name we flip it to custom.
        /// </summary>
        private readonly bool _wasStockArticle;

        /// <summary>
        /// Tracks whether the name has been edited away from the original.
        /// </summary>
        private bool _nameChanged = false;

        public WEditArticle(CMainFa mainFa, InvoiceArticle article, CSingleArticle articleControl)
        {
            InitializeComponent();
            this.mainFa         = mainFa;
            this.article        = article;
            this.articleControl = articleControl;

            isExpeditionMode = mainFa?.InvoiceType == "Expedition";

            // Capture original identity so we know when the user diverges from it
            _originalName    = article.ArticleName ?? "";
            _wasStockArticle = article.ArticleID > 0 && article.OperationID == 0;

            LoadArticleData();
            UpdateExpeditionFieldVisibility();
            UpdateTypeBadge(); // initial badge state

            // Validation handlers
            txtTVA.TextChanged       += TxtTVA_TextChanged;
            txtExpedition.TextChanged+= TxtExpedition_TextChanged;

            // Paste guards
            DataObject.AddPastingHandler(txtArticleName, OnPasteText);
            DataObject.AddPastingHandler(txtTVA,         OnPaste);
            DataObject.AddPastingHandler(txtPrice,       OnPaste);
            DataObject.AddPastingHandler(txtQuantity,    OnPaste);
            DataObject.AddPastingHandler(txtExpedition,  OnPaste);
        }

        // ── Load ──────────────────────────────────────────────────────────

        private void LoadArticleData()
        {
            txtArticleNameLabel.Text = _originalName;
            txtArticleName.Text      = article.ArticleName;
            txtPrice.Text            = article.Prix.ToString("0.00");
            txtQuantity.Text         = article.Quantite.ToString("0.##");
            txtTVA.Text              = article.TVA.ToString("0.##");

            if (isExpeditionMode)
                txtExpedition.Text = article.InitialQuantity.ToString("0.##");
        }

        private void UpdateExpeditionFieldVisibility()
        {
            ExpeditionPanel.Visibility = isExpeditionMode
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // ── Type badge ────────────────────────────────────────────────────

        private void UpdateTypeBadge()
        {
            bool isNowCustom = _wasStockArticle && _nameChanged;

            if (article.ArticleID < 0)
            {
                // Was already custom
                SetBadge("Personnalisé", "#EDE9FE", "#6D28D9");
            }
            else if (article.OperationID != 0)
            {
                SetBadge("Opération", "#DCFCE7", "#059669");
            }
            else if (isNowCustom)
            {
                // Stock article whose name has been changed → will become custom on save
                SetBadge("Personnalisé *", "#FEF3C7", "#D97706");
            }
            else
            {
                SetBadge("Stock", "#DBEAFE", "#1D4ED8");
            }
        }

        private void SetBadge(string text, string bgHex, string fgHex)
        {
            txtBadgeType.Text      = text;
            badgeType.Background   = HexBrush(bgHex);
            txtBadgeType.Foreground= HexBrush(fgHex);
        }

        // ── Name-change handler ───────────────────────────────────────────

        private void txtArticleName_TextChanged(object sender, TextChangedEventArgs e)
        {
            string current = txtArticleName.Text ?? "";
            _nameChanged   = _wasStockArticle && current != _originalName;
            UpdateTypeBadge();
        }

        // ── Validation events ─────────────────────────────────────────────

        private void TxtTVA_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            bool bad = ParseDecimal(tb.Text) > 100;
            tb.BorderBrush = bad ? Brushes.Red : (Brush)new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
            tb.ToolTip = bad ? "La TVA ne peut pas dépasser 100 %" : null;
        }

        private void TxtExpedition_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isExpeditionMode) return;
            var tb  = sender as TextBox;
            if (tb == null) return;
            bool bad = ParseDecimal(txtExpedition.Text) > ParseDecimal(txtQuantity.Text);
            tb.BorderBrush = bad ? Brushes.Red : (Brush)new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
            tb.ToolTip = bad ? $"Ne peut pas dépasser la quantité commandée ({ParseDecimal(txtQuantity.Text)})" : null;
        }

        // ── Save ──────────────────────────────────────────────────────────

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Name
                string newName = txtArticleName.Text?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(newName))
                {
                    MessageBox.Show("Veuillez entrer un nom d'article.", "Attention",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtArticleName.Focus();
                    return;
                }

                // Price
                decimal price = ParseDecimal(txtPrice.Text);
                if (price <= 0)
                {
                    MessageBox.Show("Veuillez entrer un prix valide supérieur à 0.", "Attention",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPrice.Focus();
                    return;
                }

                // Quantity
                decimal quantity = ParseDecimal(txtQuantity.Text);
                if (quantity <= 0)
                {
                    MessageBox.Show("Veuillez entrer une quantité valide supérieure à 0.", "Attention",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtQuantity.Focus();
                    return;
                }

                // TVA
                decimal tva = ParseDecimal(txtTVA.Text);
                if (tva > 100)
                {
                    MessageBox.Show("La TVA ne peut pas dépasser 100 %.\nVeuillez entrer une valeur entre 0 et 100.",
                        "TVA invalide", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtTVA.Focus(); txtTVA.SelectAll();
                    return;
                }
                if (tva < 0)
                {
                    MessageBox.Show("La TVA ne peut pas être négative.", "Attention",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtTVA.Focus(); txtTVA.SelectAll();
                    return;
                }

                // Expedition
                decimal expeditionQty = quantity;
                if (isExpeditionMode)
                {
                    expeditionQty = ParseDecimal(txtExpedition.Text);
                    if (expeditionQty < 0)
                    {
                        MessageBox.Show("La quantité expédiée ne peut pas être négative.", "Attention",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtExpedition.Focus(); txtExpedition.SelectAll();
                        return;
                    }
                    if (expeditionQty > quantity)
                    {
                        MessageBox.Show(
                            $"La quantité expédiée ({expeditionQty}) ne peut pas dépasser la quantité commandée ({quantity}).",
                            "Quantité invalide", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtExpedition.Focus(); txtExpedition.SelectAll();
                        return;
                    }
                }

                // ── Apply changes ──

                article.ArticleName = newName;
                article.Prix        = price;
                article.Quantite    = quantity;
                article.TVA         = tva;

                if (isExpeditionMode)
                {
                    article.InitialQuantity = expeditionQty;
                    article.ExpeditionTotal = expeditionQty;
                }

                // If the name of a stock article was changed → convert to custom
                // A negative ArticleID is the convention for custom articles.
                if (_wasStockArticle && _nameChanged)
                {
                    article.ArticleID  = -Math.Abs(DateTime.Now.Ticks.GetHashCode());
                    article.OperationID= 0;
                }

                // Refresh the card in the invoice and recalculate
                articleControl.UpdateArticle(article);
                mainFa.RecalculateTotals();

                MessageBox.Show("Article modifié avec succès !", "Succès",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la modification : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) => Close();

        // ── Input guards ──────────────────────────────────────────────────

        private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) { e.Handled = true; return; }
            if (e.Text == "." || e.Text == ",")
                e.Handled = tb.Text.Contains(".") || tb.Text.Contains(",");
            else
                e.Handled = !e.Text.All(char.IsDigit);
        }

        /// <summary>Paste handler for numeric-only fields.</summary>
        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string t = (string)e.DataObject.GetData(typeof(string));
                if (!t.All(c => char.IsDigit(c) || c == '.' || c == ','))
                    e.CancelCommand();
            }
            else e.CancelCommand();
        }

        /// <summary>Paste handler for the name field (allow any text).</summary>
        private void OnPasteText(object sender, DataObjectPastingEventArgs e)
        {
            // No restriction on the name field — just let it through
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private decimal ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            decimal.TryParse(text.Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture, out decimal r);
            return r;
        }

        private static SolidColorBrush HexBrush(string hex) =>
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }
}
