using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GestionComerce.Main.Vente
{
    public partial class CSingleFamilly : UserControl
    {
        public CSingleFamilly(Famille f, CMainV mainv, List<Famille> lf)
        {
            InitializeComponent();
            FamillyName.Content = f.FamilleName;
            this.f = f;
            this.mainv = mainv;
            this.lf = lf;
        }

        Famille f;
        CMainV mainv;
        List<Famille> lf;

        private async void FamillyName_Click(object sender, RoutedEventArgs e)
        {
            mainv.ArticlesContainer.Children.Clear();
            Article a = new Article();
            List<Article> Articles = await a.GetArticlesAsync();
            List<Article> articlesInFamily = Articles.Where(article => article.FamillyID == f.FamilleID).ToList();

            // Apply current sorting
            articlesInFamily = mainv.ApplySorting(articlesInFamily);

            // Display based on current layout mode
            if (mainv.isCardLayout)
            {
                // Card layout - 5 per row
                var wrapPanel = new WrapPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 1180
                };

                foreach (Article article in articlesInFamily)
                {
                    CSingleArticle1 ar = new CSingleArticle1(article, mainv, lf, mainv.lfo, 2);
                    wrapPanel.Children.Add(ar);
                }

                mainv.ArticlesContainer.Children.Add(wrapPanel);
            }
            else
            {
                // Row layout
                foreach (Article article in articlesInFamily)
                {
                    mainv.ArticlesContainer.Children.Add(new CSingleArticle1(article, mainv, lf, mainv.lfo, 0));
                }
            }
        }
    }
}