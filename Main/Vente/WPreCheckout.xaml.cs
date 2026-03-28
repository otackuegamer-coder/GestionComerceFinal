using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GestionComerce.Vente; // For WFacturePreview and FactureSettings
using GestionComerce.Main.ClientPage; // For ClientFormWindow
using GestionComerce.Main.Facturation.CreateFacture; // For WFacturePage
using GestionComerce.Main.Facturation; // For InvoiceArticle

namespace GestionComerce.Main.Vente
{
    public partial class WPreCheckout : Window
    {
        public class CartItem
        {
            public Article Article { get; set; }
            public int Quantity { get; set; }
            public decimal DiscountPercent { get; set; }
            public decimal PrixUnitaire => Article.PrixVente;
            public decimal SubTotal => PrixUnitaire * Quantity;
            public decimal DiscountAmount => SubTotal * (DiscountPercent / 100);
            public decimal Total => SubTotal - DiscountAmount;

            /// <summary>
            /// True when the article had Quantite &lt;= 0 at the moment it was added to the cart.
            /// When true, stock will NOT be deducted and the article will NOT be auto-deleted.
            /// </summary>
            public bool WasOutOfStock { get; set; }
        }

        public List<CartItem> CartItems { get; set; }
        public decimal AdditionalDiscount { get; set; }
        public decimal ClientDiscount { get; set; }
        public bool IsPercentageDiscount { get; set; }
        public string PaymentMethodName { get; set; }
        public int PaymentMethodID { get; set; }
        public int PaymentType { get; set; }
        public Client SelectedClient { get; set; }
        public decimal CreditAmount { get; set; }

        private CMainV _parentVente;
        private List<Article> _availableArticles;
        private List<Famille> _familles;
        private List<Fournisseur> _fournisseurs;
        private List<Client> _allClients;

        public bool DialogConfirmed { get; private set; }

        public WPreCheckout(
            CMainV parentVente,
            List<CartItem> cartItems,
            string paymentMethod,
            int paymentMethodID,
            int paymentType,
            List<Article> availableArticles,
            List<Famille> familles,
            List<Fournisseur> fournisseurs)
        {
            InitializeComponent();

            _parentVente = parentVente;

            // Copy items and stamp WasOutOfStock based on current article stock
            CartItems = cartItems.Select(item =>
            {
                item.WasOutOfStock = item.Article.Quantite <= 0;
                return item;
            }).ToList();

            PaymentMethodName = paymentMethod;
            PaymentMethodID = paymentMethodID;
            PaymentType = paymentType;
            _availableArticles = availableArticles;
            _familles = familles;
            _fournisseurs = fournisseurs;

            this.WindowState = WindowState.Maximized;
            this.MinWidth = 1200;
            this.MinHeight = 700;

            PaymentMethodText.Text = paymentMethod;

            // FIX: ClientSelectionRequired is a TextBlock inside ClientRequiredBanner (Border).
            // We show the Border, not the TextBlock directly.
            switch (paymentType)
            {
                case 0:
                    PaymentTypeText.Text = "VENTE COMPTANT";
                    CreditSection.Visibility = Visibility.Collapsed;
                    break;
                case 1:
                    PaymentTypeText.Text = "VENTE PARTIELLE (50/50)";
                    CreditSection.Visibility = Visibility.Visible;
                    ClientRequiredBanner.Visibility = Visibility.Visible; // FIX
                    break;
                case 2:
                    PaymentTypeText.Text = "VENTE À CRÉDIT";
                    CreditSection.Visibility = Visibility.Collapsed;
                    ClientRequiredBanner.Visibility = Visibility.Visible; // FIX
                    break;
            }

            this.Loaded += async (s, e) =>
            {
                await LoadClients();
                LoadCartItems();
                UpdateSummary();
            };
        }

