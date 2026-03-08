using GestionComerce;
using GestionComerce.Main.FournisseurPage;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GestionComerce.Main.ClientPage
{
    public partial class ClientOperationsWindow : Window
    {
        private Client _currentClient;
        private bool _isArticlesPanelVisible = false;
        private MainWindow _main;
        private User u;

        public ClientOperationsWindow(Client client, User u)
        {
            InitializeComponent();
            _currentClient = client;

            // Get MainWindow reference
            _main = Application.Current.MainWindow as MainWindow;

            LoadOperations();
            this.u = u;
        }

        // Keep the parameterless constructor for design-time support
        public ClientOperationsWindow()
        {
            InitializeComponent();
            _currentClient = null;
            _main = Application.Current.MainWindow as MainWindow;
            LoadOperations();
        }

        private void LoadOperations()
        {
            try
            {
                // Load operations from MainWindow list instead of database
                List<Operation> operations = _main?.lo ?? new List<Operation>();

                // Filter operations by client if one is specified
                if (_currentClient != null)
                {
                    operations = operations.Where(op => op.ClientID == _currentClient.ClientID).ToList();
                }

                // Sort operations by date - newest first (descending order)
                operations = operations.OrderByDescending(op => op.Date).ToList();

                OperationsList.ItemsSource = operations;

                // Clear articles list and summary when operations are loaded
                ArticlesList.ItemsSource = null;
                ClearSummary();
                // Hide articles panel initially
                if (_isArticlesPanelVisible)
                {
                    HideArticlesPanel();
                }

                // Update info text based on context
                if (_currentClient != null)
                {
                    OperationInfoText.Text = $"{operations.Count} opération(s) pour {_currentClient.Nom}";
                    // Update window title to show client name
                    this.Title = $"Opérations Client - {_currentClient.Nom}";
                }
                else
                {
                    OperationInfoText.Text = $"{operations.Count} opération(s) trouvée(s)";
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des opérations: {ex.Message}",
                              "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsPaymentOperation(string operationType)
        {
            if (string.IsNullOrEmpty(operationType))
                return false;

            // Check if operation type contains payment-related keywords
            string lowerType = operationType.ToLower();
            return lowerType.Contains("payment") ||
                   lowerType.Contains("paiement") ||
                   lowerType.Contains("règlement") ||
                   lowerType.Contains("reglement");
        }

        private void ShowArticlesPanel()
        {
            foreach (Role r in _main.lr)
            {
                if (r.RoleID == u.RoleID)
                {
                    if (!r.ViewMouvment)
                    {
                        return;
                    }
                }
            }
            if (!_isArticlesPanelVisible)
            {
                _isArticlesPanelVisible = true;

                // Calculate current center position
                double currentCenterX = this.Left + (this.Width / 2);
                double currentCenterY = this.Top + (this.Height / 2);

                // Set the new width and column width
                ArticlesColumn.Width = new GridLength(750);
                this.Width = 1500;

                // Recalculate position to keep window centered
                this.Left = currentCenterX - (this.Width / 2);
                this.Top = currentCenterY - (this.Height / 2);

                // Ensure window stays within screen bounds
                EnsureWindowOnScreen();
            }
        }

        private void HideArticlesPanel()
        {
            if (_isArticlesPanelVisible)
            {
                _isArticlesPanelVisible = false;

                // Calculate current center position
                double currentCenterX = this.Left + (this.Width / 2);
                double currentCenterY = this.Top + (this.Height / 2);

                // Hide the column and resize window
                ArticlesColumn.Width = new GridLength(0);
                this.Width = 750;

                // Recalculate position to keep window centered
                this.Left = currentCenterX - (this.Width / 2);
                this.Top = currentCenterY - (this.Height / 2);

                // Ensure window stays within screen bounds
                EnsureWindowOnScreen();
            }
        }

        private void EnsureWindowOnScreen()
        {
            // Get screen dimensions
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;

            // Ensure window doesn't go off the left or top
            if (this.Left < 0)
                this.Left = 0;
            if (this.Top < 0)
                this.Top = 0;

            // Ensure window doesn't go off the right or bottom
            if (this.Left + this.Width > screenWidth)
                this.Left = screenWidth - this.Width;
            if (this.Top + this.Height > screenHeight)
                this.Top = screenHeight - this.Height;
        }

        private void CloseArticlesButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear the selection which will trigger the hiding of the panel
            OperationsList.SelectedItem = null;
        }

        private void OperationsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OperationsList.SelectedItem is Operation selectedOperation)
            {
                try
                {
                    // Check if this is a payment operation (no articles)
                    bool isPaymentOperation = IsPaymentOperation(selectedOperation.OperationType);

                    if (isPaymentOperation)
                    {
                        // For payment operations, hide the panel - no reaction
                        ArticlesList.ItemsSource = null;
                        ClearSummary();
                        HideArticlesPanel();
                        // Deselect the item to prevent it from staying selected
                        OperationsList.SelectedItem = null;
                        return;
                    }
                    else
                    {
                        // Load from MainWindow lists instead of database
                        var allOperationArticles = _main?.loa ?? new List<OperationArticle>();
                        var operationArticles = allOperationArticles
                            .Where(oa => oa.OperationID == selectedOperation.OperationID)
                            .ToList();

                        // Check if there are any articles
                        if (operationArticles.Count == 0)
                        {
                            MessageBox.Show($"Aucun article trouvé pour l'opération #{selectedOperation.OperationID}",
                                          "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            ArticlesList.ItemsSource = null;
                            ClearSummary();
                            HideArticlesPanel();
                            return;
                        }

                        // Get all articles from MainWindow list
                        var allArticles = _main?.la ?? new List<Article>();

                        // Get all families from MainWindow list
                        var allFamilies = _main?.lf ?? new List<Famille>();

                        // Create enriched operation articles with article details
                        var enrichedOperationArticles = operationArticles.Select(oa =>
                        {
                            var article = allArticles.FirstOrDefault(a => a.ArticleID == oa.ArticleID);
                            var famille = allFamilies?.FirstOrDefault(f => f.FamilleID == article?.FamillyID);

                            return new EnrichedOperationArticle
                            {
                                OperationID = oa.OperationID,
                                ArticleID = oa.ArticleID,
                                QteArticle = oa.QteArticle,
                                UnitPrice = article?.PrixVente ?? 0,
                                Total = oa.QteArticle * (article?.PrixVente ?? 0),
                                ArticleName = article?.ArticleName ?? "Article inconnu",
                                Famille = famille?.FamilleName ?? "N/A",
                                IsReversed = oa.Reversed // Add reversed status
                            };
                        }).ToList();

                        ArticlesList.ItemsSource = enrichedOperationArticles;

                        // Update the summary and operation info
                        UpdateSummary(selectedOperation, operationArticles);

                        // Update operation info text with reversed status
                        string reversedText = selectedOperation.Reversed ? " (Reversed)" : "";
                        OperationInfoText.Text = $"Opération #{selectedOperation.OperationID} - {selectedOperation.DateOperation:dd/MM/yyyy} - {selectedOperation.OperationType ?? "N/A"}{reversedText}";

                        ShowArticlesPanel();
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Erreur lors du chargement des articles: {ex.Message}",
                                  "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);

                    // Clear articles on error
                    ArticlesList.ItemsSource = null;
                    ClearSummary();
                    HideArticlesPanel();
                }
            }
            else
            {
                // Clear articles when no operation is selected
                ArticlesList.ItemsSource = null;
                ClearSummary();
                OperationInfoText.Text = "Sélectionnez une opération";
                HideArticlesPanel();
            }
        }

        private void UpdateSummary(Operation operation, List<OperationArticle> operationArticles)
        {
            if (operation == null || operationArticles == null)
            {
                ClearSummary();
                return;
            }

            // Calculate summary values
            int totalArticles = operationArticles.Count;
            int totalQuantity = operationArticles.Sum(a => a.QteArticle);

            // Update UI
            TotalArticlesText.Text = totalArticles.ToString();
            TotalQuantityText.Text = totalQuantity.ToString();
            TotalAmountText.Text = $"{operation.PrixOperation:N2} DH";

            // Show additional operation details if available
            if (operation.Remise > 0)
            {
                // You could add a remise display here if needed in the XAML
                // RemiseText.Text = operation.Remise.ToString("C2");
            }
        }

        private void ClearSummary()
        {
            TotalArticlesText.Text = "0";
            TotalQuantityText.Text = "0";
            TotalAmountText.Text = "0,00 DH";
        }

        // Event handler for refresh button (if you have one in XAML)
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadOperations();
        }

        // Event handler for close button (if you have one in XAML)
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Optional: Method to export or print operations (if needed)
        private void ExportOperations_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for exporting operations
            MessageBox.Show("Fonctionnalité d'export à implémenter",
                           "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    // Helper class to hold enriched operation article data
    public class EnrichedOperationArticle
    {
        public int OperationID { get; set; }
        public int ArticleID { get; set; }
        public int QteArticle { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
        public string ArticleName { get; set; }
        public string Famille { get; set; }
        public bool IsReversed { get; set; } // Add reversed property
    }
}