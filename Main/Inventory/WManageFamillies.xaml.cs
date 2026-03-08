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

namespace GestionComerce.Main.Inventory
{
    /// <summary>
    /// Interaction logic for WManageFamillies.xaml
    /// </summary>
    public partial class WManageFamillies : Window
    {
        public WManageFamillies(List<Famille> lf, List<Article> la,CMainI main)
        {
            InitializeComponent();
            this.lf = lf;
            this.la = la;
            this.main = main;
            foreach (Role r in main.main.lr)
            {
                if (main.u.RoleID == r.RoleID)
                {
                    if (r.ViewFamilly == true)
                    {
                        LoadFamillies(lf);
                    }
                    if (r.AddFamilly == false)
                    {
                        AddFamilleButton.IsEnabled = false;
                    }
                    
                    break;
                }
            }
           
        }
        List<Famille> lf; List<Article> la;public CMainI main;
        public void LoadFamillies(List<Famille> lf)
        {
            FamilliesContainer.Children.Clear();    
            foreach (Famille f in lf)
            {
                CSingleRowFamilly fr = new CSingleRowFamilly(f, lf, this,la,main);
                FamilliesContainer.Children.Add(fr);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void AddFamilleButton_Click(object sender, RoutedEventArgs e)
        {
            WAddFamille w = new WAddFamille(lf, null,new Famille(),this,1);
            w.ShowDialog();
        }

        private void FamilleInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            foreach (var child in FamilliesContainer.Children)
            {
                if (child is CSingleRowFamilly fr)
                {
                    if (fr.f.FamilleName.IndexOf(FamilleInput.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        fr.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        fr.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
        private void ArticleInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            foreach (var child in ArticlesContainer.Children)
            {
                if (child is CSingleRowArticle ar)
                {
                    if (ar.a.ArticleName.IndexOf(ArticleInput.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        ar.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ar.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
    }
}
