using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GestionComerce.Main.Facturation.CreateFacture
{
    public partial class CSingleArticle : UserControl
    {
        private CMainFa        mainFa;
        private InvoiceArticle article;

        public CSingleArticle(CMainFa mainFa, InvoiceArticle article)
        {
            InitializeComponent();
            this.mainFa  = mainFa;
            this.article = article;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (article == null) return;

            txtArticleName.Text = article.ArticleName;

            bool isExpeditionMode = mainFa?.InvoiceType == "Expedition";

            if (isExpeditionMode)
            {
                txtArticleDetails.Text   = $"Qté: {article.Quantite} | Expédié: {article.InitialQuantity} | TVA: {article.TVA}%";
                ExpeditionBadge.Visibility = Visibility.Visible;
                txtExpeditionInfo.Text   = $"📦 {article.InitialQuantity} expédiés";
            }
            else
            {
                txtArticleDetails.Text   = $"Qté: {article.Quantite} | TVA: {article.TVA}%";
                ExpeditionBadge.Visibility = Visibility.Collapsed;
            }

            txtArticlePrice.Text = article.TotalTTC.ToString("0.00") + " DH";

            // Source badge
            if (article.ArticleID < 0)
            {
                // Custom article
                txtType.Text      = "Personnalisé";
                badgeType.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#EDE9FE"));
                txtType.Foreground   = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#6D28D9"));
                SideColor.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#8B5CF6"));
            }
            else if (article.OperationID == 0)
            {
                // Stock article (added manually)
                txtType.Text      = "Stock";
                badgeType.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#DBEAFE"));
                txtType.Foreground   = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#1D4ED8"));
                SideColor.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#3B82F6"));
            }
            else
            {
                // Linked to an operation
                txtType.Text      = "Opération";
                badgeType.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#DCFCE7"));
                txtType.Foreground   = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#10B981"));
                SideColor.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#10B981"));
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // If click bubbled from the edit button, ignore
            var hit = e.OriginalSource as FrameworkElement;
            while (hit != null)
            {
                if (hit == btnEdit) { e.Handled = true; return; }
                hit = VisualTreeHelper.GetParent(hit) as FrameworkElement;
            }

            e.Handled = true;

            var result = MessageBox.Show(
                $"Supprimer l'article « {article.ArticleName} » ?",
                "Confirmer la suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                RemoveArticle();
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            // Open the dedicated WEditArticle window.
            // It calls UpdateArticle() on this control when the user saves.
            var editWindow = new WEditArticle(mainFa, article, this);
            editWindow.ShowDialog();
        }

        private void RemoveArticle()
        {
            mainFa.InvoiceArticles.Remove(article);

            if (this.Parent is Panel parent)
                parent.Children.Remove(this);

            mainFa.RecalculateTotals();
        }

        /// <summary>Called by WAddArticle after saving changes in edit mode.</summary>
        public void UpdateArticle(InvoiceArticle updatedArticle)
        {
            this.article = updatedArticle;
            UpdateDisplay();
        }
    }
}
