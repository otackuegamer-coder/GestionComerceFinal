using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Superete;

namespace GestionComerce.Main.Inventory
{
    public partial class CMainI : UserControl
    {
        public List<Famille> lf;
        public MainWindow main;
        public User u;
        public List<Fournisseur> lfo;
        public List<Article> la;
        private int cardsPerRow = 7;
        private string currentIconSize = "Moyennes";
        private int articlesPerPage = 12;
        private int currentlyLoadedCount = 0;
        private List<Article> filteredArticles;
        private bool isCardLayout = false;

        // Sort index:
        // 0=NameAZ, 1=NameZA, 2=PriceAsc, 3=PriceDesc,
        // 4=QtyAsc, 5=QtyDesc, 6=Newest, 7=Oldest
        private int currentSortIndex = 6; // Default: newest to oldest

        private ParametresGeneraux _parametres;

        public CMainI(User u, List<Article> la, List<Famille> lf, List<Fournisseur> lfo, MainWindow main)
        {
            InitializeComponent();
            this.lf = lf;
            this.main = main;
            this.u = u;
            this.la = la;
            this.lfo = lfo;
            this.filteredArticles = new List<Article>();

            ChargerParametres();

            foreach (Role r in main.lr)
            {
                if (u.RoleID == r.RoleID)
                {
                    if (r.ViewFamilly == false && r.AddFamilly == false)
                        ManageFamillies.IsEnabled = false;
                    if (r.AddArticle == false)
                    {
                        NewArticleButton.IsEnabled = false;
                        AddMultipleArticlesButton.IsEnabled = false;
                    }
                    if (r.ViewFournisseur == false)
                        FournisseurManage.IsEnabled = false;
                    if (r.ViewArticle == true)
                        RefreshArticlesList(la, true);
                    break;
                }
            }
        }

        // ── Maps French DB sort string → ComboBox index ──────────────────────
        private int SortStringToIndex(string french)
        {
            switch (french)
            {
                case "Nom (A-Z)":            return 0;
                case "Nom (Z-A)":            return 1;
                case "Prix croissant":       return 2;
                case "Prix décroissant":     return 3;
                case "Quantité croissante":  return 4;
                case "Quantité décroissante":return 5;
                case "Plus récent au plus ancien": return 6;
                case "Plus ancien au plus récent": return 7;
                default:                     return 6;
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

                // 2. Tri par défaut — use index, not content string
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
                            case "Grandes":
                                IconSizeComboBox.SelectedIndex = 0;
                                cardsPerRow = 6;
                                articlesPerPage = 12;
                                break;
                            case "Moyennes":
                                IconSizeComboBox.SelectedIndex = 1;
                                cardsPerRow = 8;
                                articlesPerPage = 14;
                                break;
                            case "Petites":
                                IconSizeComboBox.SelectedIndex = 2;
                                cardsPerRow = 12;
                                articlesPerPage = 22;
                                break;
                            default:
                                IconSizeComboBox.SelectedIndex = 1;
                                cardsPerRow = 8;
                                articlesPerPage = 14;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'application des paramètres : {ex.Message}");
            }
        }

        // Icon size uses SelectedIndex — language-agnostic
        private void IconSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IconSizeComboBox == null || IconSizeComboBox.SelectedIndex < 0)
                return;

            switch (IconSizeComboBox.SelectedIndex)
            {
                case 0: // Large
                    currentIconSize = "Grandes";
                    cardsPerRow = 6;
                    articlesPerPage = 12;
                    break;
                case 1: // Medium
                    currentIconSize = "Moyennes";
                    cardsPerRow = 7;
                    articlesPerPage = 14;
                    break;
                case 2: // Small
                    currentIconSize = "Petites";
                    cardsPerRow = 11;
                    articlesPerPage = 22;
                    break;
                default:
                    currentIconSize = "Moyennes";
                    cardsPerRow = 7;
                    articlesPerPage = 14;
                    break;
            }

            if (_parametres != null)
            {
                try
                {
                    _parametres.TailleIcones = currentIconSize;
                    string connectionString = "Server=localhost\\SQLEXPRESS;Database=GESTIONCOMERCEP;Trusted_Connection=True;";
                    _parametres.MettreAJourParametres(connectionString);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur lors de la sauvegarde de la taille des icônes : {ex.Message}");
                }
            }

