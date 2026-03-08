using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GestionComerce;
using GestionComerce.Models;

namespace Superete.Main.Comptabilite.Expectation
{
    public partial class CMainEx : UserControl, INotifyPropertyChanged
    {
        #region Properties and Fields

        private List<Article> _allArticles;
        private List<CheckHistory> _allChecks;
        private bool _isLoading;
        private int _selectedPeriodMonths = 1; // Default to 1 month

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public int SelectedPeriodMonths
        {
            get => _selectedPeriodMonths;
            set
            {
                _selectedPeriodMonths = value;
                OnPropertyChanged(nameof(SelectedPeriodMonths));
                OnPropertyChanged(nameof(PeriodDisplayText));
                // Recalculate when period changes
                if (_allArticles != null && _allArticles.Count > 0)
                {
                    CalculateKPIs();
                    GenerateRecommendations();
                }
            }
        }

        public string PeriodDisplayText
        {
            get
            {
                if (_selectedPeriodMonths < 12)
                    return $"{_selectedPeriodMonths} mois";
                else if (_selectedPeriodMonths == 12)
                    return "1 an";
                else
                    return $"{_selectedPeriodMonths / 12} ans";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Constructor

        public CMainEx(User u, MainWindow main)
        {
            InitializeComponent();
            DataContext = this;
            IsLoading = false;
            InitializePeriodSelector();
        }

        #endregion

        #region Period Selection

        private void InitializePeriodSelector()
        {
            // This will be populated from XAML ComboBox
            // Values: 1, 3, 6, 12, 24, 60 (months)
        }

        private void PeriodSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PeriodSelector.SelectedItem == null) return;

            ComboBoxItem selectedItem = PeriodSelector.SelectedItem as ComboBoxItem;
            if (selectedItem != null && selectedItem.Tag != null)
            {
                SelectedPeriodMonths = int.Parse(selectedItem.Tag.ToString());
            }
        }

        #endregion

        #region Event Handlers

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDashboardData();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDashboardData();
        }

