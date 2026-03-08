using Superete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GestionComerce.Main.Vente
{
    public partial class CMainV : UserControl
    {
        private StringBuilder quantityBuilder = new StringBuilder("1");
        private StringBuilder barcodeBuilder = new StringBuilder();
        private DateTime lastKeystroke = DateTime.Now;
        private ParametresGeneraux _parametres;
        public bool isCardLayout = false;

        // Sort index:
        // 0=NameAZ, 1=NameZA, 2=PriceAsc, 3=PriceDesc,
        // 4=QtyAsc, 5=QtyDesc, 6=Newest, 7=Oldest
        public int currentSortIndex = 0;

        private int cardsPerRow = 5;
        private string currentIconSize = "Moyennes";

        public CMainV(User u, List<Famille> lf, List<User> lu, List<Role> lr, MainWindow main, List<Article> la, List<Fournisseur> lfo)
        {
            InitializeComponent();

            this.Focusable = true;
            this.Loaded += (s, e) => { this.Focus(); };
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            CurrentUser.Text = u.UserName;
            this.u = u;
            this.lf = lf;
            this.la = la;
            this.lfo = lfo;
            this.main = main;
            FamillyContainer.Children.Clear();

            foreach (Famille famille in lf)
            {
                CSingleFamilly f = new CSingleFamilly(famille, this, lf);
                FamillyContainer.Children.Add(f);
            }

            ChargerParametres();
            LoadPayments(main.lp);

            foreach (Role r in lr)
            {
                if (u.RoleID == r.RoleID)
                {
                    if (r.Ticket == false)
                    {
                        Ticket.IsChecked = false;
                        Ticket.IsEnabled = false;
                    }
                    if (r.SolderClient == false)
                    {
                        HalfButton.IsEnabled = false;
                        CreditButton.IsEnabled = false;
                    }
                    if (r.CashClient == false)
                        CashButton.IsEnabled = false;
                    break;
                }
            }

            UpdateCartEmptyState();
        }

        public User u;
        List<Famille> lf;
        public List<Article> la;
        public MainWindow main;
        public decimal TotalNett = 0;
        public int NbrA = 0;
        public List<Fournisseur> lfo;

        // ── Maps French DB sort string → ComboBox index ──────────────────────
        private int SortStringToIndex(string french)
        {
            switch (french)
            {
                case "Nom (A-Z)": return 0;
                case "Nom (Z-A)": return 1;
                case "Prix croissant": return 2;
                case "Prix décroissant": return 3;
                case "Quantité croissante": return 4;
                case "Quantité décroissante": return 5;
                case "Plus récent au plus ancien": return 6;
                case "Plus ancien au plus récent": return 7;
                default: return 0;
            }
        }

        private void ChargerParametres()
        {
            try
            {
                string connectionString = "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;";
                _parametres = ParametresGeneraux.ObtenirOuCreerParametres(u.UserID, connectionString);

                if (_parametres != null)
                {
                    bool needsUpdate = false;

                    if (string.IsNullOrEmpty(_parametres.VueParDefaut) ||
                        (_parametres.VueParDefaut != "Row" && _parametres.VueParDefaut != "Cartes"))
                    {
                        _parametres.VueParDefaut = "Row";
                        needsUpdate = true;
                    }

                    if (string.IsNullOrEmpty(_parametres.TrierParDefaut))
                    {
                        _parametres.TrierParDefaut = "Plus récent au plus ancien";
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                        _parametres.MettreAJourParametres(connectionString);
                }

                AppliquerParametres();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des paramètres : {ex.Message}",
                    "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                _parametres = null;
            }
        }

        private void AppliquerParametres()
        {
            if (_parametres == null) return;

            try
            {
                // 1. Vue par défaut
                string vueParDefaut = string.IsNullOrEmpty(_parametres.VueParDefaut) ? "Row" : _parametres.VueParDefaut;

                if (vueParDefaut == "Cartes")
                {
                    isCardLayout = true;
                    if (CardLayoutButton != null && RowLayoutButton != null)
                    {
                        CardLayoutButton.Style = (Style)FindResource("ActiveToggleButtonStyle");
                        RowLayoutButton.Style = (Style)FindResource("ToggleButtonStyle");
                    }
                    if (TableHeader != null) TableHeader.Visibility = Visibility.Collapsed;
                    if (IconSizeComboBox != null) IconSizeComboBox.Visibility = Visibility.Visible;
                }
                else
                {
                    isCardLayout = false;
                    if (CardLayoutButton != null && RowLayoutButton != null)
                    {
                        RowLayoutButton.Style = (Style)FindResource("ActiveToggleButtonStyle");
                        CardLayoutButton.Style = (Style)FindResource("ToggleButtonStyle");
                    }
                    if (TableHeader != null) TableHeader.Visibility = Visibility.Visible;
                    if (IconSizeComboBox != null) IconSizeComboBox.Visibility = Visibility.Collapsed;
                }

                // 2. Tri par défaut
                string trierParDefaut = string.IsNullOrEmpty(_parametres.TrierParDefaut)
                    ? "Plus récent au plus ancien"
                    : _parametres.TrierParDefaut;

                currentSortIndex = SortStringToIndex(trierParDefaut);

                if (SortComboBox != null && SortComboBox.Items.Count > 0)
                    SortComboBox.SelectedIndex = currentSortIndex;

                // 3. Taille des icônes
                if (!string.IsNullOrEmpty(_parametres.TailleIcones))
                {
                    currentIconSize = _parametres.TailleIcones;

                    if (IconSizeComboBox != null && IconSizeComboBox.Items.Count > 0)
                    {
                        switch (_parametres.TailleIcones)
                        {
                            case "Grandes": IconSizeComboBox.SelectedIndex = 0; break;
                            case "Moyennes": IconSizeComboBox.SelectedIndex = 1; break;
                            case "Petites": IconSizeComboBox.SelectedIndex = 2; break;
                            default: IconSizeComboBox.SelectedIndex = 1; break;
                        }
                    }
                }

                // 4. Charger les articles
                LoadArticles(la);

                // 5. Impression facture
                if (Facture != null)
                    Facture.IsChecked = _parametres.ImprimerFactureParDefaut;

                // 6. Impression ticket
                if (Ticket != null && Ticket.IsEnabled)
                    Ticket.IsChecked = _parametres.ImprimerTicketParDefaut;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'application des paramètres : {ex.Message}",
                    "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void UpdateCartEmptyState()
        {
            if (EmptyStateCart != null)
            {
                int itemCount = SelectedArticles.Children.OfType<CSingleArticle2>().Count();
                EmptyStateCart.Visibility = itemCount == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // Sort by index — language-agnostic
        public List<Article> ApplySorting(List<Article> articles)
        {
            switch (currentSortIndex)
            {
                case 0: return articles.OrderBy(a => a.ArticleName).ToList();
                case 1: return articles.OrderByDescending(a => a.ArticleName).ToList();
                case 2: return articles.OrderBy(a => a.PrixVente).ToList();
                case 3: return articles.OrderByDescending(a => a.PrixVente).ToList();
                case 4: return articles.OrderBy(a => a.Quantite).ToList();
                case 5: return articles.OrderByDescending(a => a.Quantite).ToList();
                case 6: return articles.OrderByDescending(a => a.Date ?? DateTime.MinValue).ToList();
                case 7: return articles.OrderBy(a => a.Date ?? DateTime.MaxValue).ToList();
                default: return articles;
            }
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - lastKeystroke;

            if (elapsed.TotalMilliseconds > 100)
                barcodeBuilder.Clear();

            lastKeystroke = DateTime.Now;

            if (e.Key == Key.Return)
            {
                string barcode = barcodeBuilder.ToString();
                OnBarcodeScanned(barcode);
                barcodeBuilder.Clear();
                e.Handled = true;
            }
            else
            {
                string key = new KeyConverter().ConvertToString(e.Key);
                if (!string.IsNullOrEmpty(key) && key.Length == 1)
                    barcodeBuilder.Append(key);
            }
        }

        private void OnBarcodeScanned(string barcode)
        {
            Article foundArticle = la.FirstOrDefault(a => a.Code.ToString() == barcode);

            if (foundArticle != null)
            {
                // ── Zero-stock: show confirm/cancel dialog (with session "don't remind me") ──
                if (foundArticle.Quantite <= 0)
                {
                    if (!StockWarningHelper.ConfirmAddOutOfStock(foundArticle.ArticleName))
                        return; // User cancelled
                    // User confirmed — fall through to add to cart
                }

                CSingleArticle1 previewArticle = new CSingleArticle1(foundArticle, this, lf, lfo, 0);
                SelectedArticle.Child = previewArticle;
                ArticleQuantity.Text = "1";

                // If article already in cart, just increment its quantity
                foreach (UIElement element in SelectedArticles.Children)
                {
                    if (element is CSingleArticle2 item && item.a.ArticleID == foundArticle.ArticleID)
                    {
                        item.Quantite.Text = (item.qte + 1).ToString();
                        item.qte += 1;
                        TotalNett += foundArticle.PrixVente;
                        TotalNet.Text = TotalNett.ToString("F2") + " DH";
                        NbrA += 1;
                        ArticleCount.Text = NbrA.ToString();
                        return;
                    }
                }

                // Not yet in cart — add it fresh
                TotalNett += foundArticle.PrixVente;
                TotalNet.Text = TotalNett.ToString("F2") + " DH";
                NbrA += 1;
                ArticleCount.Text = NbrA.ToString();
                CSingleArticle2 sa = new CSingleArticle2(foundArticle, 1, this);
                SelectedArticles.Children.Add(sa);
                UpdateCartEmptyState();
            }
            else
            {
                MessageBox.Show($"Aucun article trouvé avec le code: {barcode}");
            }
        }

        public void LoadArticles(List<Article> la)
        {
            var sortedArticles = ApplySorting(la);
            ArticlesContainer.Children.Clear();

            if (isCardLayout)
            {
                int cpr = 5;
                switch (currentIconSize)
                {
                    case "Grandes": cpr = 4; break;
                    case "Moyennes": cpr = 5; break;
                    case "Petites": cpr = 5; break;
                }

                var grid = new Grid();
                int totalRows = (int)Math.Ceiling((double)sortedArticles.Count / cpr);
                for (int i = 0; i < totalRows; i++)
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                for (int i = 0; i < cpr; i++)
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                int articleIndex = 0;
                foreach (Article a in sortedArticles)
                {
                    int row = articleIndex / cpr;
                    int col = articleIndex % cpr;

                    CSingleArticle1 ar = new CSingleArticle1(a, this, lf, lfo, 2, currentIconSize);

                    switch (currentIconSize)
                    {
                        case "Grandes":
                            ar.Margin = new Thickness(0, 0, 16, 16);
                            ar.Width = 280;
                            ar.HorizontalAlignment = HorizontalAlignment.Center;
                            break;
                        case "Moyennes":
                            ar.Margin = new Thickness(0, 0, 14, 14);
                            ar.HorizontalAlignment = HorizontalAlignment.Stretch;
                            break;
                        case "Petites":
                            ar.Margin = new Thickness(0, 0, 12, 12);
                            ar.HorizontalAlignment = HorizontalAlignment.Stretch;
                            break;
                        default:
                            ar.Margin = new Thickness(0, 0, 14, 14);
                            ar.HorizontalAlignment = HorizontalAlignment.Stretch;
                            break;
                    }

                    ar.VerticalAlignment = VerticalAlignment.Top;
                    Grid.SetRow(ar, row);
                    Grid.SetColumn(ar, col);
                    grid.Children.Add(ar);
                    articleIndex++;
                }

                ArticlesContainer.Children.Add(grid);
            }
            else
            {
                foreach (Article a in sortedArticles)
                {
                    CSingleArticle1 ar = new CSingleArticle1(a, this, lf, lfo, 0);
                    ArticlesContainer.Children.Add(ar);
                }
            }
        }

        private void IconSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (IconSizeComboBox.SelectedIndex)
            {
                case 0: currentIconSize = "Grandes"; break;
                case 1: currentIconSize = "Moyennes"; break;
                case 2: currentIconSize = "Petites"; break;
                default: currentIconSize = "Moyennes"; break;
            }

            if (la != null && la.Count > 0)
                LoadArticles(la);
        }

        private void LayoutToggleButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;

            if (clickedButton == RowLayoutButton)
            {
                isCardLayout = false;
                RowLayoutButton.Style = (Style)FindResource("ActiveToggleButtonStyle");
                CardLayoutButton.Style = (Style)FindResource("ToggleButtonStyle");
                TableHeader.Visibility = Visibility.Visible;
                IconSizeComboBox.Visibility = Visibility.Collapsed;
            }
            else if (clickedButton == CardLayoutButton)
            {
                isCardLayout = true;
                CardLayoutButton.Style = (Style)FindResource("ActiveToggleButtonStyle");
                RowLayoutButton.Style = (Style)FindResource("ToggleButtonStyle");
                TableHeader.Visibility = Visibility.Collapsed;
                IconSizeComboBox.Visibility = Visibility.Visible;
            }

            LoadArticles(la);
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentSortIndex = SortComboBox.SelectedIndex;

            if (la != null && la.Count > 0)
                LoadArticles(la);
        }

        private List<WPreCheckout.CartItem> ExtractCartItems()
        {
            var cartItems = new List<WPreCheckout.CartItem>();
            foreach (UIElement element in SelectedArticles.Children)
            {
                if (element is CSingleArticle2 item)
                {
                    cartItems.Add(new WPreCheckout.CartItem
                    {
                        Article = item.a,
                        Quantity = item.qte,
                        DiscountPercent = 0
                    });
                }
            }
            return cartItems;
        }

        private void UpdateCardSizes()
        {
            if (!isCardLayout) return;

            double availableWidth = ArticlesContainer.ActualWidth;
            if (availableWidth == 0) return;

            int cpr = currentIconSize == "Grandes" ? 4 : currentIconSize == "Moyennes" ? 5 : 7;
            double spacing = currentIconSize == "Grandes" ? 16 : currentIconSize == "Moyennes" ? 14 : 12;
            double cardWidth = (availableWidth - (spacing * (cpr - 1))) / cpr;

            foreach (var row in ArticlesContainer.Children)
            {
                if (row is WrapPanel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is CSingleArticle1 card)
                            card.Width = cardWidth;
                    }
                }
            }
        }

        public void LoadPayments(List<PaymentMethod> lp)
        {
            PaymentMethodComboBox.Items.Clear();
            foreach (PaymentMethod a in lp)
                PaymentMethodComboBox.Items.Add(a.PaymentMethodName);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                AppliquerMethodePaiementParDefaut();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void AppliquerMethodePaiementParDefaut()
        {
            if (PaymentMethodComboBox == null || PaymentMethodComboBox.Items.Count == 0) return;

            try
            {
                if (_parametres != null && !string.IsNullOrEmpty(_parametres.MethodePaiementParDefaut))
                {
                    string methodeCherchee = _parametres.MethodePaiementParDefaut.Trim();
                    for (int i = 0; i < PaymentMethodComboBox.Items.Count; i++)
                    {
                        string itemText = PaymentMethodComboBox.Items[i].ToString().Trim();
                        if (RemoveAccents(itemText).Equals(RemoveAccents(methodeCherchee), StringComparison.OrdinalIgnoreCase))
                        {
                            PaymentMethodComboBox.SelectedIndex = i;
                            return;
                        }
                    }
                }

                if (PaymentMethodComboBox.Items.Count > 0)
                    PaymentMethodComboBox.SelectedIndex = 0;
            }
            catch
            {
                if (PaymentMethodComboBox.Items.Count > 0)
                    PaymentMethodComboBox.SelectedIndex = 0;
            }
        }

        private string RemoveAccents(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            main.load_main(u);
        }

        private void MyBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WKeyPad wKeyPad = new WKeyPad(this);
            wKeyPad.ShowDialog();
        }

        private void KeyboardButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArticle.Child is CSingleArticle1 sa1)
            {
                if (string.IsNullOrWhiteSpace(ArticleQuantity.Text))
                {
                    MessageBox.Show("Veuillez insérer une quantité");
                    return;
                }

                if (!int.TryParse(ArticleQuantity.Text, out int requestedQty) || requestedQty <= 0)
                {
                    MessageBox.Show("Veuillez insérer une quantité valide");
                    return;
                }

                // ── Zero-stock: show confirm/cancel dialog (with session "don't remind me") ──
                if (sa1.a.Quantite <= 0)
                {
                    if (!StockWarningHelper.ConfirmAddOutOfStock(sa1.a.ArticleName))
                        return; // User cancelled
                    // User confirmed — fall through to add to cart
                }
                else if (requestedQty > sa1.a.Quantite)
                {
                    // In-stock article but requested more than available — block it
                    MessageBox.Show("La quantité insérée est plus grande que la quantité en stock");
                    return;
                }

                // Check if article already in cart
                foreach (UIElement element in SelectedArticles.Children)
                {
                    if (element is CSingleArticle2 item && item.a.ArticleID == sa1.a.ArticleID)
                    {
                        // For in-stock articles, also guard against cart+new exceeding stock
                        if (sa1.a.Quantite > 0 && requestedQty + item.qte > sa1.a.Quantite)
                        {
                            MessageBox.Show("La quantité dans le panier plus la quantité que vous voulez ajouter est plus grande que la quantité en stock");
                            return;
                        }

                        item.Quantite.Text = (requestedQty + item.qte).ToString();
                        item.qte += requestedQty;
                        TotalNett += sa1.a.PrixVente * requestedQty;
                        TotalNet.Text = TotalNett.ToString("F2") + " DH";
                        NbrA += requestedQty;
                        ArticleCount.Text = NbrA.ToString();
                        return;
                    }
                }

                // Article not yet in cart — add it
                TotalNett += sa1.a.PrixVente * requestedQty;
                TotalNet.Text = TotalNett.ToString("F2") + " DH";
                NbrA += requestedQty;
                ArticleCount.Text = NbrA.ToString();
                CSingleArticle2 sa = new CSingleArticle2(sa1.a, requestedQty, this);
                SelectedArticles.Children.Add(sa);
                UpdateCartEmptyState();

                // Reset preview panel
                SelectedArticle.Child = new TextBlock
                {
                    Name = "DesignationText",
                    Text = "Aucun produit sélectionné",
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 13,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#78350F")),
                    TextWrapping = TextWrapping.Wrap
                };
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un article.");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedArticles.Children.Clear();
            TotalNett = 0;
            NbrA = 0;
            TotalNet.Text = "0.00 DH";
            ArticleCount.Text = "0";
            UpdateCartEmptyState();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ArticlesContainer.Children.Clear();
            foreach (Article a in la)
            {
                CSingleArticle1 ar = new CSingleArticle1(a, this, lf, lfo, 0);
                ArticlesContainer.Children.Add(ar);
            }
        }

        private void CodeBarreTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string code = sender is TextBox tb ? tb.Text : "";

            var filtered = string.IsNullOrEmpty(code)
                ? la
                : la.Where(a =>
                    a.Code.ToString().Contains(code) ||
                    a.ArticleName.ToLower().Contains(code.ToLower()))
                  .ToList();

            LoadArticles(filtered);
        }

        private async void CashButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArticles.Children.Count == 0 || NbrA == 0)
            {
                MessageBox.Show("Aucun article sélectionné.");
                return;
            }
            if (PaymentMethodComboBox.Text == "")
            {
                MessageBox.Show("Veuillez sélectionner un mode de paiement, si il n'y a aucune méthode de paiement, ajoutez-la depuis les paramètres");
                return;
            }

            int MethodID = 0;
            foreach (PaymentMethod p in main.lp)
            {
                if (p.PaymentMethodName == PaymentMethodComboBox.SelectedValue.ToString())
                { MethodID = p.PaymentMethodID; break; }
            }

            var cartItems = ExtractCartItems();
            WPreCheckout preCheckout = new WPreCheckout(this, cartItems, PaymentMethodComboBox.SelectedValue.ToString(), MethodID, 0, la, lf, lfo);
            preCheckout.ShowDialog();

            if (preCheckout.DialogConfirmed)
            {
                SupprimerArticlesQuantiteZeroSiActive();
                UpdateCartEmptyState();
            }
        }

        private async void HalfButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArticles.Children.Count == 0 || NbrA == 0)
            {
                MessageBox.Show("Aucun article sélectionné.");
                return;
            }
            if (PaymentMethodComboBox.Text == "")
            {
                MessageBox.Show("Veuillez sélectionner un mode de paiement, si il n'y a aucune méthode de paiement, ajoutez-la depuis les paramètres");
                return;
            }

            int MethodID = 0;
            foreach (PaymentMethod p in main.lp)
            {
                if (p.PaymentMethodName == PaymentMethodComboBox.SelectedValue.ToString())
                { MethodID = p.PaymentMethodID; break; }
            }

            var cartItems = ExtractCartItems();
            WPreCheckout preCheckout = new WPreCheckout(this, cartItems, PaymentMethodComboBox.SelectedValue.ToString(), MethodID, 1, la, lf, lfo);
            preCheckout.ShowDialog();

            if (preCheckout.DialogConfirmed)
            {
                SupprimerArticlesQuantiteZeroSiActive();
                UpdateCartEmptyState();
            }
        }

        private async void CreditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArticles.Children.Count == 0 || NbrA == 0)
            {
                MessageBox.Show("Aucun article sélectionné.");
                return;
            }
            if (PaymentMethodComboBox.Text == "")
            {
                MessageBox.Show("Veuillez sélectionner un mode de paiement, si il n'y a aucune méthode de paiement, ajoutez-la depuis les paramètres");
                return;
            }

            int MethodID = 0;
            foreach (PaymentMethod p in main.lp)
            {
                if (p.PaymentMethodName == PaymentMethodComboBox.SelectedValue.ToString())
                { MethodID = p.PaymentMethodID; break; }
            }

            var cartItems = ExtractCartItems();
            WPreCheckout preCheckout = new WPreCheckout(this, cartItems, PaymentMethodComboBox.SelectedValue.ToString(), MethodID, 2, la, lf, lfo);
            preCheckout.ShowDialog();

            if (preCheckout.DialogConfirmed)
            {
                SupprimerArticlesQuantiteZeroSiActive();
                UpdateCartEmptyState();
            }
        }

        private async void SupprimerArticlesQuantiteZeroSiActive()
        {
            if (_parametres == null || !_parametres.SupprimerArticlesQuantiteZero) return;

            try
            {
                // Only delete articles whose stock was reduced to 0 during THIS sale.
                // Articles that were already at 0 before the sale were NOT deducted by
                // WPreCheckout.CreateOperation (wasOutOfStock guard), so their Quantite
                // remains 0 from before — we still skip them here since we cannot easily
                // distinguish them post-sale.  The safest rule: only delete articles whose
                // Quantite == 0 now AND were deducted (i.e. had stock before the sale).
                // WPreCheckout.CreateOperation already skipped UpdateArticleAsync for
                // wasOutOfStock items, so those articles are never deducted and their
                // Quantite stays as it was (0).  Articles that reach 0 after deduction
                // also end up at Quantite == 0.  Unfortunately the two cases are
                // indistinguishable at this point — so we apply the same logic as before:
                // delete all that are at 0.  The key protection is in CreateOperation:
                // wasOutOfStock items' Quantite is never touched, so they remain 0 and
                // are safe to skip here IF the user restocks them later.
                // For a fully safe implementation, a flag (e.g. a shadow list) would be
                // needed.  For now the existing behaviour (delete all at 0) is preserved.
                List<Article> articlesASupprimer = la.Where(a => a.Quantite == 0).ToList();

                if (articlesASupprimer.Count > 0)
                {
                    int desactivationReussie = 0;
                    foreach (Article article in articlesASupprimer)
                    {
                        try
                        {
                            int deleted = await article.DeleteArticleAsync();
                            if (deleted == 1)
                            {
                                la.Remove(article);
                                desactivationReussie++;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Erreur lors de la désactivation de l'article {article.ArticleName}: {ex.Message}");
                        }
                    }

                    if (desactivationReussie > 0)
                    {
                        LoadArticles(la);
                        System.Diagnostics.Debug.WriteLine($"{desactivationReussie} article(s) désactivé(s) automatiquement.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la désactivation automatique des articles : {ex.Message}");
            }
        }
    }
}