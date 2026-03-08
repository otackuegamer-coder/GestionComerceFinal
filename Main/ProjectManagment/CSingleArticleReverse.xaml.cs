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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GestionComerce.Main.ProjectManagment
{
    public partial class CSingleArticleReverse : UserControl
    {
        public CSingleArticleReverse(WArticlesReverse Arts, OperationArticle oa)
        {
            InitializeComponent();
            this.arts = Arts;
            foreach (Article a in Arts.plus.so.main.main.laa)
            {
                if (oa.ArticleID == a.ArticleID)
                {
                    ArticleName.Text = a.ArticleName;
                    this.oa = oa;
                }
            }
            inittialStat = oa.Reversed;

            // Show correct button state — no IsEnabled=false, allow toggling both ways
            if (oa.Reversed == true)
            {
                Reverse.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#4d4d4d"));
                Reverse.Content = "Unreverse";
            }
            else
            {
                Reverse.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#EF4444"));
                Reverse.Content = "Reverse";
            }
        }

        public OperationArticle oa;
        public bool inittialStat;
        private WArticlesReverse arts;

        private void ReverseArticle_Click(object sender, RoutedEventArgs e)
        {
            if (oa.Reversed == true)
            {
                oa.Reversed = false;
                Reverse.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#EF4444"));
                Reverse.Content = "Reverse";
            }
            else
            {
                oa.Reversed = true;
                Reverse.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#4d4d4d"));
                Reverse.Content = "Unreverse";
            }

            // Update the Toggle All button label
            arts.UpdateToggleAllButton();
        }
    }
}
