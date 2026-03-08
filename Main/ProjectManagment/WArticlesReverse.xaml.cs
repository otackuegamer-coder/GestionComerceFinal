using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GestionComerce.Main.ProjectManagment
{
    public partial class WArticlesReverse : Window
    {
        public WArticlesReverse(WPlus plus)
        {
            InitializeComponent();
            this.plus = plus;
            LoadRArticles();
            UpdateToggleAllButton();
        }

        public WPlus plus;

        public void LoadRArticles()
        {
            foreach (OperationArticle oa in plus.so.main.main.loa)
            {
                if (oa.OperationID == plus.so.op.OperationID)
                {
                    CSingleArticleReverse cSingleArticleReverse = new CSingleArticleReverse(this, oa);
                    ArticlesContainer.Children.Add(cSingleArticleReverse);
                }
            }
        }

        // Called by each CSingleArticleReverse after individual toggle, and by ToggleAllButton_Click
        public void UpdateToggleAllButton()
        {
            if (ArticlesContainer.Children.Count == 0) return;

            bool allReversed = ArticlesContainer.Children
                .OfType<CSingleArticleReverse>()
                .All(sar => sar.oa.Reversed);

            ToggleAllButton.Content = allReversed ? "Unreverse All" : "Reverse All";
            ToggleAllButton.Background = allReversed
                ? (SolidColorBrush)(new BrushConverter().ConvertFrom("#4d4d4d"))
                : (SolidColorBrush)(new BrushConverter().ConvertFrom("#8B5CF6"));
        }

        private void ToggleAllButton_Click(object sender, RoutedEventArgs e)
        {
            bool allReversed = ArticlesContainer.Children
                .OfType<CSingleArticleReverse>()
                .All(sar => sar.oa.Reversed);

            // If all reversed → unreverse all; otherwise → reverse all
            bool newState = !allReversed;

            foreach (CSingleArticleReverse sar in ArticlesContainer.Children)
            {
                sar.oa.Reversed = newState;

                if (newState)
                {
                    sar.Reverse.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#4d4d4d"));
                    sar.Reverse.Content = "Unreverse";
                }
                else
                {
                    sar.Reverse.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#EF4444"));
                    sar.Reverse.Content = "Reverse";
                }
            }

            UpdateToggleAllButton();
        }

        private void FermerButton_Click(object sender, RoutedEventArgs e)
        {
            // Restore all to their initial state on cancel
            foreach (CSingleArticleReverse sar in ArticlesContainer.Children)
            {
                sar.oa.Reversed = sar.inittialStat;
            }
            this.Close();
        }

        private void ConfirmerButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (CSingleArticleReverse sar in ArticlesContainer.Children)
            {
                sar.oa.UpdateOperationArticleAsync();
            }
            WReverseConfirmation wReverseConfirmation = new WReverseConfirmation(plus, this);
            wReverseConfirmation.Show();
        }
    }
}
