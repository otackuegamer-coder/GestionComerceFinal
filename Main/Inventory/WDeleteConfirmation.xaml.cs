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
    /// Logique d'interaction pour WDeleteConfirmation.xaml
    /// </summary>
    public partial class WDeleteConfirmation : Window
    {
        public WDeleteConfirmation(Article a, List<Article> la,CSingleRowFamilly sf, CMainI main)
        {
            InitializeComponent();
            this.a = a;
            this.la = la;
            this.main = main;
            this.sf = sf;

        }
        Article a; List<Article> la; CMainI main; CSingleRowFamilly sf;
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Operation Operation = new Operation();
                Operation.OperationType = "Delete";
                Operation.PrixOperation = a.Quantite * a.PrixAchat;

                Operation.UserID = main.u.UserID;

                int idd = await Operation.InsertOperationAsync();
                OperationArticle ofa = new OperationArticle();
                ofa.ArticleID = a.ArticleID;
                ofa.OperationID = idd;
                ofa.QteArticle = Convert.ToInt32(a.Quantite);
                await ofa.InsertOperationArticleAsync();
                a.DeleteArticleAsync();
                foreach (Article article in la)
                {
                    if (article.ArticleID == a.ArticleID)
                    {
                        la.Remove(article);
                        break;
                    }
                }
                sf?.LoadArticles(la);
                main?.LoadArticles(la);
                WCongratulations wCongratulations = new WCongratulations("Suppresion réussite", "Suppresion a ete effectue avec succes",1);
                wCongratulations.Show();
            }
            catch (Exception ex)
            {

                WCongratulations wCongratulations = new WCongratulations("Suppresion échoué", "Suppresion n'a pas a ete effectue ", 0);
                wCongratulations.Show();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
