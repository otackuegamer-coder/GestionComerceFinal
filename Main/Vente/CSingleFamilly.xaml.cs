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
            Article a = new Article();
            List<Article> Articles = await a.GetArticlesAsync();
            List<Article> articlesInFamily = Articles.Where(article => article.FamillyID == f.FamilleID).ToList();
            mainv.LoadArticles(articlesInFamily);
        }
    }
}