            if (la != null && la.Count > 0 && isCardLayout)
                RefreshArticlesList(la, false);
        }

        public void LoadArticles(List<Article> la)
        {
            this.la = la;
            RefreshArticlesList(la, false);
        }

        private void RefreshArticlesList(List<Article> la, bool resetPagination)
        {
            foreach (Role r in main.lr)
            {
                if (u.RoleID == r.RoleID)
                {
                    if (r.ViewArticle == true)
                    {
                        this.la = la;
                        var sortedList = ApplySorting(la);

                        filteredArticles = new List<Article>();
                        foreach (Article a in sortedList)
                            if (a.Etat) filteredArticles.Add(a);

                        int previousCount = resetPagination ? 0 : currentlyLoadedCount;
                        ArticlesContainer.Children.Clear();
                        UpdateTotalStats();

                        int articlesToLoad;
                        if (resetPagination || previousCount == 0)
                            articlesToLoad = Math.Min(articlesPerPage, filteredArticles.Count);
                        else
                            articlesToLoad = Math.Min(previousCount, filteredArticles.Count);

                        currentlyLoadedCount = articlesToLoad;
                        RefreshCurrentView();
                    }
                    break;
                }
            }
        }

        // Sort by index — completely language-agnostic
        private List<Article> ApplySorting(List<Article> articles)
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

        private void LoadMoreArticles()
        {
            int articlesToLoad = Math.Min(articlesPerPage, filteredArticles.Count - currentlyLoadedCount);
            currentlyLoadedCount += articlesToLoad;
            RefreshCurrentView();
        }

        private void UpdateViewMoreButtonVisibility()
        {
            if (ViewMoreButton != null)
                ViewMoreButton.Visibility = (currentlyLoadedCount < filteredArticles.Count)
                    ? Visibility.Visible : Visibility.Collapsed;
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

        // Sort changed — store index, not content string
        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentSortIndex = SortComboBox.SelectedIndex;

            if (la != null && la.Count > 0)
                RefreshArticlesList(la, false);
        }

        private void RefreshCurrentView()
        {
            ArticlesContainer.Children.Clear();

            if (isCardLayout)
            {
                int cpr = 0;
                switch (currentIconSize)
                {
                    case "Grandes": cpr = 6;  break;
                    case "Moyennes": cpr = 7; break;
                    case "Petites": cpr = 11; break;
                    default: cpr = 7; break;
                }

                var grid = new System.Windows.Controls.Grid();
                int articlesToShow = Math.Min(currentlyLoadedCount, filteredArticles.Count);
                int totalRows = (int)Math.Ceiling((double)articlesToShow / cpr);

                for (int i = 0; i < totalRows; i++)
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                for (int i = 0; i < cpr; i++)
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                for (int i = 0; i < articlesToShow; i++)
                {
                    Article a = filteredArticles[i];
                    CSingleArticleI ar = new CSingleArticleI(a, la, this, lf, lfo, true, currentIconSize);
                    ar.HorizontalAlignment = HorizontalAlignment.Center;
                    ar.VerticalAlignment = VerticalAlignment.Top;
                    ar.MaxWidth = currentIconSize == "Petites" ? 140 : (currentIconSize == "Grandes" ? 300 : 270);

                    System.Windows.Controls.Grid.SetRow(ar, i / cpr);
                    System.Windows.Controls.Grid.SetColumn(ar, i % cpr);
                    grid.Children.Add(ar);
                }

                ArticlesContainer.Children.Add(grid);
            }
            else
            {
                int articlesToShow = Math.Min(currentlyLoadedCount, filteredArticles.Count);
                for (int i = 0; i < articlesToShow; i++)
                {
                    Article a = filteredArticles[i];
                    CSingleArticleI ar = new CSingleArticleI(a, la, this, lf, lfo, false);
                    ArticlesContainer.Children.Add(ar);
                }
            }

            UpdateViewMoreButtonVisibility();
        }

        private void UpdateTotalStats()
        {
            List<Article> allActiveArticles = la.Where(a => a.Etat).ToList();

            int count = allActiveArticles.Count;
            decimal PrixATotall = 0, PrixMPTotall = 0, PrixVTotall = 0;
            int QuantiteTotall = 0;

            ArticlesTotal.Text = count.ToString();

            foreach (Article a in allActiveArticles)
            {
                PrixATotall  += a.PrixAchat * a.Quantite;
                PrixMPTotall += a.PrixMP    * a.Quantite;
                PrixVTotall  += a.PrixVente * a.Quantite;
                QuantiteTotall += a.Quantite;
            }

            PrixATotal.Text   = PrixATotall.ToString("0.00")  + " DH";
            PrixMPTotal.Text  = PrixMPTotall.ToString("0.00") + " DH";
            PrixVTotal.Text   = PrixVTotall.ToString("0.00")  + " DH";
            QuantiteTotal.Text = QuantiteTotall.ToString();
        }

        private void ViewMoreButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMoreArticles();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            main.load_main(u);
        }

        private void SearchCriteriaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchTextBox != null && ArticlesContainer != null)
                ApplySearchFilter();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ApplySearchFilter();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
        }

        // Search filter — uses SelectedIndex, not Content string
        private void ApplySearchFilter()
        {
            string searchText = SearchTextBox.Text.Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                filteredArticles = la.Where(a => a.Etat).ToList();
                filteredArticles = ApplySorting(filteredArticles);
                currentlyLoadedCount = 0;
                ArticlesContainer.Children.Clear();
                LoadMoreArticles();
                return;
            }

            // SearchCriteriaComboBox indices:
            // 0=Code, 1=Article, 2=Supplier, 3=Family, 4=LotNumber, 5=DeliveryNote, 6=Brand
            int criteriaIndex = SearchCriteriaComboBox.SelectedIndex;

            filteredArticles = new List<Article>();

            foreach (Article a in la)
            {
                if (!a.Etat) continue;

                bool matches = false;

                switch (criteriaIndex)
                {
                    case 0: // Code
                        matches = a.Code.ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                        break;
                    case 1: // Article name
                        matches = a.ArticleName != null &&
                                  a.ArticleName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                        break;
                    case 2: // Supplier
                        string fournisseurName = GetFournisseurName(a.FournisseurID);
                        matches = !string.IsNullOrEmpty(fournisseurName) &&
                                  fournisseurName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                        break;
                    case 3: // Family
                        string familleName = GetFamilleName(a.FamillyID);
                        matches = !string.IsNullOrEmpty(familleName) &&
                                  familleName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                        break;
                    case 4: // Lot number
                        matches = !string.IsNullOrEmpty(a.numeroLot) &&
                                  a.numeroLot.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                        break;
                    case 5: // Delivery note
                        matches = !string.IsNullOrEmpty(a.bonlivraison) &&
                                  a.bonlivraison.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                        break;
                    case 6: // Brand
                        matches = !string.IsNullOrEmpty(a.marque) &&
                                  a.marque.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                        break;
                }

                if (matches) filteredArticles.Add(a);
            }

            filteredArticles = ApplySorting(filteredArticles);
            currentlyLoadedCount = 0;
            ArticlesContainer.Children.Clear();
            LoadMoreArticles();
        }

        private string GetFournisseurName(int fournisseurId)
        {
            foreach (var f in lfo)
                if (f.FournisseurID == fournisseurId) return f.Nom;
            return string.Empty;
        }

        private string GetFamilleName(int familleId)
        {
            foreach (var f in lf)
                if (f.FamilleID == familleId) return f.FamilleName;
            return string.Empty;
        }

        private void NewArticleButton_Click(object sender, RoutedEventArgs e)
        {
            WNouveauStock wNouveauStock = new WNouveauStock(lf, la, lfo, this, 1, null, null);
            wNouveauStock.ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            main.load_fournisseur(u);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            WManageFamillies wManageFamillies = new WManageFamillies(lf, la, this);
            wManageFamillies.ShowDialog();
        }

        private void AddMultipleArticlesButton_Click(object sender, RoutedEventArgs e)
        {
            WAddMultipleArticles wAddMultipleArticles = new WAddMultipleArticles(this);
            wAddMultipleArticles.ShowDialog();
        }

        private void GenerateDevisButton_Click(object sender, RoutedEventArgs e)
        {
            bool hasPermission = false;
            foreach (Role r in main.lr)
            {
                if (u.RoleID == r.RoleID)
                {
                    if (r.ViewArticle == true) hasPermission = true;
                    break;
                }
            }

            if (!hasPermission)
            {
                MessageBox.Show("Vous n'avez pas la permission de générer un devis.",
                    "Accès refusé", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (la == null || la.Count == 0)
            {
                MessageBox.Show("Aucun article disponible pour générer un devis.",
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            WManualArticleSelection selectionWindow = new WManualArticleSelection(la, lf, lfo);
            bool? selectionResult = selectionWindow.ShowDialog();

            if (selectionResult == true)
            {
                List<Article> selectedArticles = selectionWindow.SelectedArticles;

                if (selectedArticles == null || selectedArticles.Count == 0)
                {
                    MessageBox.Show("Aucun article sélectionné.",
                        "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                WDevisCustomization customizationWindow = new WDevisCustomization(selectedArticles, lf, lfo);
                customizationWindow.ShowDialog();
            }
        }
    }
}
