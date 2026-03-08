using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GestionComerce.Main.Vente
{
    public partial class WSelectArticle : Window
    {
        private List<Article> _allArticles;
        private List<Famille> _familles;
        private List<Fournisseur> _fournisseurs;

        public Article SelectedArticle { get; private set; }
        public int SelectedQuantity { get; private set; }

        public WSelectArticle(List<Article> articles, List<Famille> familles, List<Fournisseur> fournisseurs)
        {
            InitializeComponent();

            _allArticles = articles.Where(a => a.Quantite > 0).ToList(); // Only show articles with stock
            _familles = familles;
            _fournisseurs = fournisseurs;

            LoadArticles(_allArticles);
        }

        private void LoadArticles(List<Article> articles)
        {
            ArticlesListBox.ItemsSource = articles;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                LoadArticles(_allArticles);
            }
            else
            {
                var filtered = _allArticles.Where(a =>
                    a.ArticleName.ToLower().Contains(searchText) ||
                    a.Code.ToString().Contains(searchText)
                ).ToList();

                LoadArticles(filtered);
            }
        }

        private void ArticleItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Article article)
            {
                SelectedArticle = article;

                // Highlight selection
                foreach (var child in (ArticlesListBox.ItemsSource as IEnumerable<Article>))
                {
                    var container = ArticlesListBox.ItemContainerGenerator.ContainerFromItem(child);
                    if (container is ContentPresenter presenter && presenter.Content is Article art)
                    {
                        var itemBorder = FindVisualChild<Border>(presenter);
                        if (itemBorder != null)
                        {
                            itemBorder.Background = art.ArticleID == article.ArticleID
                                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 246, 255))
                                : System.Windows.Media.Brushes.Transparent;
                        }
                    }
                }

                border.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 246, 255));
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedArticle == null)
            {
                MessageBox.Show("Veuillez sélectionner un article.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Veuillez entrer une quantité valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (quantity > SelectedArticle.Quantite)
            {
                MessageBox.Show($"Quantité disponible: {SelectedArticle.Quantite}", "Stock Insuffisant", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedQuantity = quantity;
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}