        private void ArticleSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = ArticleSearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                ArticleSuggestionsList.Visibility = Visibility.Collapsed;
                return;
            }

            if (_allArticles == null || _allArticles.Count == 0)
            {
                return;
            }

            // Filter articles by name (case-insensitive)
            var filteredArticles = _allArticles
                .Where(a => a.ArticleName != null &&
                           a.ArticleName.ToLower().Contains(searchText.ToLower()))
                .Take(10)
                .Select(a => a.ArticleName)
                .ToList();

            if (filteredArticles.Count > 0)
            {
                ArticleSuggestionsList.ItemsSource = filteredArticles;
                ArticleSuggestionsList.Visibility = Visibility.Visible;
            }
            else
            {
                ArticleSuggestionsList.Visibility = Visibility.Collapsed;
            }
        }

        private void ArticleSuggestionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ArticleSuggestionsList.SelectedItem == null) return;

            string selectedName = ArticleSuggestionsList.SelectedItem.ToString();
            ArticleSearchBox.Text = selectedName;
            ArticleSuggestionsList.Visibility = Visibility.Collapsed;

            // Find the selected article
            var selectedArticle = _allArticles.FirstOrDefault(a => a.ArticleName == selectedName);
            if (selectedArticle != null)
            {
                DisplayArticleProjections(selectedArticle);
            }
        }

        #endregion

        #region Main Data Loading

        private async Task LoadDashboardData()
        {
            try
            {
                IsLoading = true;

                // Load data asynchronously
                await Task.Run(async () =>
                {
                    var articleLoader = new Article();
                    var checkLoader = new CheckHistory();

                    _allArticles = await articleLoader.GetArticlesAsync();
                    _allChecks = await checkLoader.GetAllChecksAsync();
                });

                // Calculate KPIs
                CalculateKPIs();

                // Generate recommendations
                GenerateRecommendations();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des données: {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region KPI Calculations

        private void CalculateKPIs()
        {
            if (_allArticles == null || _allArticles.Count == 0)
            {
                SetDefaultKPIs();
                return;
            }

            Dispatcher.Invoke(() =>
            {
                DateTime now = DateTime.Now;
                DateTime periodEndDate = now.AddMonths(_selectedPeriodMonths);

                // 1. PROJECTED REVENUE: Sum of (Quantity * SalePrice) adjusted for period
                // For period projections, we estimate based on current stock depletion rate
                decimal projectedRevenue = CalculateProjectedRevenue(periodEndDate);
                ProjectedRevenueText.Text = $"{projectedRevenue:N2} DH";

                // 2. POTENTIAL NET PROFIT: Sum of (Quantity * (SalePrice - PurchasePrice))
                decimal potentialProfit = CalculateProjectedProfit(periodEndDate);
                PotentialProfitText.Text = $"{potentialProfit:N2} DH";

                // Calculate overall profit margin
                decimal overallMargin = projectedRevenue > 0
                    ? (potentialProfit / projectedRevenue) * 100
                    : 0;

                ProfitMarginText.Text = $"Marge: {overallMargin:F1}%";

                // 3. INVENTORY HEALTH: Percentage of stock that is healthy for the selected period
                int totalArticles = _allArticles.Count(a => !a.IsUnlimitedStock);
                int healthyArticles = 0;

                foreach (var article in _allArticles.Where(a => !a.IsUnlimitedStock))
                {
                    bool isExpiringInPeriod = article.DateExpiration.HasValue &&
                                              article.DateExpiration.Value <= periodEndDate &&
                                              article.DateExpiration.Value > now;

                    bool isLowStock = article.Quantite < 10 && article.Quantite > 0;

                    if (!isExpiringInPeriod && !isLowStock)
                    {
                        healthyArticles++;
                    }
                }

                double healthPercentage = totalArticles > 0
                    ? (healthyArticles * 100.0 / totalArticles)
                    : 100.0;

                InventoryHealthText.Text = $"{healthPercentage:F0}%";
                InventoryHealthBar.Value = healthPercentage;

                // Change color based on health
                if (healthPercentage >= 70)
                {
                    InventoryHealthBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    InventoryHealthText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
                }
                else if (healthPercentage >= 40)
                {
                    InventoryHealthBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57C00"));
                    InventoryHealthText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57C00"));
                }
                else
                {
                    InventoryHealthBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"));
                    InventoryHealthText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"));
                }

                // 4. PENDING CHECKS: Sum of checks with status "En Attente"
                if (_allChecks != null && _allChecks.Count > 0)
                {
                    var pendingChecks = _allChecks
                        .Where(c => c.CheckStatus == "En Attente")
                        .ToList();

                    decimal pendingAmount = pendingChecks
                        .Sum(c => c.CheckAmount ?? 0);

                    int pendingCount = pendingChecks.Count;

                    PendingChecksText.Text = $"{pendingAmount:N2} DH";
                    PendingChecksCountText.Text = $"{pendingCount} chèque{(pendingCount > 1 ? "s" : "")}";
                }
                else
                {
                    PendingChecksText.Text = "0.00 DH";
                    PendingChecksCountText.Text = "0 chèques";
                }
            });
        }

        private decimal CalculateProjectedRevenue(DateTime periodEndDate)
        {
            decimal totalRevenue = 0;

            foreach (var article in _allArticles.Where(a => !a.IsUnlimitedStock))
            {
                // Calculate estimated sales based on stock velocity
                double monthlyVelocityRate = 0.20; // Assume 20% of stock sold per month (adjustable)
                double periodMultiplier = _selectedPeriodMonths * monthlyVelocityRate;

                // Don't sell more than available stock
                int estimatedUnitsSold = (int)Math.Min(article.Quantite, article.Quantite * periodMultiplier);

                // Check if product expires during period
                if (article.DateExpiration.HasValue && article.DateExpiration.Value <= periodEndDate)
                {
                    // If expiring, assume we can only sell before expiration
                    DateTime now = DateTime.Now;
                    double monthsUntilExpiry = (article.DateExpiration.Value - now).TotalDays / 30.0;

                    if (monthsUntilExpiry > 0 && monthsUntilExpiry < _selectedPeriodMonths)
                    {
                        estimatedUnitsSold = (int)Math.Min(article.Quantite, article.Quantite * monthsUntilExpiry * monthlyVelocityRate);
                    }
                }

                totalRevenue += estimatedUnitsSold * article.PrixVente;
            }

            return totalRevenue;
        }

        private decimal CalculateProjectedProfit(DateTime periodEndDate)
        {
            decimal totalProfit = 0;

            foreach (var article in _allArticles.Where(a => !a.IsUnlimitedStock))
            {
                double monthlyVelocityRate = 0.20;
                double periodMultiplier = _selectedPeriodMonths * monthlyVelocityRate;

                int estimatedUnitsSold = (int)Math.Min(article.Quantite, article.Quantite * periodMultiplier);

                if (article.DateExpiration.HasValue && article.DateExpiration.Value <= periodEndDate)
                {
                    DateTime now = DateTime.Now;
                    double monthsUntilExpiry = (article.DateExpiration.Value - now).TotalDays / 30.0;

                    if (monthsUntilExpiry > 0 && monthsUntilExpiry < _selectedPeriodMonths)
                    {
                        estimatedUnitsSold = (int)Math.Min(article.Quantite, article.Quantite * monthsUntilExpiry * monthlyVelocityRate);
                    }
                }

                decimal profitPerUnit = article.PrixVente - article.PrixAchat;
                totalProfit += estimatedUnitsSold * profitPerUnit;
            }

            return totalProfit;
        }

        private void SetDefaultKPIs()
        {
            Dispatcher.Invoke(() =>
            {
                ProjectedRevenueText.Text = "0.00 DH";
                PotentialProfitText.Text = "0.00 DH";
                ProfitMarginText.Text = "Marge: 0%";
                InventoryHealthText.Text = "0%";
                InventoryHealthBar.Value = 0;
                PendingChecksText.Text = "0.00 DH";
                PendingChecksCountText.Text = "0 chèques";
            });
        }

        #endregion

        #region Article Projections

        private void DisplayArticleProjections(Article article)
        {
            if (article == null) return;

            Dispatcher.Invoke(() =>
            {
                // Show the details card
                ArticleDetailsCard.Visibility = Visibility.Visible;

                // Article name
                SelectedArticleNameText.Text = article.ArticleName;

                // Current stock and pricing
                string stockDisplay = article.IsUnlimitedStock ? "∞" : article.Quantite.ToString();
                CurrentStockText.Text = $"{stockDisplay} unités";
                SalePriceText.Text = $"{article.PrixVente:N2} DH";

                decimal stockValue = article.IsUnlimitedStock ? 0 : (article.Quantite * article.PrixVente);
                StockValueText.Text = $"{stockValue:N2} DH";

                // Calculate profit margin
                decimal margin = article.PrixVente > 0
                    ? ((article.PrixVente - article.PrixAchat) / article.PrixVente) * 100
                    : 0;

                ProfitMarginValueText.Text = $"{margin:F1}%";
                ProfitMarginBar.Value = (double)Math.Min(100m, Math.Max(0m, margin));

                // Change margin color based on value
                if (margin >= 30)
                {
                    ProfitMarginValueText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
                    ProfitMarginBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                }
                else if (margin >= 15)
                {
                    ProfitMarginValueText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57C00"));
                    ProfitMarginBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                }
                else
                {
                    ProfitMarginValueText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"));
                    ProfitMarginBar.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                }

                // Calculate Days Until Stockout and Restock Date based on selected period
                if (article.IsUnlimitedStock)
                {
                    DaysUntilStockoutText.Text = "∞";
                    DaysUntilStockoutText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    RestockDateText.Text = "Non nécessaire";
                    StockVelocityText.Text = "Stock illimité";
                    ProjectedSalesText.Text = "N/A";
                }
                else
                {
                    // Calculate velocity based on assumed monthly sales rate
                    double monthlyVelocityRate = 0.20; // 20% per month
                    double monthlyVelocity = article.Quantite * monthlyVelocityRate;
                    double weeklyVelocity = monthlyVelocity / 4.0;

                    if (weeklyVelocity > 0)
                    {
                        double weeksUntilStockout = article.Quantite / weeklyVelocity;
                        int daysUntilStockout = (int)(weeksUntilStockout * 7);

                        DaysUntilStockoutText.Text = $"{daysUntilStockout} jours";
                        StockVelocityText.Text = $"Vélocité: {weeklyVelocity:F1}/semaine";

                        // Color coding for urgency
                        if (daysUntilStockout <= 7)
                        {
                            DaysUntilStockoutText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"));
                        }
                        else if (daysUntilStockout <= 30)
                        {
                            DaysUntilStockoutText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57C00"));
                        }
                        else
                        {
                            DaysUntilStockoutText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                        }

                        // Restock date: recommend ordering 7 days before stockout
                        DateTime restockDate = DateTime.Now.AddDays(Math.Max(0, daysUntilStockout - 7));
                        RestockDateText.Text = restockDate.ToString("dd/MM/yyyy");

                        // Calculate projected sales for selected period
                        double periodMultiplier = _selectedPeriodMonths * monthlyVelocityRate;
                        int projectedUnitsSold = (int)Math.Min(article.Quantite, article.Quantite * periodMultiplier);
                        decimal projectedRevenue = projectedUnitsSold * article.PrixVente;

                        ProjectedSalesText.Text = $"{projectedUnitsSold} unités ({projectedRevenue:N2} DH) sur {PeriodDisplayText}";
                    }
                    else
                    {
                        DaysUntilStockoutText.Text = "> 365 jours";
                        DaysUntilStockoutText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                        RestockDateText.Text = "Pas urgent";
                        StockVelocityText.Text = "Vente très lente";
                        ProjectedSalesText.Text = $"Ventes minimales sur {PeriodDisplayText}";
                    }
                }
            });
        }

        #endregion

        #region Smart Recommendations Engine

        private void GenerateRecommendations()
        {
            var recommendations = new ObservableCollection<RecommendationItem>();

            if (_allArticles == null || _allArticles.Count == 0)
            {
                DisplayNoRecommendations();
                return;
            }

            DateTime now = DateTime.Now;
            DateTime periodEndDate = now.AddMonths(_selectedPeriodMonths);

            // ===== LOGIC A: EXPIRATION ALERTS (within selected period) =====
            var expiringArticles = _allArticles
                .Where(a => a.DateExpiration.HasValue &&
                           !a.IsUnlimitedStock &&
                           a.Quantite > 0 &&
                           a.DateExpiration.Value <= periodEndDate &&
                           a.DateExpiration.Value > now)
                .OrderBy(a => a.DateExpiration)
                .ToList();

            foreach (var article in expiringArticles)
            {
                int daysUntilExpiry = (int)(article.DateExpiration.Value - now).TotalDays;

                string urgencyMessage = daysUntilExpiry <= 7
                    ? "ACTION URGENTE: Réduction de prix immédiate recommandée (au moins 30%)."
                    : daysUntilExpiry <= 30
                    ? "Recommandation: Appliquer une réduction de 15-20% pour écouler le stock."
                    : $"Alerte: Expire dans {daysUntilExpiry} jours sur la période de {PeriodDisplayText}.";

                recommendations.Add(new RecommendationItem
                {
                    Icon = "⚠️",
                    Title = "Alerte Expiration",
                    Message = $"{article.ArticleName} expire le {article.DateExpiration.Value:dd/MM/yyyy} ({article.Quantite} unités en stock).",
                    Action = urgencyMessage,
                    BackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0")),
                    BorderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57C00")),
                    TitleColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E65100")),
                    ActionColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57C00"))
                });
            }

            // ===== LOGIC B: STOCK DEPLETION FORECAST =====
            // Articles that will run out within the selected period
            var depletingArticles = _allArticles
                .Where(a => !a.IsUnlimitedStock && a.Quantite > 0)
                .Select(a => new
                {
                    Article = a,
                    DaysUntilStockout = CalculateDaysUntilStockout(a)
                })
                .Where(x => x.DaysUntilStockout <= (_selectedPeriodMonths * 30) && x.DaysUntilStockout > 0)
                .OrderBy(x => x.DaysUntilStockout)
                .Take(5)
                .ToList();

            foreach (var item in depletingArticles)
            {
                recommendations.Add(new RecommendationItem
                {
                    Icon = "📉",
                    Title = "Prévision Rupture de Stock",
                    Message = $"{item.Article.ArticleName} - Rupture prévue dans ~{item.DaysUntilStockout} jours.",
                    Action = $"Recommandation: Commander avant {DateTime.Now.AddDays(item.DaysUntilStockout - 7):dd/MM/yyyy} pour éviter la rupture.",
                    BackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF9C4")),
                    BorderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBC02D")),
                    TitleColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17")),
                    ActionColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9A825"))
                });
            }

            // ===== LOGIC C: CASH FLOW - PENDING CHECKS =====
            if (_allChecks != null && _allChecks.Count > 0)
            {
                var pendingChecks = _allChecks
                    .Where(c => c.CheckStatus == "En Attente")
                    .ToList();

                decimal totalPending = pendingChecks.Sum(c => c.CheckAmount ?? 0);

                if (totalPending > 10000)
                {
                    recommendations.Add(new RecommendationItem
                    {
                        Icon = "💳",
                        Title = "Chèques En Attente - Action Requise",
                        Message = $"{totalPending:N2} DH en chèques en attente ({pendingChecks.Count} chèque{(pendingChecks.Count > 1 ? "s" : "")}).",
                        Action = "Action: Contacter les banques pour vérifier le statut. Mettre à jour les paiements encaissés.",
                        BackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE")),
                        BorderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E57373")),
                        TitleColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828")),
                        ActionColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"))
                    });
                }

                var oldChecks = pendingChecks
                    .Where(c => (now - c.CheckDate).TotalDays > 30)
                    .ToList();

                if (oldChecks.Count > 0)
                {
                    decimal oldAmount = oldChecks.Sum(c => c.CheckAmount ?? 0);

                    recommendations.Add(new RecommendationItem
                    {
                        Icon = "🕐",
                        Title = "Chèques Anciens Détectés",
                        Message = $"{oldChecks.Count} chèque{(oldChecks.Count > 1 ? "s" : "")} en attente depuis plus de 30 jours (Total: {oldAmount:N2} DH).",
                        Action = "Action: Vérifier urgence avec les clients/fournisseurs. Risque de non-encaissement.",
                        BackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF9C4")),
                        BorderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FBC02D")),
                        TitleColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F57F17")),
                        ActionColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F9A825"))
                    });
                }
            }

            // ===== LOGIC D: LOW STOCK ALERTS =====
            var lowStockArticles = _allArticles
                .Where(a => !a.IsUnlimitedStock &&
                           a.Quantite > 0 &&
                           a.Quantite <= 10)
                .OrderBy(a => a.Quantite)
                .Take(5)
                .ToList();

            foreach (var article in lowStockArticles)
            {
                recommendations.Add(new RecommendationItem
                {
                    Icon = "📦",
                    Title = "Stock Faible Détecté",
                    Message = $"{article.ArticleName} - Seulement {article.Quantite} unité{(article.Quantite > 1 ? "s" : "")} restante{(article.Quantite > 1 ? "s" : "")}.",
                    Action = "Recommandation: Passer une commande de réapprovisionnement.",
                    BackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E3F2FD")),
                    BorderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#42A5F5")),
                    TitleColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1565C0")),
                    ActionColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2"))
                });
            }

            // ===== LOGIC E: OUT OF STOCK ALERTS =====
            var outOfStockArticles = _allArticles
                .Where(a => !a.IsUnlimitedStock && a.Quantite == 0)
                .Take(3)
                .ToList();

            foreach (var article in outOfStockArticles)
            {
                recommendations.Add(new RecommendationItem
                {
                    Icon = "🚨",
                    Title = "Rupture de Stock",
                    Message = $"{article.ArticleName} - Stock épuisé.",
                    Action = "ACTION URGENTE: Réapprovisionner immédiatement ou retirer de la vente.",
                    BackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEBEE")),
                    BorderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF5350")),
                    TitleColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828")),
                    ActionColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D32F2F"))
                });
            }

            // ===== LOGIC F: HIGH PROFIT OPPORTUNITIES FOR PERIOD =====
            var highMarginArticles = _allArticles
                .Where(a => !a.IsUnlimitedStock &&
                           a.Quantite > 20 &&
                           a.PrixVente > 0 &&
                           ((a.PrixVente - a.PrixAchat) / a.PrixVente) * 100 >= 50)
                .OrderByDescending(a => (a.PrixVente - a.PrixAchat) / a.PrixVente)
                .Take(2)
                .ToList();

            foreach (var article in highMarginArticles)
            {
                decimal margin = ((article.PrixVente - article.PrixAchat) / article.PrixVente) * 100;

                // Calculate potential revenue for this high-margin item over the period
                double monthlyVelocityRate = 0.20;
                int estimatedSales = (int)Math.Min(article.Quantite, article.Quantite * _selectedPeriodMonths * monthlyVelocityRate);
                decimal potentialRevenue = estimatedSales * article.PrixVente;

                recommendations.Add(new RecommendationItem
                {
                    Icon = "💎",
                    Title = "Opportunité à Forte Marge",
                    Message = $"{article.ArticleName} - Marge exceptionnelle de {margin:F0}% ({article.Quantite} unités disponibles).",
                    Action = $"Recommandation: Promouvoir ce produit. Potentiel: {estimatedSales} ventes / {potentialRevenue:N2} DH sur {PeriodDisplayText}.",
                    BackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")),
                    BorderColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#66BB6A")),
                    TitleColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32")),
                    ActionColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#388E3C"))
                });
            }

            // Display recommendations
            Dispatcher.Invoke(() =>
            {
                if (recommendations.Count > 0)
                {
                    RecommendationsPanel.ItemsSource = recommendations;
                    RecommendationCountText.Text = $"{recommendations.Count} recommandation{(recommendations.Count > 1 ? "s" : "")} pour {PeriodDisplayText}";
                    NoRecommendationsPanel.Visibility = Visibility.Collapsed;
                    RecommendationsPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    DisplayNoRecommendations();
                }
            });
        }

        private int CalculateDaysUntilStockout(Article article)
        {
            if (article.IsUnlimitedStock || article.Quantite == 0)
                return int.MaxValue;

            // Assume 20% of stock sold per month
            double monthlyVelocityRate = 0.20;
            double weeklyVelocity = (article.Quantite * monthlyVelocityRate) / 4.0;

            if (weeklyVelocity > 0)
            {
                double weeksUntilStockout = article.Quantite / weeklyVelocity;
                return (int)(weeksUntilStockout * 7);
            }

            return int.MaxValue;
        }

        private void DisplayNoRecommendations()
        {
            Dispatcher.Invoke(() =>
            {
                RecommendationsPanel.Visibility = Visibility.Collapsed;
                NoRecommendationsPanel.Visibility = Visibility.Visible;
                RecommendationCountText.Text = $"0 recommandations pour {PeriodDisplayText}";
            });
        }

        #endregion
    }

    #region Recommendation Item Model

    public class RecommendationItem
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Action { get; set; }
        public Brush BackgroundColor { get; set; }
        public Brush BorderColor { get; set; }
        public Brush TitleColor { get; set; }
        public Brush ActionColor { get; set; }
    }

    #endregion
}