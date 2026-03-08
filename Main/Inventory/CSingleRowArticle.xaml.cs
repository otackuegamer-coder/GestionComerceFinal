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

namespace GestionComerce.Main.Inventory
{
    /// <summary>
    /// Interaction logic for CSingleRowArticle.xaml
    /// </summary>
    public partial class CSingleRowArticle : UserControl
    {
        public CSingleRowArticle(Article a, List<Article> la, CSingleRowFamilly sf, CMainI Main, int s,WExistingArticles ea, WNouveauStock ns,int q)
        {
            InitializeComponent();
            this.a = a;
            this.la = la;
            this.Main = Main;
            this.sf = sf;
            this.s = s;
            this.ea = ea;
            this.ns = ns;
            foreach (Fournisseur fo in Main.lfo)
                if (fo.FournisseurID == a.FournisseurID)
                {
                    Fournisseur.Text = fo.Nom;
                    break;
                }
            foreach (Role r in Main.main.lr)
            {
                if (Main.u.RoleID == r.RoleID)
                {
                    if (r.DeleteArticle == false )
                    {
                        DeleteButton.IsEnabled = false;
                    }
                    break;
                }
            }
            ArticleName.Text = a.ArticleName;
            Quantite.Text = "x"+a.Quantite.ToString();
            if (s == 0)
            {


                ButtonsContainerPanel.Width = 67;
                EditButton.Visibility = Visibility.Collapsed;
                DeleteButton.Visibility = Visibility.Visible;
            }
            if (s == 6)
            {
                Quantite.Text = "x" + q;
            }
            EditButton.Visibility = Visibility.Collapsed;
            if (s == 1 || s==5)
            {

                ButtonsContainerPanel.Width = 67;
                EditButton.Visibility = Visibility.Visible;
                DeleteButton.Visibility = Visibility.Collapsed;
            }
            //else if (s == 1)
            //{
            //    DeleteButton.Visibility = Visibility.Collapsed;
            //    ButtonsContainer.Width = new GridLength(0);
            //}
            else if (s == 7 || s==6) {
                ButtonsContainerPanel.Width = 67;
            }
            if (s == 6)
            {
                Fournisseur.Text = "Ajout de quantite";
            }
            else if (s == 7) {
                Fournisseur.Text = "Nouvelle Article";
            }
        }
        public Article a; List<Article> la;public CMainI Main; CSingleRowFamilly sf; int s;public WExistingArticles ea; WNouveauStock ns;

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            WAjoutQuantite w = new WAjoutQuantite(this,s,ns);
            w.ShowDialog();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (s == 7 || s==6)
            {
                ns.AMA.ArticlesContainer.Children.Remove(this);
            }
            if (s == 0) {
                WDeleteConfirmation wDeleteConfirmation=new WDeleteConfirmation(a,la,sf,sf.Main);
                wDeleteConfirmation.ShowDialog();
            }
        }
        
        //private void MyGrid_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        //{
        //    WAddArticle af = new WAddArticle(a, la, Main.lf, Main.lfo, Main,2,ea,ns);
        //    af.ShowDialog();
        //}
    }
}