        private async System.Threading.Tasks.Task LoadClients()
        {
            try
            {
                Client clientHelper = new Client();
                _allClients = await clientHelper.GetClientsAsync();
                FilterAndLoadClients("");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des clients: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterAndLoadClients(string searchText)
        {
            ClientComboBox.Items.Clear();
            ClientComboBox.Items.Add("Sans Client");

            if (_allClients == null) return;

            var filteredClients = _allClients;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.ToLower();
                filteredClients = _allClients.Where(c =>
                    c.Nom.ToLower().Contains(searchText) ||
                    (c.Telephone != null && c.Telephone.Contains(searchText)) ||
                    (c.ICE != null && c.ICE.ToLower().Contains(searchText))
                ).ToList();
            }

            foreach (var client in filteredClients)
                ClientComboBox.Items.Add($"{client.Nom} - {client.Telephone}");

            if (ClientComboBox.Items.Count > 0)
                ClientComboBox.SelectedIndex = 0;
        }

        private void ClientSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterAndLoadClients(ClientSearchBox.Text);
        }

        private void LoadCartItems()
        {
            ArticlesContainer.Children.Clear();

            if (CartItems == null || CartItems.Count == 0)
                return;

            foreach (var item in CartItems)
                ArticlesContainer.Children.Add(CreateCartItemControl(item));
        }

