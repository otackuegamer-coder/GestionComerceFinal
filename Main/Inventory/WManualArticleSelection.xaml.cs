using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GestionComerce.Main.Inventory
{
    public partial class WManualArticleSelection : Window
    {
        private List<Article> allArticles;
        private List<Famille> allFamilles;
        private List<Fournisseur> allFournisseurs;
        public List<Article> SelectedArticles { get; private set; }
        private int selectedCategoryId = -1; // -1 means "All categories"

        public WManualArticleSelection(List<Article> articles, List<Famille> familles, List<Fournisseur> fournisseurs)
        {
            InitializeComponent();
            this.allArticles = articles;
            this.allFamilles = familles;
            this.allFournisseurs = fournisseurs;
            this.SelectedArticles = new List<Article>();

            LoadCategoryFilter();
            LoadArticles();
        }

        private void LoadCategoryFilter()
        {
            CategoryFilterComboBox.Items.Clear();

            // Add "All Categories" option
            ComboBoxItem allItem = new ComboBoxItem
            {
                Content = "📦 Toutes les catégories",
                Tag = -1,
                FontWeight = FontWeights.Bold
            };
            CategoryFilterComboBox.Items.Add(allItem);

            // Add each family with article count
            foreach (Famille famille in allFamilles)
            {
                // Count articles in this family
                int articleCount = 0;
                foreach (Article a in allArticles)
                {
                    if (a.FamillyID == famille.FamilleID && a.Etat)
                    {
                        articleCount++;
                    }
                }

                if (articleCount > 0)
                {
                    ComboBoxItem item = new ComboBoxItem
                    {
                        Content = $"📁 {famille.FamilleName} ({articleCount})",
                        Tag = famille.FamilleID
                    };
                    CategoryFilterComboBox.Items.Add(item);
                }
            }

            // Select "All Categories" by default
            CategoryFilterComboBox.SelectedIndex = 0;
        }

        private void CategoryFilter_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CategoryFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                selectedCategoryId = (int)selectedItem.Tag;
                LoadArticles(SearchTextBox?.Text?.Trim() ?? "");
            }
        }

        private void LoadArticles(string searchText = "")
        {
            ArticlesListPanel.Children.Clear();

            int totalCount = 0;
            int displayedCount = 0;

            foreach (Article article in allArticles)
            {
                if (!article.Etat) continue;

                totalCount++;

                // Filter by search text
                if (!string.IsNullOrEmpty(searchText))
                {
                    bool matches = false;

                    if (article.ArticleName != null &&
                        article.ArticleName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        matches = true;

                    if (article.Code.ToString().Contains(searchText))
                        matches = true;

                    string familleName = GetFamilleName(article.FamillyID);
                    if (!string.IsNullOrEmpty(familleName) &&
                        familleName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        matches = true;

                    string supplierName = GetFournisseurName(article.FournisseurID);
                    if (!string.IsNullOrEmpty(supplierName) &&
                        supplierName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        matches = true;

                    if (!matches) continue;
                }

                displayedCount++;
                CreateArticleRow(article);
            }

            TotalArticlesText.Text = $"{displayedCount} article{(displayedCount > 1 ? "s" : "")}";
            UpdateSelectedCount();
        }

        private void CreateArticleRow(Article article)
        {
            Border rowBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = (Brush)FindResource("BorderLight"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 4)
            };

            Grid rowGrid = new Grid();
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // CheckBox
            CheckBox cb = new CheckBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                Tag = article.ArticleID,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            cb.Checked += CheckBox_Changed;
            cb.Unchecked += CheckBox_Changed;
            Grid.SetColumn(cb, 0);
            rowGrid.Children.Add(cb);

            // Article Info
            StackPanel infoPanel = new StackPanel
            {
                Margin = new Thickness(8, 0, 0, 0)
            };

            // Main info
            StackPanel mainInfo = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            TextBlock nameText = new TextBlock
            {
                Text = article.ArticleName ?? "Sans nom",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextPrimary"),
                Margin = new Thickness(0, 0, 12, 0)
            };
            mainInfo.Children.Add(nameText);

            Border codeBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 8, 0)
            };
            codeBadge.Child = new TextBlock
            {
                Text = $"Code: {article.Code}",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("PrimaryBlue")
            };
            mainInfo.Children.Add(codeBadge);

            infoPanel.Children.Add(mainInfo);

            // Details
            StackPanel detailsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 6, 0, 0)
            };

            string familyName = GetFamilleName(article.FamillyID);
            AddDetailBadge(detailsPanel, "📁", familyName);

            string supplierName = GetFournisseurName(article.FournisseurID);
            AddDetailBadge(detailsPanel, "🏢", supplierName);

            AddDetailBadge(detailsPanel, "📦", $"Stock: {article.Quantite}");
            AddDetailBadge(detailsPanel, "💰", $"{article.PrixVente:F2} DH");

            infoPanel.Children.Add(detailsPanel);

            Grid.SetColumn(infoPanel, 1);
            rowGrid.Children.Add(infoPanel);

            rowBorder.Child = rowGrid;
            ArticlesListPanel.Children.Add(rowBorder);
        }

        private void AddDetailBadge(StackPanel parent, string icon, string text)
        {
            Border badge = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 8, 0)
            };

            TextBlock textBlock = new TextBlock
            {
                FontSize = 11,
                Foreground = (Brush)FindResource("TextSecondary")
            };
            textBlock.Inlines.Add(new System.Windows.Documents.Run(icon + " "));
            textBlock.Inlines.Add(new System.Windows.Documents.Run(text));

            badge.Child = textBlock;
            parent.Children.Add(badge);
        }

        private string GetFamilleName(int familleId)
        {
            foreach (var famille in allFamilles)
            {
                if (famille.FamilleID == familleId)
                    return famille.FamilleName;
            }
            return "N/A";
        }

        private string GetFournisseurName(int fournisseurId)
        {
            foreach (var fournisseur in allFournisseurs)
            {
                if (fournisseur.FournisseurID == fournisseurId)
                    return fournisseur.Nom;
            }
            return "N/A";
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectedCount();
        }

        private void UpdateSelectedCount()
        {
            int count = 0;
            foreach (var child in ArticlesListPanel.Children)
            {
                if (child is Border border && border.Child is Grid grid)
                {
                    foreach (var gridChild in grid.Children)
                    {
                        if (gridChild is CheckBox cb && cb.IsChecked == true)
                        {
                            count++;
                            break;
                        }
                    }
                }
            }
            SelectedCountText.Text = count.ToString();
        }

        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            LoadArticles(SearchTextBox.Text.Trim());
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in ArticlesListPanel.Children)
            {
                if (child is Border border && border.Child is Grid grid)
                {
                    foreach (var gridChild in grid.Children)
                    {
                        if (gridChild is CheckBox cb)
                        {
                            cb.IsChecked = true;
                            break;
                        }
                    }
                }
            }
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in ArticlesListPanel.Children)
            {
                if (child is Border border && border.Child is Grid grid)
                {
                    foreach (var gridChild in grid.Children)
                    {
                        if (gridChild is CheckBox cb)
                        {
                            cb.IsChecked = false;
                            break;
                        }
                    }
                }
            }
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            SelectedArticles.Clear();

            foreach (var child in ArticlesListPanel.Children)
            {
                if (child is Border border && border.Child is Grid grid)
                {
                    foreach (var gridChild in grid.Children)
                    {
                        if (gridChild is CheckBox cb && cb.IsChecked == true && cb.Tag != null)
                        {
                            int articleId = (int)cb.Tag;

                            // Find the article
                            foreach (Article a in allArticles)
                            {
                                if (a.ArticleID == articleId)
                                {
                                    SelectedArticles.Add(a);
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
            }

            if (SelectedArticles.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner au moins un article.",
                    "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}