using Superete;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace GestionComerce.Main.Facturation.CreateFacture
{
    public partial class WAddArticle : Window
    {
        // ── deps ──────────────────────────────────────────────────────────
        private readonly CMainFa       _mainFa;
        private List<Article>          _allStockArticles;
        private Article                _selectedStockArticle;

        // ── mode flags ────────────────────────────────────────────────────
        private bool _isStockMode      = true;
        private bool _isExpeditionMode = false;

        // ── edit-single mode removed — editing is handled by WEditArticle ──

        // ── staged list (normal add mode) ─────────────────────────────────
        private readonly List<InvoiceArticle> _staged = new List<InvoiceArticle>();

        /// <summary>
        /// When >= 0, "Ajouter à la liste" replaces this index instead of appending.
        /// Set by OpenInlineEdit(), cleared after use.
        /// </summary>
        private int _inlineEditIndex = -1;

        // ══════════════════════════════════════════════════════════════════
        //  CONSTRUCTORS
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Normal "add articles" mode.</summary>
        public WAddArticle(CMainFa mainFa)
        {
            InitializeComponent();
            _mainFa           = mainFa;
            _isExpeditionMode = mainFa?.InvoiceType == "Expedition";

            LoadStockArticles();
            UpdateExpeditionVisibility();
            HookChangeHandlers();
            RefreshTable();
        }

        // ══════════════════════════════════════════════════════════════════
        //  INIT
        // ══════════════════════════════════════════════════════════════════

        private void LoadStockArticles()
        {
            try
            {
                _allStockArticles = _mainFa?.main?.la ?? new List<Article>();
                PopulateComboBox(_allStockArticles);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des articles : {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateComboBox(List<Article> articles)
        {
            cmbStockArticles.Items.Clear();
            foreach (var a in articles)
            {
                cmbStockArticles.Items.Add(new ComboBoxItem
                {
                    Content = $"{a.ArticleName}  —  {a.PrixVente:0.00} DH",
                    Tag     = a
                });
            }
        }

        private void UpdateExpeditionVisibility()
        {
            var v = _isExpeditionMode ? Visibility.Visible : Visibility.Collapsed;
            StockExpeditionPanel.Visibility  = v;
            CustomExpeditionPanel.Visibility = v;
        }

        private void HookChangeHandlers()
        {
            // Validation
            txtStockTVA.TextChanged         += TxtTVA_TextChanged;
            txtCustomTVA.TextChanged        += TxtTVA_TextChanged;
            txtStockExpedition.TextChanged  += TxtStockExpedition_TextChanged;
            txtCustomExpedition.TextChanged += TxtCustomExpedition_TextChanged;

            // Enable / disable Add button on any field change
            txtStockPrice.TextChanged    += (s, e) => UpdateAddButton();
            txtStockQuantity.TextChanged += (s, e) => UpdateAddButton();
            txtCustomName.TextChanged    += (s, e) => UpdateAddButton();
            txtCustomPrice.TextChanged   += (s, e) => UpdateAddButton();
            txtCustomQuantity.TextChanged+= (s, e) => UpdateAddButton();

            // Paste guards
            foreach (var tb in new[] { txtStockTVA, txtCustomTVA, txtStockPrice,
                                        txtCustomPrice, txtStockQuantity, txtCustomQuantity,
                                        txtStockExpedition, txtCustomExpedition })
                DataObject.AddPastingHandler(tb, OnPaste);
        }

        // ══════════════════════════════════════════════════════════════════
        //  MODE TOGGLE
        // ══════════════════════════════════════════════════════════════════

        private void btnStockArticle_Click(object sender, RoutedEventArgs e)  => SwitchToStockMode();
        private void btnCustomArticle_Click(object sender, RoutedEventArgs e) => SwitchToCustomMode();

        private void SwitchToStockMode()
        {
            _isStockMode = true;
            StockArticlePanel.Visibility  = Visibility.Visible;
            CustomArticlePanel.Visibility = Visibility.Collapsed;
            btnStockArticle.Background    = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6));
            btnStockArticle.Foreground    = Brushes.White;
            btnCustomArticle.Background   = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
            btnCustomArticle.Foreground   = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
            UpdateAddButton();
        }

        private void SwitchToCustomMode()
        {
            _isStockMode = false;
            StockArticlePanel.Visibility  = Visibility.Collapsed;
            CustomArticlePanel.Visibility = Visibility.Visible;
            btnCustomArticle.Background   = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
            btnCustomArticle.Foreground   = Brushes.White;
            btnStockArticle.Background    = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
            btnStockArticle.Foreground    = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
            UpdateAddButton();
        }

        // ══════════════════════════════════════════════════════════════════
        //  VALIDATION EVENTS
        // ══════════════════════════════════════════════════════════════════

        private void TxtTVA_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            bool bad = ParseDecimal(tb.Text) > 100;
            tb.BorderBrush = bad
                ? Brushes.Red
                : new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
            tb.ToolTip = bad ? "La TVA ne peut pas dépasser 100 %" : null;
        }

        private void TxtStockExpedition_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isExpeditionMode) return;
            bool bad = ParseDecimal(txtStockExpedition.Text) > ParseDecimal(txtStockQuantity.Text);
            txtStockExpedition.BorderBrush = bad
                ? Brushes.Red
                : new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
        }

        private void TxtCustomExpedition_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isExpeditionMode) return;
            bool bad = ParseDecimal(txtCustomExpedition.Text) > ParseDecimal(txtCustomQuantity.Text);
            txtCustomExpedition.BorderBrush = bad
                ? Brushes.Red
                : new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
        }

        private void UpdateAddButton()
        {
            if (btnAddToList == null) return;
            bool valid = _isStockMode
                ? (_selectedStockArticle != null
                   && ParseDecimal(txtStockQuantity.Text) > 0
                   && ParseDecimal(txtStockPrice.Text) > 0)
                : (!string.IsNullOrWhiteSpace(txtCustomName?.Text)
                   && ParseDecimal(txtCustomQuantity.Text) > 0
                   && ParseDecimal(txtCustomPrice.Text) > 0);
            btnAddToList.IsEnabled = valid;
        }

        // ══════════════════════════════════════════════════════════════════
        //  STOCK COMBO
        // ══════════════════════════════════════════════════════════════════

        private void txtSearchArticle_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = txtSearchArticle.Text?.Trim() ?? "";
            var filtered = string.IsNullOrWhiteSpace(q)
                ? _allStockArticles
                : _allStockArticles.Where(a =>
                    (a.ArticleName ?? "").ToLower().Contains(q.ToLower()) ||
                    a.PrixVente.ToString().Contains(q)).ToList();
            PopulateComboBox(filtered);
            if (filtered.Count > 0) cmbStockArticles.IsDropDownOpen = true;
        }

        private void cmbStockArticles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedStockArticle      = null;
            StockInfoBorder.Visibility = Visibility.Collapsed;

            if (cmbStockArticles.SelectedItem is ComboBoxItem item && item.Tag is Article art)
            {
                _selectedStockArticle      = art;
                txtStockPrice.Text         = art.PrixVente.ToString("0.00");
                txtStockTVA.Text           = art.tva.ToString("0.##");
                txtAvailableStock.Text     = GetStockQty(art).ToString();
                StockInfoBorder.Visibility = Visibility.Visible;
            }
            UpdateAddButton();
        }

        private int GetStockQty(Article art)
        {
            if (art == null) return 0;
            try
            {
                var p = art.GetType().GetProperties()
                    .FirstOrDefault(x =>
                        x.Name.ToLower().Contains("stock") ||
                        x.Name.ToLower().Contains("quantity") ||
                        x.Name.ToLower().Contains("quantite") ||
                        x.Name.ToLower().Contains("qte"));
                if (p != null)
                {
                    var v = p.GetValue(art);
                    if (v is int i)     return i;
                    if (v is decimal d) return (int)d;
                    if (v is double db) return (int)db;
                }
            }
            catch { }
            return 0;
        }

        // ══════════════════════════════════════════════════════════════════
        //  "AJOUTER À LA LISTE" — add new OR update existing row
        // ══════════════════════════════════════════════════════════════════

        private void btnAddToList_Click(object sender, RoutedEventArgs e)
        {
            InvoiceArticle article = _isStockMode ? BuildStockArticle() : BuildCustomArticle();
            if (article == null) return;

            if (_inlineEditIndex >= 0 && _inlineEditIndex < _staged.Count)
            {
                // ── REPLACE existing staged row ──
                _staged[_inlineEditIndex] = article;
                _inlineEditIndex          = -1;
                btnAddToList.Content      = "＋  Ajouter à la liste";
            }
            else
            {
                // ── APPEND new row ──
                _staged.Add(article);
            }

            RefreshTable();
            UpdateFooterButtons();

            if (_isStockMode) ResetStockForm();
            else              ResetCustomForm();
        }

        // ══════════════════════════════════════════════════════════════════
        //  BUILD ARTICLES
        // ══════════════════════════════════════════════════════════════════

        private InvoiceArticle BuildStockArticle()
        {
            decimal qty   = ParseDecimal(txtStockQuantity.Text);
            decimal price = ParseDecimal(txtStockPrice.Text);
            decimal tva   = ParseDecimal(txtStockTVA.Text);
            if (!ValidateCommon(qty, price, tva)) return null;

            int  stockQty    = GetStockQty(_selectedStockArticle);
            bool reduceStock = true;

            if (stockQty == 0)
            {
                var r = MessageBox.Show("Stock à 0. Ajouter quand même ?",
                    "Stock vide", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.No) return null;
                reduceStock = false;
            }
            else if (qty > stockQty)
            {
                var r = MessageBox.Show(
                    $"Quantité insuffisante. Disponible : {stockQty}\nAjouter quand même ?",
                    "Stock insuffisant", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.No) return null;
                reduceStock = false;
            }

            decimal expQty = _isExpeditionMode ? ParseDecimal(txtStockExpedition.Text) : qty;
            if (_isExpeditionMode && !ValidateExpedition(expQty, qty)) return null;

            return new InvoiceArticle
            {
                OperationID     = 0,
                ArticleID       = _selectedStockArticle.ArticleID,
                ArticleName     = _selectedStockArticle.ArticleName,
                Prix            = price,
                Quantite        = (int)Math.Round(qty),
                TVA             = tva,
                Reversed        = false,
                InitialQuantity = (int)Math.Round(expQty),
                ExpeditionTotal = (int)Math.Round(expQty),
                ReduceStock     = reduceStock
            };
        }

        private InvoiceArticle BuildCustomArticle()
        {
            string name  = txtCustomName.Text.Trim();
            decimal qty  = ParseDecimal(txtCustomQuantity.Text);
            decimal price= ParseDecimal(txtCustomPrice.Text);
            decimal tva  = ParseDecimal(txtCustomTVA.Text);

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Veuillez entrer un nom d'article.", "Attention",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCustomName.Focus(); return null;
            }
            if (!ValidateCommon(qty, price, tva)) return null;

            decimal expQty = _isExpeditionMode ? ParseDecimal(txtCustomExpedition.Text) : qty;
            if (_isExpeditionMode && !ValidateExpedition(expQty, qty)) return null;

            return new InvoiceArticle
            {
                OperationID     = 0,
                ArticleID       = -Math.Abs(DateTime.Now.Ticks.GetHashCode()),
                ArticleName     = name,
                Prix            = price,
                Quantite        = (int)Math.Round(qty),
                TVA             = tva,
                Reversed        = false,
                InitialQuantity = (int)Math.Round(expQty),
                ExpeditionTotal = (int)Math.Round(expQty),
                ReduceStock     = false
            };
        }

        private bool ValidateCommon(decimal qty, decimal price, decimal tva)
        {
            if (qty <= 0)
            {
                MessageBox.Show("Quantité invalide (doit être > 0).", "Attention",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (price <= 0)
            {
                MessageBox.Show("Prix invalide (doit être > 0).", "Attention",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (tva < 0 || tva > 100)
            {
                MessageBox.Show("TVA doit être entre 0 et 100 %.", "TVA invalide",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private bool ValidateExpedition(decimal expQty, decimal qty)
        {
            if (expQty < 0)
            {
                MessageBox.Show("La quantité expédiée ne peut pas être négative.", "Attention",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (expQty > qty)
            {
                MessageBox.Show(
                    $"La quantité expédiée ({expQty}) ne peut dépasser la quantité commandée ({qty}).",
                    "Quantité invalide", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        // ══════════════════════════════════════════════════════════════════
        //  TABLE RENDERING
        // ══════════════════════════════════════════════════════════════════

        private void RefreshTable()
        {
            ArticleTableRows.Children.Clear();

            decimal totalHT  = 0;
            decimal totalTVA = 0;

            for (int i = 0; i < _staged.Count; i++)
            {
                var a = _staged[i];
                totalHT  += a.TotalHT;
                totalTVA += a.MontantTVA;
                ArticleTableRows.Children.Add(BuildRow(a, i));
            }

            txtStagedTotalHT.Text  = totalHT.ToString("N2")             + " DH";
            txtStagedTVA.Text      = totalTVA.ToString("N2")            + " DH";
            txtStagedTotalTTC.Text = (totalHT + totalTVA).ToString("N2")+ " DH";

            txtEmptyHint.Visibility  = _staged.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            txtArticleCount.Text     = $"{_staged.Count} article(s) en attente";
        }

        private Border BuildRow(InvoiceArticle a, int index)
        {
            bool isCustom = a.ArticleID < 0;
            var rowBg = index % 2 == 0 ? Colors.White
                                        : Color.FromRgb(0xFA, 0xFB, 0xFC);

            var row = new Border
            {
                Background      = new SolidColorBrush(rowBg),
                BorderBrush     = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding         = new Thickness(16, 10, 16, 10)
            };

            var grid = new Grid();
            int[] widths = { 34, 0, 100, 100, 70, 65, 110, 90 };
            foreach (var w in widths)
                grid.ColumnDefinitions.Add(w == 0
                    ? new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                    : new ColumnDefinition { Width = new GridLength(w) });

            // # 
            SetCol(grid, 0, Cell($"{index + 1}", "#94A3B8", 12, FontWeights.Normal));

            // Name (+ expedition note if applicable)
            var namePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            namePanel.Children.Add(new TextBlock
            {
                Text       = a.ArticleName,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize   = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A))
            });
            if (_isExpeditionMode)
                namePanel.Children.Add(new TextBlock
                {
                    Text       = $"Expédié : {a.InitialQuantity}",
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize   = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)),
                    Margin     = new Thickness(0, 2, 0, 0)
                });
            Grid.SetColumn(namePanel, 1);
            grid.Children.Add(namePanel);

            // Type badge
            string bgHex = isCustom ? "#EDE9FE" : "#DBEAFE";
            string fgHex = isCustom ? "#6D28D9" : "#1D4ED8";
            string label = isCustom ? "Personnalisé" : "Stock";
            var badge = new Border
            {
                Background          = HexBrush(bgHex),
                CornerRadius        = new CornerRadius(4),
                Padding             = new Thickness(7, 3, 7, 3),
                VerticalAlignment   = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text       = label,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize   = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = HexBrush(fgHex)
                }
            };
            SetCol(grid, 2, badge);

            // Numeric cells
            SetCol(grid, 3, Cell(a.Prix.ToString("N2") + " DH",  "#0F172A", 13, FontWeights.Normal));
            SetCol(grid, 4, Cell(a.Quantite.ToString("0.##"),     "#0F172A", 13, FontWeights.Normal));
            SetCol(grid, 5, Cell(a.TVA.ToString("0.##") + " %",  "#64748B", 12, FontWeights.Normal));
            SetCol(grid, 6, Cell(a.TotalTTC.ToString("N2") + " DH", "#8B5CF6", 13, FontWeights.Bold));

            // Action buttons
            int captured = index;
            var editBtn = IconBtn("✏️", "#3B82F6");
            var delBtn  = IconBtn("🗑", "#EF4444");
            editBtn.Click += (s, e) => OpenInlineEdit(captured);
            delBtn.Click  += (s, e) =>
            {
                _staged.RemoveAt(captured);
                // If we were editing this row, cancel inline edit
                if (_inlineEditIndex == captured)
                {
                    _inlineEditIndex      = -1;
                    btnAddToList.Content  = "＋  Ajouter à la liste";
                }
                RefreshTable();
                UpdateFooterButtons();
            };

            var actions = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                VerticalAlignment   = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            actions.Children.Add(editBtn);
            actions.Children.Add(new Border { Width = 6 });
            actions.Children.Add(delBtn);
            SetCol(grid, 7, actions);

            row.Child = grid;
            return row;
        }

        // ══════════════════════════════════════════════════════════════════
        //  INLINE EDIT (edit a staged row via the form)
        // ══════════════════════════════════════════════════════════════════

        private void OpenInlineEdit(int index)
        {
            var art = _staged[index];
            _inlineEditIndex     = index;
            btnAddToList.Content = "💾  Mettre à jour";
            btnAddToList.IsEnabled = true;

            if (art.ArticleID < 0)
            {
                SwitchToCustomMode();
                txtCustomName.Text     = art.ArticleName;
                txtCustomPrice.Text    = art.Prix.ToString("0.00");
                txtCustomQuantity.Text = art.Quantite.ToString("0.##");
                txtCustomTVA.Text      = art.TVA.ToString("0.##");
                if (_isExpeditionMode)
                    txtCustomExpedition.Text = art.InitialQuantity.ToString("0.##");
            }
            else
            {
                SwitchToStockMode();
                foreach (ComboBoxItem item in cmbStockArticles.Items)
                {
                    if (item.Tag is Article a && a.ArticleID == art.ArticleID)
                    { cmbStockArticles.SelectedItem = item; break; }
                }
                txtStockPrice.Text    = art.Prix.ToString("0.00");
                txtStockQuantity.Text = art.Quantite.ToString("0.##");
                txtStockTVA.Text      = art.TVA.ToString("0.##");
                if (_isExpeditionMode)
                    txtStockExpedition.Text = art.InitialQuantity.ToString("0.##");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  CONFIRM / CLEAR / CANCEL
        // ══════════════════════════════════════════════════════════════════

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            // commit all staged articles
            if (_staged.Count == 0)
            {
                MessageBox.Show("Aucun article à confirmer.", "Attention",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            foreach (var art in _staged)
                _mainFa.AddManualArticle(art);
            Close();
        }

        private void btnClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (_staged.Count == 0) return;
            var r = MessageBox.Show("Vider toute la liste ?", "Confirmation",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (r != MessageBoxResult.Yes) return;

            _staged.Clear();
            _inlineEditIndex     = -1;
            btnAddToList.Content = "＋  Ajouter à la liste";
            RefreshTable();
            UpdateFooterButtons();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) => Close();

        private void UpdateFooterButtons()
        {
            btnConfirm.IsEnabled  = _staged.Count > 0;
            btnClearAll.IsEnabled = _staged.Count > 0;
        }

        // ══════════════════════════════════════════════════════════════════
        //  FORM RESETS
        // ══════════════════════════════════════════════════════════════════

        private void ResetStockForm()
        {
            txtSearchArticle.Text      = "";
            cmbStockArticles.SelectedIndex = -1;
            txtStockPrice.Text         = "";
            txtStockQuantity.Text      = "";
            txtStockTVA.Text           = "";
            txtAvailableStock.Text     = "";
            StockInfoBorder.Visibility = Visibility.Collapsed;
            if (_isExpeditionMode) txtStockExpedition.Text = "0";
            _selectedStockArticle = null;
            btnAddToList.IsEnabled = false;
            LoadStockArticles();
        }

        private void ResetCustomForm()
        {
            txtCustomName.Text     = "";
            txtCustomPrice.Text    = "";
            txtCustomQuantity.Text = "";
            txtCustomTVA.Text      = "";
            if (_isExpeditionMode) txtCustomExpedition.Text = "0";
            btnAddToList.IsEnabled = false;
            txtCustomName.Focus();
        }

        // ══════════════════════════════════════════════════════════════════
        //  INPUT GUARDS
        // ══════════════════════════════════════════════════════════════════

        private void DecimalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) { e.Handled = true; return; }
            if (e.Text == "." || e.Text == ",")
                e.Handled = tb.Text.Contains(".") || tb.Text.Contains(",");
            else
                e.Handled = !e.Text.All(char.IsDigit);
        }

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

        // ══════════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════════

        private decimal ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            decimal.TryParse(text.Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture, out decimal r);
            return r;
        }

        private static SolidColorBrush HexBrush(string hex) =>
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

        private static TextBlock Cell(string text, string hex, double size, FontWeight w) =>
            new TextBlock
            {
                Text              = text,
                FontFamily        = new FontFamily("Segoe UI"),
                FontSize          = size,
                FontWeight        = w,
                Foreground        = HexBrush(hex),
                VerticalAlignment = VerticalAlignment.Center
            };

        private static void SetCol(Grid g, int col, UIElement el)
        {
            Grid.SetColumn(el, col);
            g.Children.Add(el);
        }

        private static Button IconBtn(string icon, string bgHex)
        {
            var btn = new Button
            {
                Content         = icon,
                Width           = 30,
                Height          = 30,
                FontSize        = 13,
                Cursor          = Cursors.Hand,
                BorderThickness = new Thickness(0),
                Background      = HexBrush(bgHex),
                Foreground      = Brushes.White
            };
            var t  = new ControlTemplate(typeof(Button));
            var bd = new FrameworkElementFactory(typeof(Border));
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            bd.SetBinding(Border.BackgroundProperty,
                new Binding("Background")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
                });
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty,   VerticalAlignment.Center);
            bd.AppendChild(cp);
            t.VisualTree  = bd;
            btn.Template  = t;
            return btn;
        }
    }
}
