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
    /// Interaction logic for CSingleRowFamilly.xaml
    /// </summary>
    public partial class CSingleRowFamilly : UserControl
    {
        public CSingleRowFamilly(Famille f, List<Famille> lf, WManageFamillies main,List<Article> la, CMainI Main)
        {
            InitializeComponent();
            this.f = f;
            this.lf = lf;
            this.main = main;
            this.Main = Main;
            this.la = la;
            FamillyName.Text = f.FamilleName;
            foreach (Role r in Main.main.lr)
            {
                if (Main.u.RoleID == r.RoleID)
                {
                    if (r.EditFamilly == false)
                    {
                        UpdateButton.IsEnabled = false;
                    }
                    if (r.DeleteFamilly == false)
                    {
                        DeleteButton.IsEnabled = false;
                    }
                    break;
                }
            }
        }
        public Famille f; List<Famille> lf; WManageFamillies main; List<Article> la; public CMainI Main;
        private void MyGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            foreach (Role r in Main.main.lr)
            {
                if (Main.u.RoleID == r.RoleID)
                {
                    if (r.ViewArticle == true)
                    {

                        main.Width = 1100;
                        double screenWidth = SystemParameters.WorkArea.Width;
                        double screenHeight = SystemParameters.WorkArea.Height;
                        main.Left = (screenWidth - main.Width) / 2;
                        main.Top = (screenHeight - main.Height) / 2;
                        main.ArticlesColumn.Width = new GridLength(3, GridUnitType.Star);
                        main.ArticlesTitle.Text = $"Articles de {f.FamilleName}";
                        LoadArticles(la);
                        if (la.Where(a => a.FamillyID == f.FamilleID).ToList().Count == 0)
                        {
                            main.ArticlesContainer.Children.Add(new TextBlock() { Text = "Aucun article dans cette famille.", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
                        }
                    }
                    break;
                }
            }
        }
        public void LoadArticles(List<Article> laa)
        {
            main.ArticlesContainer.Children.Clear();
            foreach (Article a in laa.Where(a => a.FamillyID == f.FamilleID).ToList())
            {
                CSingleRowArticle ar = new CSingleRowArticle(a, laa, this, Main,0,null,null,0);
                main.ArticlesContainer.Children.Add(ar);
            }
        }
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            WAddFamille af = new WAddFamille(lf,null, f, main,1);
            af.ShowDialog();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (la.Where(a => a.FamillyID == f.FamilleID).ToList().Count != 0)
            {
                MessageBox.Show("vous puvez pas supprimer une famille qui contient des articles.");
                return;
            }
            WDeleteFamillyConfirmation w = new WDeleteFamillyConfirmation(f, lf, main);
            w.ShowDialog();


        }
    }
}