        private Border CreateCartItemControl(CartItem item)
        {
            var border = new Border
            {
                // Out-of-stock items get a warm amber tint so the user can spot them instantly
                Background = item.WasOutOfStock
                    ? new SolidColorBrush(Color.FromRgb(255, 251, 235))
                    : Brushes.White,
                BorderBrush = item.WasOutOfStock
                    ? new SolidColorBrush(Color.FromRgb(245, 158, 11))
                    : (SolidColorBrush)FindResource("BorderLight"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(12) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ── TOP ROW ──────────────────────────────────────────────────────
            var infoGrid = new Grid();
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 250 });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 180 });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 180 });
            Grid.SetRow(infoGrid, 0);

            // Left: name + badge + details
            var leftPanel = new StackPanel { Orientation = Orientation.Vertical };

            if (item.WasOutOfStock)
            {
                var badge = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(254, 243, 199)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 3, 8, 3),
                    Margin = new Thickness(0, 0, 0, 6),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                badge.Child = new TextBlock
                {
                    Text = "⚠️ Rupture de stock — stock non déduit",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(146, 64, 14))
                };
                leftPanel.Children.Add(badge);
            }

            leftPanel.Children.Add(new TextBlock
            {
                Text = item.Article.ArticleName,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)FindResource("TextPrimary"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 6)
            });

            var detailsPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 4, 0, 0) };

            if (!string.IsNullOrWhiteSpace(item.Article.marque))
                detailsPanel.Children.Add(CreateInfoText($"Marque: {item.Article.marque}", 11));

            if (!string.IsNullOrWhiteSpace(item.Article.Description))
            {
                var desc = item.Article.Description.Length > 80
                    ? item.Article.Description.Substring(0, 80) + "..." : item.Article.Description;
                detailsPanel.Children.Add(CreateInfoText(desc, 11, true));
            }

            if (item.Article.DateExpiration.HasValue)
            {
                var days = (item.Article.DateExpiration.Value - DateTime.Now).Days;
                var col = days < 30 ? new SolidColorBrush(Color.FromRgb(239, 68, 68))
                        : days < 90 ? new SolidColorBrush(Color.FromRgb(245, 158, 11))
                        : (SolidColorBrush)FindResource("TextSecondary");
                detailsPanel.Children.Add(CreateInfoText($"Expiration: {item.Article.DateExpiration.Value:dd/MM/yyyy}", 11, false, col));
            }

            leftPanel.Children.Add(detailsPanel);
            Grid.SetColumn(leftPanel, 0);

            // Middle: stock & packaging
            var middlePanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(16, 0, 0, 0) };

            var stockRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            stockRow.Children.Add(new TextBlock { Text = "Stock: ", FontSize = 11, Foreground = (SolidColorBrush)FindResource("TextSecondary") });
            stockRow.Children.Add(new TextBlock
            {
                Text = item.Article.GetStockDisplayString(),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = item.Article.Quantite <= 0
                    ? new SolidColorBrush(Color.FromRgb(239, 68, 68))
                    : item.Article.IsLowStock()
                        ? new SolidColorBrush(Color.FromRgb(245, 158, 11))
                        : new SolidColorBrush(Color.FromRgb(34, 197, 94))
            });
            middlePanel.Children.Add(stockRow);

            if (item.Article.PiecesPerPackage > 1)
            {
                int packs = (int)Math.Ceiling((double)item.Quantity / item.Article.PiecesPerPackage);
                middlePanel.Children.Add(CreateInfoText($"📦 {item.Article.PiecesPerPackage} pcs/paquet", 11));
                middlePanel.Children.Add(CreateInfoText($"Paquets: {packs}", 11, false, new SolidColorBrush(Color.FromRgb(59, 130, 246))));
            }

            if (!string.IsNullOrWhiteSpace(item.Article.PackageType))
                middlePanel.Children.Add(CreateInfoText($"Type: {item.Article.PackageType}", 11));

            if (!string.IsNullOrWhiteSpace(item.Article.UnitOfMeasure) && item.Article.UnitOfMeasure != "piece")
                middlePanel.Children.Add(CreateInfoText($"Unité: {item.Article.UnitOfMeasure}", 11));

            if (!string.IsNullOrWhiteSpace(item.Article.StorageLocation))
                middlePanel.Children.Add(CreateInfoText($"📍 {item.Article.StorageLocation}", 11));

            Grid.SetColumn(middlePanel, 1);

            // Right: codes & misc
            var rightPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(16, 0, 0, 0) };

            if (!string.IsNullOrEmpty(item.Article.Code))
                rightPanel.Children.Add(CreateInfoText($"Code-barres: {item.Article.Code}", 11, false, new SolidColorBrush(Color.FromRgb(59, 130, 246))));
            if (!string.IsNullOrWhiteSpace(item.Article.SKU))
                rightPanel.Children.Add(CreateInfoText($"SKU: {item.Article.SKU}", 11));

            if (!string.IsNullOrWhiteSpace(item.Article.numeroLot))
                rightPanel.Children.Add(CreateInfoText($"Lot: {item.Article.numeroLot}", 11));

            if (item.Article.tva > 0)
                rightPanel.Children.Add(CreateInfoText($"TVA: {item.Article.tva}%", 11));

            if (item.Article.MinQuantityForGros > 0 && item.Article.PrixGros > 0)
            {
                bool isWholesale = item.Quantity >= item.Article.MinQuantityForGros;
                rightPanel.Children.Add(isWholesale
                    ? CreateInfoText("💰 Prix Gros actif!", 11, false, new SolidColorBrush(Color.FromRgb(34, 197, 94)))
                    : CreateInfoText($"Prix Gros: {item.Article.MinQuantityForGros}+ unités", 10, true));
            }

            Grid.SetColumn(rightPanel, 2);

            infoGrid.Children.Add(leftPanel);
            infoGrid.Children.Add(middlePanel);
            infoGrid.Children.Add(rightPanel);

            // ── BOTTOM ROW: Controls ─────────────────────────────────────────
            var controlsGrid = new Grid();
            controlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            controlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            controlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            controlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            controlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            controlsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            Grid.SetRow(controlsGrid, 2);

            var totalText = new TextBlock
            {
                Text = item.Total.ToString("F2") + " DH",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = (SolidColorBrush)FindResource("PrimaryBlue"),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right
            };
            Grid.SetColumn(totalText, 4);

            // Price
            var pricePanel = CreateControlPanel("Prix Unit.", priceBox =>
            {
                priceBox.Text = item.PrixUnitaire.ToString("F2");
                priceBox.TextChanged += (s, e) =>
                {
                    if (decimal.TryParse(priceBox.Text, out decimal np) && np >= 0)
                    {
                        item.Article.PrixVente = np;
                        totalText.Text = item.Total.ToString("F2") + " DH";
                        UpdateSummary();
                    }
                };
            });
            Grid.SetColumn(pricePanel, 0);

            // Quantity
            var qtyPanel = CreateControlPanel("Quantité", qtyBox =>
            {
                qtyBox.Text = item.Quantity.ToString();
                qtyBox.TextAlignment = TextAlignment.Center;
                qtyBox.TextChanged += (s, e) =>
                {
                    if (int.TryParse(qtyBox.Text, out int nq) && nq > 0)
                    {
                        // Out-of-stock items: allow any quantity, stock won't be touched anyway
                        if (item.WasOutOfStock || item.Article.IsUnlimitedStock || nq <= item.Article.Quantite)
                        {
                            item.Quantity = nq;
                            totalText.Text = item.Total.ToString("F2") + " DH";
                            UpdateSummary();
                        }
                        else
                        {
                            MessageBox.Show($"Quantité disponible: {item.Article.Quantite}",
                                "Stock Insuffisant", MessageBoxButton.OK, MessageBoxImage.Warning);
                            qtyBox.Text = item.Quantity.ToString();
                        }
                    }
                };
            });
            Grid.SetColumn(qtyPanel, 1);

            // Discount
            var discountPanel = CreateControlPanel("Remise %", discountBox =>
            {
                discountBox.Text = item.DiscountPercent.ToString("F2");
                discountBox.TextAlignment = TextAlignment.Center;
                discountBox.TextChanged += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(discountBox.Text))
                    {
                        item.DiscountPercent = 0;
                        discountBox.Text = "0";
                        totalText.Text = item.Total.ToString("F2") + " DH";
                        UpdateSummary();
                        return;
                    }

                    if (decimal.TryParse(discountBox.Text, out decimal nd))
                    {
                        if (nd >= 0 && nd <= 100)
                            item.DiscountPercent = nd;
                        else if (nd > 100)
                        { item.DiscountPercent = 100; discountBox.Text = "100"; }
                        else
                        { item.DiscountPercent = 0; discountBox.Text = "0"; }

                        totalText.Text = item.Total.ToString("F2") + " DH";
                        UpdateSummary();
                    }
                };
            });
            Grid.SetColumn(discountPanel, 2);

            // Delete
            var deleteButton = new Button
            {
                Content = "🗑️",
                FontSize = 18,
                Width = 42,
                Height = 42,
                Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            deleteButton.Click += (s, e) =>
            {
                if (MessageBox.Show($"Supprimer '{item.Article.ArticleName}' du panier?",
                        "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    CartItems.Remove(item);
                    LoadCartItems();
                    UpdateSummary();
                }
            };
            Grid.SetColumn(deleteButton, 5);

            controlsGrid.Children.Add(pricePanel);
            controlsGrid.Children.Add(qtyPanel);
            controlsGrid.Children.Add(discountPanel);
            controlsGrid.Children.Add(totalText);
            controlsGrid.Children.Add(deleteButton);

            mainGrid.Children.Add(infoGrid);
            mainGrid.Children.Add(controlsGrid);

            border.Child = mainGrid;
            return border;
        }

        private TextBlock CreateInfoText(string text, double fontSize, bool isItalic = false, SolidColorBrush customColor = null)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontStyle = isItalic ? FontStyles.Italic : FontStyles.Normal,
                Foreground = customColor ?? (SolidColorBrush)FindResource("TextSecondary"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 2)
            };
        }

        private StackPanel CreateControlPanel(string label, Action<TextBox> configureTextBox)
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 8, 0) };

            panel.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 10,
                Foreground = (SolidColorBrush)FindResource("TextSecondary"),
                Margin = new Thickness(0, 0, 0, 4)
            });

            var textBox = new TextBox
            {
                Height = 40,
                FontSize = 13,
                TextAlignment = TextAlignment.Right,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Brushes.White,
                BorderBrush = (SolidColorBrush)FindResource("BorderLight"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 8, 10, 8)
            };

            configureTextBox(textBox);
            panel.Children.Add(textBox);
            return panel;
        }

        private void UpdateSummary()
        {
            if (CartItems == null || CartItems.Count == 0)
            {
                SubtotalText.Text = "0.00 DH";
                TotalDiscountText.Text = "0.00 DH";
                TotalTTCText.Text = "0.00 DH";
                return;
            }

            decimal subtotal = CartItems.Sum(i => i.SubTotal);
            decimal itemDiscounts = CartItems.Sum(i => i.DiscountAmount);
            decimal afterItemDiscount = subtotal - itemDiscounts;

            decimal additionalDiscountAmount = 0;
            if (decimal.TryParse(AdditionalDiscountTextBox.Text, out decimal addDiscount) && addDiscount > 0)
            {
                additionalDiscountAmount = DiscountTypeComboBox.SelectedIndex == 0
                    ? afterItemDiscount * (addDiscount / 100m)
                    : addDiscount;
            }

            decimal totalDiscount = itemDiscounts + additionalDiscountAmount;
            decimal totalTTC = Math.Max(0, subtotal - totalDiscount);

            SubtotalText.Text = subtotal.ToString("F2") + " DH";
            TotalDiscountText.Text = totalDiscount.ToString("F2") + " DH";
            TotalTTCText.Text = totalTTC.ToString("F2") + " DH";

            AdditionalDiscount = additionalDiscountAmount;
            ClientDiscount = 0;
            IsPercentageDiscount = DiscountTypeComboBox.SelectedIndex == 0;
        }

        private void ClientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClientComboBox.SelectedIndex == 0 || ClientComboBox.SelectedIndex == -1)
            {
                SelectedClient = null;
                ClientDiscount = 0;
                ClientInfoPanel.Visibility = Visibility.Collapsed;
                AdditionalDiscountTextBox.Text = "0";
            }
            else
            {
                string clientName = ClientComboBox.SelectedItem.ToString().Split('-')[0].Trim();
                Client selectedClient = _allClients.FirstOrDefault(c => c.Nom == clientName);

                if (selectedClient != null)
                {
                    SelectedClient = selectedClient;
                    decimal remise = SelectedClient.Remise ?? 0;

                    ClientInfoPanel.Visibility = Visibility.Visible;
                    ClientNameText.Text = SelectedClient.Nom;
                    ClientPhoneText.Text = SelectedClient.Telephone;
                    ClientDiscountValue.Text = remise.ToString("F2") + " %";

                    AdditionalDiscountTextBox.Text = remise.ToString("F2");
                    DiscountTypeComboBox.SelectedIndex = 0;
                }
            }

            UpdateSummary();
        }

        private void AdditionalDiscountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CartItems != null && CartItems.Count > 0)
                UpdateSummary();
        }

        private void DiscountTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CartItems != null && CartItems.Count > 0)
                UpdateSummary();
        }

        private void CreditTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CreditAmount = decimal.TryParse(CreditTextBox.Text, out decimal c) ? c : 0;
        }

        private void AddArticleButton_Click(object sender, RoutedEventArgs e)
        {
            var selectWindow = new WSelectArticle(_availableArticles, _familles, _fournisseurs);
            if (selectWindow.ShowDialog() == true && selectWindow.SelectedArticle != null)
            {
                var selectedArticle = selectWindow.SelectedArticle;
                int selectedQty = selectWindow.SelectedQuantity;
                bool wasOutOfStock = selectedArticle.Quantite <= 0;

                var existingItem = CartItems.FirstOrDefault(i => i.Article.ArticleID == selectedArticle.ArticleID);
                if (existingItem != null)
                {
                    // For in-stock items validate; for out-of-stock items allow freely
                    if (!existingItem.WasOutOfStock && existingItem.Quantity + selectedQty > selectedArticle.Quantite)
                    {
                        MessageBox.Show($"Quantité disponible: {selectedArticle.Quantite}",
                            "Stock Insuffisant", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    existingItem.Quantity += selectedQty;
                }
                else
                {
                    if (wasOutOfStock)
                    {
                        if (!StockWarningHelper.ConfirmAddOutOfStock(selectedArticle.ArticleName))
                            return; // User cancelled
                        // User confirmed — fall through to add
                    }

                    CartItems.Add(new CartItem
                    {
                        Article = selectedArticle,
                        Quantity = selectedQty,
                        DiscountPercent = 0,
                        WasOutOfStock = wasOutOfStock
                    });
                }

                LoadCartItems();
                UpdateSummary();
            }
        }

        private async void NewClientButton_Click(object sender, RoutedEventArgs e)
        {
            ClientFormWindow addClientWindow = new ClientFormWindow(_parentVente.main);
            bool? result = addClientWindow.ShowDialog();

            if (result == true)
            {
                await LoadClients();
                if (ClientComboBox.Items.Count > 1)
                    ClientComboBox.SelectedIndex = ClientComboBox.Items.Count - 1;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogConfirmed = false;
            this.Close();
        }

        private async void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            if (CartItems.Count == 0)
            {
                MessageBox.Show("Le panier est vide!", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if ((PaymentType == 1 || PaymentType == 2) && SelectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client pour une vente à crédit ou partielle.",
                    "Client requis", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (PaymentType == 1)
            {
                if (string.IsNullOrWhiteSpace(CreditTextBox.Text))
                {
                    MessageBox.Show("Veuillez entrer le montant du crédit.",
                        "Crédit requis", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal totalTTC = decimal.Parse(TotalTTCText.Text.Replace(" DH", ""));
                if (CreditAmount > totalTTC)
                {
                    MessageBox.Show("Le montant du crédit ne peut pas dépasser le total.",
                        "Montant invalide", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (PaymentType == 2)
                CreditAmount = decimal.Parse(TotalTTCText.Text.Replace(" DH", ""));

            try
            {
                await ProcessSale();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du traitement de la vente: {ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task ProcessSale()
        {
            ContinueButton.IsEnabled = false;

            try
            {
                var ticketArticles = CartItems.Select(item => new TicketArticleData
                {
                    ArticleName = item.Article.ArticleName,
                    Quantity = item.Quantity,
                    UnitPrice = item.PrixUnitaire,
                    Total = item.Total,
                    TVA = item.Article.tva
                }).ToList();

                decimal subtotal = CartItems.Sum(i => i.SubTotal);
                decimal totalDiscount = AdditionalDiscount + ClientDiscount + CartItems.Sum(i => i.DiscountAmount);
                decimal totalTTC = subtotal - totalDiscount;

                string operationTypeLabel = PaymentType == 0 ? "VENTE COMPTANT"
                                          : PaymentType == 1 ? "VENTE PARTIELLE"
                                          : "VENTE À CRÉDIT";

                // Show ticket preview if enabled
                if (_parentVente.Ticket != null && _parentVente.Ticket.IsChecked == true)
                {
                    FactureSettings settings = await FactureSettings.LoadSettingsAsync() ?? new FactureSettings();

                    WFacturePreview factureWindow = new WFacturePreview(
                        settings, 0, DateTime.Now, SelectedClient,
                        ticketArticles, totalTTC, totalDiscount, CreditAmount,
                        PaymentMethodName, operationTypeLabel);

                    bool? result = factureWindow.ShowDialog();
                    if (result == false || !factureWindow.ShouldPrint)
                    {
                        MessageBox.Show("Opération annulée par l'utilisateur.",
                            "Opération annulée", MessageBoxButton.OK, MessageBoxImage.Information);
                        ContinueButton.IsEnabled = true;
                        return;
                    }
                }

                int operationId = await CreateOperation(totalTTC, totalDiscount);

                if (_parentVente.Ticket != null && _parentVente.Ticket.IsChecked == true)
                    await PrintTicket(operationId, ticketArticles, totalTTC, totalDiscount, operationTypeLabel);

                // Clear parent cart
                _parentVente.SelectedArticles.Children.Clear();
                _parentVente.TotalNet.Text = "0.00 DH";
                _parentVente.ArticleCount.Text = "0";
                _parentVente.TotalNett = 0;
                _parentVente.NbrA = 0;
                _parentVente.UpdateCartEmptyState();

                DialogConfirmed = true;
                this.Close();

                bool shouldShowFacture = _parentVente.Facture != null && _parentVente.Facture.IsChecked == true;

                if (shouldShowFacture)
                {
                    try
                    {
                        var factureInfo = await PrepareFactureInfo(operationId, totalTTC, totalDiscount, operationTypeLabel);
                        var invoiceArticles = PrepareInvoiceArticles();
                        new WFacturePage(null, factureInfo, invoiceArticles).ShowDialog();
                    }
                    catch (Exception navEx)
                    {
                        MessageBox.Show($"Erreur lors de l'affichage de la facture: {navEx.Message}",
                            "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    new WCongratulations("Opération réussie", "Opération a été effectuée avec succès", 1).ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ContinueButton.IsEnabled = true;
                new WCongratulations("Opération échouée", $"Opération n'a pas été effectuée: {ex.Message}", 0).ShowDialog();
            }
        }

        private async System.Threading.Tasks.Task<int> CreateOperation(decimal totalTTC, decimal totalDiscount)
        {
            Operation operation = new Operation
            {
                PaymentMethodID = PaymentMethodID,
                PrixOperation = totalTTC + totalDiscount,
                Remise = totalDiscount,
                UserID = _parentVente.u.UserID,
                ClientID = SelectedClient?.ClientID
            };

            int operationId;

            if (PaymentType == 0)
            {
                operation.OperationType = "VenteCa";
                operationId = await operation.InsertOperationAsync();
            }
            else if (PaymentType == 1)
            {
                operation.OperationType = "Vente50";
                operation.CreditValue = CreditAmount;
                operation.CreditID = await UpdateOrCreateCredit(CreditAmount);
                operationId = await operation.InsertOperationAsync();
            }
            else
            {
                operation.OperationType = "VenteCr";
                operation.CreditValue = CreditAmount;
                operation.CreditID = await UpdateOrCreateCredit(CreditAmount);
                operationId = await operation.InsertOperationAsync();
            }

            foreach (var item in CartItems)
            {
                await new OperationArticle
                {
                    ArticleID = item.Article.ArticleID,
                    OperationID = operationId,
                    QteArticle = item.Quantity
                }.InsertOperationArticleAsync();

                // ── Zero-stock guard ─────────────────────────────────────────
                if (!item.WasOutOfStock)
                {
                    item.Article.Quantite -= item.Quantity;
                    await item.Article.UpdateArticleAsync();

                    var local = _parentVente.la.FirstOrDefault(a => a.ArticleID == item.Article.ArticleID);
                    if (local != null)
                        local.Quantite = item.Article.Quantite;
                }
            }

            _parentVente.LoadArticles(_parentVente.la);

            return operationId;
        }

        private async System.Threading.Tasks.Task<int> UpdateOrCreateCredit(decimal amount)
        {
            Credit creditHelper = new Credit();
            var credits = await creditHelper.GetCreditsAsync();
            var existing = credits.FirstOrDefault(c => c.ClientID == SelectedClient.ClientID);

            if (existing != null)
            {
                existing.Total += amount;
                await existing.UpdateCreditAsync();
                return existing.CreditID;
            }
            else
            {
                return await new Credit { ClientID = SelectedClient.ClientID, Total = amount }.InsertCreditAsync();
            }
        }

        private async System.Threading.Tasks.Task<Dictionary<string, string>> PrepareFactureInfo(
            int operationId, decimal totalTTC, decimal totalDiscount, string operationType)
        {
            decimal subtotal = CartItems.Sum(i => i.SubTotal);
            decimal itemDiscounts = CartItems.Sum(i => i.DiscountAmount);
            decimal totalHT = subtotal - itemDiscounts;

            decimal tvaRate = 0;
            decimal totalBeforeTVA = CartItems.Sum(i => i.SubTotal);
            if (totalBeforeTVA > 0)
                tvaRate = CartItems.Sum(i => (i.SubTotal / totalBeforeTVA) * i.Article.tva);

            decimal tvaAmount = totalHT * (tvaRate / 100);
            decimal totalWithTVA = totalHT + tvaAmount;

            string companyName = "", companyICE = "", companyVAT = "", companyPhone = "",
                   companyAddress = "", companyEtatJuridique = "", companyId = "",
                   companySiege = "", companyLogo = "";

            try
            {
                var factureData = await new Superete.Facture().GetFactureAsync();
                if (factureData != null)
                {
                    companyName = factureData.Name ?? "";
                    companyICE = factureData.ICE ?? "";
                    companyVAT = factureData.VAT ?? "";
                    companyPhone = factureData.Telephone ?? "";
                    companyAddress = factureData.Adresse ?? "";
                    companyEtatJuridique = factureData.EtatJuridic ?? "";
                    companyId = factureData.CompanyId ?? "";
                    companySiege = factureData.SiegeEntreprise ?? "";
                    companyLogo = factureData.LogoPath ?? "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des informations de l'entreprise: {ex.Message}",
                    "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return new Dictionary<string, string>
            {
                ["NFacture"] = $"INV-{operationId}",
                ["Date"] = DateTime.Now.ToString("dd/MM/yyyy"),
                ["Type"] = "Facture",
                ["IndexDeFacture"] = operationId.ToString(),

                ["NomC"] = SelectedClient?.Nom ?? "Client Anonyme",
                ["ICEC"] = SelectedClient?.ICE ?? "",
                ["VATC"] = "",
                ["TelephoneC"] = SelectedClient?.Telephone ?? "",
                ["AdressC"] = SelectedClient?.Adresse ?? "",
                ["EtatJuridiqueC"] = SelectedClient?.EtatJuridique ?? "",
                ["IdSocieteC"] = SelectedClient?.Code ?? "",
                ["SiegeEntrepriseC"] = SelectedClient?.SiegeEntreprise ?? "",

                ["NomU"] = companyName,
                ["ICEU"] = companyICE,
                ["VATU"] = companyVAT,
                ["TelephoneU"] = companyPhone,
                ["AdressU"] = companyAddress,
                ["EtatJuridiqueU"] = companyEtatJuridique,
                ["IdSocieteU"] = companyId,
                ["SiegeEntrepriseU"] = companySiege,

                ["PaymentMethod"] = PaymentMethodName,
                ["GivenBy"] = _parentVente.u?.UserName ?? "",
                ["ReceivedBy"] = SelectedClient?.Nom ?? "Client",
                ["Device"] = "DH",

                ["MontantTotal"] = totalHT.ToString("F2"),
                ["TVA"] = tvaRate.ToString("F2"),
                ["MontantTVA"] = tvaAmount.ToString("F2"),
                ["MontantApresTVA"] = totalWithTVA.ToString("F2"),
                ["Remise"] = totalDiscount.ToString("F2"),
                ["MontantApresRemise"] = totalTTC.ToString("F2"),

                ["Object"] = operationType,
                ["Description"] = GetOperationDescription(),
                ["AmountInLetters"] = ConvertNumberToWords(totalTTC),
                ["Reversed"] = "Normal",
                ["EtatFature"] = "1",
                ["Logo"] = companyLogo,

                ["CreditClientName"] = (PaymentType == 1 || PaymentType == 2) ? (SelectedClient?.Nom ?? "") : "",
                ["CreditMontant"] = (PaymentType == 1 || PaymentType == 2) ? CreditAmount.ToString("F2") : "0.00"
            };
        }

        private List<InvoiceArticle> PrepareInvoiceArticles() =>
            CartItems.Select(item => new InvoiceArticle
            {
                OperationID = 0,
                ArticleID = item.Article.ArticleID,
                ArticleName = item.Article.ArticleName,
                Prix = item.Article.PrixVente,
                Quantite = item.Quantity,
                TVA = item.Article.tva,
                Reversed = false,
                InitialQuantity = item.Quantity,
                ExpeditionTotal = 0
            }).ToList();

        private string GetOperationDescription()
        {
            switch (PaymentType)
            {
                case 0: return "Vente au comptant - Paiement intégral reçu";
                case 1: return $"Vente partielle - Crédit de {CreditAmount:F2} DH";
                case 2: return $"Vente à crédit - Montant total à crédit: {CreditAmount:F2} DH";
                default: return "Vente";
            }
        }

        private string ConvertNumberToWords(decimal amount)
        {
            int intPart = (int)amount;
            int decPart = (int)((amount - intPart) * 100);

            string[] units = { "", "un", "deux", "trois", "quatre", "cinq", "six", "sept", "huit", "neuf" };
            string[] teens = { "dix", "onze", "douze", "treize", "quatorze", "quinze", "seize", "dix-sept", "dix-huit", "dix-neuf" };
            string[] tens = { "", "", "vingt", "trente", "quarante", "cinquante", "soixante", "soixante-dix", "quatre-vingt", "quatre-vingt-dix" };

            string result;

            if (intPart == 0) result = "zéro";
            else if (intPart < 10) result = units[intPart];
            else if (intPart < 20) result = teens[intPart - 10];
            else if (intPart < 100)
            {
                result = tens[intPart / 10];
                if (intPart % 10 > 0) result += " " + units[intPart % 10];
            }
            else if (intPart < 1000)
            {
                int h = intPart / 100, r = intPart % 100;
                result = h == 1 ? "cent" : units[h] + " cent";
                if (r > 0)
                {
                    if (r < 10) result += " " + units[r];
                    else if (r < 20) result += " " + teens[r - 10];
                    else { result += " " + tens[r / 10]; if (r % 10 > 0) result += " " + units[r % 10]; }
                }
            }
            else result = intPart.ToString();

            result += " dirhams";
            if (decPart > 0) result += " et " + decPart + " centimes";
            return result;
        }

        private async System.Threading.Tasks.Task PrintTicket(
            int operationId, List<TicketArticleData> articles,
            decimal total, decimal discount, string operationType)
        {
            try
            {
                FactureSettings settings = await FactureSettings.LoadSettingsAsync() ?? new FactureSettings();

                new WFacturePreview(
                    settings, operationId, DateTime.Now, SelectedClient,
                    articles, total, discount, CreditAmount,
                    PaymentMethodName, operationType).PrintFacture();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'impression: {ex.Message}",
                    "Erreur d'impression", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}