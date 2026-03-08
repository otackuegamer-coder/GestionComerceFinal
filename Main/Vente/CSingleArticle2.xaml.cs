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

namespace GestionComerce.Main.Vente
{
    /// <summary>
    /// Interaction logic for CSingleArticle2.xaml
    /// </summary>
    public partial class CSingleArticle2 : UserControl
    {
        public CSingleArticle2(Article a, int qte, CMainV main)
        {
            InitializeComponent();
            ArticleName.Text = a.ArticleName;
            Prix.Text = a.PrixVente.ToString("F2");
            Quantite.Text = qte.ToString();
            Total.Text = (Convert.ToDecimal(a.PrixVente) * Convert.ToInt32(qte)).ToString("F2");

            foreach (Fournisseur fo in main.lfo)
            {
                if (a.FournisseurID == fo.FournisseurID)
                {
                    Fournisseur.Text = fo.Nom;
                    break;
                }
            }

            this.a = a;
            this.main = main;
            this.qte = qte;
        }

        public Article a;
        CMainV main;
        public int qte;

        public void SelectedArticleClicked(object sender, RoutedEventArgs e)
        {
                main.SelectedArticles.Children.Remove(this);
                main.TotalNett -= a.PrixVente * Convert.ToInt32(qte);
                main.TotalNet.Text = main.TotalNett.ToString("F2") + " DH";
                main.NbrA -= Convert.ToInt32(qte);
                main.ArticleCount.Text = main.NbrA.ToString();
                main.UpdateCartEmptyState();
        }
    }
}